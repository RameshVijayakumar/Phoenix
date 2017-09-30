using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using Phoenix.DataAccess;
using Omu.ValueInjecter;
using Phoenix.Common;
using System.Diagnostics;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Phoenix.RuleEngine;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;

namespace Phoenix.Web.Models
{
    public class POSMapService : IPOSMapService
    {
        private IRepository _repository;
        private DbContext _context;
        private IRepository _odsRepository;
        private DbContext _odsContext;
        private IAuditLogService _auditLogger;
        private ICommonService _commonService;
        //private ICommonPOSMapService _commonPOSMapService;
        private RuleService _ruleService;

        private string _lastActionResult;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        public int Count { get; set; }
        public int Pages { get; set; }
        public delegate void StatusUpdateHandler(object sender, ProgressChangedEventArgs e);
        public event StatusUpdateHandler OnUpdateStatus;

        public enum StatusTypes
        {
            Info = 1,
            Success = 2,
            Error = 3,
            Other = 4
        }
        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public POSMapService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _commonService = new CommonService(_repository);

            _odsContext = new PhoenixODSContext();
            _odsRepository = new GenericRepository(_odsContext);

            _ruleService = new RuleService(RuleService.CallType.Web);
            //_commonPOSMapService = new CommonPOSMapService(_context, _repository, _odsContext, _odsRepository,RuleService.CallType.Web);
        }

        // get list of items for a given parent group id and network object id
        public List<MenuItemModelPOS> GetMenuItems(int parentGroupId, int networkObjectId, int menuId, KendoGridRequest grdRequest)
        {
            List<MenuItemModelPOS> retList = new List<MenuItemModelPOS>();
            List<POSMappedItemsResult> dbItems = null;
            var odsDataList = new List<ODSPOSData>();
            var network = _repository.GetQuery<NetworkObject>(s => s.NetworkObjectId == networkObjectId).FirstOrDefault();
            if (network != null)
            {
                var vwPOSMappingGrid = new KendoGrid<POSMappedItemsResult>();
                var filtering = vwPOSMappingGrid.GetFiltering(grdRequest);
                var sorting = vwPOSMappingGrid.GetSorting(grdRequest);
                
                if (parentGroupId == 0)
                {
                    // get items under categories.
                    // TASK - Performance improvement for POSMapping: Used StoredProcedures
                    dbItems = ((_context as ProductMasterContext).usp_POSMappedItems_Category(networkObjectId, menuId)).ToList();
                    
                }
                else
                {
                    // get items under collection.
                    dbItems = ((_context as ProductMasterContext).usp_POSMappedItems_Collection(networkObjectId, menuId, parentGroupId)).ToList();
                }

                var itemMasterIds = dbItems.Where(x => x.ParentItemId.HasValue).Select(x => x.ParentItemId).Distinct().ToList();

                var itemDetails = _repository.GetQuery<ItemPOSDataLink>(x => itemMasterIds.Contains(x.ItemId)).Include("POSData").ToList();

                if (network.NetworkObjectTypeId == NetworkObjectTypes.Site && dbItems.Any())
                {
                    // fetch data from db
                    odsDataList = _odsRepository.GetQuery<ODSPOSData>(o => o.IrisId == network.IrisId && string.IsNullOrEmpty(o.ScreenGroupName) == false)
                                    .ToList();
                }

                //// TASK - Performance improvement for POSMapping: Used StoredProcedures
                foreach (var item in dbItems)
                {
                    var itemPOSList = new List<ItemPOSDataLink>();
                    if (item.ParentItemId.HasValue)
                    {
                        itemPOSList = itemDetails.Where(x => x.ItemId == item.ParentItemId).ToList();
                    }

                    ODSPOSData odsDetail = null;
                    if (odsDataList.Any())
                    {
                        if (item.MappedPLU.HasValue && item.MappedPLU == 0)
                        {
                            // for zero plu, include name in the search
                            odsDetail = odsDataList.Where(p => p.PLU == item.MappedPLU && p.ItemName == item.MappedPOSItemName).FirstOrDefault();
                        }
                        else
                        {
                            odsDetail = odsDataList.Where(x => item.MappedPLU.HasValue && x.PLU == item.MappedPLU.Value).FirstOrDefault();
                        }
                    }
                    retList.Add(copyToModel(item, itemPOSList, odsDetail));
                }
                retList = retList.OrderBy(x => x.ParentName).ThenBy(x => x.ItemName).ToList();
            }
            return retList;
        }

        /// <summary>
        /// Returns a list of models contains operational POS data for a given networkobject id
        /// </summary>
        /// <param name="networkObjectId">Network object id of the site</param>
        /// <returns>List of OperationalPOSDataModel</returns>
        public List<OperationalPOSDataModel> GetOperationalPOSData(int networkObjectId)
        {
            List<OperationalPOSDataModel> retList = new List<OperationalPOSDataModel>();
            var site = _repository.GetQuery<SiteInfo>(s => s.NetworkObjectId == networkObjectId).FirstOrDefault();

            if (site != null)
            {
                // fetch data from db
                var opsDataList = _odsRepository.GetQuery<ODSPOSData>(o => o.IrisId == site.NetworkObject.IrisId && string.IsNullOrEmpty(o.ScreenGroupName) == false)
                                .OrderBy(o => o.ScreenGroupName)
                                .ToList();

                // map to local model and add to return list
                foreach (var dbOPSData in opsDataList)
                {
                    OperationalPOSDataModel opsData = new OperationalPOSDataModel();
                    opsData.InjectFrom(dbOPSData);

                    retList.Add(opsData);
                }
            }
            return retList;
        }

        /// <summary>
        /// Returns a list of models contains operational POS data for a given networkobject id
        /// </summary>
        /// <param name="networkObjectId">Network object id of the site</param>
        /// <returns>List of OperationalPOSDataModel</returns>
        public List<POSDataModel> GetPOSDataList(int networkObjectId, bool includeMappped = true, KendoGridRequest grdRequest = null)
        {
            List<POSDataModel> retList = new List<POSDataModel>();

            try
            {
                KendoGrid<POSData> gridPOSData = new KendoGrid<POSData>();
                var filtering = gridPOSData.GetFiltering(grdRequest);
                var sorting = gridPOSData.GetSorting(grdRequest);
                // fetch data from db
                var posDataQuery = _repository.GetQuery<POSData>(o => o.NetworkObjectId == networkObjectId);

                if (includeMappped == false)
                {
                    posDataQuery = posDataQuery.Where(x => x.ItemPOSDataLinks.Any() == false);
                    posDataQuery = posDataQuery.Include("ItemPOSDataLinks");
                }

                var posDataList = new List<POSData>();
                if (grdRequest.PageSize > 0)
                {
                    posDataList = posDataQuery.Where(filtering).OrderBy(sorting).Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
                }
                else
                {
                    posDataList = posDataQuery.Where(filtering).OrderBy(sorting).ToList();
                }

                // map to local model and add to return list
                foreach (var dbPOSData in posDataList)
                {
                    POSDataModel posData = new POSDataModel();
                    posData.InjectFrom(dbPOSData);

                    retList.Add(posData);
                }

                Count = posDataQuery.Where(filtering).Count();
                Pages = grdRequest.PageSize != 0 ? (int)Math.Ceiling((decimal)Count / grdRequest.PageSize) : 1;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
                _lastActionResult = Constants.StatusMessage.ErrUnExpectedErrorOccured;
            }
            return retList;
        }

        /// <summary>
        /// Create/Save POS Item
        /// </summary>
        /// <param name="posItemModel"></param>
        /// <param name="actionStatus"></param>
        /// <returns></returns>
        public POSDataModel SavePOSItem(POSDataModel posItemModel, out bool actionStatus)
        {
            actionStatus = false;
            try
            {
                //Check for uniqueness of PLU if PLU is changes
                var uniqueErrMsg = string.Empty;
                var entityWithError = string.Empty;
                if (IsPLUUniqueinBrand(posItemModel.PLU, posItemModel.AlternatePLU, posItemModel.POSDataId, out uniqueErrMsg, out entityWithError, (int?)posItemModel.NetworkObjectId))
                {
                    var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == posItemModel.POSDataId).FirstOrDefault();
                    if (posItem == null)
                    {
                        posItem = new POSData();
                    }                        
                    var createdDate = posItem.CreatedDate;

                    posItem = mapPOSItem(posItemModel);

                    if (posItemModel.POSDataId == 0)
                    {
                        posItem.CreatedDate = DateTime.Now;
                        posItem.UpdatedDate = DateTime.Now;
                        _repository.Add<POSData>(posItem);
                        _lastActionResult = string.Format(Constants.AuditMessage.POSItemCreatedT, posItemModel.POSItemName);
                    }
                    else
                    {
                        posItem.CreatedDate = createdDate;
                        posItem.UpdatedDate = DateTime.Now;
                        _repository.Update<POSData>(posItem);
                        _lastActionResult = string.Format(Constants.AuditMessage.POSItemUpdatedT, posItemModel.POSItemName);
                    }
                    _context.SaveChanges();
                    _auditLogger.Write(posItemModel.POSDataId == 0 ? OperationPerformed.Created : OperationPerformed.Updated, EntityType.POSItem, entityNameList: posItem.POSItemName);
                    posItemModel.POSDataId = posItem.POSDataId;
                    actionStatus = true;
                }
                else
                {
                    _lastActionResult = uniqueErrMsg;
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemSaveT, posItemModel.POSItemName);
                Logger.WriteError(ex);
            }
            return posItemModel;
        }

        /// <summary>
        /// Delete Unmapped POSItem
        /// </summary>
        /// <param name="posItemId"></param>
        /// <returns></returns>
        public bool DeletePOSItem(int posItemId)
        {
            var actionStatus = false;
            var posname = string.Empty;
            try
            {
                var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == posItemId).Include("ItemPOSDataLinks").FirstOrDefault();
                if (posItem != null)
                {
                    posname = posItem.POSItemName;
                    if (posItem.ItemPOSDataLinks.Any(x => x.ParentMasterItemId == null && x.IsDefault))
                    {
                        //If the deleting POS Item is default then set next one as default
                        var itemId = posItem.ItemPOSDataLinks.FirstOrDefault(x => x.ParentMasterItemId == null).ItemId;
                        var item = _repository.GetQuery<Item>(o => o.ItemId == itemId).Include("ItemPOSDataLinks").FirstOrDefault();

                        var nextPOSdata = item.ItemPOSDataLinks.FirstOrDefault(x => x.POSDataId != posItem.POSDataId);
                        if (nextPOSdata != null)
                        {
                            nextPOSdata.IsDefault = true;
                        }
                    }
                    _repository.Delete<ItemPOSDataLink>(x => x.POSDataId == posItem.POSDataId);
                    _repository.Delete<POSData>(posItem);
                    _lastActionResult = string.Format(Constants.AuditMessage.POSItemDeletedT, posItem.POSItemName);
                    _context.SaveChanges();
                    actionStatus = true;
                    _auditLogger.Write(OperationPerformed.Deleted, EntityType.POSItem, entityNameList: posItem.POSItemName);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemDeleteT, posname);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemDeleteT, posname);
                Logger.WriteError(ex);
            }
            return actionStatus;
        }

        /// <summary>
        /// Attach POSItem to MasterItem
        /// </summary>
        /// <param name="posItemId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool AttachPOSItemToMasterItem(int posItemId, int itemId)
        {
            var actionStatus = false;
            var posname = string.Empty;
            try
            {
                var item = _repository.GetQuery<Item>(o => o.ItemId == itemId && o.ParentItemId == null).Include("ItemPOSDataLinks").FirstOrDefault();
                var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == posItemId).Include("ItemPOSDataLinks").FirstOrDefault();
                if (posItem != null && item != null)
                {
                    posname = posItem.POSItemName;
                    if (posItem.ItemPOSDataLinks.Any() == false)
                    {
                        var isdefault = item.ItemPOSDataLinks.Any() == false;
                        var newitemPOSDataLink = new ItemPOSDataLink
                        {
                            ItemId = item.ItemId,
                            POSDataId = posItem.POSDataId,
                            NetworkObjectId = item.NetworkObjectId,
                            UpdatedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            IsDefault = isdefault

                        };
                        _repository.Add<ItemPOSDataLink>(newitemPOSDataLink);
                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.POSItemAttachedT, posItem.POSItemName, posItem.PLU, item.ItemName);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.POSItem, entityNameList: posItem.POSItemName, operationDescription: _lastActionResult);
                        actionStatus = true;
                    }
                    else
                    {
                        if (posItem.ItemPOSDataLinks.Any(x => x.ItemId == item.ItemId) == true)
                        {
                            _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemAlreadyAttachToSameT, posname);
                        }
                        else
                        {
                            _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemAlreadyAttachT, posname);

                        }
                    }

                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemNotFoundT);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemAttachT, posname);
                Logger.WriteError(ex);
            }
            return actionStatus;
        }

        /// <summary>
        /// Remove POSItem from MasterItem
        /// </summary>
        /// <param name="posItemId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool RemovePOSItemFromMasterItem(int posItemId, int itemId)
        {
            var actionStatus = false;
            string posname = string.Empty;
            var itemname = string.Empty;
            try
            {
                var item = _repository.GetQuery<Item>(o => o.ItemId == itemId && o.ParentItemId == null).Include("ItemPOSDataLinks").FirstOrDefault();
                var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == posItemId).Include("ItemPOSDataLinks").FirstOrDefault();
                if (posItem != null && item != null)
                {
                    itemname = item.DisplayName;
                    posname = posItem.POSItemName;
                    var ipltoDelete = item.ItemPOSDataLinks.Where(x => x.POSDataId == posItemId).FirstOrDefault();
                    if (ipltoDelete != null)
                    {

                        ItemPOSDataLink defaultPOS = null;

                        if (ipltoDelete.IsDefault)
                        {
                            //Set different POSItem as default
                            defaultPOS = item.ItemPOSDataLinks.Where(x => x.POSDataId != posItemId).FirstOrDefault();
                        }
                        else
                        {
                            //Find default POSItem
                            defaultPOS = item.ItemPOSDataLinks.Where(x => x.IsDefault && x.POSDataId != posItemId).FirstOrDefault();
                        }

                        // Remove the POS item from all child network Menu Items
                        if (defaultPOS != null)
                        {
                            defaultPOS.IsDefault = true;
                            //If this POS is set default for Menu Items, then change reference to new default value
                            var ovrItemsPOSdata = _repository.GetQuery<ItemPOSDataLink>(x => x.POSDataId == ipltoDelete.POSDataId && x.Item.ParentItemId == item.ItemId).ToList();
                            ovrItemsPOSdata.ForEach(x => x.POSDataId = defaultPOS.POSDataId);
                        }
                        else
                        {
                            _repository.Delete<ItemPOSDataLink>(x => x.POSDataId == ipltoDelete.POSDataId && x.Item.ParentItemId == item.ItemId);
                        }
                        _repository.Delete<ItemPOSDataLink>(ipltoDelete);
                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.POSItemDetachedT, posItem.POSItemName, posItem.PLU, item.ItemName);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.POSItem, entityNameList: posItem.POSItemName, operationDescription: _lastActionResult);
                        actionStatus = true;
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemLinkNotFoundT, posname);
                    }

                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemNotFoundT);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemDetachT, posname, itemname);
                Logger.WriteError(ex);
            }
            return actionStatus;
        }

        /// <summary>
        /// Change POSItem select for a MenuItem
        /// </summary>
        /// <param name="posItemId"></param>
        /// <param name="itemId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns>action status</returns>
        public bool ChangePOSItemOfMenuItem(int posItemId, int itemId, int networkObjectId, int menuId)
        {
            var actionStatus = false;
            var posname = string.Empty;
            var itemname = string.Empty;
            try
            {
                var item = _repository.GetQuery<Item>(o => o.ItemId == itemId && o.ParentItemId != null).Include("ItemPOSDataLinks").FirstOrDefault();
                var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == posItemId).Include("ItemPOSDataLinks").FirstOrDefault();
                var menu = _repository.GetQuery<Menu>(o => o.MenuId == menuId).FirstOrDefault();
                if ((posItemId == 0 || posItem != null) && item != null && menu != null)
                {
                    itemname = item.DisplayName;
                    posname = posItemId == 0 ? "Placeholder" : posItem.POSItemName;

                    var parentNetworkIds = _ruleService.GetNetworkParents(networkObjectId);

                    var lastItemPOSLink = _repository.GetQuery<ItemPOSDataLink>(x => x.ItemId == itemId && parentNetworkIds.Contains(x.NetworkObjectId)).Include("NetworkObject").OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    var addNewPOSItemLink = false;
                    if (lastItemPOSLink != null)
                    {
                        if (lastItemPOSLink.NetworkObjectId == networkObjectId)
                        {
                            addNewPOSItemLink = false;
                            lastItemPOSLink.POSDataId = posItemId == 0 ? null : (int?)posItem.POSDataId; // if POSDataId is zero then null else positem
                        }
                        else
                        {
                            addNewPOSItemLink = true;
                        }
                    }
                    else
                    {
                        addNewPOSItemLink = true;
                    }

                    if (addNewPOSItemLink)
                    {
                        var newitemPOSDataLink = new ItemPOSDataLink
                        {
                            ItemId = item.ItemId,
                            POSDataId = posItemId == 0 ? null : (int?)posItem.POSDataId, // if POSDataId is zero then null else positem
                            NetworkObjectId = networkObjectId,
                            ParentMasterItemId = item.ParentItemId,
                            UpdatedDate = DateTime.UtcNow,
                            CreatedDate = DateTime.UtcNow,
                            IsDefault = true
                        };
                        _repository.Add<ItemPOSDataLink>(newitemPOSDataLink);
                    }
                    _commonService.SetMenuNetworksDateUpdated(menuId, networkObjectId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.POSItemChangedT, posname, itemname);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, entityNameList: item.ItemName, operationDescription: string.Format(Constants.AuditMessage.POSItemChangedDetailsT, posname, posItem == null ? string.Empty : "-" + posItem.PLU, item.ItemName, menu.InternalName));
                    actionStatus = true;

                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemNotFoundT);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPOSItemChangedT, posname, itemname);
                Logger.WriteError(ex);
            }
            return actionStatus;

        }


        /// <summary>
        /// Check if PLU and ALt Combination is unique
        /// </summary>
        /// <param name="PLU"></param>
        /// <param name="AltPLU"></param>
        /// <param name="posdataId"></param>
        /// <param name="errorMsg"></param>
        /// <param name="entityWithError"></param>
        /// <param name="brandId"></param>
        /// <returns></returns>
        public bool IsPLUUniqueinBrand(int? PLU, string AltPLU, int posdataId, out string errorMsg, out string entityWithError, int? brandId = null)
        {
            bool retVal = true;
            errorMsg = string.Empty;
            entityWithError = "PLU";
            if (brandId.HasValue == false)
            {
                var positem = _repository.FindOne<POSData>(x => x.POSDataId == posdataId);
                brandId = positem == null ? 0 : positem.NetworkObjectId;
            }

            var positems = _repository.Find<POSData>(x => x.NetworkObjectId == brandId && (x.PLU == PLU || string.Compare(x.AlternatePLU, AltPLU, true) == 0)).ToList();
            if (PLU.HasValue == false && string.IsNullOrEmpty(AltPLU) == true)
            {
                retVal = false;
                entityWithError = "PLU";
                errorMsg = Constants.StatusMessage.ErrMasterItemPLUAltIdEmptyT;

            }
            //For PLUNumber = 0 or empty PLU, check if Item exists in database by same ALT.
            else if (PLU == 0 || PLU.HasValue == false)
            {
                var existingItemWithSameALT = positems.Where(e => string.Compare(e.AlternatePLU, AltPLU, true) == 0 && !string.IsNullOrEmpty(AltPLU) && (e.PLU == 0 || e.PLU.HasValue == false) && (posdataId == 0 || e.POSDataId != posdataId)).FirstOrDefault();
                if (null != existingItemWithSameALT)
                {
                    retVal = false;
                    entityWithError = "AlternatePLU";
                    if (PLU == 0)
                    {
                        errorMsg = Constants.StatusMessage.ErrMasterItemPLUandALTIdAlreadyExistsT;
                    }
                    else
                    {
                        errorMsg = Constants.StatusMessage.ErrMasterItemALTIdAlreadyExistsT;
                    }
                }
            }
            else //For NonZero PLUNumbers, check if Item exists in database by same PLUNumber, ALT number.
            {
                //validate only PLU if alt number is empty. Else validate the combination og at number and plu
                if (string.IsNullOrWhiteSpace(AltPLU))
                {
                    //PLU Number
                    var existingItemPLU = positems.Where(e => e.PLU == PLU && string.IsNullOrWhiteSpace(e.AlternatePLU) && (posdataId == 0 || e.POSDataId != posdataId)).FirstOrDefault();
                    if (null != existingItemPLU)
                    {
                        retVal = false;
                        errorMsg = Constants.StatusMessage.ErrMasterItemPLUAlreadyExistsT;
                    }
                }
                else
                {
                    //ALT number.
                    var existingItemNameALT = positems.Where(e => e.PLU == PLU && string.Compare(e.AlternatePLU, AltPLU, true) == 0 && !string.IsNullOrWhiteSpace(e.AlternatePLU) && (posdataId == 0 || e.POSDataId != posdataId)).FirstOrDefault();
                    if (null != existingItemNameALT && existingItemNameALT.PLU == PLU)
                    {
                        retVal = false;
                        entityWithError = "AlternatePLU";
                        errorMsg = Constants.StatusMessage.ErrMasterItemPLUandALTIdAlreadyExistsT;
                    }
                }
            }

            return retVal;
        }

        ///// <summary>
        ///// Maps items to POS data for a given site
        ///// </summary>
        ///// <param name="networkObjectId">Site id for which mapping is to be fone</param>
        ///// <param name="mappedIds">List of item and operational pos data ids that will be mapped</param>
        ///// <returns>True if successfull. LastActionResult contains description of error</returns>
        //public bool MapPOSData(int networkObjectId, int itemId, int odsPOSId, int MenuId)
        //{
        //    bool retVal = false;
        //    _lastActionResult = string.Empty;

        //    try
        //    {
        //        _context.Configuration.AutoDetectChangesEnabled = false;
        //        _context.Configuration.ValidateOnSaveEnabled = false;

        //        // get associated item
        //        var item = _repository.GetQuery<Item>(i => i.ItemId == itemId)
        //            .Include("POSDatas")
        //            .FirstOrDefault();

        //        // get all associated oprn pos data 
        //        var odsPOSData = _odsRepository.GetQuery<ODSPOSData>(p => p.POSDataId == odsPOSId)
        //            .FirstOrDefault();

        //        if (item != null && odsPOSData != null)
        //        {
        //            //Choose by selected Menu
        //            var menu = _repository.GetQuery<Menu>(m => m.MenuId == MenuId)
        //                .Include("MenuNetworkObjectLinks")
        //                .FirstOrDefault();

        //            // check if this site + menu has link already
        //            var menuNetObjectLink = menu.MenuNetworkObjectLinks.Where(l => l.NetworkObjectId == networkObjectId).FirstOrDefault();
        //            if (menuNetObjectLink == null)
        //            {
        //                // create a link
        //                menuNetObjectLink = new MenuNetworkObjectLink
        //                {
        //                    NetworkObjectId = networkObjectId,
        //                    MenuId = menu.MenuId, 
        //                    IsPOSMapped = true,
        //                    LastUpdatedDate = DateTime.UtcNow,
        //                    IsMenuOverriden = false
        //                };
        //                _repository.Add<MenuNetworkObjectLink>(menuNetObjectLink);
        //            }
        //            else
        //            {
        //                // update timestamp & flag
        //                menuNetObjectLink.LastUpdatedDate = DateTime.UtcNow;
        //                menuNetObjectLink.IsPOSMapped = true;
        //                _repository.Update<MenuNetworkObjectLink>(menuNetObjectLink);

        //                // delete existing POS Mapping
        //                var toDelete = item.ItemPOSDataLinks.Where(p => p.NetworkObjectId == menuNetObjectLink.NetworkObjectId).FirstOrDefault();
        //                if (toDelete != null)
        //                {
        //                    _repository.Delete<ItemPOSDataLink>(toDelete);
        //                }
        //            }

        //            // create a new POSData entity
        //            addNewPOSData(menuNetObjectLink, item, odsPOSData, 3, true);    // manual

        //            // commit
        //            _repository.UnitOfWork.SaveChanges();

        //            _lastActionResult = string.Format("Mapped PLU {0} to {1}", odsPOSData.PLU, item.DisplayName);                    
        //            _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, entityNameList: item.DisplayName, operationDescription: string.Format("Menu:{0}({1}) Mapped Item:{2}-{3} To ODS:{4}-{5}", menu.InternalName, menu.MenuId, item.ItemName, item.BasePLU, odsPOSData.ItemName, odsPOSData.PLU));
        //            retVal = true;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //    }
        //    return retVal;
        //}

        //public bool UnmapPOSData(int networkObjectId, int itemId, int MenuId)
        //{
        //    bool retVal = false;
        //    _lastActionResult = string.Empty;

        //    try
        //    {
        //        // get associated item
        //        var item = _repository.GetQuery<Item>(i => i.ItemId == itemId)
        //            .Include("POSDatas")
        //            .FirstOrDefault();

        //        //Choose by selected menu
        //        var menu = _repository.GetQuery<Menu>(m => m.MenuId == MenuId)
        //            .Include("MenuNetworkObjectLinks")
        //            .FirstOrDefault();

        //        // get site + menu link
        //        var menuNetObjectLink = menu.MenuNetworkObjectLinks.Where(l => l.NetworkObjectId == networkObjectId).FirstOrDefault();

        //        if (item != null && menuNetObjectLink != null)
        //        {
        //            // delete existing POS Mapping
        //            // TODO : Link POSData to Menu
        //            var toDelete = item.ItemPOSDataLinks.Where(p => p.NetworkObjectId == menuNetObjectLink.NetworkObjectId).FirstOrDefault();
        //            if (toDelete != null)
        //            {

        //                _repository.Delete<ItemPOSDataLink>(toDelete);
        //            }

        //            // update timestamp
        //            menuNetObjectLink.LastUpdatedDate = DateTime.UtcNow;
        //            _repository.Update<MenuNetworkObjectLink>(menuNetObjectLink);

        //            // update database
        //            _context.SaveChanges();

        //            retVal = true;
        //            _lastActionResult = string.Format("Unmapped {0}", item.DisplayName);
        //            _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, entityNameList: item.DisplayName, operationDescription: string.Format("Menu:{0}({1}) UnMapped Item :{2}-{3}", menu.InternalName, menu.MenuId, item.ItemName, item.BasePLU));
        //        }
        //        else
        //        {
        //            _lastActionResult = "Unmap failed, its an invalid item";
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //    }
        //    return retVal;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="networkObjectId"></param>
        ///// <param name="menuId"></param>
        ///// <returns></returns>
        //public bool AutoMapPOSByPLU(int networkObjectId, int menuId)
        //{
        //    bool retVal = false;
        //    try
        //    {
        //        // Code moved to commonPOSMapService
        //        retVal = _commonPOSMapService.AutoMapPOSByPLU(networkObjectId, menuId);
        //        if (retVal)
        //        {
        //            _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, operationDescription: _commonPOSMapService.LastActionResult);
        //        }
        //        _lastActionResult = _commonPOSMapService.LastActionResult;
        //        Logger.WriteInfo(_commonPOSMapService.LastActionResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //        _lastActionResult = "Unexpected error. Please try again";
        //    }
        //    return retVal;
        //}

        /////TODO - NewSchema changes Need Review
        ///// <summary>
        ///// Auto maps using another site as a reference.
        ///// </summary>
        ///// <param name="networkObjectId">Site to be automapped</param>
        ///// <param name="refNetworkObjectId">Site that will be used as a reference</param>
        ///// <returns>True if successfull. LastActionResult contains description of error</returns>
        //public bool AutoMapPOSByReferenceSite(int networkObjectId, int refNetworkObjectId, int menuId)
        //{
        //    Stopwatch sWatch = Stopwatch.StartNew();
        //    int dbErrorCode = 0;
        //    bool retVal = false;
        //    _lastActionResult = string.Empty;
        //    try
        //    {
        //        //Used Selected Menu
        //        var menu = _repository.GetQuery<Menu>(m => m.MenuId == menuId)
        //                            .Include("MenuNetworkObjectLinks")
        //                            .Include("MenuNetworkObjectLinks.NetworkObject")
        //                            .Include("MenuNetworkObjectLinks.NetworkObject.SiteInfoes")
        //                            .FirstOrDefault();

        //        var menuNetObjectLink = menu.MenuNetworkObjectLinks.Where(l => l.NetworkObjectId == networkObjectId).FirstOrDefault();
        //        var refSiteMenuNetObjectLink = menu.MenuNetworkObjectLinks.Where(l => l.NetworkObjectId == refNetworkObjectId && l.IsPOSMapped == true).FirstOrDefault();

        //        var site = _repository.GetQuery<SiteInfo>(s => s.NetworkObjectId == networkObjectId)
        //            .Include("NetworkObject").FirstOrDefault();

        //        // -- validations --
        //        if (site == null)
        //        {
        //            _lastActionResult = "Invalid site";
        //            return retVal;
        //        }

        //        List<ItemPOSDataLink> refSitePOSDataList = null;
        //        if (refSiteMenuNetObjectLink != null)
        //        {
        //            // get posdata from ref site
        //            refSitePOSDataList = _repository.GetQuery<ItemPOSDataLink>(pd => pd.NetworkObjectId == refSiteMenuNetObjectLink.NetworkObjectId && pd.Item != null)
        //                            .Include("Item")
        //                            .ToList();

        //            if (refSitePOSDataList == null || refSitePOSDataList.Count == 0)
        //            {
        //                _lastActionResult = "No POS mapping exists in reference site";
        //                return retVal;
        //            }
        //        }
        //        else
        //        {
        //            _lastActionResult = "No POS mapping exists in reference site";
        //            return retVal;
        //        }

        //        // get operational data list for given site
        //        var odsPOSDataList = _odsRepository.GetQuery<ODSPOSData>(p => p.SiteId == site.SiteId).ToList();

        //        if (odsPOSDataList == null || odsPOSDataList.Count == 0)
        //        {
        //            _lastActionResult = "No operational data found for site to map";
        //            return retVal;
        //        }
        //        var categoryMenuLinkIds = _repository.GetQuery<CategoryMenuLink>(l => l.MenuId == menuId).Select(x => x.CategoryId).ToList();

        //        // get all item ids for this menu (from categories & collections)
        //        var allItemIdsInMenu = (from co in _repository.GetQuery<CategoryObject>()
        //                                join cat in _repository.GetQuery<Category>(c => categoryMenuLinkIds.Contains(c.CategoryId))
        //                                                    on co.CategoryId equals cat.CategoryId
        //                                select co.ItemId)
        //                  .Concat(from ic in _repository.GetQuery<ItemCollection>(c => c.MenuId == menuId)
        //                          join ico in _repository.GetAll<ItemCollectionObject>() on ic.CollectionId equals ico.CollectionId
        //                          select ico.ItemId)
        //                  .Concat(from ic in _repository.GetQuery<ItemCollection>(c => c.MenuId == menuId)
        //                          join icl in _repository.GetAll<ItemCollectionLink>() on ic.CollectionId equals icl.CollectionId
        //                          select icl.ItemId)
        //                  .Distinct();

        //        _context.Configuration.AutoDetectChangesEnabled = false;
        //        _context.Configuration.ValidateOnSaveEnabled = false;
        //        _repository.UnitOfWork.BeginTransaction();

        //        List<Item> siteItemList = null;
        //        if (menuNetObjectLink == null)
        //        {
        //            // -- pos mapping not found for this site & menu combo --

        //            // create a link
        //            menuNetObjectLink = new MenuNetworkObjectLink
        //            {
        //                NetworkObjectId = networkObjectId,
        //                MenuId = menu.MenuId, 
        //                IsPOSMapped = true,
        //                LastUpdatedDate = DateTime.UtcNow,
        //                IsMenuOverriden = false
        //            };
        //            _repository.Add<MenuNetworkObjectLink>(menuNetObjectLink);
        //        }
        //        else
        //        {
        //            // -- pos mapping does exist for this site,menu combo --

        //            // delete the existing mapping for this site,menu combo
        //            // TODO : Link POSData to Menu
        //            _repository.Delete<ItemPOSDataLink>(pd => pd.NetworkObjectId == menuNetObjectLink.NetworkObjectId);
        //            _repository.Delete<POSData>(pd => pd.NetworkObjectId == menuNetObjectLink.NetworkObjectId);
        //            //var errorCode = new ObjectParameter("ErrorCode", typeof(Int32));
        //            //var rowsAffected = ((ProductMasterContext)_context).USP_DeletePOSDataBySite(menuNetObjectLink.MenuNetworkObjectLinkId, errorCode);
        //            //if (errorCode != null && errorCode.Value != null)
        //            //{
        //            //    dbErrorCode = Int32.Parse(errorCode.Value.ToString());
        //            //}

        //            // update link timestamp
        //            menuNetObjectLink.LastUpdatedDate = DateTime.UtcNow;
        //            menuNetObjectLink.IsPOSMapped = true;
        //            _repository.Update<MenuNetworkObjectLink>(menuNetObjectLink);
        //        }
        //        if (dbErrorCode == 0)
        //        {
        //            // get all the items from selected menu
        //            IQueryable<Item> itemListQ = from i in _repository.GetQuery<Item>() //i => i.PLU != null && i.PLU != 0)
        //                                         join ic in allItemIdsInMenu on i.ItemId equals ic
        //                                         select i;

        //            siteItemList = itemListQ.ToList();

        //            int mapCount = 0;
        //            foreach (var item in siteItemList)
        //            {
        //                // get pos data form ref site for this item
        //                var refSitePOSData = refSitePOSDataList.Where(pd => pd.ItemId == item.ItemId).FirstOrDefault();
        //                if (refSitePOSData != null && refSitePOSData.POSData != null)
        //                {
        //                    ODSPOSData matchedODSData = null;
        //                    if (refSitePOSData.POSData.PLU == 0)
        //                    {
        //                        // if the ref site item is mapped to zero plu item, then match ODS PLU and Name
        //                        matchedODSData = odsPOSDataList.Where(p => p.PLU == refSitePOSData.POSData.PLU && p.ItemName == refSitePOSData.POSData.ItemName).FirstOrDefault();
        //                    }
        //                    else
        //                    {
        //                        // match the ref site's PLU with ODS pos data
        //                        matchedODSData = odsPOSDataList.Where(p => p.PLU == refSitePOSData.POSData.PLU).FirstOrDefault();
        //                    }

        //                    if (matchedODSData != null)
        //                    {
        //                        // create a new POSData entity
        //                        addNewPOSData(menuNetObjectLink, item, matchedODSData, (int)refSitePOSData.MapTypeId, refSitePOSData.IsItemEnabled);
        //                        mapCount++;
        //                    }
        //                }
        //            }
        //            _repository.UnitOfWork.CommitTransaction();

        //            retVal = true;
        //            _lastActionResult = string.Format("Auto-mapped {0} items", mapCount);

        //            var detailMsg = string.Format("Auto-mapped {0} items in '{1}' using '{2}'. {3:0.00}s",
        //                mapCount,
        //                site.NetworkObject.Name,
        //                refSiteMenuNetObjectLink.NetworkObject.Name,
        //                sWatch.Elapsed.TotalSeconds);

        //            Logger.WriteInfo(detailMsg);
        //            _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, operationDescription: _lastActionResult, operationDetail: detailMsg);                    
        //        }
        //        else
        //        {
        //            Logger.WriteError(string.Format("SP call failed with code {0}", dbErrorCode));
        //            _lastActionResult = "Database error occurred. Please contact the administrator.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //    }

        //    // if there was an error, roll back the transaction
        //    if (_repository.UnitOfWork.IsInTransaction && retVal == false)
        //    {
        //        _repository.UnitOfWork.RollBackTransaction();
        //    }

        //    return retVal;

        //}

        ///// <summary>
        ///// Sets an item's status to enabled or disabled in the POSData table. This item status is per site.
        ///// </summary>
        ///// <param name="itemId">Item Id</param>
        ///// <param name="networkObjectId">Site's network object id</param>
        ///// <param name="menuId">Menu Id where the item is present</param>
        ///// <param name="isEnabled">status flag</param>
        ///// <returns>True if successfull. LastActionResult contains description of error</returns>
        //public bool SetItemStatus(int itemId, int networkObjectId, int menuId, bool isEnabled)
        //{
        //    bool retVal = false;
        //    _lastActionResult = string.Empty;

        //    try
        //    {
        //        _context.Configuration.AutoDetectChangesEnabled = false;
        //        _context.Configuration.ValidateOnSaveEnabled = false;

        //        var menu = _repository.GetQuery<Menu>(m => m.MenuId == menuId).FirstOrDefault();
        //        // get the pos data item
        //        var posData = _repository.GetQuery<ItemPOSDataLink>(p => p.ItemId == itemId && p.NetworkObjectId == networkObjectId)
        //            .FirstOrDefault();

        //        if (posData != null)
        //        {
        //            // update only if db has a different value
        //            if (posData.IsItemEnabled != isEnabled)
        //            {
        //                // update the db with new value
        //                posData.IsItemEnabled = isEnabled;
        //                _repository.Update<ItemPOSDataLink>(posData);

        //                // update link timestamp
        //                posData.UpdatedDate = DateTime.UtcNow;
        //                _commonService.SetMenuNetworksDateUpdated(menuId, networkObjectId, false);
        //                _lastActionResult = isEnabled ? string.Format("Enabled {0}", posData.Item.DisplayName) : string.Format("Disabled {0}", posData.Item.DisplayName);

        //                _repository.UnitOfWork.SaveChanges();
        //                _auditLogger.Write(OperationPerformed.Other, EntityType.POSMap, entityNameList: posData.Item.DisplayName, operationDescription: string.Format("Menu: {0}({1}), {3} Item: {2} ", menu.InternalName, menuId, posData.Item.ItemName, isEnabled ? "Enabled" : "Disabled"));      
        //            }
        //            else
        //            {
        //                _lastActionResult = string.Format("Item {0} was not updated.", posData.Item.DisplayName);
        //            }
        //            retVal = true;
        //        }
        //        else
        //        {
        //            _lastActionResult = "Cannot change status for unmapped items.";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //    }
        //    return retVal;
        //}

        ///// <summary>
        ///// Auto Map selected Menu in all the networks selected
        ///// </summary>
        ///// <param name="selectedMenuId"></param>
        ///// <param name="selectednetworkObjectIdList"></param>
        ///// <returns></returns>
        //public bool AutoMapMultipleSites(int selectedMenuId, List<int> selectednetworkObjectIdList)
        //{
        //    bool retVal = false;
        //    Stopwatch sWatch = Stopwatch.StartNew();
        //    int percentage = 1;

        //    try
        //    {
        //        updateStatus(percentage, "Starting AutoMap...");

        //        //get Site NetworkObjects under provided NetworkObjectIds
        //        var siteIds = _commonService.GetSiteIdsUnderNetworkObjects(selectednetworkObjectIdList);

        //        // get all the stores with this menu
        //        var networkObjects = _repository.GetQuery<NetworkObject>(m => siteIds.Contains(m.NetworkObjectId)).ToList();
        //        var menu = _repository.GetQuery<Menu> ( m => m.MenuId == selectedMenuId).FirstOrDefault();

        //        if (menu != null && networkObjects != null && networkObjects.Count > 0)
        //        {
        //            var siteCount = networkObjects.Count();
        //            int counter = 0;

        //            foreach (var n in networkObjects)
        //            {
        //                counter++;
        //                var IsMenuAvailable = checkIsMenuAvailableforNetwork(menu, n.NetworkObjectId);
        //                if (IsMenuAvailable)
        //                {
        //                    updateStatus(percentage, string.Format("Processing site '{0}' ( {1} of {2} )", n.Name, counter, siteCount), StatusTypes.Other, n.Name);

        //                    // refresh data access
        //                    _context = new ProductMasterContext();
        //                    _repository = new GenericRepository(_context);

        //                    var result = _commonPOSMapService.AutoMapPOSByPLU(n.NetworkObjectId, selectedMenuId);

        //                    percentage = (int)((float)counter / (float)siteCount * 100);
        //                    updateStatus(percentage, _commonPOSMapService.LastActionResult, result ? StatusTypes.Success : StatusTypes.Error, n.Name);
        //                }
        //                else
        //                {
        //                    percentage = (int)((float)counter / (float)siteCount * 100);
        //                    updateStatus(percentage, string.Format("Selected Menu is not available for site '{0}'", n.Name), StatusTypes.Info, n.Name);
        //                }
        //            }

        //            updateStatus(100, string.Format("Auto mapped {0} sites in {1:00}s", siteCount, sWatch.Elapsed.TotalSeconds));
        //        }
        //        else
        //        {
        //            updateStatus(100, "No sites found.", StatusTypes.Other);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //        updateStatus(-1, _lastActionResult);

        //    }
        //    return retVal;
        //}

        ///// <summary>
        ///// Auto maps given menu in all sites (that has atleast one item mapped already)
        ///// </summary>
        ///// <param name="selectedMenuId"></param>
        ///// <returns></returns>
        //public bool AutoMapAll(int selectedMenuId)
        //{
        //    bool retVal = false;
        //    Stopwatch sWatch = Stopwatch.StartNew();
        //    int percentage = 1;

        //    try
        //    {
        //        updateStatus(percentage, "Starting...");

        //        // get all the stores with this menu
        //        var networkObjects = _repository.GetQuery<MenuNetworkObjectLink>(m => m.MenuId == selectedMenuId)
        //            .Select(m => m.NetworkObject)
        //            .ToList();

        //        if (networkObjects != null && networkObjects.Count > 0)
        //        {
        //            var siteCount = networkObjects.Count();
        //            int counter = 0;

        //            foreach (var n in networkObjects)
        //            {
        //                // refresh data access
        //                _context = new ProductMasterContext();
        //                _repository = new GenericRepository(_context);

        //                counter++;
        //                updateStatus(percentage, string.Format("Processing site '{0}' ( {1} of {2} )", n.Name, counter, siteCount));

        //                _commonPOSMapService.AutoMapPOSByPLU(n.NetworkObjectId, selectedMenuId);

        //                percentage = (int)((float)counter / (float)siteCount * 100);
        //                updateStatus(percentage, _lastActionResult);
        //            }

        //            updateStatus(100, string.Format("Auto mapped {0} sites in {1:00}s", siteCount, sWatch.Elapsed.TotalSeconds));
        //        }
        //        else
        //        {
        //            updateStatus(100, "No sites found.");
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        Logger.WriteError(e);
        //        _lastActionResult = "Unexpected error. Please try again";
        //        updateStatus(-1, _lastActionResult);

        //    }
        //    return retVal;
        //}

        private bool checkIsMenuAvailableforNetwork(Menu selectedMenu, int netId)
        {
            var retVal = false;

            _ruleService.NetworkObjectId = netId;
            var parentNetworks = _ruleService.ParentNetworkNodesList;

            //If the networkObject of where Menu is created is one of the selected Network Parent, then Menu is available.
            if (parentNetworks.Contains(selectedMenu.NetworkObjectId))
            {
                retVal = true;
            }

            return retVal;
        }

        #region private methods

        private void updateStatus(int percentCompleted, string msg, StatusTypes status = StatusTypes.Other, string entity = null)
        {
            // make sure there is an event listener
            if (OnUpdateStatus == null)
            {
                return;
            }

            ProgressChangedEventArgs args = new ProgressChangedEventArgs(percentCompleted, msg, Enum.GetName(typeof(StatusTypes), status), entity);
            OnUpdateStatus(this, args);
        }

        //private void addNewPOSData(MenuNetworkObjectLink menuNetObjectLink, Item item, ODSPOSData odsPOSData, int mapTypeId, bool isItemEnabled)
        //{
        //    ItemPOSDataLink newPOSData = new ItemPOSDataLink();
        //    newPOSData.InjectFrom(odsPOSData);
        //    newPOSData.ItemId = item.ItemId;
        //    newPOSData.MapTypeId = (MapStatusTypes)mapTypeId;
        //    newPOSData.NetworkObjectId = menuNetObjectLink.NetworkObjectId;
        //    newPOSData.IsItemEnabled = isItemEnabled;

        //    _repository.Add<ItemPOSDataLink>(newPOSData);
        //}

        /// <summary>
        /// Map fields from POSMappedItemsResult to MenuItemModelPOS object
        /// </summary>
        /// <param name="dbItem">Database item object</param>
        /// <returns>local model object</returns>
        private MenuItemModelPOS copyToModel(POSMappedItemsResult dbItem, List<ItemPOSDataLink> itemPOSDataLinks, ODSPOSData odsDetail)
        {
            return new MenuItemModelPOS
                    {
                        Id = dbItem.ItemId,
                        ParentId = dbItem.ParentId,
                        ParentName = dbItem.ParentName,
                        ItemName = dbItem.ItemName,
                        DisplayName = dbItem.DisplayName,
                        IsAvailable = dbItem.IsAvailable,
                        MappedPLU = !dbItem.MappedPLU.HasValue ? null : dbItem.MappedPLU.Value.ToString(),
                        MappedPOSDataId = !dbItem.MappedPOSDataId.HasValue ? 0 : dbItem.MappedPOSDataId.Value,
                        //MappedPOSItemName = string.IsNullOrWhiteSpace(dbItem.MappedPOSItemName) ? string.Empty : dbItem.MappedPOSItemName,
                        //Description = dbItem.DisplayDescription,
                        //IsOverride = dbItem.IsOverride == 1 ? true : false,
                        POSDataList = _commonService.MapPOSDataDropdown(itemPOSDataLinks),
                        ODSData = mapODSData(odsDetail),
                        IsODSAvailable = odsDetail != null
                    };
        }

        private OperationalPOSDataModel mapODSData(ODSPOSData odsDetail)
        {
            var odsModel = new OperationalPOSDataModel();
            if (odsDetail != null)
            {
                odsModel.InjectFrom(odsDetail);
            }
            return odsModel;
        }

        private POSData mapPOSItem(POSDataModel posItemModel)
        {
            var posItem = new POSData();
            if (posItemModel != null)
            {
                posItem.InjectFrom(posItemModel);
                posItem.POSItemName = string.IsNullOrWhiteSpace(posItem.POSItemName) ? string.Empty : posItem.POSItemName.Trim();
                posItem.MenuItemName = string.IsNullOrWhiteSpace(posItem.MenuItemName) ? string.Empty : posItem.MenuItemName.Trim();
                posItem.AlternatePLU = string.IsNullOrWhiteSpace(posItem.AlternatePLU) ? string.Empty : posItem.AlternatePLU.Trim();
                posItem.ButtonText = string.IsNullOrWhiteSpace(posItem.ButtonText) ? string.Empty : posItem.ButtonText.Trim();
            }
            return posItem;
        }
        #endregion
    }

    public interface IPOSMapService
    {
        string LastActionResult { get; }
        int Count { get; set; }
        int Pages { get; set; }
        List<MenuItemModelPOS> GetMenuItems(int parentGroupId, int networkObjectId, int MenuId, KendoGridRequest grdRequest);
        List<OperationalPOSDataModel> GetOperationalPOSData(int networkObjectId);
        List<POSDataModel> GetPOSDataList(int networkObjectId, bool includeMappped = true, KendoGridRequest grdRequest = null);
        //bool MapPOSData(int networkObjectId, int itemId, int odsPOSId, int MenuId);
        //bool UnmapPOSData(int networkObjectId, int itemId, int MenuId);
        //bool AutoMapPOSByPLU(int networkObjectId, int MenuId);
        //bool AutoMapPOSByPLU(string siteId);
        //bool AutoMapPOSByReferenceSite(int networkObjectId, int refNetworkObjectId, int MenuId);
        //bool SetItemStatus(int itemId, int networkObjectId, int menuId, bool isEnabled);
        //bool AutoMapMultipleSites(int selectedMenuId, List<int> selectedNetworkIds);
        //bool AutoMapAll(int selectedMenuId);

        POSDataModel SavePOSItem(POSDataModel positem, out bool actionStatus);
        bool DeletePOSItem(int posItemId);
        bool AttachPOSItemToMasterItem(int posItemId, int itemId);
        bool RemovePOSItemFromMasterItem(int posItemId, int itemId);
        bool ChangePOSItemOfMenuItem(int posItemId, int itemId, int networkObjectId, int menuId);
    }
}