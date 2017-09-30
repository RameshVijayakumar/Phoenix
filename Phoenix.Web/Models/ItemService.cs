using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;
using Omu.ValueInjecter;
using System.Web.Script.Serialization;
using Phoenix.RuleEngine;
using System.Threading.Tasks;
using SnowMaker;
using Microsoft.Practices.Unity;

namespace Phoenix.Web.Models
{
    public class ItemService : KendoGrid<Item>, IItemService
    {
        private static string _IMAGES_CONTAINER = "mmsimages";
        private IRepository _repository;
        private DbContext _context;

        public string cDN { get; set; }
        public int Count { get; set; }
        private string _lastActionResult;

        private IAuditLogService _auditLogger;
        private IAssetService _assetService;
        private RuleService _ruleService;
        private CommonService _commonService;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        private UniqueIdGenerator _irisIdGenerator;

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
        public ItemService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _assetService = new AssetService();
            _ruleService = new RuleService(RuleService.CallType.Web);
            _commonService = new CommonService(_repository);
            //Complete the Asset Url
            cDN = string.Format("{0}/{1}/", System.Configuration.ConfigurationManager.AppSettings.Get("CDNEndpoint").TrimEnd('/'), _IMAGES_CONTAINER);

        }

        /// <summary>
        /// return Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns>Item</returns>
        public ItemModel GetItem(int itemId, string[] childern, int networkObjectId = 0)
        {
            cDN = string.Format("{0}/{1}/", System.Configuration.ConfigurationManager.AppSettings.Get("CDNEndpoint").TrimEnd('/'), _IMAGES_CONTAINER);
            var query = _repository.GetQuery<Item>(i => i.ItemId == itemId);
            query = childern.Aggregate(query, (current, child) => current.Include(child));
            var itm = query.FirstOrDefault();
            var itmModel = new ItemModel();
            mapItemtoItemModel(itm, ref itmModel, true, networkObjectId);
            //item.NetworkObjectId is brand Id where the item is created
            itmModel.IsDWFieldsEnabled = _commonService.CheckFeatureEnabled(itm.NetworkObjectId, NetworkFeaturesSet.MasterItemDWColumnsEnabled);
            return itmModel;

        }

        /// <summary>
        /// Check whether DW fields are enabled for the brand
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public bool CheckDWFieldsEnabled(int networkObjectId)
        {
            return _commonService.CheckFeatureEnabled(networkObjectId, NetworkFeaturesSet.MasterItemDWColumnsEnabled);
        }
        /// <summary>
        ///  return SelectList of ItemLookups available
        /// </summary>
        /// <param name="typeid"></param>
        /// <param name="abbr">Abbreviation</param>
        /// <returns></returns>
        public SelectList GetItemLookups(int typeid, string abbr)
        {
            var itmLookups = (from DWItemLookup i in _repository.GetQuery<DWItemLookup>(p => p.TypeId == typeid && p.Abbreviation.CompareTo(abbr) == 0).ToList().OrderBy(p => p.Sequence) select new { Value = i.DWItemLookupKey, Text = i.Sequence.ToString() + " - " + i.Name }).OrderBy(o => o.Text).ToList();

            return new SelectList(itmLookups, "Value", "Text");
        }

        /// <summary>
        /// return SelectList of ItemCookTimes available
        /// </summary>
        /// <returns></returns>
        public SelectList GetItemCookTimes()
        {
            var cookTimes = _repository.GetQuery<DWItemCookTime>(p => !string.IsNullOrEmpty(p.Description)).ToList();

            cookTimes.Add(new DWItemCookTime{ DWItemCookTimeKey = -1, Description ="Select a Cook Time"});

            var itmCookTimes = (from DWItemCookTime i in cookTimes.ToList().OrderBy(p => p.DWItemCookTimeKey) select new { Value = i.DWItemCookTimeKey, Text = i.Description.ToString() }).ToList();
                        
            return new SelectList(itmCookTimes, "Value", "Text");
        }

        /// <summary>
        /// return SelectList of ItemCategorizations available
        /// </summary>
        /// <returns></returns>
        public SelectList GetItemCategorizations()
        {
            var itmCategorizations = (from DWItemCategorization i in _repository.GetQuery<DWItemCategorization>(p => !string.IsNullOrEmpty(p.Name)).ToList().OrderBy(p => p.DWItemCategorizationKey) select new { Value = i.DWItemCategorizationKey, Text = i.Name.ToString() }).OrderBy(o => o.Text).ToList();

            return new SelectList(itmCategorizations, "Value", "Text");
        }

        /// <summary>
        /// return SelectList of ItemSubTypes available
        /// </summary>
        /// <returns></returns>
        public SelectList GetItemSubTypes()
        {
            var itmSubTypes = (from DWItemSubType i in _repository.GetQuery<DWItemSubType>(p => !string.IsNullOrEmpty(p.Name)).ToList().OrderBy(p => p.DWItemSubTypeKey) select new { Value = i.DWItemSubTypeKey, Text = i.Name.ToString() }).OrderBy(o => o.Text).ToList();

            return new SelectList(itmSubTypes, "Value", "Text");
        }

        /// <summary>
        /// return SelectList of Franchise Names available for RequestedBy
        /// </summary>
        /// <returns></returns>
        public SelectList GetItemRequestedBy()
        {
            var franchiseNames = (from NetworkObject i in _repository.GetQuery<NetworkObject>(p => !string.IsNullOrEmpty(p.Name) && p.NetworkObjectTypeId == NetworkObjectTypes.Franchise).ToList().OrderBy(p => p.Name) select new { Value = i.NetworkObjectId, Text = i.Name.ToString() }).ToList();

            return new SelectList(franchiseNames, "Value", "Text");
        }


        /// <summary>
        /// Update the Display name of given Item
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool UpdateItemDisplayName(ItemModel model)
        {
            bool returnVal = false;

            try
            {
                var item = _repository.GetQuery<Item>(x => x.ItemId == model.ItemId).FirstOrDefault();

                if (model != null && item != null)
                {
                    item.DisplayName = model.DisplayName;
                    _repository.Update<Item>(item);
                    _context.SaveChanges();
                }

            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }

            return returnVal;
        }

        #region Not Used
        ///// <summary>
        ///// return List of Descriptions for an Item
        ///// </summary>
        ///// <param name="ItemId"></param>
        ///// <returns>List ItemDescription Model</returns>
        //public List<ItemDescriptionModel> GetItemDescriptions(int ItemId)
        //{
        //    var descs = (from ItemDescription n in _repository.GetQuery<ItemDescription>(p => p.ItemId == ItemId).ToList() select new ItemDescriptionModel { ItemDescriptionId = n.ItemDescriptionId, Description = n.Description.ToString(), IsActive = n.IsActive, ItemId = n.ItemId }).ToList();
        //    var baseDesc = _repository.GetQuery<Item>(i => i.ItemId == ItemId).Select(i => i.DisplayDescription).FirstOrDefault();
        //    if (baseDesc != null)
        //    {
        //        descs.Add(new ItemDescriptionModel { ItemDescriptionId = 0, Description = baseDesc, ItemId = ItemId, IsActive = (descs != null && descs.Where(x => x.IsActive).Any()) ? false : true });
        //    }
        //    return descs.OrderBy(x => x.ItemDescriptionId).ToList();
        //}
        ///// <summary>
        ///// Add new Description for an Item from Model
        ///// </summary>
        ///// <param name="itemDescModel"></param>
        ///// <returns>New Id of the created Item</returns>
        //public int AddItemDescription(ItemDescriptionModel itemDescModel)
        //{
        //    try
        //    {

        //        var item = _repository.GetQuery<Item>(x => x.ItemId == itemDescModel.ItemId).FirstOrDefault();

        //        if (itemDescModel != null && item != null)
        //        {
        //            ItemDescription itemDesc = new ItemDescription
        //            {
        //                ItemId = itemDescModel.ItemId,
        //                Description = itemDescModel.Description,
        //                IsActive = itemDescModel.IsActive,
        //            };
        //            _repository.Add<ItemDescription>(itemDesc);
        //            _context.SaveChanges();

        //            itemDescModel.ItemDescriptionId = itemDesc.ItemDescriptionId;
        //            _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: string.Format(Constants.AuditMessage.MasterItemDescriptionAddT, item.DisplayName));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }
        //    return itemDescModel.ItemDescriptionId;
        //}

        ///// <summary>
        ///// Update a Description from Model
        ///// </summary>
        ///// <param name="itemDescModel"></param>
        ///// <returns>result true/false</returns>
        //public bool UpdateItemDescription(ItemDescriptionModel itemDescModel)
        //{
        //    bool returnVal = false;

        //    try
        //    {
        //        var item = _repository.GetQuery<Item>(x => x.ItemId == itemDescModel.ItemId).FirstOrDefault();

        //        if (itemDescModel != null && item != null)
        //        {
        //            // If Id is zero it updates the Display description of the Item
        //            if (itemDescModel.ItemDescriptionId != 0)
        //            {
        //                var itemDesc = _repository.GetQuery<ItemDescription>(x => x.ItemDescriptionId == itemDescModel.ItemDescriptionId).FirstOrDefault();
        //                if (itemDesc != null)
        //                {
        //                    itemDesc.Description = itemDescModel.Description;
        //                    itemDesc.IsActive = itemDescModel.IsActive;

        //                    _repository.Update<ItemDescription>(itemDesc);
        //                    _context.SaveChanges();

        //                    returnVal = true;
        //                    _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: string.Format(Constants.AuditMessage.MasterItemDescriptionUpdateT, item.DisplayName));
        //                }
        //            }
        //            else
        //            {
        //                item.DisplayDescription = itemDescModel.Description;
        //                _context.SaveChanges();

        //                returnVal = true;
        //                _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: string.Format(Constants.AuditMessage.MasterItemDescriptionUpdateT, item.DisplayName));
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }

        //    return returnVal;
        //}

        ///// <summary>
        ///// Delete a Description from Model
        ///// </summary>
        ///// <param name="itemDescModel"></param>
        ///// <returns>result true/false</returns>
        //public bool DeleteItemDescription(ItemDescriptionModel itemDescModel)
        //{
        //    bool returnVal = false;

        //    try
        //    {
        //        var item = _repository.GetQuery<Item>(x => x.ItemId == itemDescModel.ItemId).FirstOrDefault();

        //        if (itemDescModel != null && item != null)
        //        {
        //            var itemDesc = _repository.GetQuery<ItemDescription>(x => x.ItemDescriptionId == itemDescModel.ItemDescriptionId).FirstOrDefault();

        //            if (itemDesc != null)
        //            {
        //                _repository.Delete<ItemDescription>(itemDesc);
        //                _context.SaveChanges();

        //                returnVal = true;
        //                _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: string.Format(Constants.AuditMessage.MasterItemDescriptionDeleteT, item.DisplayName));
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }

        //    return returnVal;
        //}

        //public bool IsPLUUniqueinBrand(int? PLU, string AltPLU, int itemId, out string errorMsg,out string entityWithError, int? brandId = null)
        //{
        //    bool retVal = true;
        //    errorMsg = string.Empty;
        //    entityWithError = "BasePLU";
        //    if (brandId.HasValue == false)
        //    {
        //        var item = _repository.FindOne<Item>(x => x.ItemId == itemId);
        //        brandId = item == null ? 0 : item.NetworkObjectId;
        //    }

        //    var items = _repository.Find<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId && (x.BasePLU == PLU || string.Compare(x.AlternatePLU, AltPLU, true) == 0));
        //    //For PLUNumber = 0 or empty PLU, check if Item exists in database by same ALT.
        //    if (PLU == 0 || PLU.HasValue == false)
        //    {
        //        var existingItemWithSameALT = items.Where(e => string.Compare(e.AlternatePLU, AltPLU, true) == 0 && !string.IsNullOrEmpty(AltPLU) && (e.BasePLU == 0 || e.BasePLU.HasValue == false)).FirstOrDefault();
        //        if (null != existingItemWithSameALT && (itemId == 0 || existingItemWithSameALT.ItemId != itemId))
        //        {
        //            retVal = false;
        //            entityWithError = "AlternatePLU";
        //            if (PLU == 0)
        //            {
        //                errorMsg = Constants.StatusMessage.ErrMasterItemPLUandALTIdAlreadyExistsT; 
        //            }
        //            else
        //            {
        //                errorMsg = Constants.StatusMessage.ErrMasterItemALTIdAlreadyExistsT; 
        //            }
        //        }
        //    }
        //    else //For NonZero PLUNumbers, check if Item exists in database by same PLUNumber, ALT number.
        //    {
        //        //validate only PLU if alt number is empty. Else validate the combination og at number and plu
        //        if (string.IsNullOrWhiteSpace(AltPLU))
        //        {
        //            //PLU Number
        //            var existingItemPLU = items.Where(e => e.BasePLU == PLU && string.IsNullOrWhiteSpace(e.AlternatePLU)).FirstOrDefault();
        //            if (null != existingItemPLU && (itemId == 0 || existingItemPLU.ItemId != itemId))
        //            {
        //                retVal = false;
        //                errorMsg = Constants.StatusMessage.ErrMasterItemPLUAlreadyExistsT;
        //            }
        //        }
        //        else
        //        {
        //            //ALT number.
        //            var existingItemNameALT = items.Where(e => e.BasePLU == PLU && string.Compare(e.AlternatePLU, AltPLU, true) == 0 && !string.IsNullOrWhiteSpace(e.AlternatePLU)).FirstOrDefault();
        //            if (null != existingItemNameALT && (itemId == 0 || existingItemNameALT.ItemId != itemId) && existingItemNameALT.BasePLU == PLU)
        //            {
        //                retVal = false;
        //                entityWithError = "AlternatePLU";
        //                errorMsg = Constants.StatusMessage.ErrMasterItemPLUandALTIdAlreadyExistsT; 
        //            }
        //        }
        //    }

        //    return retVal;
        //}

        //public bool IsAltPLUUniqueinBrand(string altPLU, int itemId, int? brandId = null)
        //{
        //    bool retVal = true;
        //    try
        //    {
        //        if (brandId.HasValue == false)
        //        {
        //            var item = _repository.FindOne<Item>(x => x.ItemId == itemId);
        //            brandId = item == null ? 0 : item.NetworkObjectId;
        //        }

        //        if (string.IsNullOrWhiteSpace(altPLU) == false)
        //        {
        //            if (itemId == 0)
        //            {
        //                retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId && x.AlternatePLU.ToLower().CompareTo(altPLU.ToLower()) == 0) == null ? true : false;
        //            }
        //            else
        //            {
        //                retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.ItemId != itemId && x.NetworkObjectId == brandId && x.AlternatePLU.ToLower().CompareTo(altPLU.ToLower()) == 0) == null ? true : false;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }
        //    return retVal;
        //}


        #endregion

        /// <summary>
        /// Check if DeepLinkId is Unique in given brand
        /// </summary>
        /// <param name="deepLinkId"></param>
        /// <param name="itemId"></param>
        /// <param name="brandId"></param>
        /// <returns></returns>
        public bool IsDeepLinkIdUniqueinBrand(string deepLinkId, int itemId, int? brandId = null)
        {
            var retVal = true;
            try
            {
                if (brandId.HasValue == false)
                {
                    var item = _repository.FindOne<Item>(x => x.ItemId == itemId);
                    brandId = item == null ? 0 : item.NetworkObjectId;
                }

                if (string.IsNullOrWhiteSpace(deepLinkId) == false)
                {
                    if (itemId == 0)
                    {
                        retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId && x.DeepLinkId.ToLower().CompareTo(deepLinkId.ToLower()) == 0) == null ? true : false;
                    }
                    else
                    {
                        retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId && x.ItemId != itemId && x.DeepLinkId.ToLower().CompareTo(deepLinkId.ToLower()) == 0) == null ? true : false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retVal;
        }
        
        /// <summary>
        /// NOt used - Check if DeepLinkId is Unique in given brand
        /// </summary>
        /// <param name="internalName"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool IsItemInternalNameUnique(string internalName, int itemId)
        {
            var retVal = true;

            if (string.IsNullOrWhiteSpace(internalName))
            {
                retVal = false;
            }
            else
            {
                if (itemId == 0)
                {
                    retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.ItemName.ToLower().CompareTo(internalName.ToLower()) == 0) == null ? true : false;
                }
                else
                {
                    retVal = _repository.FindOne<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.ItemId != itemId && x.ItemName.ToLower().CompareTo(internalName.ToLower()) == 0) == null ? true : false;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Save/Create Master Item
        /// </summary>
        /// <param name="itmModel"></param>
        /// <returns>rebinded Item</returns>
        public ItemModel SaveMasterItem(ItemModel itmModel)
        {
            var itm = new Item();
            try
            {
                var originalItemModel = new ItemModel();
                var overridenItems = new List<Item>();
                ////If Generate PLu is selected then get Next max PLU
                //if (itmModel.IsGeneratePLU)
                //{
                //    var maxBasePLU = 0;
                //    if (_repository.GetQuery<Item>().Where(x => x.BasePLU.HasValue).Any())
                //    {
                //        maxBasePLU = _repository.GetQuery<Item>().Where(x => x.BasePLU.HasValue).Max(x => x.BasePLU).Value;
                //    }

                //    if (maxBasePLU > 0)
                //    {
                //        itmModel.BasePLU = maxBasePLU + 1;
                //    }
                //    else
                //    {
                //        itmModel.BasePLU = 1;
                //    }
                //}

                //If Zero PLU is selected
                if (itmModel.IsZeroPLU)
                {
                    itmModel.BasePLU = 0;
                }
                //Not a new Item
                if (itmModel.ItemId != 0)
                {
                    itm = _repository.GetQuery<Item>(x => x.ItemId == itmModel.ItemId).FirstOrDefault();
                    mapItemtoItemModel(itm, ref originalItemModel, false);
                    overridenItems = _repository.GetQuery<Item>(x => x.ParentItemId == itm.ItemId).ToList();
                    itmModel.IrisId = itm.IrisId;
                    itmModel.CreatedDate = itm.CreatedDate;
                    itm.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    itm.ItemId = 0;
                    itm.IrisId = IrisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName); //Create new Guid for New Item
                    itm.UpdatedDate = itm.CreatedDate = DateTime.UtcNow;
                }

                _lastActionResult = string.Empty;

                //Map the changed properties
                mapItemModeltoMasterItem(itmModel, ref itm);

                if (itmModel.Assets != null)
                {
                    //Loop for and deletions or additions only
                    foreach (var asset in itmModel.Assets.Where(x => x.ToDelete || x.AssetItemLinkId == 0))
                    {
                        //Add asset
                        if (asset.AssetItemLinkId == 0 && !asset.ToDelete)
                        {
                            itm.AssetItemLinks.Add(new AssetItemLink
                            {
                                AssetId = asset.AssetId
                            });
                        }
                        //Remove Asset
                        if (asset.AssetItemLinkId != 0 && asset.ToDelete)
                        {
                            var ailtoDelete = itm.AssetItemLinks.FirstOrDefault(x => x.AssetId == asset.AssetId);
                            if (ailtoDelete != null)
                            {
                                itm.AssetItemLinks.Remove(ailtoDelete);
                                _repository.Delete<AssetItemLink>(x => x.AssetItemLinkId == ailtoDelete.AssetItemLinkId);
                            }
                        }
                    }
                }
                //If new descriptions are added then add to Item
                if (itmModel.ItemDescriptions != null)
                {
                    //Add only which have todelete is false and Description not empty
                    foreach (var itemDesc in itmModel.ItemDescriptions) //.Where(x => x.ItemDescriptionId == 0 && !x.ToDelete && !string.IsNullOrWhiteSpace(x.Description)))
                    {
                        if (!itemDesc.ToDelete && !string.IsNullOrWhiteSpace(itemDesc.Description))
                        {
                            if (itemDesc.ItemDescriptionId == 0)
                            {
                                itm.ItemDescriptions.Add(new ItemDescription
                                {
                                    Description = itemDesc.Description.Trim(),
                                    IsActive = itemDesc.IsActive
                                });
                            }
                            else
                            {
                                var itemDesctoUpdate = itm.ItemDescriptions.FirstOrDefault(x => x.ItemDescriptionId == itemDesc.ItemDescriptionId);
                                if (itemDesctoUpdate != null)
                                {
                                    itemDesctoUpdate.Description = itemDesc.Description.Trim();
                                    _repository.Update<ItemDescription>(itemDesctoUpdate);
                                }
                            }
                        }
                        else
                        {
                            if (itemDesc.ItemDescriptionId != 0)
                            {
                                var itemDesctoDelete = itm.ItemDescriptions.FirstOrDefault(x => x.ItemDescriptionId == itemDesc.ItemDescriptionId);
                                if (itemDesctoDelete != null)
                                {
                                    itm.ItemDescriptions.Remove(itemDesctoDelete);
                                    if (overridenItems.Any())
                                    {//remove reference from all overridden Items
                                        overridenItems.Where(x => x.SelectedDescriptionId.HasValue && x.SelectedDescriptionId == itemDesc.ItemDescriptionId).ToList().ForEach(x => x.SelectedDescriptionId = null);
                                    }
                                    _repository.Delete<ItemDescription>(x => x.ItemDescriptionId == itemDesc.ItemDescriptionId);
                                }
                            }
                        }
                    }
                }

                if (itmModel.POSDatas != null)
                {
                    //If nothing is is set as default, then manual set first POS as default
                    if (itmModel.POSDatas.Any(x => x.ToRemove == false) == true && itmModel.POSDatas.Any(x => x.IsDefault && x.ToRemove == false) == false)
                    {
                        itmModel.POSDatas.FirstOrDefault(x => x.ToRemove == false).IsDefault = true;
                    }

                    foreach (var posdata in itmModel.POSDatas)
                    {
                        var defaultPOS = itmModel.POSDatas.FirstOrDefault(x => x.IsDefault && x.ToRemove == false);

                        //Add POS
                        if (posdata.ItemPOSDataLinkId == 0 && !posdata.ToRemove)
                        {
                            itm.ItemPOSDataLinks.Add(new ItemPOSDataLink
                            {
                                POSDataId = posdata.POSDataId,
                                NetworkObjectId = itmModel.NetworkObjectId,
                                UpdatedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                IsDefault = posdata.IsDefault
                            });
                        }
                        //Remove POS
                        if (posdata.ItemPOSDataLinkId != 0 && posdata.ToRemove)
                        {
                            var ipltoDelete = itm.ItemPOSDataLinks.FirstOrDefault(x => x.POSDataId == posdata.POSDataId);
                            if (ipltoDelete != null)
                            {
                                if (defaultPOS != null)
                                {
                                    //If this POS is set default for Menu Items, then change reference to new default value
                                    var ovrItemsPOSData = _repository.GetQuery<ItemPOSDataLink>(x => x.POSDataId == ipltoDelete.POSDataId && x.Item.ParentItemId == itm.ItemId).ToList();
                                    ovrItemsPOSData.ForEach(x => x.POSDataId = defaultPOS.POSDataId);
                                }
                                else
                                {
                                    _repository.Delete<ItemPOSDataLink>(x => x.POSDataId == ipltoDelete.POSDataId && x.Item.ParentItemId == itm.ItemId);
                                }
                                itm.ItemPOSDataLinks.Remove(ipltoDelete);
                                _repository.Delete<ItemPOSDataLink>(x => x.ItemPOSDataLinkId == ipltoDelete.ItemPOSDataLinkId);
                                if (posdata.ToDelete)
                                {
                                    _repository.Delete<POSData>(x => x.POSDataId == ipltoDelete.POSDataId);
                                }
                            }
                        }

                        //Delete POS
                        if (posdata.ItemPOSDataLinkId == 0 && posdata.ToDelete)
                        {
                            var posToDelete = _repository.GetQuery<POSData>(x => x.POSDataId == posdata.POSDataId).FirstOrDefault();
                            if (posToDelete != null)
                            {
                                if (posToDelete.ItemPOSDataLinks.Any() == false)
                                {
                                    _repository.Delete<POSData>(x => x.POSDataId == posToDelete.POSDataId);
                                }
                            }
                        }

                        //update POS
                        if (posdata.ItemPOSDataLinkId != 0 && !posdata.ToRemove)
                        {
                            var ipltoUpdate = itm.ItemPOSDataLinks.FirstOrDefault(x => x.POSDataId == posdata.POSDataId);
                            if (ipltoUpdate != null)
                            {
                                ipltoUpdate.IsDefault = posdata.IsDefault;
                                _repository.Update<ItemPOSDataLink>(ipltoUpdate);
                            }
                        }
                    }
                }
                if (itmModel.ItemId != 0)
                {
                    _repository.Update<Item>(itm);
                    if (overridenItems.Any())
                    {
                        overridenItems.Where(x => x.DeepLinkId == originalItemModel.DeepLinkId).ToList().ForEach(x => x.DeepLinkId = itm.DeepLinkId);
                        overridenItems.Where(x => x.ItemName == originalItemModel.ItemName).ToList().ForEach(x => x.ItemName = itm.ItemName);
                        overridenItems.Where(x => x.DisplayDescription == originalItemModel.DisplayDescription).ToList().ForEach(x => x.DisplayDescription = itm.DisplayDescription);
                        overridenItems.Where(x => x.DisplayName == originalItemModel.DisplayName).ToList().ForEach(x => x.DisplayName = itm.DisplayName);
                        overridenItems.Where(x => x.IsModifier == originalItemModel.IsModifier).ToList().ForEach(x => x.IsModifier = itm.IsModifier);
                        overridenItems.Where(x => x.IsAlcohol == originalItemModel.IsAlcohol).ToList().ForEach(x => x.IsAlcohol = itm.IsAlcohol);
                        overridenItems.Where(x => x.Feeds == originalItemModel.Feeds).ToList().ForEach(x => x.Feeds = itm.Feeds);
                    }
                    _lastActionResult = string.Format(Constants.AuditMessage.ItemUpdateT, itmModel.ItemName);
                }
                else
                {
                    _repository.Add<Item>(itm);
                    _lastActionResult = string.Format(Constants.AuditMessage.ItemCreateT, itmModel.ItemName);
                }

                var isMenusDateUpdated = _commonService.SetLastUpdatedDateofMenusUsingItems(new List<int> { itm.ItemId });
                _context.SaveChanges();

                if (itmModel.ItemId == 0)
                {
                    _auditLogger.Write(OperationPerformed.Created, EntityType.Item, entityNameList: itmModel.ItemName);
                }
                else
                {
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.Item, entityNameList: itmModel.ItemName);
                }

                //Populated the Model back
                List<string> children = new List<string>();
                children.Add("AssetItemLinks");
                children.Add("AssetItemLinks.Asset");
                children.Add("ItemDescriptions");
                children.Add("ItemPOSDataLinks");
                children.Add("ItemPOSDataLinks.POSData");
                itmModel = GetItem(itm.ItemId, children.ToArray());
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemAddT, itmModel.ItemName);
                Logger.WriteError(ex);
            }

            return itmModel;
        }

        /// <summary>
        /// Delete a Master Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns>id</returns>
        public int DeleteMasterItem(int itemId)
        {
            var itemname = string.Empty;
            CleanUpDataModel orphanRecords = new CleanUpDataModel();
            try
            {
                var item = _repository.GetQuery<Item>(x => x.ItemId == itemId).FirstOrDefault();
                if (item != null)
                {
                    itemname = item.ItemName;

                    //Make overriden Items independent of parent and Delete 
                    var childItems = _repository.GetQuery<Item>(x => x.ParentItemId == itemId).ToList();
                    childItems.ForEach(x => x.ParentItemId = item.ParentItemId);
                    orphanRecords.ItemIds.AddRange(childItems.Select(x => x.ItemId));

                    //Delete - As per new BR rule - Master items will not have direct overrides
                    ////Make overriden items independent of parent and Delete 
                    //var ovrItems = _repository.GetQuery<Item>(x => x.OverrideItemId == itemId).ToList();
                    //ovrItems.ForEach(x => x.OverrideItemId = item.OverrideItemId);
                    //orphanRecords.ItemIds.AddRange(ovrItems.Select(x => x.ItemId));

                    _repository.Delete<ItemCollectionObject>(x => x.ItemId == itemId);
                    _repository.Delete<ItemCollectionLink>(x => x.ItemId == itemId);
                    _repository.Delete<CategoryObject>(x => x.ItemId == itemId);

                    //Delete all overrides of this Item
                    _repository.Delete<ItemCollectionObject>(x => x.ParentItemId == itemId);
                    _repository.Delete<CategoryObject>(x => x.ParentItemId == itemId);

                    _repository.Delete<AssetItemLink>(x => x.ItemId == itemId);
                    _repository.Delete<ItemDescription>(x => x.ItemId == itemId);
                    _repository.Delete<ItemPOSDataLink>(x => x.ItemId == itemId);
                    _repository.Delete<MenuItemCycleInSchedule>(x => x.MenuItemScheduleLink.ItemId == itemId);
                    _repository.Delete<MenuItemScheduleLink>(x => x.ItemId == itemId);
                    _repository.Delete<TempSchedule>(x => x.ItemId == itemId);

                    _repository.Delete<Item>(item);
                    _commonService.SetLastUpdatedDateofMenusUsingItems(new List<int> { itemId });
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.MasterItemDeleteT, item.ItemName);
                    _auditLogger.Write(OperationPerformed.Deleted, EntityType.Item, entityNameList: item.ItemName);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrInvalidItemT, item.ItemName);
                }
                if (orphanRecords.ItemIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMasterItemDeleteT, itemname);
                Logger.WriteError(ex);
            }
            return itemId;
        }

        /// <summary>
        /// Update Master Item Status to active/inactive
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="updateStatus"></param>
        public void UpdateMasterItemStatus(int itemId, bool updateStatus)
        {
            if (updateStatus)
            {
                ActivateMasterItem(itemId);
            }
            else
            {
                DeactivateMasterItem(itemId);
            }
        }

        /// <summary>
        /// Deactivate Master item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public int DeactivateMasterItem(int itemId)
        {
            var itemname = string.Empty;
            CleanUpDataModel orphanRecords = new CleanUpDataModel();
            try
            {
                var item = _repository.GetQuery<Item>(x => x.ItemId == itemId).FirstOrDefault();
                if (item != null)
                {
                    item.IsEnabled = false;
                    _repository.Update<Item>(item);

                    //Delete - As per new BR rule - Master items overrides are determined by ParentItemId not OverrideItemId
                    //Make overriden items independent of parent - store all orphan records
                    var ovrItems = _repository.GetQuery<Item>(x => x.ParentItemId == itemId).ToList();
                    orphanRecords.ItemIds.AddRange(ovrItems.Select(x => x.ItemId));

                    //Remove it from all Menus
                    _repository.Delete<ItemCollectionObject>(x => x.Item.ParentItemId == itemId);
                    _repository.Delete<ItemCollectionLink>(x => x.Item.ParentItemId == itemId);
                    _repository.Delete<CategoryObject>(x => x.Item.ParentItemId == itemId);

                    //Delete all overrides of this Item
                    _repository.Delete<ItemCollectionObject>(x => x.Item1 != null && x.Item1.ParentItemId == itemId);
                    _repository.Delete<CategoryObject>(x => x.Item1 != null && x.Item1.ParentItemId == itemId);

                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.MasterItemDeactivateT, item.ItemName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrInvalidItemT, item.ItemName);
                }
                if (orphanRecords.ItemIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMasterItemDeactivateT, itemname);
                Logger.WriteError(ex);
            }
            return itemId;
        }

        /// <summary>
        /// Activate Master item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public int ActivateMasterItem(int itemId)
        {
            var itemname = string.Empty;
            try
            {
                var item = _repository.GetQuery<Item>(x => x.ItemId == itemId).FirstOrDefault();
                if (item != null)
                {
                    item.IsEnabled = true;
                    _repository.Update<Item>(item);

                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.MasterItemActivateT, item.ItemName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Item, entityNameList: item.ItemName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrInvalidItemT, item.ItemName);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMasterItemActivateT, itemname);
                Logger.WriteError(ex);
            }
            return itemId;
        }

        /// <summary>
        /// return Item list to view model
        /// </summary>
        /// <param name="brandId"> Brand Id of items to be fetched</param>
        /// <param name="parentId"> send ParentId if list is fetched to add a child </param>
        /// <param name="prntType"> send type of parent if list is fetched to add a child</param>
        /// <param name="includeItemExtraProperties"> Include Items extra properties like images,descriptions,pos items in the list</param>
        /// <param name="grdRequest">grid filtering, sorting, paging class</param>
        /// <param name="excludeNoPLUItems">Exclude items without PLU </param>
        /// <param name="netId">send network id where operarion is performed if list is fetched to add a child</param>
        /// <param name="excludeDeactivated">Exclude deactivated items</param>
        /// <param name="isGrid">Is the result shown inside a grid</param>
        /// <returns>UI version of Item -> ItemEdit</returns>
        public List<ItemModel> GetItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, KendoGridRequest grdRequest)
        {
            var itemList = new List<ItemModel>();
            try
            {
                var vwItemGrid = new KendoGrid<vwItemwithPOS>();
                var filtering = vwItemGrid.GetFiltering(grdRequest);
                var sorting = vwItemGrid.GetSorting(grdRequest);
                
                //Get Only Parent Items(Master) and Enabled Items
                var qryList = (from i in _repository.GetQuery<Item>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId)
                               select i);

                //If list should contain only items with PLU then exclude Non PLU items
                if (excludeNoPLUItems.HasValue && excludeNoPLUItems.Value)
                {
                    //excludeNoPLUItems is not used hence it will always false
                    //qryList = qryList.Where(x => x.BasePLU != null);
                }

                if (excludeDeactivated.HasValue && excludeDeactivated.Value)
                {
                    qryList = qryList.Where(x => x.IsEnabled);
                }
                qryList = qryList.Where(filtering).OrderBy(sorting);

                Count = qryList.Count();
                // If it is not grid the it is POSItems paging is custom hence determine no items per page based on page count
                if (isGrid.HasValue && isGrid.Value == false)
                {
                    var noOfPages = Math.Ceiling((double)(Count / grdRequest.PageSize));

                    if (noOfPages > 10)
                    {
                        grdRequest.PageSize = (int)Math.Ceiling((double)(Count / 10));
                    }
                }

                //If Paging is enabled then filter the items based on no of items
                var allItems = grdRequest.PageSize > 0 ? qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList() : qryList.ToList();

                if (includeItemExtraProperties.HasValue && includeItemExtraProperties == true)
                {
                    //Get Items by joining the view
                    var itemIds = (from o in allItems select o.ItemId).ToList();


                    #region Get Item POS Data

                    var itemPOSDataQuery = _repository.GetQuery<ItemPOSDataLink>(x => x.NetworkObjectId == brandId);
                    itemPOSDataQuery = itemPOSDataQuery.Include("POSData");
                    var allItemPOSDataList = (from i in itemPOSDataQuery
                                            //join vw in allvwItems on i.ItemId equals vw.ItemId
                                            where itemIds.Contains(i.ItemId)
                                            select i).ToList();
                    #endregion

                    foreach (var item in allItems)
                    {
                        var defaultPOSLink = allItemPOSDataList.FirstOrDefault(x => x.ItemId == item.ItemId && x.IsDefault);

                        itemList.Add(new ItemModel
                        {
                            ItemId = item.ItemId,
                            DisplayName = item.DisplayName,
                            ItemName = item.ItemName,
                            DisplayDescription = item.DisplayDescription,
                            IsEnabled = item.IsEnabled,
                            POSItemName = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.POSItemName : string.Empty,
                            BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? (int?)defaultPOSLink.POSData.PLU : null,
                            AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty,
                            BasePrice = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.BasePrice : null,
                            IsAlcohol = defaultPOSLink != null && defaultPOSLink.POSData != null && defaultPOSLink.POSData.IsAlcohol,
                            IsModifier = defaultPOSLink != null && defaultPOSLink.POSData != null && defaultPOSLink.POSData.IsModifier,
                            DeepLinkId = item.DeepLinkId,
                            Feeds = item.Feeds,
                            //ItemDescriptions = item.ItemDescriptions.Any() ? mapItemDescriptionstoDescriptionModelList(item.ItemDescriptions.ToList()) : null,
                            POSDatas = allItemPOSDataList.Any(x => x.ItemId == item.ItemId) ? mapItemPOSLinktoPOSModelList(allItemPOSDataList.Where(x => x.ItemId == item.ItemId).ToList()) : null,
                            ////StartDate = i.StartDate.HasValue ? (DateTime?)i.StartDate.Value.ToLocalTime() : i.StartDate,
                            ////EndDate = i.EndDate.HasValue ? (DateTime?)i.EndDate.Value.ToLocalTime() : i.EndDate,
                            //URL = item.AssetItemLinks.Any() ? cDN + item.AssetItemLinks.FirstOrDefault().Asset.Blobname : string.Empty,
                            //cDN = cDN,
                            //Assets = item.AssetItemLinks.Any() ? mapAssetItemLinktoAssetModelList(item.AssetItemLinks.ToList()) : null
                        });
                    }

                    //after fetching the data sort as per the gridRequest as sorting is lost after querying the items again.
                    var secondFilterQryList = (from i in itemList select i).AsQueryable<ItemModel>();
                    itemList = secondFilterQryList.OrderBy(sorting).ToList();
                }
                else
                {
                    itemList.AddRange(allItems.Select(item => new ItemModel
                    {
                        ItemId = item.ItemId, 
                        DisplayName = item.DisplayName, 
                        ItemName = item.ItemName, 
                        IsEnabled = item.IsEnabled, 
                        NetworkObjectId = item.NetworkObjectId, 
                        IrisId = item.IrisId,
                        DeepLinkId = item.DeepLinkId
                    }));
                }

            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return itemList;
        }

        public List<ItemModel> GetPOSItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, string gridType, KendoGridRequest grdRequest, out bool onlyfewReturned)
        {
            var itemList = new List<ItemModel>();
            onlyfewReturned = false;
            try
            {
                var vwPOSItemGrid = new KendoGrid<vwPOSwithItem>();
                var filtering = vwPOSItemGrid.GetFiltering(grdRequest);
                var sorting = vwPOSItemGrid.GetSorting(grdRequest);

                var parentIds = new List<int>();
                var loop = false;
                var excludeItems = new List<int>();

                //Get Only Parent Items(Master) and Enabled Items
                var qryList = (from i in _repository.GetQuery<vwPOSwithItem>(x => x.OverrideItemId == null && x.ParentItemId == null && x.NetworkObjectId == brandId)
                               select i);

                ////If an Item is being added to collection then the parent items of that collection cannot be added, Hence exclude them
                //if (parentId.HasValue && netId.HasValue && prntType.HasValue)
                //{
                //    _commonService.ParentNetworkNodes = _ruleService.GetNetworkParents(netId.Value);
                //    parentIds.Add(parentId.Value);
                //    do
                //    {
                //        switch (prntType)
                //        {
                //            case MenuType.ItemCollection:
                //                //get all parentItems of given collection
                //                loop = _commonService.GetparentItemsFromCollection(parentIds, excludeItems);
                //                break;
                //            case MenuType.Item:
                //                //get all parent items of given item
                //                loop = _commonService.GetparentItemsFromItem(parentIds, excludeItems);
                //                break;
                //        }

                //    } while (loop);

                //    //get the parent prepend items(only prepend items(of type Item) can be added if parent is item
                //    if (prntType == MenuType.Item)
                //    {
                //        loop = false;
                //        parentIds.Add(parentId.Value);
                //        do
                //        {
                //            //get all parent prependitems of given item
                //            loop = _commonService.GetparentPrependItemsFromItem(parentIds, excludeItems);
                //        } while (loop);

                //    }
                //    _commonService.GetImmediateItems(parentId.Value, excludeItems, prntType.Value);
                //}

                if(string.IsNullOrWhiteSpace(gridType) == false && gridType.CompareTo("item") == 0)
                {
                    //exclude all the parent items found
                    qryList = qryList.Where(x => x.ItemId.HasValue && !excludeItems.Contains(x.ItemId.Value));
                }
                else
                {
                    //exclude all the parent items found
                    qryList = qryList.Where(x => x.POSDataId.HasValue);

                }

                //If list should contain only items with PLU then exclude Non PLU items
                if (excludeNoPLUItems.HasValue && excludeNoPLUItems.Value)
                {
                    qryList = qryList.Where(x => x.BasePLU != null);
                }

                if (excludeDeactivated.HasValue && excludeDeactivated.Value)
                {
                    qryList = qryList.Where(x => x.ItemId.HasValue && x.IsEnabled.Value);
                }
                qryList = qryList.Where(filtering).OrderBy(sorting);

                try
                {
                    ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_context).ObjectContext.CommandTimeout = 3;
                    Count = qryList.Count();
                }
                catch(Exception ex)
                {
                    onlyfewReturned = true;
                    Count = 5000;
                }

                //Set back the command timeou to default time - 30 seconds
                ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_context).ObjectContext.CommandTimeout = 30;


                // If it is not grid the it is POSItems paging is custom hence determine no items per page based on page count
                if (isGrid.HasValue && isGrid.Value == false)
                {
                    var noOfPages = Math.Ceiling((double)(Count / grdRequest.PageSize));

                    if (noOfPages > 10)
                    {
                        grdRequest.PageSize = (int)Math.Ceiling((double)(Count / 10));
                    }
                }

                //If Paging is enabled then filter the items based on no of items
                var allvwItems = grdRequest.PageSize > 0 ? qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList() : qryList.ToList();

                foreach(var positem in allvwItems)
                {
                    var itemModel = new ItemModel();
                    itemModel.InjectFrom(positem);
                    itemModel.ItemId = positem.ItemId.HasValue ? positem.ItemId.Value : 0;
                    itemModel.POSDataId = positem.POSDataId.HasValue ? positem.POSDataId.Value : 0;
                    itemModel.IsDefault = positem.IsDefault.HasValue ? positem.IsDefault.Value : true;
                    itemModel.IsAlcohol = positem.IsAlcohol.HasValue ? positem.IsAlcohol.Value : false;
                    itemModel.IsModifier = positem.IsModifier.HasValue ? positem.IsModifier.Value : false;
                    itemList.Add(itemModel);
                }

            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return itemList;
        }

        /// <summary>
        /// return Item list to view model
        /// </summary>
        /// /// <param name="brandId"> Brand Id of items to be fetched</param>
        /// <param name="parentId"> send ParentId if list is fetched to add a child </param>
        /// <param name="prntType"> send type of parent if list is fetched to add a child</param>
        /// <param name="menuId"> send MenuId where items are fetched</param>
        /// <param name="grdRequest">grid filtering, sorting, paging class</param>
        /// <param name="excludeNoPLUItems">Exclude items without PLU </param>
        /// <param name="netId">send network id where operarion is performed if list is fetched to add a child</param>
        /// <param name="excludeDeactivated">Exclude deactivated items</param>
        /// <returns>UI version of Item -> ItemEdit</returns>
        public List<ItemModel> GetOverriddenItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, int? menuId, KendoGridRequest grdRequest)
        {
            var itemList = new List<ItemModel>();
            try
            {
                var vwItemGrid = new KendoGrid<vwItemwithPOS>();
                var filtering = vwItemGrid.GetFiltering(grdRequest);
                var sorting = vwItemGrid.GetSorting(grdRequest);

                var parentIds = new List<int>();
                var loop = false;
                var excludeItems = new List<int>();

                if (netId != null)
                {
                    var parentNetworkObjectIds = _ruleService.GetNetworkParents(netId.Value);


                    var itemIdsInMenu = (_context as ProductMasterContext).fnNetworkItems(netId, menuId, true).ToList().Select(x => x.ItemId);

                    //Get Only Menu Items(Non Master) and Enabled Items and Items in current Menu
                    var qryList = (from i in _repository.GetQuery<vwItemwithPOS>(x => x.ParentItemId.HasValue && x.OverrideItemId.HasValue == false && parentNetworkObjectIds.Contains(x.NetworkObjectId) && itemIdsInMenu.Contains(x.ItemId))
                        select i);

                    //If an Item is being added to collection then the parent items of that collection cannot be added, Hence exclude them
                    if (parentId.HasValue && prntType.HasValue)
                    {
                        _commonService.ParentNetworkNodes = parentNetworkObjectIds;
                        parentIds.Add(parentId.Value);
                        do
                        {
                            switch (prntType)
                            {
                                case MenuType.ItemCollection:
                                    //get all parentItems of given collection
                                    loop = _commonService.GetparentItemsFromCollection(parentIds, excludeItems);
                                    break;
                                case MenuType.Item:
                                    //get all parent items of given item
                                    loop = _commonService.GetparentItemsFromItem(parentIds, excludeItems);
                                    break;
                            }

                        } while (loop);

                        //get the parent prepend items(only prepend items(of type Item) cannot be added if parent is item
                        if (prntType == MenuType.Item)
                        {
                            loop = false;
                            parentIds.Add(parentId.Value);
                            do
                            {
                                //get all parent prependitems of given item
                                loop = _commonService.GetparentPrependItemsFromItem(parentIds, excludeItems);
                            } while (loop);

                        }
                        //_commonService.GetImmediateItems(parentId.Value, excludeItems, prntType.Value);

                        //exclude all the parent items found
                        qryList = qryList.Where(x => !excludeItems.Contains(x.ItemId));
                    }

                    //If list should contain only items with PLU then exclude Non PLU items
                    if (excludeNoPLUItems.HasValue && excludeNoPLUItems.Value)
                    {
                        qryList = qryList.Where(x => x.BasePLU != null);
                    }

                    if (excludeDeactivated.HasValue && excludeDeactivated.Value)
                    {
                        qryList = qryList.Where(x => x.IsEnabled);
                    }
                    qryList = qryList.Where(filtering).OrderBy(sorting);

                    Count = qryList.Count();
                    //If Paging is enabled then filter the items based on no of items
                    var allvwItems = grdRequest.PageSize > 0 ? qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList() : qryList.ToList();
                    
                    var itemQuery = _repository.GetQuery<Item>();

                    //Include POS info
                    //itemQuery = itemQuery.Include("ItemPOSDataLinks").Include("ItemPOSDataLinks.POSData");

                    //Get Items by joining the view
                    var itemIds = (from o in allvwItems select o.ItemId).ToList();
                    var allItems = (from i in itemQuery
                                    //join vw in allvwItems on i.ItemId equals vw.ItemId
                                    where itemIds.Contains(i.ItemId)
                                    select i).ToList();

                    #region Get Item POS Data
                    var _itemPOSDataList = _repository.GetQuery<ItemPOSDataLink>(x => x.Item.MenuId == menuId && parentNetworkObjectIds.Contains(x.NetworkObjectId)).Include("POSData").ToList();
                    #endregion

                    foreach (var item in allItems)
                    {
                        // get the POS applicable(pick last parent value) for this network - 
                        var defaultPOSLink = _itemPOSDataList.Where(x => x.ItemId == item.ItemId && x.IsDefault && parentNetworkObjectIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        itemList.Add(new ItemModel
                        {
                            ItemId = item.ItemId,
                            DisplayName = item.DisplayName,
                            ItemName = item.ItemName,
                            DisplayDescription = item.DisplayDescription,
                            IsEnabled = item.IsEnabled,
                            POSItemName = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.POSItemName : string.Empty,
                            BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? (int?)defaultPOSLink.POSData.PLU : null,
                            AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty,
                            BasePrice = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.BasePrice : null,
                            IsAlcohol = defaultPOSLink != null && defaultPOSLink.POSData != null && defaultPOSLink.POSData.IsAlcohol,
                            IsModifier = defaultPOSLink != null && defaultPOSLink.POSData != null && defaultPOSLink.POSData.IsModifier,
                            DeepLinkId = item.DeepLinkId,
                            IsDefault = true, // All Existing Menu item have the same PLU when they hence set Is Default to true so they wont show as italic
                        });
                    }

                    //after fetching the data sort as per the gridRequest as sorting is lost after querying the items again.
                    var secondFilterQryList = (from i in itemList select i).AsQueryable<ItemModel>();
                    itemList = secondFilterQryList.OrderBy(sorting).ToList();
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return itemList;
        }

        /// <summary>
        /// returns list of ItemModels for given Ids
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<ItemModel> GetSelectedItemList(int[] ids)
        {
            var items = (from i in _repository.GetQuery<Item>(x => ids.Contains(x.ItemId))
                         select new ItemModel
                         {
                             ItemId = i.ItemId,
                             ItemName = i.ItemName,
                             DisplayName = i.DisplayName
                             //BasePLU = i.ItemPOSDataLinks.Any(x => x.IsDefault && x.POSData != null) ? i.ItemPOSDataLinks.Where(x => x.IsDefault).FirstOrDefault().POSData.PLU : null
                         }).ToList();

            return items;
        }

        /// <summary>
        /// Map List of Item Descriptions to List of Models
        /// </summary>
        /// <param name="descriptions"></param>
        /// <returns></returns>
        private List<ItemDescriptionModel> mapItemDescriptionstoDescriptionModelList(IEnumerable<ItemDescription> descriptions)
        {
            var descriptionsModel = descriptions.Select(desc => new ItemDescriptionModel
            {
                Description = desc.Description, 
                IsActive = desc.IsActive, 
                ItemDescriptionId = desc.ItemDescriptionId, 
                ItemId = desc.ItemId
            }).ToList();
            return descriptionsModel.ToList();
        }

        /// <summary>
        /// Map Item Model to DB Object Item
        /// </summary>
        /// <param name="itmModel"></param>
        /// <param name="itm"></param>
        private void mapItemModeltoMasterItem(ItemModel itmModel, ref Item itm)
        {
            //itm.InjectFrom(itmModel);
            itm.DisplayName = string.IsNullOrWhiteSpace(itmModel.DisplayName) ? string.Empty : itmModel.DisplayName.Trim();
            itm.ItemName = string.IsNullOrWhiteSpace(itmModel.ItemName) ? string.Empty : itmModel.ItemName.Trim();
            itm.DisplayDescription = string.IsNullOrWhiteSpace(itmModel.DisplayDescription) ? string.Empty : itmModel.DisplayDescription.Trim();
            itm.ButtonText = string.IsNullOrWhiteSpace(itmModel.ButtonText) ? string.Empty : itmModel.ButtonText.Trim();
            itm.PrintOnOrder = itmModel.PrintOnOrder;
            itm.PrintOnReceipt = itmModel.PrintOnReceipt;
            itm.PrintOnSameLine = itmModel.PrintOnSameLine;
            itm.PrintRecipe = itmModel.PrintRecipe;
            itm.ForceRecipe = itmModel.ForceRecipe;
            itm.IsBeverage = itmModel.IsBeverage;
            itm.IsEntreeApp = itmModel.IsEntreeApp;
            itm.IsCore = itmModel.IsCore;
            itm.IsEnabled = itmModel.IsEnabled;
            itm.StartDate = itmModel.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(itmModel.StartDate.Value) : itmModel.StartDate;
            itm.EndDate = itmModel.EndDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(itmModel.EndDate.Value) : itmModel.EndDate;
            itm.NetworkObjectId = itmModel.NetworkObjectId;
            itm.RequestedBy = itmModel.RequestedBy;
            itm.DeepLinkId = string.IsNullOrWhiteSpace(itmModel.DeepLinkId) ? null : itmModel.DeepLinkId.Trim();
            itm.ShowPrice = itmModel.ShowPrice;
            itm.IsAvailable = itmModel.IsAvailable;
            itm.CookTime = itmModel.CookTime;
            itm.PrepOrderTime = itmModel.PrepOrderTime;
            itm.DWItemCategorizationKey = itmModel.DWItemCategorizationKey;
            itm.DWItemSubTypeKey = itmModel.DWItemSubTypeKey;
            itm.RequestedBy = itmModel.RequestedBy;
            itm.Feeds = itmModel.Feeds;

        }

        /// <summary>
        /// Map database object Item to its Model
        /// </summary>
        /// <param name="itm"></param>
        /// <param name="itmModel"></param>
        /// <param name="includeChildren"></param>
        /// <param name="networkObjectId"></param>
        private void mapItemtoItemModel(Item itm, ref ItemModel itmModel, bool includeChildren = true, int networkObjectId = 0)
        {
            itmModel.InjectFrom(itm);
            itmModel.OverridenPrice = decimal.Round(itmModel.OverridenPrice, 2);
            if (includeChildren)
            {
                var masterItem = itm.MasterItem ?? itm;
                var selectedDescription = masterItem.ItemDescriptions.Any() && itm.SelectedDescriptionId != null ? masterItem.ItemDescriptions.FirstOrDefault(p => p.ItemDescriptionId == itm.SelectedDescriptionId) : null;
                itmModel.SelectedDescription = selectedDescription == null ? itm.DisplayDescription : selectedDescription.Description;
                itmModel.SelectedDescriptionId = selectedDescription == null ? 0 : selectedDescription.ItemDescriptionId;

                itmModel.StartDate = itm.StartDate.HasValue ? (DateTime?)itm.StartDate.Value.ToLocalTime() : itm.StartDate;
                itmModel.EndDate = itm.EndDate.HasValue ? (DateTime?)itm.EndDate.Value.ToLocalTime() : itm.EndDate;
                itmModel.ItemDescriptions = mapItemDescriptionstoDescriptionModelList(masterItem.ItemDescriptions.ToList());
                itmModel.Assets = mapAssetItemLinktoAssetModelList(masterItem.AssetItemLinks.ToList());

                ItemPOSDataLink defaultPOSLink = null;
                if (networkObjectId != 0)
                {
                    var parentNetworkObjectIds = _ruleService.GetNetworkParents(networkObjectId);
                    // get the POS applicable(pick last parent value) for this network - 
                    defaultPOSLink = itm.ItemPOSDataLinks.Any(x => x.IsDefault && parentNetworkObjectIds.Contains(x.NetworkObjectId)) ? itm.ItemPOSDataLinks.Where(x => x.IsDefault && parentNetworkObjectIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault() : null;
                }
                else
                {
                    defaultPOSLink = itm.ItemPOSDataLinks.Any(x => x.IsDefault) ? itm.ItemPOSDataLinks.FirstOrDefault(x => x.IsDefault) : null;
                }

                if (defaultPOSLink != null && defaultPOSLink.POSDataId.HasValue)
                {
                    itmModel.SelectedPOSDataId = itmModel.PreviousPOSDataIdSelected = defaultPOSLink.POSDataId.Value;
                    itmModel.BasePLU = defaultPOSLink.POSData.PLU;
                    itmModel.POSItemName = defaultPOSLink.POSData.POSItemName;
                    itmModel.AlternatePLU = defaultPOSLink.POSData.AlternatePLU;
                    itmModel.BasePrice = defaultPOSLink.POSData.BasePrice;
                    itmModel.IsAlcohol = defaultPOSLink.POSData.IsAlcohol;
                    itmModel.IsModifier = defaultPOSLink.POSData.IsModifier;
                }
                itmModel.POSDatas = mapItemPOSLinktoPOSModelList(masterItem.ItemPOSDataLinks.ToList());

                itmModel.URL = masterItem.AssetItemLinks.Any() ? cDN + masterItem.AssetItemLinks.FirstOrDefault().Asset.Blobname : "";
                itmModel.cDN = cDN;
                itmModel.AdditionalDescCount = itmModel.ItemDescriptions != null && itmModel.ItemDescriptions.Any() ? itmModel.ItemDescriptions.Count() : 0;
            }
        }

        /// <summary>
        /// Map List of ITEMPOS Objects to List of Models
        /// </summary>
        /// <param name="itemPOSLinks"></param>
        /// <returns></returns>
        private List<POSDataModel> mapItemPOSLinktoPOSModelList(List<ItemPOSDataLink> itemPOSLinks)
        {
            var posModels = new List<POSDataModel>();

            foreach (var link in itemPOSLinks)
            {
                POSDataModel posItemModel = new POSDataModel();
                posItemModel.InjectFrom(link.POSData);
                posItemModel.ItemPOSDataLinkId = link.ItemPOSDataLinkId;
                posItemModel.ItemId = link.ItemId;
                posItemModel.IsDefault = link.IsDefault;

                posModels.Add(posItemModel);
            }
            return posModels;
        }

        /// <summary>
        /// Map List of AssetItem Objects to List of Models
        /// </summary>
        /// <param name="assetItemLinks"></param>
        /// <returns></returns>
        private List<AssetItemModel> mapAssetItemLinktoAssetModelList(List<AssetItemLink> assetItemLinks)
        {
            var assetModels = new List<AssetItemModel>();

            foreach (var link in assetItemLinks)
            {
                assetModels.Add(new AssetItemModel
                {
                    AssetItemLinkId = link.AssetItemLinkId,
                    AssetId = link.AssetId,
                    AssetTypeId = (int)link.Asset.AssetTypeId,
                    BlobName = link.Asset.Blobname,
                    ThumbNailBlobName = link.Asset.ThumbnailBlob,
                    FileName = link.Asset.Filename,
                    URL = cDN + link.Asset.Blobname
                });
            }
            return assetModels;
        }

        ///// <summary>
        ///// Map List of PLUItem Objects to List of Models
        ///// </summary>
        ///// <param name="PLUItemLinks"></param>
        ///// <returns></returns>
        //public List<PLUItemModel> MapPluItemLinktoPluItemModelList(List<PLUItemLink> PLUItemLinks)
        //{
        //    List<PLUItemModel> PLUItemModels = new List<PLUItemModel>();

        //    foreach (var link in PLUItemLinks)
        //    {
        //        PLUItemModels.Add(new PLUItemModel
        //        {
        //            PLUItemId = link.PLUItemLinkId,
        //            PLU = link.PLU,
        //            ItemId = link.ItemId,
        //            IsActive = link.IsActive
        //        });
        //    }
        //    return PLUItemModels;
        //}

    }

    public interface IItemService
    {
        string LastActionResult { get; }
        int Count { get; set; }
        string cDN { get; set; }

        ItemModel GetItem(int itemId, string[] children, int networkObjectId = 0);
        List<ItemModel> GetItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, KendoGridRequest grdRequest);
        List<ItemModel> GetPOSItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, string gridType, KendoGridRequest grdRequest, out bool onlyfewReturned); 
        List<ItemModel> GetOverriddenItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, int? menuId, KendoGridRequest grdRequest);
        List<ItemModel> GetSelectedItemList(int[] ids);

        SelectList GetItemLookups(int typeid, string abbr);
        SelectList GetItemCookTimes();
        SelectList GetItemCategorizations();
        SelectList GetItemSubTypes();
        SelectList GetItemRequestedBy();

        //List<ItemDescriptionModel> GetItemDescriptions(int ItemId);
        //int AddItemDescription(ItemDescriptionModel itemDesc);
        //bool UpdateItemDescription(ItemDescriptionModel itemDesc);
        //bool DeleteItemDescription(ItemDescriptionModel itemDesc);

        //bool IsPLUUniqueinBrand(int? PLU, string AltPLU, int itemId, out string errorMsg,out string entityWithError, int? brandId = null);
        bool IsDeepLinkIdUniqueinBrand(string deepLinkId, int itemId, int? brandId = null);
        //bool IsAltPLUUniqueinBrand(string altPLU, int itemId, int? brandId = null);
        bool UpdateItemDisplayName(ItemModel model);
        ItemModel SaveMasterItem(ItemModel itmModel);
        int DeleteMasterItem(int itemId);
        int DeactivateMasterItem(int itemId);
        int ActivateMasterItem(int itemId);
        void UpdateMasterItemStatus(int itemId, bool updateStatus);
        bool CheckDWFieldsEnabled(int networkObjectId);
    }
}