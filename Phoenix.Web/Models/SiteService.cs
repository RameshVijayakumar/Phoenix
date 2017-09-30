using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using Phoenix.Web.Models.Grid;
using SnowMaker;

namespace Phoenix.Web.Models
{


    public class SiteService : KendoGrid<SiteInfo>, ISiteService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        private UniqueIdGenerator _irisIdGenerator;
        public List<NetworkPermissions> Permissions { get; set; }
        private IAuditLogService _auditLogger;
        private RuleService _ruleService;
        private IAdminService _adminService;

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
        public SiteService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _ruleService = new RuleService(RuleService.CallType.Web);
            _adminService = new AdminService();
        }

        /// <summary>
        /// returns site details for a  given site      
        /// </summary>
        /// <param name="siteId">site id</param>
        /// <returns>View model for Site</returns>
        public SiteModel GetSiteDetails(long siteId)
        {
            var model = new SiteModel();
            var siteInfo = _repository.GetQuery<SiteInfo>(s => s.NetworkObject.IrisId == siteId).Include("NetworkObject").FirstOrDefault();
            if (siteInfo != null)
            {
                model.SiteId = siteInfo.SiteId;
                model.StoreNumber = siteInfo.StoreNumber;
                model.IrisId = siteInfo.NetworkObject.IrisId;
                model.NetworkObject.Name = siteInfo.NetworkObject.Name;
                model.Address1 = siteInfo.Address1 ?? string.Empty;
                model.Address2 = siteInfo.Address2 ?? string.Empty;
                model.City = siteInfo.City;
                model.State = siteInfo.State;
                model.Zip = siteInfo.Zip;
                model.Phone = siteInfo.Phone;
                model.Latitude = siteInfo.Latitude;
                model.Longitude = siteInfo.Longitude;
                model.ServicesOffered = mapServices(siteInfo.NetworkObject.SerivceNetworkObjectLinks);
                model.CuisinesOffered = mapCuisines(siteInfo.NetworkObject.CuisineNetworkObjectLinks);
                model.SiteTimeZone = string.IsNullOrWhiteSpace(siteInfo.TimeZoneId) ? model.SiteTimeZone : TimeZoneInfo.FindSystemTimeZoneById(siteInfo.TimeZoneId);
                model.SiteTimeZoneId = model.SiteTimeZone.Id;
                model.LastUpdatedDate = siteInfo.LastUpdatedDate;
            }
            return model;
        }


        /// <summary>
        /// return site list to view model
        /// </summary>
        /// <param name="netId">selected NetworkObject</param>
        /// <param name="resultCount"></param>
        /// <param name="grdRequest"></param>
        /// <returns>site list</returns>
        public List<SiteModel> GetSitelist(int? netId, out int resultCount, KendoGridRequest grdRequest)
        {
            var sitelist = new List<SiteModel>();
            resultCount = 0;
            try
            {
                if (netId.HasValue)
                {
                    var filtering = GetFiltering(grdRequest);
                    var sorting = GetSorting(grdRequest);

                    var childNetworkIds = _ruleService.GetNetworkChilds(netId.Value);
                    //Include selected site/network in the list too
                    childNetworkIds.Add(netId.Value);
                    var networks = _repository.GetQuery<NetworkObject>(x =>childNetworkIds.Contains((x.NetworkObjectId)) && x.NetworkObjectTypeId == NetworkObjectTypes.Site).ToList();
                    var permittedNetworkObjectIdList = (from no in networks
                                               join up in Permissions on new { no.IrisId, no.NetworkObjectTypeId } equals new { IrisId = up.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(up.NetworkType) }
                                               where up.HasAccess
                                               select no.NetworkObjectId).ToList();

                    var indeterminateSites = _repository.GetQuery<SiteInfo>(p => permittedNetworkObjectIdList.Contains(p.NetworkObjectId)).Include("NetworkObject")
                                    .Select(site => new SiteModel
                                    {
                                        IrisId = site.NetworkObject.IrisId,
                                        SiteId = site.SiteId,
                                        Address1 = site.Address1 ?? string.Empty,
                                        Address2 = site.Address2 ?? string.Empty,
                                        StoreNumber = site.StoreNumber,
                                        City = site.City ?? string.Empty,
                                        State = site.State ?? string.Empty,
                                        Zip = site.Zip ?? string.Empty,
                                        Phone = site.Phone,
                                        Latitude = site.Latitude,
                                        Longitude = site.Longitude,
                                        NetworkObjectId = site.NetworkObjectId,
                                        NetworkObject = new NetworkObjectModel { Name = site.NetworkObject.Name, ParentNetworkObjectId = site.NetworkObject.ParentId, IrisId = site.NetworkObject.IrisId }
                                    })
                                .Where(filtering);

                    resultCount = indeterminateSites.Count();

                    sitelist = indeterminateSites.OrderBy(sorting).Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return sitelist;
        }

        /// <summary>
        /// Updates site details for  a selected site
        /// </summary>
        /// <param name="model">site details</param>   
        /// <param name="hasActionFailed"></param>  
        public SiteModel UpdateSite(SiteModel model, out bool hasActionFailed)
        {
            hasActionFailed = false;
            var siteInfo = _repository.GetQuery<SiteInfo>(s => s.NetworkObject.IrisId == model.IrisId)
                                .Include("NetworkObject")
                                .FirstOrDefault();
            try
            {
                if (siteInfo != null)
                {
                    //siteInfo.StoreNumber = model.StoreNumber;
                    //siteInfo.Address1 = model.Address1;
                    //siteInfo.Address2 = model.Address2;
                    //siteInfo.City = model.City;
                    //siteInfo.State = model.State;
                    //siteInfo.Zip = model.Zip;
                    siteInfo.Phone = string.IsNullOrEmpty(model.Phone) == false ? model.Phone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(" ", "") : string.Empty;
                    siteInfo.Latitude = model.Latitude;
                    siteInfo.Longitude = model.Longitude;
                    siteInfo.TimeZoneId = model.SiteTimeZone.Id;

                    model.CuisinesAdded = string.IsNullOrEmpty(model.CuisinesAdded) ? model.CuisinesAdded : model.CuisinesAdded.Remove(0, 1);
                    model.CuisinesRemoved = string.IsNullOrEmpty(model.CuisinesRemoved) ? model.CuisinesRemoved : model.CuisinesRemoved.Remove(0, 1);
                    if (!string.IsNullOrEmpty(model.CuisinesAdded) || !string.IsNullOrEmpty(model.CuisinesRemoved))
                    {
                        //Delete Network-Cuisine Links
                        if (!string.IsNullOrEmpty(model.CuisinesRemoved))
                        {
                            var cuisineIdsToBeRemoved = model.CuisinesRemoved.Split(',');
                            var cuisineIdsToBeRemovedCnt = cuisineIdsToBeRemoved.Count();
                            if (cuisineIdsToBeRemovedCnt > 0)
                            {
                                var intCuisineIds = cuisineIdsToBeRemoved.Select(int.Parse);
                                _repository.Delete<CuisineNetworkObjectLink>(w => intCuisineIds.Contains(w.CuisineId) && w.NetworkObjectId == siteInfo.NetworkObjectId);
                            }
                        }

                        //Add Tag-Entity Links
                        if (!string.IsNullOrEmpty(model.CuisinesAdded))
                        {
                            var cuisineIdsToBeAdded = model.CuisinesAdded.Split(',');
                            if (cuisineIdsToBeAdded.Any())
                            {
                                foreach (var cuisineId in cuisineIdsToBeAdded)
                                {
                                    _repository.Add(new CuisineNetworkObjectLink
                                    {
                                        NetworkObjectId = siteInfo.NetworkObjectId,
                                        CuisineId = Convert.ToInt32(cuisineId)
                                    });
                                }

                            }
                        }
                    }

                    foreach (var serviceUpdated in model.ServicesOffered)
                    {
                        var existingService = siteInfo.NetworkObject.SerivceNetworkObjectLinks.FirstOrDefault(x => x.SerivceNetworkObjectLinkId != 0 && x.ServiceTypeId == serviceUpdated.ServiceTypeId);

                        if (existingService != null && serviceUpdated.ToDelete == false)
                        {//update
                            existingService.Fee = serviceUpdated.Fee;
                            existingService.MinOrder = serviceUpdated.MinOrder;
                            existingService.EstimatedTime = serviceUpdated.EstimatedTime;
                            existingService.AreaCovered = serviceUpdated.AreaCovered;
                            existingService.TaxTypeId = serviceUpdated.TaxTypeId;

                        }
                        else if (existingService != null && serviceUpdated.ToDelete)
                        {//delete
                            _repository.Delete(existingService);
                        }
                        else if (serviceUpdated.ToDelete == false)
                        {//add
                            siteInfo.NetworkObject.SerivceNetworkObjectLinks.Add(new SerivceNetworkObjectLink
                            {
                                ServiceTypeId = serviceUpdated.ServiceTypeId,
                                Fee = serviceUpdated.Fee,
                                MinOrder = serviceUpdated.MinOrder,
                                EstimatedTime = serviceUpdated.EstimatedTime,
                                AreaCovered = serviceUpdated.AreaCovered,
                                TaxTypeId = serviceUpdated.TaxTypeId
                            });
                        }
                    }

                    siteInfo.LastUpdatedDate = DateTime.Now;
                    //Not allowing to update name and parent
                    ////Bug 126: Determine if we need to update the Network Name with the updated SiteInfo.Name
                    //if (siteInfo.NetworkObject != null)
                    //{
                    //    if (siteInfo.NetworkObject.Name != model.NetworkObject.Name)
                    //        siteInfo.NetworkObject.Name = model.NetworkObject.Name;

                    //    //Move Site
                    //    siteInfo.NetworkObject.ParentId = model.NetworkObject.ParentNetworkObjectId;
                    //}
                    _context.SaveChanges();
                    model = GetSiteDetails(siteInfo.NetworkObject.IrisId);
                    _lastActionResult = string.Format(Constants.AuditMessage.SiteUpdatedT, model.NetworkObject.Name);
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.Site, entityId: new[] { siteInfo.NetworkObjectId });
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrSiteUpdateT, model.NetworkObject.Name);
                }
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSiteUpdateT, model.NetworkObject.Name);
                Logger.WriteError(ex);

            }
            return model;
        }

        /// <summary>
        /// Adds new site to market.
        /// </summary>
        /// <param name="model">site detail data</param>
        public bool AddSite(SiteModel model)
        {
            var hasActionFailed = false;
            try
            {
                var networkObject = new NetworkObject
                {
                    Name = model.NetworkObject.Name,
                    ParentId = model.NetworkObject.ParentNetworkObjectId,
                    NetworkObjectTypeId = NetworkObjectTypes.Site,
                    IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName)
                };
                _repository.Add(networkObject);

                var siteInfo = new SiteInfo
                {
                    SiteId = Guid.NewGuid(),
                    StoreNumber = model.StoreNumber,
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    City = model.City,
                    State = model.State,
                    Zip = model.Zip,
                    NetworkObjectId = networkObject.NetworkObjectId,
                    LastUpdatedDate = DateTime.Now,
                };
                _repository.Add(siteInfo);
                _context.SaveChanges();

                if (model.NetworkObjectId == 0)
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.SiteCreateT, model.NetworkObject.Name);
                    _auditLogger.Write(OperationPerformed.Created, EntityType.Site, entityNameList: model.NetworkObject.Name);
                }
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSiteCreateT, model.NetworkObject.Name);
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        public bool DeleteSite(String siteId, bool supressMessageLoggingNSaving = false)
        {
            var hasActionFailed = false;
            var siteName = string.Empty;
            try
            {
                Guid result;
                var isSiteId = Guid.TryParse(siteId, out result);
                SiteInfo site;
                int networkObjectId;
                var isNetworkObjectId = int.TryParse(siteId, out networkObjectId);
                if (isSiteId)
                {
                    site = _repository.GetQuery<SiteInfo>(n => n.SiteId == result)
                        .Include("NetworkObject")
                        .FirstOrDefault();
                }
                else
                {
                    var id = networkObjectId;
                    site = _repository.GetQuery<SiteInfo>(n => n.NetworkObjectId == id)
                        .Include("NetworkObject")
                        .FirstOrDefault();
                }

                if ((isSiteId || isNetworkObjectId) && site != null)
                {
                    siteName = site.NetworkObject.Name;
                    networkObjectId = site.NetworkObject.NetworkObjectId;

                    // delete NetworkObject
                    _repository.Delete(site.NetworkObject);
                    //delete siteinfo
                    _repository.Delete(site);

                    if (!supressMessageLoggingNSaving)
                    {
                        deleteNetworkLinkedData(networkObjectId);

                        _context.SaveChanges();

                        _lastActionResult = string.Format(Constants.AuditMessage.SiteDeletedT, siteName);

                        _auditLogger.Write(OperationPerformed.Deleted, EntityType.Site, entityNameList: siteName);
                    }
                }
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSiteDeleteT, siteName);
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public GroupModel GetGroupDetails(int networkObjectId)
        {
            var model = new GroupModel();
            var networkObject = _repository.Find<NetworkObject>(n => n.NetworkObjectId == networkObjectId).FirstOrDefault();
            if (networkObject != null)
            {
                model.NetowrkObjectId = networkObject.NetworkObjectId;
                model.GroupName = networkObject.Name;
                model.GroupTypeId = ((int)networkObject.NetworkObjectTypeId).ToString();
                model.GroupType = networkObject.NetworkObjectTypeId.ToString();
                model.ParentName = networkObject.NetworkObject2.Name;
                model.ParentNetworkObjectId = networkObject.NetworkObject2.NetworkObjectId;
            }
            else
            {
                model = null;
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public bool UpdateGroup(GroupModel model)
        {
            var hasActionFailed = false;
            var networkObject = _repository.Find<NetworkObject>(n => n.NetworkObjectId == model.NetowrkObjectId).FirstOrDefault();
            try
            {
                if (networkObject != null && networkObject.Name != model.GroupName)
                {
                    networkObject.Name = model.GroupName;
                }
                _context.SaveChanges();

                _lastActionResult = string.Format(Constants.AuditMessage.SiteUpdatedT, model.GroupName);
                _auditLogger.Write(OperationPerformed.Updated, EntityType.Group, entityNameList: model.GroupName);
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrGroupUpdateT, model.GroupName);
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public bool AddGroup(GroupModel model)
        {
            var hasActionFailed = false;
            try
            {
                NetworkObjectTypes networkObjectType;
                if (Enum.TryParse(model.GroupType, false, out networkObjectType))
                {

                }

                NetworkObject networkObject = new NetworkObject
                {
                    Name = model.GroupName,
                    ParentId = model.ParentNetworkObjectId,
                    NetworkObjectTypeId = networkObjectType,
                    IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName)
                };
                _repository.Add(networkObject);

                _context.SaveChanges();
                if (model.NetowrkObjectId == 0)
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.SiteCreateT, model.GroupName);
                    _auditLogger.Write(OperationPerformed.Created, EntityType.Group, entityNameList: model.GroupName);
                }
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrGroupCreateT, model.GroupName);
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        public bool DeleteGroup(int groupId, string groupName)
        {
            bool hasActionFailed = false;
            try
            {
                // delete related UserPermission        
                _repository.Delete<UserPermission>(p => p.NetworkObjectId == groupId);

                // delete Group
                deleteGroup(groupId);

                _context.SaveChanges();
                _lastActionResult = string.Format(Constants.AuditMessage.GroupDeletedT, groupName);
                _auditLogger.Write(OperationPerformed.Deleted, EntityType.Group, entityNameList: groupName);
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSiteDeleteT, groupName);
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        private void deleteNetworkLinkedData(int networkObjectId)
        {
            // delete MenuNetworkObjectLinks\POSDatas
            var mnuNetworkLinks = _repository.GetQuery<MenuNetworkObjectLink>(m => m.NetworkObjectId == networkObjectId);
            foreach (var pd in mnuNetworkLinks)
            {
                _repository.Delete<POSData>(p => p.NetworkObjectId == pd.NetworkObjectId);
                _repository.Delete<ItemPOSDataLink>(p => p.NetworkObjectId == pd.NetworkObjectId);
                _repository.Delete<PrependItemLink>(i => i.MenuNetworkObjectLinkId == pd.MenuNetworkObjectLinkId);
                _repository.Delete<SpecialNoticeMenuLink>(i => i.MenuNetworkObjectLinkId == pd.MenuNetworkObjectLinkId);
            }

            // delete related MenuNetworkObjectLinks
            _repository.Delete<MenuNetworkObjectLink>(m => m.NetworkObjectId == networkObjectId);

            //Delete corresponding overrides created by this NetworkObject
            _repository.Delete<ItemCollectionLink>(i => i.NetworkObjectId == networkObjectId);
            _repository.Delete<ItemCollectionObject>(i => i.NetworkObjectId == networkObjectId);
            _repository.Delete<CategoryObject>(i => i.NetworkObjectId == networkObjectId);
            _repository.Delete<CategoryMenuLink>(i => i.NetworkObjectId == networkObjectId);
            _repository.Delete<SubCategoryLink>(i => i.NetworkObjectId == networkObjectId);

            _repository.Delete<MenuSyncTarget>(i => i.NetworkObjectId == networkObjectId);
            _repository.Delete<ModifierFlag>(i => i.NetworkObjectId == networkObjectId);

            var menuIds = _repository.GetQuery<Menu>(m => m.NetworkObjectId == networkObjectId).Select(x => x.MenuId).ToList();

            var colIds = _repository.GetQuery<ItemCollection>(m => menuIds.Contains(m.MenuId)).Select(x => x.CollectionId).ToList();
            _repository.Delete<ItemCollectionLink>(i => colIds.Contains(i.CollectionId));
            _repository.Delete<ItemCollectionObject>(i => colIds.Contains(i.CollectionId));
            _repository.Delete<ItemCollection>(i => i.NetworkObjectId == networkObjectId || colIds.Contains(i.CollectionId));

            //delete item
            var itemIds = _repository.GetQuery<Item>(m => m.NetworkObjectId == networkObjectId).Select(x => x.ItemId).ToList();
            _repository.Delete<AssetItemLink>(i => i.ItemId.HasValue && itemIds.Contains(i.ItemId.Value));
            _repository.Delete<ItemCollectionObject>(i => itemIds.Contains(i.ItemId));
            _repository.Delete<CategoryObject>(i => itemIds.Contains(i.ItemId));
            _repository.Delete<ItemDescription>(i => itemIds.Contains(i.ItemId));
            _repository.Delete<TempSchedule>(i => itemIds.Contains(i.ItemId));
            _repository.Delete<MenuItemCycleInSchedule>(i => itemIds.Contains(i.MenuItemScheduleLink.ItemId));
            _repository.Delete<MenuItemScheduleLink>(i => itemIds.Contains(i.ItemId));
            _repository.Delete<Item>(i => i.NetworkObjectId == networkObjectId);

            // delete category
            var catIds = _repository.GetQuery<Category>(m => m.NetworkObjectId == networkObjectId).Select(x => x.CategoryId).ToList();
            catIds.AddRange(_repository.GetQuery<Category>(m => menuIds.Contains(m.MenuId)).Select(x => x.CategoryId).ToList());
            _repository.Delete<AssetCategoryLink>(i => i.CategoryId.HasValue && catIds.Contains(i.CategoryId.Value));
            _repository.Delete<CategoryMenuLink>(i => i.ParentCategoryId.HasValue && catIds.Contains(i.ParentCategoryId.Value));
            _repository.Delete<CategoryMenuLink>(i => catIds.Contains(i.CategoryId));
            _repository.Delete<SubCategoryLink>(i => i.OverrideParentSubCategoryId.HasValue && catIds.Contains(i.OverrideParentSubCategoryId.Value));
            _repository.Delete<SubCategoryLink>(i => catIds.Contains(i.SubCategoryId));
            _repository.Delete<MenuCategoryCycleInSchedule>(i => catIds.Contains(i.MenuCategoryScheduleLink.CategoryId));
            _repository.Delete<MenuCategoryScheduleLink>(i => catIds.Contains(i.CategoryId));
            _repository.Delete<Category>(i => i.OverrideCategoryId.HasValue && catIds.Contains(i.OverrideCategoryId.Value));
            _repository.Delete<Category>(i => i.NetworkObjectId == networkObjectId || catIds.Contains(i.CategoryId));

            // delete menu
            _repository.Delete<SpecialNoticeMenuLink>(m => m.MenuNetworkObjectLink.MenuId.HasValue && menuIds.Contains(m.MenuNetworkObjectLink.MenuId.Value));
            _repository.Delete<MenuNetworkObjectLink>(m => m.MenuId.HasValue && menuIds.Contains(m.MenuId.Value));
            _repository.Delete<Menu>(i => i.NetworkObjectId == networkObjectId);


            //Schedule Details.. Delete Orphan Schedule Manually
            _repository.Delete<SchNetworkObjectLink>(i => i.NetworkObjectId == networkObjectId);

            //delete tag
            var tagIds = _repository.GetQuery<Tag>(m => m.NetworkObjectId == networkObjectId).Select(x => x.Id).ToList();
            _repository.Delete<TagAssetLink>(i => tagIds.Contains(i.TagId));
            _repository.Delete<Tag>(i => i.NetworkObjectId == networkObjectId);

            // delete asset
            var assetIds = _repository.GetQuery<Asset>(m => m.NetworkObjectId == networkObjectId).Select(x => x.AssetId).ToList();
            _repository.Delete<AssetItemLink>(i => assetIds.Contains(i.AssetId));
            _repository.Delete<AssetCategoryLink>(i => assetIds.Contains(i.AssetId));
            _repository.Delete<TagAssetLink>(i => assetIds.Contains(i.AssetId));
            _repository.Delete<Asset>(i => i.NetworkObjectId == networkObjectId);

            _repository.Delete<SpecialNotice>(i => i.NetworkObjectId == networkObjectId);
            // usr permissions
            _repository.Delete<UserPermission>(i => i.NetworkObjectId == networkObjectId);
        }

        private void deleteGroup(int groupId)
        {
            var subNetworkObjects = _repository.GetQuery<NetworkObject>(p => p.ParentId == groupId).ToList();
            if (subNetworkObjects.Any())
            {
                if (subNetworkObjects.ElementAt(0).NetworkObjectTypeId != NetworkObjectTypes.Site)
                {
                    foreach (var subNetworkObject in subNetworkObjects)
                    {
                        deleteGroup(subNetworkObject.NetworkObjectId);
                    }
                }
                else
                {
                    var siteIdList = subNetworkObjects.Select(p => p.NetworkObjectId);
                    foreach (var siteId in siteIdList)
                    {
                        DeleteSite(siteId.ToString(), true);
                    }
                }
            }
            //delete all the Linked data of a Network
            deleteNetworkLinkedData(groupId);
            _repository.Delete<NetworkObject>(p => p.NetworkObjectId == groupId);
        }


        private List<CuisineModel> mapCuisines(ICollection<CuisineNetworkObjectLink> cuisinesOffered)
        {
            return cuisinesOffered.Select(cuisine => new CuisineModel
            {
                CuisineId = cuisine.CuisineId, CuisineName = cuisine.Cuisine.CuisineName
            }).ToList();
        }

        private List<SiteServiceModel> mapServices(ICollection<SerivceNetworkObjectLink> servicesOffered)
        {
            return servicesOffered.Select(serivceOffered => new SiteServiceModel
            {
                ServiceTypeId = serivceOffered.ServiceTypeId, 
                ServiceTypeName = serivceOffered.ServiceType.ServiceName, 
                Fee = serivceOffered.Fee, 
                MinOrder = serivceOffered.MinOrder, 
                EstimatedTime = serivceOffered.EstimatedTime, 
                AreaCovered = serivceOffered.AreaCovered, 
                TaxTypeId = serivceOffered.TaxTypeId,
            }).ToList();
        }

        ///// <summary>
        ///// Walk the selected Nodes Children for groups
        ///// </summary>
        ///// <param name="networkObjects"></param>
        ///// <param name="selectedNetworkObject"></param>
        ///// <param name="retList"></param>
        //public void WalkGroups(IEnumerable<NetworkObject> networkObjects, NetworkObject selectedNetworkObject, List<GroupModel> retList)
        //{
        //    if (selectedNetworkObject.NetworkObjectTypeId == NetworkObjectTypes.Site) return;
        //    var selectedgroup = from no in networkObjects.Where(n => n.ParentId == selectedNetworkObject.NetworkObjectId && n.NetworkObjectTypeId != NetworkObjectTypes.Site && n.NetworkObjectTypeId == selectedNetworkObject.NetworkObjectTypeId + 1)
        //                        join up in Permissions on new { no.IrisId, no.NetworkObjectTypeId } equals new { IrisId = up.Id, NetworkObjectTypeId = (NetworkObjectTypes)NetworkItemHelper.GetNetworkObjectType(up.NetworkType) }
        //                        where up.HasAccess == true
        //                        select no;
        //    if (selectedgroup == null)
        //        return;

        //    foreach (var group in selectedgroup)
        //    {
        //        retList.Add(new GroupModel
        //        {
        //            GroupName = group.Name,
        //            GroupTypeId = ((int)group.NetworkObjectTypeId).ToString(),
        //            GroupType = group.NetworkObjectTypeId.ToString(),
        //            NetowrkObjectId = group.NetworkObjectId,
        //            ParentName = group.ParentId.HasValue ? group.NetworkObject2.Name : string.Empty,
        //            ParentNetworkObjectId = group.ParentId.HasValue ? group.NetworkObject2.NetworkObjectId : 0
        //        });

        //        //Walk through the children
        //        var innergroup = networkObjects.Where(n => n.ParentId == group.NetworkObjectId);
        //        WalkGroups(networkObjects, group, retList);
        //    }
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public List<GroupModel> GetGroups(int? networkObjectId)
        {
            var retList = new List<GroupModel>();

            try
            {
                List<NetworkObject> allgroups;
                if (networkObjectId.HasValue)
                {
                    var childNetworkIds = _ruleService.GetNetworkChilds(networkObjectId.Value);
                    //Include selected site/network in the list too
                    childNetworkIds.Add(networkObjectId.Value);
                    var networks = _repository.GetQuery<NetworkObject>(x => childNetworkIds.Contains((x.NetworkObjectId)) && x.NetworkObjectTypeId != NetworkObjectTypes.Site).Include("NetworkObject2").ToList();
                    allgroups = (from no in networks
                                                 join up in Permissions on new { no.IrisId, no.NetworkObjectTypeId } equals new { IrisId = up.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(up.NetworkType) }
                                                 where up.HasAccess
                                                 select no).ToList();

                }
                else
                {
                    //var selectedNetworkObject = networkObjects.Where(n => n.NetworkObjectId == 1).FirstOrDefault(); // root
                    var networkObjects = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == NetworkObjectTypes.Brand || n.NetworkObjectTypeId == NetworkObjectTypes.Franchise).ToList();
                    allgroups = (from no in networkObjects
                                     join up in Permissions on new { no.IrisId, no.NetworkObjectTypeId } equals new { IrisId = up.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(up.NetworkType) }
                                     where up.HasAccess
                                     select no).ToList();
                }

                foreach (var networkObject in allgroups)
                {
                    retList.Add(new GroupModel
                    {
                        GroupName = networkObject.Name,
                        GroupTypeId = ((int)networkObject.NetworkObjectTypeId).ToString(),
                        GroupType = networkObject.NetworkObjectTypeId.ToString(),
                        NetowrkObjectId = networkObject.NetworkObjectId,
                        ParentName = networkObject.ParentId.HasValue ? networkObject.NetworkObject2.Name : string.Empty,
                        ParentNetworkObjectId = networkObject.ParentId.HasValue ? networkObject.NetworkObject2.NetworkObjectId : 0
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retList;

        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="networkItemId"></param>
        ///// <param name="allnetworkObjects"></param>
        ///// <returns></returns>
        //public List<SiteInfo> GetChildSites(int networkItemId, IQueryable<NetworkObject> allnetworkObjects)
        //{
        //    //allnetworkObjects =  _repository.GetQuery<NetworkObject>(n => n.NetworkObjectId > 0).Include("SiteInfoes");
        //    var retList = new List<SiteInfo>();
        //    try
        //    {
        //        var selectedNetworkObject = allnetworkObjects.Where(n => n.NetworkObjectId == networkItemId).FirstOrDefault();
        //        if (selectedNetworkObject != null)
        //        {
        //            if (selectedNetworkObject.NetworkObjectTypeId == NetworkObjectTypes.Site)
        //            {
        //                var siteInfo = selectedNetworkObject.SiteInfoes.First();
        //                if (retList.Contains(siteInfo))
        //                {

        //                }
        //                else
        //                {
        //                    retList.Add(siteInfo);
        //                }
        //            }
        //            else
        //            {
        //                var childNetworkObjects = allnetworkObjects.Where(n => n.ParentId == networkItemId);
        //                if (childNetworkObjects != null)
        //                {
        //                    foreach (var child in childNetworkObjects)
        //                    {
        //                        retList.AddRange(GetChildSites(child.NetworkObjectId, allnetworkObjects));
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // write an error.
        //        Logger.WriteError(ex);
        //    }

        //    return retList;

        //}

        private string GetImagePath(NetworkObjectTypes networkObjectType)
        {
            var retVal = string.Empty;
            switch (networkObjectType)
            {
                case NetworkObjectTypes.Brand:
                    retVal = Constants.NetworkObjectImages.Brand;
                    break;

                case NetworkObjectTypes.Franchise:
                    retVal = Constants.NetworkObjectImages.Franchise;
                    break;

                case NetworkObjectTypes.Market:
                    retVal = Constants.NetworkObjectImages.Market;
                    break;

                case NetworkObjectTypes.Site:
                    retVal = Constants.NetworkObjectImages.Site;
                    break;
            }
            return retVal;
        }


        private void WalkNetwork(List<NetworkObject> networkObjects, TreeNetworkModel selectedNetworkObject, List<TreeNetworkModel> retList, bool includeaccess)
        {
            var selectedgroup = (from no in networkObjects.Where(n => n.ParentId == selectedNetworkObject.Id)
                                 join up in Permissions on new { no.IrisId, no.NetworkObjectTypeId } equals new { IrisId = up.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(up.NetworkType) }
                                 where up.HasAccess
                                 select new TreeNetworkModel
                                 {
                                     items = new List<TreeNetworkModel>(),
                                     Id = no.NetworkObjectId,
                                     HasChildren = no.NetworkObjectTypeId != NetworkObjectTypes.Site,
                                     ItemType = no.NetworkObjectTypeId,
                                     Name = no.Name,
                                     Image = GetImagePath(no.NetworkObjectTypeId),
                                     HasAccess = includeaccess && up.HasAccess
                                 }).ToList();

            foreach (var neto in selectedgroup)
            {
                retList.Add(neto);

                //Walk through the children
                WalkNetwork(networkObjects, neto, neto.items, includeaccess);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="includeaccess">Inlcude only accessiable networks</param>
        /// <param name="expandUntilNWType">Exapand the tree until provided NetworkType</param>
        /// <param name="includeUntilNWType">Include networks until this network type</param>
        /// <param name="includefeatures">Add Network Features to the return list</param>
        /// <returns></returns>
        public List<TreeNetworkModel> GetNetworkObjectData(int? id, bool includeaccess, NetworkObjectTypes expandUntilNWType = 0, NetworkObjectTypes includeUntilNWType = NetworkObjectTypes.Site, bool includefeatures = false)
        {
            var networksite = new List<TreeNetworkModel>();
            try
            {
                var up = Permissions.AsQueryable();


                List<NetworkObject> allItems = null;
                //bug - 4781 - Expanable icon shouldn't display it is has no children
                List<int> allItemIds;
                NetworkObject brand = null;
                if (id == null)
                {
                    //Coming from User Access screen that need Root level
                    //if (!includeaccess)
                    //allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectType.NetworkObjectType1 == NetworkObjectTypes.Root).ToList();
                    //else  //Normal Site Trees display at Franchise level
                    //    allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectType.NetworkObjectType1 == NetworkObjectTypes.Franchise).ToList();

                    //Id is null i.e. first time into tree. 
                    //So pull highest level NetworkObject the current user has access 
                    var topLevelPermission = up.FirstOrDefault();
                    if (topLevelPermission != null)
                    {
                        var highestType = NetworkItemHelper.GetNetworkObjectType(topLevelPermission.NetworkType);
                        allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType).OrderBy(ni => ni.Name).ToList();
                        if (allItems.Count == 0)
                        {
                            allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType + 1).OrderBy(ni => ni.Name).ToList();
                        }
                    }
                }
                else //All subsequent requests pull based on parent.
                {
                    allItems = _repository.GetQuery<NetworkObject>(n => n.ParentId == id).OrderBy(ni => ni.Name).ToList();
                }
                //bug - 4781 - Expanable icon shouldn't display it is has no children
                if (allItems != null)
                {
                    allItemIds = allItems.Select(x => x.NetworkObjectId).Distinct().ToList();
                    var allchildNetworks = _repository.GetQuery<NetworkObject>(n => n.ParentId.HasValue && allItemIds.Contains(n.ParentId.Value)).ToList();

                    if (allItemIds.Any())
                    {
                        brand = _ruleService.GetBrandNetworkObject(allItemIds.FirstOrDefault());
                    }
                    networksite = (from neto in allItems
                        join q in up on new { neto.IrisId, neto.NetworkObjectTypeId } equals new { IrisId = q.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(q.NetworkType) }
                        select new TreeNetworkModel
                        {
                            Id = neto.NetworkObjectId,
                            //set hasChildren to false when includeUntilNWType is reached
                            HasChildren = neto.NetworkObjectTypeId != (includeUntilNWType == 0 ? NetworkObjectTypes.Site : includeUntilNWType) && allchildNetworks.Any(x => x.ParentId == neto.NetworkObjectId),
                            ItemType = neto.NetworkObjectTypeId,
                            Name = neto.Name,
                            Image = GetImagePath(neto.NetworkObjectTypeId),
                            HasAccess = includeaccess && q.HasAccess,
                            parentId = neto.ParentId,
                            Features = includefeatures ? mapFeatures(brand) : null,
                            //Configure this according to the application's requirement                                   
                            expanded = expandUntilNWType != 0 && NetworkItemHelper.GetNetworkObjectType(q.NetworkType) <= expandUntilNWType
                        }).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return networksite.ToList();
        }

        /// <summary>
        /// Get all featues of a network
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        private List<int> mapFeatures(NetworkObject brand)
        {
            var features = new List<int>();
            if (brand != null)
            {
                features = (from NetworkFeaturesSet feature in Enum.GetValues(typeof(NetworkFeaturesSet))
                            where (brand.FeaturesSet & (int)feature) == (int)feature
                            select (int)feature).ToList();
            }
            return features;
        }

        //public List<TreeNetworkModel> GetNetworkObjectDataAndSelectedUserAccess(int? Id, bool includeaccess, NetworkObjectTypes networkObjectType = 0, int? userIdForAccess = null)
        //{
        //    List<TreeNetworkModel> networksite = new List<TreeNetworkModel>();
        //    try
        //    {
        //        bool IsExpandBasedOnChild = true;
        //        IQueryable<Lima.DAL.uspUserGetUserPermissions_Result> up = Permissions.AsQueryable();
        //        List<NetworkObject> allItems = null;
        //        if (Id == null)
        //        {
        //            _selectedUserAccess = _adminService.GetUserAcccessFromRoot(userIdForAccess, null);
        //            NetworkObjectTypes highestType = NetworkItemHelper.GetNetworkObjectType(up.FirstOrDefault().NetworkItemTypeId);
        //            allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType).OrderBy(ni => ni.Name).ToList();
        //            if (allItems.Count == 0)
        //            {
        //                allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType + 1).OrderBy(ni => ni.Name).ToList();
        //            }
        //            IsExpandBasedOnChild = false;
        //        }
        //        else //All subsequent requests pull based on parent.
        //        {
        //            if (_selectedUserAccess == null)
        //            {
        //                _selectedUserAccess = _adminService.GetUserAcccessFromRoot(userIdForAccess, null);
        //            }
        //            allItems = _repository.GetQuery<NetworkObject>(n => n.ParentId == Id).OrderBy(ni => ni.Name).ToList();
        //            //If the Maximum level of access is lessthan InitialExpansion
        //            if (IsExpandBasedOnChild)
        //            {
        //                //If currentNetworkType is greatthan or equalto IntialExpansion then by default any child is not expanded. Hence set (parent's) isExpanded based on child's Access.
        //                var currentNetTypeId = _repository.GetQuery<NetworkObject>(x => x.NetworkObjectId == Id).FirstOrDefault().NetworkObjectTypeId;
        //                if (currentNetTypeId >= networkObjectType)
        //                {
        //                    IsExpandBasedOnChild = true;
        //                }
        //                else
        //                {
        //                    IsExpandBasedOnChild = false;
        //                }
        //            }
        //        }

        //        networksite = (from neto in allItems
        //                       join q in up on new { neto.IrisId, neto.NetworkObjectTypeId } equals new { IrisId = q.IrisId, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(q.NetworkItemTypeId) }
        //                       join s in _selectedUserAccess on new { neto.NetworkObjectId, neto.NetworkObjectTypeId } equals new { NetworkObjectId = s.NetworkObjectId, NetworkObjectTypeId = (NetworkObjectTypes)s.NetworkObjectTypeId } into sua
        //                       from g in sua.DefaultIfEmpty()
        //                       select new TreeNetworkModel
        //                       {
        //                           Id = neto.NetworkObjectId,
        //                           HasChildren = neto.NetworkObjectTypeId == NetworkObjectTypes.Site ? false : true,
        //                           ItemType = neto.NetworkObjectTypeId,
        //                           Name = neto.Name,
        //                           Image = GetImagePath(neto.NetworkObjectTypeId),
        //                           HasAccess = includeaccess ? (g == null ? false : g.hasAccess) : false,
        //                           parentId = neto.ParentId,

        //                           //Configure this according to the application's requirement                                   
        //                           expanded = networkObjectType == 0 ? false : (IsExpandBasedOnChild ? (g == null ? false : !g.hasAccess) : NetworkItemHelper.GetNetworkObjectType(q.NetworkItemTypeId) <= networkObjectType)
        //                       }).ToList();
        //        //}

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }
        //    return networksite.ToList();
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SelectList GetNetworkTypes()
        {
            var networkObjectType = from NetworkObjectTypes n in Enum.GetValues(typeof(NetworkObjectTypes)) select new { Id = (int)Enum.Parse(typeof(NetworkObjectTypes), n.ToString()), Text = n.ToString() };
            return new SelectList(networkObjectType, "Id", "Text");
        }

        /// <summary>
        /// return tree list
        /// </summary>
        /// <param name="netId">selected NetworkObject</param>
        /// <param name="networkObjectType"></param>
        /// <returns>tree list</returns>
        public List<TreeNetworkModel> BrandLeveTree(int? netId, NetworkObjectTypes networkObjectType = 0)
        {
            var up = Permissions.ToList();
            List<NetworkObject> allItems = null;
            if (netId == null)
            {
                var topLevelNetworkPermission = up.FirstOrDefault();
                if (topLevelNetworkPermission != null)
                {
                    var highestType = NetworkItemHelper.GetNetworkObjectType(topLevelNetworkPermission.NetworkType);
                    allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType).OrderBy(ni => ni.Name).ToList();
                    if (allItems.Count == 0)
                    {
                        allItems = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == highestType + 1).OrderBy(ni => ni.Name).ToList();
                    }
                }
            }
            else //All subsequent requests pull based on parent.
            {
                allItems = _repository.GetQuery<NetworkObject>(n => n.ParentId == netId).OrderBy(ni => ni.Name).ToList();
            }

            var tree = (from neto in allItems
                join q in up on new { neto.IrisId, neto.NetworkObjectTypeId } equals new { IrisId = q.Id, NetworkObjectTypeId = NetworkItemHelper.GetNetworkObjectType(q.NetworkType) }
                select new TreeNetworkModel
                {
                    Id = neto.NetworkObjectId,
                    HasChildren = neto.NetworkObjectTypeId == NetworkObjectTypes.Root,
                    ItemType = neto.NetworkObjectTypeId,
                    Name = neto.Name,
                    Image = GetImagePath(neto.NetworkObjectTypeId),
                    parentId = neto.ParentId,

                    //Configure this according to the application's requirement                                   
                    expanded = networkObjectType != 0 && NetworkItemHelper.GetNetworkObjectType(q.NetworkType) <= networkObjectType
                }).ToList();

            return tree;
        }

        public List<SelectListItem> GetMarketListForSite(int networkObjectId)
        {
            var marketList = new List<SelectListItem>();

            var tmpMarketList = getMarketList(networkObjectId);
            if (tmpMarketList == null || !tmpMarketList.Any()) return marketList;
            marketList.AddRange(tmpMarketList.Select(tmpMarket => new SelectListItem
            {
                Text = tmpMarket.Name, Value = tmpMarket.NetworkObjectId.ToString()
            }));

            return marketList;
        }

        // ReSharper disable once InconsistentNaming
        private List<NetworkObject> getMarketList(int networkObjectId)
        {
            List<NetworkObject> marketList = null;
            IQueryable<int?> marketIdList;
            var networkObjectTreeRecord = _repository.FindOne<vwNetworkObjectTree>(p => (p.Franchise.HasValue && p.Franchise.Value == networkObjectId) || (p.Market.HasValue && p.Market.Value == networkObjectId) || (p.Site.HasValue && p.Site.Value == networkObjectId));
            if (networkObjectTreeRecord != null)
            {
                var tmpFranchiseId = networkObjectTreeRecord.Franchise;
                if (tmpFranchiseId.HasValue)
                {
                    marketIdList = _repository.GetQuery<vwNetworkObjectTree>(p => p.Franchise.HasValue && p.Franchise.Value == tmpFranchiseId).Distinct().Select(p => p.Market);
                    marketList = _repository.GetQuery<NetworkObject>(p => marketIdList.Contains(p.NetworkObjectId)).ToList();
                }
            }
            return marketList;
        }

        internal SelectList GetAllCuisines()
        {
            var cuisines = (from Cuisine i in _repository.GetQuery<Cuisine>().ToList().OrderBy(p => p.CuisineId) select new { Value = i.CuisineId, Text = i.CuisineName }).ToList();

            return new SelectList(cuisines, "Value", "Text");
        }

        internal dynamic AllServices()
        {
            var serviceTypes = (from ServiceType i in _repository.GetQuery<ServiceType>().ToList().OrderBy(p => p.ServiceTypeId) select new { Value = i.ServiceTypeId, Text = i.ServiceName }).ToList();

            return new SelectList(serviceTypes, "Value", "Text");
        }
    }

    public interface ISiteService
    {
        List<SiteModel> GetSitelist(int? netId, out int totalResultSet, KendoGridRequest grdRequest);
        SiteModel GetSiteDetails(long siteId);
        SiteModel UpdateSite(SiteModel model, out bool hasActionfailed);
        bool AddSite(SiteModel model);
        List<GroupModel> GetGroups(int? networkItemId);
        List<TreeNetworkModel> GetNetworkObjectData(int? id, bool includeaccess, NetworkObjectTypes networkObjectType, NetworkObjectTypes includeUntilNWType, bool includefeatures);
        GroupModel GetGroupDetails(int networkObjectId);
        bool UpdateGroup(GroupModel model);
        bool AddGroup(GroupModel model);
        SelectList GetNetworkTypes();
        bool DeleteSite(String siteId, bool supressMessageLoggingNSaving = false);
        bool DeleteGroup(int groupId, string groupName);
        List<TreeNetworkModel> BrandLeveTree(int? netId, NetworkObjectTypes networkObjectType);
        List<SelectListItem> GetMarketListForSite(int networkObjectId);
        //List<TreeNetworkModel> GetNetworkObjectDataAndSelectedUserAccess(int? Id, bool includeaccess, NetworkObjectTypes networkObjectType = 0, int? userIdForAccess = null);
    }
}