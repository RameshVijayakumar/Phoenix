using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Web.Script.Serialization;
using System.IO;
using Phoenix.Common;
using Phoenix.DataAccess;
using System.Configuration;
using Phoenix.RuleEngine;
using SnowMaker;
using Microsoft.Practices.Unity;


namespace Phoenix.Web.Models
{
    public class AssetService : IAssetService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private UniqueIdGenerator _irisIdGenerator;
        private string _lastActionResult;
        public IAssetStorageService _assetStorageService;
        public List<NetworkPermissions> Permissions { get; set; }

        private IAuditLogService _auditLogger;
        private ICommonService _commonService;
        private RuleService _ruleService;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        [Dependency]
        public UniqueIdGenerator IrisIdGenerator
        {
            get
            {
                return _irisIdGenerator;
            }
            set
            {
                _irisIdGenerator = value;
            }
        }
        /// <summary>
        /// .ctor
        /// </summary>
        public AssetService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _assetStorageService = new AssetStorageService();
            _commonService = new CommonService(_repository);
            _ruleService = new RuleService(RuleService.CallType.Web);
        }

        /// <summary>
        /// return assetmodel list
        /// </summary>
        /// <param name="netId">selected NetworkObject</param>
        /// <returns>asset list</returns>
        public List<AssetModel> GetAssetlist(int? netId)
        {
            List<AssetModel> assetlist = new List<AssetModel>();
            try
            {

                if (netId.HasValue)
                {
                    var assetdata = _repository.GetAll<Asset>().ToList();

                    var networkObjectList = _ruleService.GetNetworkParents(netId.Value);
                    var assets = from a in _repository.GetQuery<Asset>(a => networkObjectList.Contains(a.NetworkObjectId) && a.IsCurrent == true).Include("AssetItemLinks").Include("AssetItemLinks.Item").Include("AssetCategoryLinks").Include("AssetCategoryLinks.Category").Include("TagAssetLinks").Include("TagAssetLinks.Tag").ToList()
                                 //join up in Permissions on a.NetworkObject.IrisId equals up.IrisId
                                 //where up.HasAccess == true && networkObjectList.Contains(a.NetworkObjectId) && a.IsCurrent == true
                                 select a;
                    foreach (var asset in assets)
                    {
                        bool hasVersions = false;
                        //Check whether it has any versions
                        if (assetdata.Where(x => x.Filename.CompareTo(asset.Filename) == 0 && x.DimX == asset.DimX && x.DimY == asset.DimY && x.NetworkObjectId == asset.NetworkObjectId && x.AssetTypeId == asset.AssetTypeId).Count() > 1)
                        {
                            hasVersions = true;
                        }
                        assetlist.Add(new AssetModel
                        {
                            AssetId = asset.AssetId,
                            IrisId = asset.IrisId,
                            BlobName = asset.Blobname == null ? string.Empty : asset.Blobname,
                            FileName = asset.Filename == null ? string.Empty : asset.Filename,
                            FileHash = asset.FileHash == null ? string.Empty : asset.FileHash,
                            DimX = asset.DimX,
                            DimY = asset.DimY,
                            Size = asset.Size,
                            CreatedDate = asset.CreatedDate,
                            IsActive = asset.IsActive,
                            IsCurrent = asset.IsCurrent,
                            BrandId = asset.NetworkObjectId,
                            HasVersions = hasVersions,
                            AssetTypeId = (int)asset.AssetTypeId,
                            AssetType = asset.AssetTypeId.ToString(),
                            ThumbnailBlob = asset.ThumbnailBlob == null ? string.Empty : asset.ThumbnailBlob,
                            AssetItemCount = asset.AssetItemLinks == null || asset.NetworkObjectId != netId ? 0 : asset.AssetItemLinks.Count(),
                            ItemName = asset.AssetItemLinks == null || asset.NetworkObjectId != netId ? "-" : (asset.AssetItemLinks.Count() == 0 ? "UNKNOWN" : (asset.AssetItemLinks.Count() == 1 ? (asset.AssetItemLinks.FirstOrDefault().Item == null ? string.Empty : asset.AssetItemLinks.FirstOrDefault().Item.ItemName) : "MULTIPLE")),
                            CatName = asset.AssetCategoryLinks == null || asset.NetworkObjectId != netId ? "-" : (asset.AssetCategoryLinks.Count() == 0 ? "UNKNOWN" : (asset.AssetCategoryLinks.Count() == 1 ? (asset.AssetCategoryLinks.FirstOrDefault().Category == null ? string.Empty : asset.AssetCategoryLinks.FirstOrDefault().Category.InternalName) : "MULTIPLE")),
                            TagName = getAssetRelatedTags(asset),
                            IsInherited = asset.NetworkObjectId != netId.Value
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return assetlist;
        }

        /// <summary>
        /// return item list to view model
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <returns>item list</returns>
        public List<ItemModel> GetAssetItemlist(int assetId, int networkObjectId)
        {
            List<ItemModel> itemlist = new List<ItemModel>();

            try
            {
                var items = (from a in _repository.GetQuery<AssetItemLink>(x => x.AssetId == assetId)
                             join i in _repository.GetQuery<Item>().Include("ItemPOSDataLinks") on a.ItemId equals i.ItemId
                             where i.OverrideItemId == null && i.ParentItemId == null // Only Master items
                             && i.NetworkObjectId == networkObjectId // Exclude Other brand items for Root assets
                             select i);
                foreach (var item in items)
                {
                    var defaultPOSLink = item.ItemPOSDataLinks.Where(x => x.IsDefault).FirstOrDefault();
                    itemlist.Add(new ItemModel
                    {
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        DisplayName = item.DisplayName,
                        IrisId = item.IrisId,
                        BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ?  (int?)defaultPOSLink.POSData.PLU : null,
                        AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty
                    });
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return itemlist;
        }

        /// <summary>
        /// return category list to view model
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <returns>category list</returns>
        public List<CategoryModel> GetAssetCategorylist(int assetId, int networkObjectId)
        {
            List<CategoryModel> catlist = new List<CategoryModel>();

            try
            {
                var parentNetworkIds = _ruleService.GetNetworkParents(networkObjectId);
                var childNetworkIds = _ruleService.GetNetworkChilds(networkObjectId);

                var cats = (from acl in _repository.GetQuery<AssetCategoryLink>(x => x.AssetId == assetId)
                            join c in _repository.GetQuery<Category>() on acl.CategoryId equals c.CategoryId
                            where c.OverrideCategoryId == null
                            && (c.NetworkObjectId == networkObjectId || childNetworkIds.Contains(c.NetworkObjectId)) // Exclude Other brand items for Root assets and include categories created by children 
                            select c);

                foreach (var cat in cats)
                {
                    catlist.Add(new CategoryModel
                    {
                        CategoryId = cat.CategoryId,
                        InternalName = cat.InternalName,
                        DisplayName = cat.DisplayName,
                        IrisId = cat.IrisId,
                    });
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return catlist;
        }

        /// <summary>
        /// return versions of a asset
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <returns>asset list</returns>
        public List<AssetModel> GetAssetVersionList(int? assetId)
        {
            List<AssetModel> assetList = new List<AssetModel>();

            try
            {
                if (assetId != null)
                {
                    var asset = (from a in _repository.GetQuery<Asset>(a => a.AssetId == assetId.Value)
                                 //join up in Permissions on a.NetworkObject.IrisId equals up.IrisId
                                 //where up.HasAccess == true && a.AssetId == assetId.Value
                                 select a).FirstOrDefault();

                    //var versions = from a in _repository.GetAll<Asset>()
                    //               where a.Filename == asset.Filename && a.DimX == asset.DimX && a.DimY == asset.DimY && a.NetworkObjectId == asset.NetworkObjectId && a.AssetTypeId == asset.AssetTypeId
                    //               select a;
                    var versions = getAssetVersionQuery(asset).ToList();

                    foreach (var version in versions)
                    {
                        assetList.Add(new AssetModel
                        {
                            AssetId = version.AssetId,
                            IrisId = version.IrisId,
                            FileName = version.Filename,
                            CreatedDate = version.CreatedDate,
                            BlobName = version.Blobname,
                            DimX = version.DimX,
                            DimY = version.DimY,
                            Size = version.Size,
                            IsActive = version.IsActive,
                            IsCurrent = version.IsCurrent,
                            BrandId = version.NetworkObjectId,
                            ThumbnailBlob = version.ThumbnailBlob,
                            AssetTypeId = (int)version.AssetTypeId,
                            AssetType = version.AssetTypeId.ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return assetList;
        }

        /// <summary>
        /// update item current asset to new asset id
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <param name="currentassetId">current AssetId</param>
        /// <returns></returns>
        public void MakeAssetCurrent(int assetId)
        {
            var filename = "";
            try
            {
                var selectedasset = _repository.GetQuery<Asset>(a => a.AssetId == assetId).FirstOrDefault();
                //var currentasset = _repository.GetQuery<Asset>(a => a.Filename.CompareTo(selectedasset.Filename) == 0 && a.DimX == selectedasset.DimX && a.DimY == selectedasset.DimY && a.NetworkObjectId == selectedasset.NetworkObjectId && a.AssetTypeId == selectedasset.AssetTypeId && a.IsCurrent).FirstOrDefault();
                var currentasset = getAssetVersionQuery(selectedasset).Where(x => x.IsCurrent).FirstOrDefault();

                filename = selectedasset.Filename;
                if (selectedasset.AssetId != currentasset.AssetId)
                {
                    currentasset.IsCurrent = false;
                    selectedasset.IsCurrent = true;

                    var assetlinks = _repository.GetQuery<AssetItemLink>(p => p.AssetId == currentasset.AssetId).ToList();
                    foreach (var ail in assetlinks)
                    {
                        ail.AssetId = assetId;
                    }

                    var assetcatlinks = _repository.GetQuery<AssetCategoryLink>(p => p.AssetId == currentasset.AssetId).ToList();
                    foreach (var acl in assetcatlinks)
                    {
                        acl.AssetId = assetId;
                    }

                    var assetTagLinks = _repository.GetQuery<TagAssetLink>(p => p.AssetId == currentasset.AssetId).ToList();
                    foreach (var assetTagLink in assetTagLinks)
                    {
                        assetTagLink.AssetId = assetId;
                    }

                    _commonService.SetLastUpdatedDateofMenusUsingAssets(new List<int> { currentasset.AssetId });
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.AssetVersionUpdateT, selectedasset.Filename);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Asset, entityNameList: selectedasset.Filename, operationDescription: _lastActionResult);
                }
                Logger.WriteInfo(_lastActionResult);
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetVersionUpdateT, filename);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Delete assets along with its Versions/ only version
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <returns></returns>
        public void DeleteAssets(int[] assetIds)
        {
            try
            {
                List<string> deletedAssets = new List<string>(), invalidAssets = new List<string>(), errAssets = new List<string>();
            
                var assets = _repository.GetQuery<Asset>(a => assetIds.Contains(a.AssetId)).ToList();

                List<string> blobnames = new List<string>();
                string container = ConfigurationManager.AppSettings["AssetBlobContainer"].ToString();
                foreach (var asset in assets)
                {
                    //Deleting Versions, if complete asset should be deleted.
                    //var assetversions = _repository.GetQuery<Asset>(a => a.Filename.CompareTo(asset.Filename) == 0 && a.DimX == asset.DimX && a.DimY == asset.DimY && a.NetworkObjectId == asset.NetworkObjectId && a.AssetTypeId == asset.AssetTypeId && a.AssetId != asset.AssetId).ToList();
                    var assetversions = getAssetVersionQuery(asset).Where(a => a.AssetId != asset.AssetId).ToList();
                    foreach (var version in assetversions)
                    {
                        // delete related AssetItemLink
                        var lstverAssetItemLinks = version.AssetItemLinks.ToList();
                        foreach (var ail in lstverAssetItemLinks)
                        {
                            _repository.Delete<AssetItemLink>(ail);
                        }
                        // delete related AssetCategoryLink
                        var lstverAssetCatLinks = version.AssetCategoryLinks.ToList();
                        foreach (var acl in lstverAssetCatLinks)
                        {
                            _repository.Delete<AssetCategoryLink>(acl);
                        }
                        // delete related TagAssetLink
                        var lstverAssetTagLinks = version.TagAssetLinks.ToList();
                        foreach (var tal in lstverAssetTagLinks)
                        {
                            _repository.Delete<TagAssetLink>(tal);
                        }
                        // delete Asset
                        _repository.Delete<Asset>(version);

                        //Add blobnames to delete at the end
                        blobnames.Add(version.Blobname);
                        blobnames.Add(version.ThumbnailBlob);
                    }
                    // delete related AssetItemLink
                    var lstAssetItemLinks = asset.AssetItemLinks.ToList();
                    foreach (var ail in lstAssetItemLinks)
                    {
                        _repository.Delete<AssetItemLink>(ail);
                    }
                    // delete related AssetCategoryLink
                    var lstAssetCatLinks = asset.AssetCategoryLinks.ToList();
                    foreach (var acl in lstAssetCatLinks)
                    {
                        _repository.Delete<AssetCategoryLink>(acl);
                    }
                    // delete related TagAssetLink
                    var lstAssetTagLinks = asset.TagAssetLinks.ToList();
                    foreach (var tal in lstAssetTagLinks)
                    {
                        _repository.Delete<TagAssetLink>(tal);
                    }
                    // delete Asset
                    _repository.Delete<Asset>(asset);

                    blobnames.Add(asset.Blobname);
                    blobnames.Add(asset.ThumbnailBlob);

                    deletedAssets.Add(asset.Filename);
                }
                _commonService.SetLastUpdatedDateofMenusUsingAssets(assetIds.ToList());
                _context.SaveChanges();

                //Audit Logging
                _lastActionResult = string.Format(Constants.AuditMessage.AssetDeletedT, string.Join(",", deletedAssets.ToList()));
                _auditLogger.Write(OperationPerformed.Deleted, EntityType.Asset, entityNameList: string.Join(",", assets.Select(p => p.Filename).ToList()));

                // Looping after delete to ensure assets are deleted(saveChanges) without error.
                foreach (var blobname in blobnames)
                {
                    // Remove blob 
                    AssetStorageService assetStorageService = new AssetStorageService();
                    assetStorageService.RemoveFromStorage(blobname, container);
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetDeleteT);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Delete assets only version
        /// </summary>
        /// <param name="assetId">selected AssetId</param>
        /// <returns>returns the assetId only if the current version is deleted. Otherwise returns 0</returns>
        public int DeleteAssetVersions(int[] assetIds, bool isAllVersionsToDelete)
        {
            int currentAssetid = 0;
            try
            {
                List<string> deletedAssets = new List<string>(), invalidAssets = new List<string>(), errAssets = new List<string>();
                var assets = (from a in _repository.GetQuery<Asset>().Include("AssetItemLinks").Include("AssetCategoryLinks").Include("TagAssetLinks")
                              where assetIds.Contains(a.AssetId)
                              select a).ToList();

                List<string> blobnames = new List<string>();
                string container = ConfigurationManager.AppSettings["AssetBlobContainer"].ToString();
                _lastActionResult = "";
                foreach (var asset in assets)
                {
                    //Update the assetitemlinks to next current version
                    if (asset.IsCurrent && !isAllVersionsToDelete)
                    {
                        //var lastCurrentAsset = _repository.GetQuery<Asset>(a => a.Filename.CompareTo(asset.Filename) == 0 && a.DimX == asset.DimX && a.DimY == asset.DimY && a.NetworkObjectId == asset.NetworkObjectId && a.AssetTypeId == asset.AssetTypeId && !assetIds.Contains(a.AssetId)).OrderByDescending(a => a.CreatedDate).FirstOrDefault();
                        var lastCurrentAsset = getAssetVersionQuery(asset).Where(a => !assetIds.Contains(a.AssetId)).OrderByDescending(a => a.CreatedDate).FirstOrDefault();
                        if (lastCurrentAsset != null)
                        {
                            // update related AssetItemLink
                            var lstAssetItemLinks = asset.AssetItemLinks.ToList();
                            foreach (var ail in lstAssetItemLinks)
                            {
                                ail.AssetId = lastCurrentAsset.AssetId;
                            }
                            // update related AssetCategoryLink
                            var lstAssetCatLinks = asset.AssetCategoryLinks.ToList();
                            foreach (var acl in lstAssetCatLinks)
                            {
                                acl.AssetId = lastCurrentAsset.AssetId;
                            }
                            // update related AssetTagLink
                            var lstAssetTagLinks = asset.TagAssetLinks.ToList();
                            foreach (var atl in lstAssetTagLinks)
                            {
                                atl.AssetId = lastCurrentAsset.AssetId;
                            }
                            lastCurrentAsset.IsCurrent = true;
                            currentAssetid = lastCurrentAsset.AssetId;
                        }
                    }

                    //If this is last asset or all versions are deleted, then their wont be other asset to make current
                    if (isAllVersionsToDelete)
                    {
                        // delete related AssetItemList
                        var lstAssetItmLinks = asset.AssetItemLinks.ToList();
                        foreach (var ail in lstAssetItmLinks)
                        {
                            _repository.Delete<AssetItemLink>(ail);
                        }
                        // delete related AssetCatLink
                        var lstAssetCatLinks = asset.AssetCategoryLinks.ToList();
                        foreach (var acl in lstAssetCatLinks)
                        {
                            _repository.Delete<AssetCategoryLink>(acl);
                        }

                        // delete related TagAssetLink
                        var lstAssetTagLinks = asset.TagAssetLinks.ToList();
                        foreach (var tal in lstAssetTagLinks)
                        {
                            _repository.Delete<TagAssetLink>(tal);
                        }
                    }
                    // delete Asset
                    _repository.Delete<Asset>(asset);

                    blobnames.Add(asset.Blobname);
                    blobnames.Add(asset.ThumbnailBlob);

                    deletedAssets.Add(asset.Filename);

                }
                _commonService.SetLastUpdatedDateofMenusUsingAssets(assetIds.ToList());
                _context.SaveChanges();

                //Audit Logging
                _lastActionResult = string.Format(Constants.AuditMessage.AssetDeletedT, string.Join(",", deletedAssets.ToList()));
                _auditLogger.Write(OperationPerformed.Deleted, EntityType.Asset, entityNameList: string.Join(",", assets.Select(p => p.Filename).ToList()));

                // Looping after delete to ensure assets are deleted without error.
                foreach (var blobname in blobnames)
                {
                    // Remove blob 
                    AssetStorageService assetStorageService = new AssetStorageService();
                    assetStorageService.RemoveFromStorage(blobname, container);
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetDeleteT);
                Logger.WriteError(ex);
            }
            // returns the assetId only if the current version is deleted. Otherwise return Zero
            return currentAssetid;
        }

        /// <summary>
        /// Create Asset
        /// </summary>
        /// <param name="assetId">Asset Model</param>
        /// <returns></returns>
        public int AddAsset(AssetModel assetModel, string tagIdsToLink = "")
        {
            try
            {
                //1. Deserialize the Data
                var js = new JavaScriptSerializer();

                //1.1 Deserialize JSON String:networkObjectIds to List<int>()
                var tagIds = js.Deserialize<IEnumerable<int>>(tagIdsToLink);

                //var existingAssets = _repository.GetQuery<Asset>(a => a.Filename == assetModel.FileName && a.DimX == assetModel.DimX && a.DimY == assetModel.DimY && a.NetworkObjectId == assetModel.BrandId).ToList();
                //var lastCurrentAsset = _repository.GetQuery<Asset>(a => a.Filename == assetModel.FileName && a.DimX == assetModel.DimX && a.DimY == assetModel.DimY && a.NetworkObjectId == assetModel.BrandId && a.AssetTypeId == (AssetTypes)assetModel.AssetTypeId && a.IsCurrent).FirstOrDefault();
                var lastCurrentAsset = getAssetVersionQuery(assetModel).Where(a => a.IsCurrent).FirstOrDefault();
                //if (existingAssets != null && existingAssets.Count() > 0)
                //{
                //    existingAssets.ForEach(e => e.IsCurrent = false);
                //}
                var ails = new List<AssetItemLink>();
                var acls = new List<AssetCategoryLink>();
                List<TagAssetLink> tals = null;
                IEnumerable<int> existingTagIds = null;
                if (lastCurrentAsset != null)
                {
                    lastCurrentAsset.IsCurrent = false;
                    ails = lastCurrentAsset.AssetItemLinks.ToList();
                    acls = lastCurrentAsset.AssetCategoryLinks.ToList();
                    tals = lastCurrentAsset.TagAssetLinks.ToList();
                    existingTagIds = tals.Select(m => m.TagId);
                }

                Asset asset = new Asset
                {
                    Filename = assetModel.FileName,
                    Blobname = assetModel.BlobName,
                    FileHash = assetModel.FileHash,
                    DimX = assetModel.DimX,
                    DimY = assetModel.DimY,
                    Size = assetModel.Size,
                    CreatedDate = DateTime.Now,
                    IsActive = true,
                    IsCurrent = true,
                    NetworkObjectId = assetModel.BrandId,
                    ThumbnailBlob = assetModel.ThumbnailBlob,
                    AssetTypeId = (AssetTypes)assetModel.AssetTypeId,
                    IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName)
                };

                //remove ail from lst asset and add to current asset
                if (ails != null && ails.Any())
                {
                    foreach (var ail in ails)
                    {
                        asset.AssetItemLinks.Add(new AssetItemLink
                        {
                            ItemId = ail.ItemId
                        });
                        _repository.Delete<AssetItemLink>(ail);
                    }
                }

                //remove acl from lst asset and add to current asset
                if (acls != null && acls.Any())
                {
                    foreach (var acl in acls)
                    {
                        asset.AssetCategoryLinks.Add(new AssetCategoryLink
                        {
                            CategoryId = acl.CategoryId
                        });
                        _repository.Delete<AssetCategoryLink>(acl);
                    }
                }

                //Remove the tags associated with the previously market 'current' asset
                if (tals != null && tals.Any())
                {
                    foreach (var tal in tals)
                    {
                        _repository.Delete<TagAssetLink>(tal);
                    }
                }

                //Union the tags selected by the user at the time of upload assets and the tags which were already associated with the previous version of assets
                tagIds = (existingTagIds != null && existingTagIds.Any()) ? ((tagIds != null && tagIds.Any()) ? tagIds.Union(existingTagIds) : existingTagIds) : tagIds;
                if (tagIds != null && tagIds.Any())
                {
                    foreach (var tagId in tagIds)
                    {
                        asset.TagAssetLinks.Add(new TagAssetLink
                        {
                            TagId = tagId
                        });
                    }
                }

                _repository.Add<Asset>(asset);
                if (lastCurrentAsset != null)
                {
                    _commonService.SetLastUpdatedDateofMenusUsingAssets(new List<int> { lastCurrentAsset.AssetId });
                }
                _context.SaveChanges();
                assetModel.AssetId = asset.AssetId;
                var networkName = _repository.GetQuery<NetworkObject>(x => x.NetworkObjectId == asset.NetworkObjectId).FirstOrDefault().Name;
                _lastActionResult = string.Format(Constants.AuditMessage.AssetCreateT, assetModel.FileName, networkName);
                _auditLogger.Write(OperationPerformed.Created, EntityType.Asset, entityNameList: assetModel.FileName);
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetCreateT, assetModel.FileName);
            }
            return assetModel.AssetId;
        }

        public int SaveImagetoStorageandCreateAsset(HttpPostedFileBase file, string container, int? netId, int? imgType, string tagIdsToLink)
        {
            int assetId = 0;

            try
            {
                //Deserialize JSON String:networkObjectIds to List<int>()
                var js = new JavaScriptSerializer();
                var tagIds = js.Deserialize<int[]>(tagIdsToLink);

                // Create Blob Image
                string blobname = _assetStorageService.SaveFileinStorage(file, container);

                AssetModel asset = new AssetModel();

                asset.Size = file.ContentLength; // ContentLength is lost after creating image hence save before.
                var img = new System.Web.Helpers.WebImage(file.InputStream);

                asset.DimX = img.Width;
                asset.DimY = img.Height;

                var thumbnail = img;
                byte[] fileBytes = img.GetBytes();
                System.Security.Cryptography.HashAlgorithm hashingAlgorithm = new System.Security.Cryptography.SHA1Managed();
                byte[] hashedData = hashingAlgorithm.ComputeHash(fileBytes);

                //Resize the Image
                var thumbnailImg = thumbnail.Resize(103, 77);
                byte[] thumbnailBytes = thumbnailImg.GetBytes();
                Stream thumbNailstream = new MemoryStream(thumbnailBytes);

                string thumbnailBlob = _assetStorageService.SaveFileStreaminStorage(thumbNailstream, container, file.ContentType, Path.GetExtension(file.FileName));
                asset.FileName = Path.GetFileName(file.FileName);
                asset.BlobName = blobname;
                //Create FileHash
                asset.FileHash = Convert.ToBase64String(hashedData);
                asset.BrandId = netId.Value;
                asset.AssetTypeId = imgType.Value;
                asset.ThumbnailBlob = thumbnailBlob;

                //  Check is it a version and add as new Asset or version of existing asset
                assetId = AddAsset(asset, tagIdsToLink);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return assetId;
        }

        /// <summary>
        /// Get Asset by Name - Only used to delete latest uploaded file
        /// </summary>
        /// <param name="filename">File Name</param>
        /// <returns>AssetModel</returns>
        public AssetModel GetAssetbyFileName(string filename, int? netId)
        {
            AssetModel assetModel = new AssetModel();
            try
            {
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    var asset = (from a in _repository.GetQuery<Asset>(a => a.Filename == filename)
                                 //join up in Permissions on a.NetworkObject.IrisId equals up.IrisId
                                 //where up.HasAccess == true && a.Filename == filename
                                 where netId.HasValue ? a.NetworkObjectId == netId : 1 == 1
                                 orderby a.CreatedDate descending
                                 select a).FirstOrDefault();

                    if (asset != null)
                    {
                        assetModel.AssetId = asset.AssetId;
                        assetModel.IrisId = asset.IrisId;
                        assetModel.FileName = asset.Filename;
                        assetModel.BlobName = asset.Blobname;
                        assetModel.FileHash = asset.FileHash;
                        assetModel.DimX = asset.DimX;
                        assetModel.DimY = asset.DimY;
                        assetModel.Size = asset.Size;
                        assetModel.CreatedDate = asset.CreatedDate;
                        assetModel.IsActive = asset.IsActive;
                        assetModel.IsCurrent = asset.IsCurrent;
                        assetModel.BrandId = asset.NetworkObjectId;
                        assetModel.ThumbnailBlob = asset.ThumbnailBlob;
                        assetModel.AssetTypeId = (int)asset.AssetTypeId;
                        assetModel.AssetType = asset.AssetTypeId.ToString();
                    }
                    else
                    {
                        assetModel = null;
                    }
                }
                else
                {
                    assetModel = null;
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return assetModel;
        }

        /// <summary>
        /// Delete asset - Use this only to Delete recently uploaded Asset
        /// </summary>
        /// <param name="assetModel">Asset</param>
        /// <returns></returns>
        public void DeleteLatestAsset(AssetModel assetModel)
        {
            try
            {
                string container = ConfigurationManager.AppSettings["AssetBlobContainer"].ToString();

                var asset = _repository.GetQuery<Asset>(a => a.AssetId == assetModel.AssetId).FirstOrDefault();
                //var lastCurrentAsset = _repository.GetQuery<Asset>(a => a.Filename.CompareTo(assetModel.FileName) == 0 && a.DimX == assetModel.DimX && a.DimY == assetModel.DimY && a.NetworkObjectId == assetModel.BrandId && a.AssetTypeId == (AssetTypes)assetModel.AssetTypeId).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                var lastCurrentAsset = getAssetVersionQuery(assetModel).OrderByDescending(x => x.CreatedDate).FirstOrDefault();
                // If it has a version, then those AILs are changed, hence revert the changes.
                if (lastCurrentAsset != null)
                {
                    lastCurrentAsset.IsCurrent = true;
                    // update related AssetItemLink
                    var lstAssetItemLinks = asset.AssetItemLinks.ToList();
                    foreach (var ail in lstAssetItemLinks)
                    {
                        ail.AssetId = lastCurrentAsset.AssetId;
                    }
                    // update related AssetCatLink
                    var lstAssetCatLinks = asset.AssetCategoryLinks.ToList();
                    foreach (var acl in lstAssetCatLinks)
                    {
                        acl.AssetId = lastCurrentAsset.AssetId;
                    }
                }
                else
                {
                    // delete related AssetItemLink
                    var lstAssetItemLinks = asset.AssetItemLinks.ToList();
                    foreach (var ail in lstAssetItemLinks)
                    {
                        _repository.Delete<AssetItemLink>(ail);
                    }
                    // delete related AssetCategoryLink
                    var lstAssetCatLinks = asset.AssetCategoryLinks.ToList();
                    foreach (var acl in lstAssetCatLinks)
                    {
                        _repository.Delete<AssetCategoryLink>(acl);
                    }
                    // delete related TagAssetLink
                    var lstAssetTagLinks = asset.TagAssetLinks.ToList();
                    foreach (var tal in lstAssetTagLinks)
                    {
                        _repository.Delete<TagAssetLink>(tal);
                    }
                }

                // Remove blob 
                AssetStorageService assetStorageService = new AssetStorageService();
                assetStorageService.RemoveFromStorage(asset.Blobname, container);
                assetStorageService.RemoveFromStorage(asset.ThumbnailBlob, container);

                // delete Asset
                _repository.Delete<Asset>(asset);
                _commonService.SetLastUpdatedDateofMenusUsingAssets(new List<int> { asset.AssetId });
                _context.SaveChanges();


                _lastActionResult = string.Format(Constants.AuditMessage.AssetDeletedT, assetModel.FileName);
                _auditLogger.Write(OperationPerformed.Deleted, EntityType.Asset, entityNameList: assetModel.FileName);
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetDeleteT, assetModel.FileName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Remove from AssetItemLink
        /// </summary>
        /// <param name="itemIds">item Ids</param>
        /// <param name="assetId">AssetId</param>
        /// <returns></returns>
        public void RemoveItemsfromAsset(int[] itemIds, int assetId)
        {
            var fileName = "";
            var itemname = "";
            var removedItemNameList = string.Empty;
            try
            {
                fileName = _repository.GetQuery<Asset>(x => x.AssetId == assetId).FirstOrDefault().Filename;
                var lstAssetItemLinks = _repository.GetQuery<AssetItemLink>(a => a.ItemId.HasValue && itemIds.Contains(a.ItemId.Value) && a.AssetId == assetId).Include("item");

                foreach (var ail in lstAssetItemLinks)
                {
                    itemname = ail.Item != null ? ail.Item.ItemName : "";
                    int netId = ail.Asset.NetworkObjectId;
                    _repository.Delete<AssetItemLink>(ail);
                    removedItemNameList = string.Format("{0},{1}", removedItemNameList, itemname);
                }
                _commonService.SetLastUpdatedDateofMenusUsingItems(itemIds.ToList());
                _context.SaveChanges();

                _lastActionResult = string.Format(Constants.AuditMessage.AssetItemLinkDeletedT, fileName, removedItemNameList.Remove(0, 1));
                _auditLogger.Write(OperationPerformed.Other, EntityType.Asset, entityNameList: fileName, operationDescription: string.Format(Constants.AuditMessage.AssetItemLinkDeletedT, fileName, removedItemNameList.Remove(0, 1)));
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetItemLinkDeleteT, fileName, itemname);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Add to AssetItemLink
        /// </summary>
        /// <param name="itemIds">itemId</param>
        /// <param name="assetId">AssetId</param>
        /// <returns></returns>
        public void AddItemtoAsset(int[] itemIds, int assetId)
        {
            var itemname = "";
            var filename = "";
            List<string> addedItems = new List<string>(), invalidItems = new List<string>(), invalidAssets = new List<string>(), errItems = new List<string>(), addeditemIds = new List<string>();
            try
            {
                if (itemIds != null)
                {
                    var asset = _repository.GetQuery<Asset>(a => a.AssetId == assetId).FirstOrDefault();

                    var ails = _repository.GetQuery<AssetItemLink>(i => i.ItemId.HasValue && itemIds.Contains(i.ItemId.Value)).ToList();
                    var items = _repository.GetQuery<Item>(i => itemIds.Contains(i.ItemId)).ToList();
                    foreach (var itemToAdd in items)
                    {
                        var lstAssetItemLinks = ails.Where(x => x.ItemId == itemToAdd.ItemId).ToList();
                        var item = items.Where(x => x.ItemId == itemToAdd.ItemId).FirstOrDefault();
                        bool hasSimilarAssetLinked = false;
                        // if it valid item then perform the action
                        if (item != null && asset != null)
                        {
                            itemname = item.ItemName;
                            filename = asset.Filename;
                            if (lstAssetItemLinks != null) // update exsiting record if it already present.
                            {
                                foreach (var ail in lstAssetItemLinks)
                                {
                                    var linkedAsset = getAssetVersionQuery(asset).Where(a => a.AssetId == ail.AssetId).FirstOrDefault();
                                    //if (linkedAsset.Filename.CompareTo(asset.Filename) == 0 && linkedAsset.DimX == asset.DimX && linkedAsset.DimY == asset.DimY && linkedAsset.NetworkObjectId == asset.NetworkObjectId && linkedAsset.AssetTypeId == asset.AssetTypeId)
                                    if (linkedAsset != null)
                                    {
                                        ail.AssetId = assetId;
                                        hasSimilarAssetLinked = true;
                                    }
                                }
                            }
                            //Insert New record if it is not present
                            if (lstAssetItemLinks == null || !hasSimilarAssetLinked)
                            {
                                AssetItemLink ail = new AssetItemLink
                                {
                                    AssetId = assetId,
                                    ItemId = item.ItemId
                                };
                                _repository.Add<AssetItemLink>(ail);
                            }
                            addedItems.Add(item.ItemName);
                            addeditemIds.Add(item.ItemId.ToString());
                        }
                        else
                        {
                            if (item != null)
                            {
                                invalidItems.Add(item.ItemName);
                            }
                            else
                            {
                                invalidAssets.Add(asset.Filename);
                            }
                        }

                    }
                    _commonService.SetLastUpdatedDateofMenusUsingItems(itemIds.ToList());
                    _context.SaveChanges();
                    if (addedItems.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.AssetItemLinkCreateT, string.Join(",", addedItems), asset.Filename);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.Asset, entityNameList: asset.Filename, operationDescription: _lastActionResult);
                    }
                    if (invalidItems.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, string.Join(",", invalidItems));
                    }
                    if (invalidAssets.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidAssetT, string.Join(",", invalidAssets));
                    }

                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetItemLinkAddT, filename, itemname);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Remove from AssetCategoryLink
        /// </summary>
        /// <param name="catIds">cat Ids</param>
        /// <param name="assetId">AssetId</param>
        /// <returns></returns>
        public void RemoveCatsfromAsset(int[] catIds, int assetId)
        {
            var fileName = "";
            var catname = "";
            var removedCategoryNameList = string.Empty;
            try
            {
                fileName = _repository.GetQuery<Asset>(x => x.AssetId == assetId).FirstOrDefault().Filename;
                var lstAssetItemLinks = _repository.GetQuery<AssetCategoryLink>(a => a.CategoryId.HasValue && catIds.Contains(a.CategoryId.Value) && a.AssetId == assetId).Include("Category");

                foreach (var ail in lstAssetItemLinks)
                {
                    catname = ail.Category == null ? string.Empty : ail.Category.InternalName;
                    int netId = ail.Asset.NetworkObjectId;
                    _repository.Delete<AssetCategoryLink>(ail);
                    removedCategoryNameList = string.Format("{0},{1}", removedCategoryNameList, catname);
                }
                _commonService.SetLastUpdatedDateofMenusUsingCats(catIds.ToList());
                _context.SaveChanges();

                _lastActionResult = string.Format(Constants.AuditMessage.AssetCategoryLinkDeletedT, fileName, removedCategoryNameList.Remove(0, 1));
                _auditLogger.Write(OperationPerformed.Other, EntityType.Asset, entityNameList: fileName, operationDescription: string.Format(Constants.AuditMessage.AssetCategoryLinkDeletedT, fileName, removedCategoryNameList.Remove(0, 1)));
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetCategoryLinkDeleteT, fileName, catname);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Add to AssetCategoryLink
        /// </summary>
        /// <param name="catIds">catIds</param>
        /// <param name="assetId">AssetId</param>
        /// <returns></returns>
        public void AddCatstoAsset(int[] catIds, int assetId)
        {
            var catname = "";
            var filename = "";
            List<string> addedCats = new List<string>(), invalidCats = new List<string>(), errCats = new List<string>(), invalidAssets = new List<string>(), addedCatIds = new List<string>();
            try
            {
                if (catIds != null)
                {
                    var asset = _repository.GetQuery<Asset>(a => a.AssetId == assetId).FirstOrDefault();
                    var cats = _repository.GetQuery<Category>(i => catIds.Contains(i.CategoryId)).ToList();
                    foreach (var catToAdd in cats)
                    {

                        var lstAssetCategoryLinks = _repository.GetQuery<AssetCategoryLink>(i => i.CategoryId == catToAdd.CategoryId).ToList();
                        var cat = cats.FirstOrDefault(i => i.CategoryId == catToAdd.CategoryId);
                        bool hasSimilarAssetLinked = false;
                        // if it valid item then perform the action
                        if (cat != null && asset != null)
                        {
                            catname = cat.InternalName;
                            filename = asset.Filename;
                            if (lstAssetCategoryLinks != null) // update exsiting record if it already present.
                            {
                                foreach (var ail in lstAssetCategoryLinks)
                                {
                                    var linkedAsset = getAssetVersionQuery(asset).Where(a => a.AssetId == ail.AssetId).FirstOrDefault();
                                    //if (linkedAsset.Filename.CompareTo(asset.Filename) == 0 && linkedAsset.DimX == asset.DimX && linkedAsset.DimY == asset.DimY && linkedAsset.NetworkObjectId == asset.NetworkObjectId && linkedAsset.AssetTypeId == asset.AssetTypeId)
                                    if (linkedAsset != null)
                                    {
                                        //update the asset id if another version is already present
                                        ail.AssetId = assetId;
                                        hasSimilarAssetLinked = true;
                                    }
                                }
                            }
                            //Insert New record if it is not present
                            if (lstAssetCategoryLinks == null || !hasSimilarAssetLinked)
                            {
                                AssetCategoryLink ail = new AssetCategoryLink
                                {
                                    AssetId = assetId,
                                    CategoryId = cat.CategoryId
                                };
                                _repository.Add<AssetCategoryLink>(ail);
                            }
                            addedCats.Add(cat.InternalName);
                            addedCatIds.Add(cat.CategoryId.ToString());
                        }
                        else
                        {
                            if (cat != null)
                            {
                                invalidCats.Add(cat.InternalName);
                            }
                            else
                            {
                                invalidAssets.Add(asset.Filename);
                            }
                        }

                    }
                    _commonService.SetLastUpdatedDateofMenusUsingCats(catIds.ToList());
                    _context.SaveChanges();
                    if (addedCats.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.AssetCategoryLinkCreateT, string.Join(",", addedCats), asset.Filename);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.Asset, entityNameList: asset.Filename, operationDescription: _lastActionResult);
                    }
                    if (invalidCats.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidCatT, string.Join(",", invalidCats));
                    }
                    if (invalidAssets.Any())
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidAssetT, string.Join(",", invalidAssets));
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrAssetCategoryLinkAddT, filename, catname);
                Logger.WriteError(ex);
            }
        }



        public bool IsImageAVersion(string name, int size, int? netId, int? imgType)
        {
            var retVal = false;
            try
            {
                var count = _repository.GetQuery<Asset>(a => a.Filename.CompareTo(name) == 0 && a.Size == size && a.NetworkObjectId == netId && a.AssetTypeId == (AssetTypes)imgType.Value).Count();
                if(count > 0)
                {
                    retVal = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retVal;
        }

        public IEnumerable<KeyValuePair<int, string>> GetImageTypes()
        {
            var retVal = from AssetTypes e in Enum.GetValues(typeof(AssetTypes))
                         select new KeyValuePair<int, string>
                             (
                                 (int)Enum.Parse(typeof(AssetTypes), e.ToString()),
                                 e.ToString()
                             );
            return retVal;
        }

        private string getAssetRelatedTags(Asset asset)
        {
            var commaSeperatedTags = string.Empty;
            var tagNames = (asset.TagAssetLinks != null && asset.TagAssetLinks.Any()) ? asset.TagAssetLinks.Where(x => x.Tag != null).Select(p => p.Tag.TagName) : null;

            if (tagNames != null && tagNames.Any())
            {
                commaSeperatedTags = string.Join(",", tagNames.ToList());
            }

            return commaSeperatedTags;
        }
        private IQueryable<Asset> getAssetVersionQuery(Asset selectedasset)
        {
            return _repository.GetQuery<Asset>(a => a.Filename.CompareTo(selectedasset.Filename) == 0 && a.DimX == selectedasset.DimX && a.DimY == selectedasset.DimY && a.NetworkObjectId == selectedasset.NetworkObjectId && a.AssetTypeId == selectedasset.AssetTypeId);
        }
        private IQueryable<Asset> getAssetVersionQuery(AssetModel selectedasset)
        {
            return _repository.GetQuery<Asset>(a => a.Filename.CompareTo(selectedasset.FileName) == 0 && a.DimX == selectedasset.DimX && a.DimY == selectedasset.DimY && a.NetworkObjectId == selectedasset.BrandId && a.AssetTypeId == (AssetTypes)selectedasset.AssetTypeId);
        }
    }

    public interface IAssetService
    {
        string LastActionResult { get; }
        List<AssetModel> GetAssetlist(int? netId);
        List<ItemModel> GetAssetItemlist(int assetId, int networkObjectId);
        List<CategoryModel> GetAssetCategorylist(int assetId, int networkObjectId);
        List<AssetModel> GetAssetVersionList(int? assetId);
        void MakeAssetCurrent(int assetId);
        AssetModel GetAssetbyFileName(string filename,int? netId);
        int AddAsset(AssetModel assetModel, string tagIdsToLink = "");
        void DeleteAssets(int[] assetIds);
        void DeleteLatestAsset(AssetModel assetModel);
        void RemoveItemsfromAsset(int[] itemIds, int assetId);
        void AddItemtoAsset(int[] itemIds, int assetId);
        void RemoveCatsfromAsset(int[] catIds, int assetId);
        void AddCatstoAsset(int[] catIds, int assetId);
        int DeleteAssetVersions(int[] assetIds, bool isAllVersionsToDelete);
        IEnumerable<KeyValuePair<int, string>> GetImageTypes();
        int SaveImagetoStorageandCreateAsset(HttpPostedFileBase file, string container, int? netId, int? imgType, string tagIdsToLink);
    }
}