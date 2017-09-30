//using Phoenix.Common;
//using Phoenix.DataAccess;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Omu.ValueInjecter;

//namespace Phoenix.RuleEngine
//{
//    public class CommonPOSMapService : ICommonPOSMapService
//    {
//        private IRepository _repository;
//        private DbContext _context;
//        private IRepository _odsRepository;
//        private DbContext _odsContext;
//        private RuleService _ruleService;

//        private string _lastActionResult;
//        public string LastActionResult
//        {
//            get { return _lastActionResult; }
//        }

//        /// <summary>
//        /// Constructor where DBContext object and Repository object gets initialised.
//        /// </summary>
//        public CommonPOSMapService(RuleService.CallType callType)
//        {
//            //TODO: inject these interfaces
//            _context = new ProductMasterContext();
//            _repository = new GenericRepository(_context);
//            _odsContext = new PhoenixODSContext();
//            _odsRepository = new GenericRepository(_odsContext);
//            _ruleService = new RuleService(callType);
//        }

//        public CommonPOSMapService(DbContext context, IRepository repo,DbContext odscontext, IRepository odsrepo, RuleService.CallType callType)
//        {
//            _context = context;
//            _repository = repo;
//            _odsContext = odscontext;
//            _odsRepository = odsrepo;
//            _ruleService = new RuleService(callType);
//        }
//        /// <summary>
//        /// Auto map items by matching PLU for all menus in a given site
//        /// </summary>
//        /// <param name="siteId">Site guid</param>
//        /// <returns>True if successfull. LastActionResult contains description of error</returns>
//        public bool AutoMapPOSByPLU(string siteId)
//        {
//            bool retVal = false;
//            try
//            {
//                Logger.WriteInfo(string.Format("Received auto-map request for {0}", siteId));

//                var siteGuid = new Guid();
//                if (Guid.TryParse(siteId, out siteGuid))
//                {

//                    // get the networkObject
//                    var siteInfo = _repository.GetQuery<SiteInfo>(s => s.SiteId == siteGuid)
//                        .Include("NetworkObject")
//                        .FirstOrDefault();

//                    if (siteInfo != null)
//                    {
//                        string msg = null;

//                        // get all the menus for this site
//                        var menus = new List<Menu>();
//                        _ruleService.GetMenus(siteInfo.NetworkObjectId, menus);
//                        foreach (var menu in menus)
//                        {
//                            if (menu != null)
//                            {
//                                retVal = AutoMapPOSByPLU(siteInfo.NetworkObjectId, (int)menu.MenuId);
//                                msg = string.Format("{0} {1}", msg, _lastActionResult);
//                            }
//                        }
//                        if (string.IsNullOrEmpty(msg))
//                        {
//                            _lastActionResult = string.Format("No menus are linked to this site: {0} {1} {2}", siteInfo.NetworkObject.Name, siteInfo.StoreNumber, siteId); ;
//                        }
//                        else
//                        {
//                            _lastActionResult = string.Format("{0}. Site:{1} {2} {3}", msg, siteInfo.NetworkObject.Name, siteInfo.StoreNumber, siteId);
//                        }
//                    }
//                    else
//                    {
//                        _lastActionResult = string.Format("Invalid site id {0}", siteId);
//                    }
//                }
//                else
//                {
//                    _lastActionResult = string.Format("Invalid site id {0}", siteId);
//                }
//            }
//            catch (Exception e)
//            {
//                //Logger.WriteError(e);
//                _lastActionResult = "Unexpected error. Please try again";
//                throw e;
//            }
//            return retVal;
//        }

//        ///TODO - NewSchema changes Need Review
//        /// <summary>
//        /// Maps Item to POSData by matching item PLU with operational POSData's PLU
//        /// Items  wtih null PLU or zero PLU or mapped already are IGNORED
//        /// Already mapped items are ignored
//        /// </summary>
//        /// <param name="networkObjectId"></param>
//        /// <returns>True if successfull. LastActionResult contains description of error</returns>
//        public bool AutoMapPOSByPLU(int networkObjectId, int menuId)
//        {
//            Stopwatch sWatch = Stopwatch.StartNew();

//            bool retVal = false;
//            _lastActionResult = string.Empty;
//            try
//            {

//                //Use selected Menu
//                var menu = _repository.GetQuery<Menu>(m => m.MenuId == menuId)
//                    .Include("MenuNetworkObjectLinks")
//                    .FirstOrDefault();
//                var menuNetObjectLink = menu.MenuNetworkObjectLinks.Where(l => l.NetworkObjectId == networkObjectId).FirstOrDefault();
//                var categoryMenuLinkIds = _repository.GetQuery<CategoryMenuLink>(l => l.MenuId == menuId).Select(x => x.CategoryId).ToList();

//                var allItemIdsInMenu = (from co in _repository.GetQuery<CategoryObject>()
//                                        join cat in _repository.GetQuery<Category>(c => categoryMenuLinkIds.Contains(c.CategoryId))
//                                                            on co.CategoryId equals cat.CategoryId
//                                        select co.ItemId)
//                             .Concat(from ic in _repository.GetQuery<ItemCollection>(c => c.MenuId == menuId)
//                                     join ico in _repository.GetAll<ItemCollectionObject>() on ic.CollectionId equals ico.CollectionId
//                                     select ico.ItemId)
//                             .Concat(from ic in _repository.GetQuery<ItemCollection>(c => c.MenuId == menuId)
//                                     join icl in _repository.GetAll<ItemCollectionLink>() on ic.CollectionId equals icl.CollectionId
//                                     select icl.ItemId)
//                             .Distinct();

//                // get all the items (Menu selected from UI) 
//                // exclude null and zero plu -- leave those manual mappings
//                IQueryable<Item> itemLst = from i in _repository.GetQuery<Item>(i => i.BasePLU != null && i.BasePLU != 0)
//                                           join ic in allItemIdsInMenu on i.ItemId equals ic
//                                           select i;


//                List<Item> itemList = itemLst.ToList();

//                // get pos data 
//                List<ItemPOSDataLink> existingPOSDataLinks = null;

//                if (menuNetObjectLink == null)
//                {
//                    // no data
//                    existingPOSDataLinks = new List<ItemPOSDataLink>();

//                    // create a link
//                    menuNetObjectLink = new MenuNetworkObjectLink
//                    {
//                        NetworkObjectId = networkObjectId,
//                        MenuId = menu.MenuId,
//                        IsPOSMapped = true,
//                        LastUpdatedDate = DateTime.UtcNow,
//                        IsMenuOverriden = false
//                    };
//                    _repository.Add<MenuNetworkObjectLink>(menuNetObjectLink);
//                }
//                else
//                {
//                    existingPOSDataLinks = _repository.GetQuery<ItemPOSDataLink>(p => p.NetworkObjectId == menuNetObjectLink.NetworkObjectId).ToList();

//                    // update link timestamp
//                    menuNetObjectLink.LastUpdatedDate = DateTime.UtcNow;
//                    menuNetObjectLink.IsPOSMapped = true;
//                    _repository.Update<MenuNetworkObjectLink>(menuNetObjectLink);
//                }

//                // get site 
//                var site = _repository.GetQuery<SiteInfo>(s => s.NetworkObjectId == networkObjectId).FirstOrDefault();

//                // get operational data list for given site
//                var odsPOSDataList = _odsRepository.GetQuery<ODSPOSData>(p => p.SiteId == site.SiteId).ToList();

//                if (odsPOSDataList.Any())
//                {
//                    _context.Configuration.AutoDetectChangesEnabled = false;
//                    _context.Configuration.ValidateOnSaveEnabled = false;
//                    _repository.UnitOfWork.BeginTransaction();

//                    int mapsCreated = 0;
//                    int mapsUpdated = 0;
//                    int mapsRemoved = 0;
//                    foreach (var item in itemList)
//                    {
//                        ODSPOSData odsPOSData = null;

//                        // check if this item has POS mapped already
//                        //var existingPOSData = item.POSDatas.Where(p => p.MenuNetworkObjectLinkId == menuNetObjectLink.MenuNetworkObjectLinkId).FirstOrDefault();
//                        var existingPOSDataLink = existingPOSDataLinks.Where(i => i.ItemId == item.ItemId).FirstOrDefault();

//                        if (existingPOSDataLink != null && existingPOSDataLink.POSData != null)
//                        {
//                            if (existingPOSDataLink.POSData.PLU == 0)
//                            {
//                                // if PLU is zero, then compare name
//                                odsPOSData = odsPOSDataList.Where(p => p.PLU == existingPOSDataLink.POSData.PLU && p.ItemName == existingPOSDataLink.POSData.POSItemName).FirstOrDefault();

//                            }
//                            else
//                            {

//                                if (existingPOSDataLink.MapTypeId == MapStatusTypes.Auto)  //auto
//                                {
//                                    // get item' PLU in ODS
//                                    odsPOSData = odsPOSDataList.Where(p => p.PLU == item.BasePLU).FirstOrDefault();
//                                }
//                                else if (existingPOSDataLink.MapTypeId == MapStatusTypes.Manual) // manual
//                                {
//                                    // get existing map's PLU in ODS
//                                    odsPOSData = odsPOSDataList.Where(p => p.PLU == existingPOSDataLink.POSData.PLU).FirstOrDefault();
//                                }
//                            }

//                            if (odsPOSData != null)
//                            {
//                                // create a new POSData entity
//                                addNewPOSData(menuNetObjectLink, item, odsPOSData, (int)existingPOSDataLink.MapTypeId, existingPOSDataLink.IsItemEnabled);
//                                mapsUpdated++;
//                            }
//                            else
//                            {
//                                mapsRemoved++;
//                            }

//                            // delete existing mapping
//                            _repository.Delete<ItemPOSDataLink>(existingPOSDataLink);

//                        }
//                        else
//                        {
//                            // get item' PLU in ODS
//                            if (item.BasePLU == 0)
//                            {
//                                // for zero plu, include name in the search
//                                odsPOSData = odsPOSDataList.Where(p => p.PLU == item.BasePLU && p.ItemName == item.ItemName).FirstOrDefault();
//                            }
//                            else
//                            {
//                                odsPOSData = odsPOSDataList.Where(p => p.PLU == item.BasePLU).FirstOrDefault();
//                            }

//                            if (odsPOSData != null)
//                            {

//                                // create a new POSData entity
//                                addNewPOSData(menuNetObjectLink, item, odsPOSData, 2, true); // auto
//                                mapsCreated++;
//                            }
//                        }
//                    }

//                    // save to database
//                    _repository.UnitOfWork.CommitTransaction();

//                    _lastActionResult = string.Format("Auto mapped {0} (Updated:{1} New:{2} Deleted:{3}) in menu '{4}' {5:.}s", (mapsCreated + mapsUpdated), mapsUpdated, mapsCreated, mapsRemoved, menu.InternalName, sWatch.Elapsed.TotalSeconds);
//                    Logger.WriteInfo(string.Format("{0}. NOId {1} {2}s", _lastActionResult, networkObjectId, sWatch.Elapsed.TotalSeconds));
//                }
//                else
//                {
//                    _lastActionResult = "There is no ODSdata to map.";
//                }
//                retVal = true;
//            }
//            catch (Exception e)
//            {
//                //Logger.WriteError(e);
//                _lastActionResult = "Unexpected error. Please try again";
//                throw e;
//            }

//            return retVal;
//        }

//        public void addNewPOSData(MenuNetworkObjectLink menuNetObjectLink, Item item, ODSPOSData odsPOSData, int mapTypeId, bool isItemEnabled)
//        {
//            ItemPOSDataLink newPOSData = new ItemPOSDataLink();
//            newPOSData.InjectFrom(odsPOSData);
//            newPOSData.ItemId = item.ItemId;
//            newPOSData.MapTypeId = (MapStatusTypes)mapTypeId;
//            newPOSData.NetworkObjectId = menuNetObjectLink.NetworkObjectId;
//            newPOSData.IsItemEnabled = isItemEnabled;

//            _repository.Add<ItemPOSDataLink>(newPOSData);
//        }
//    }

//    public interface ICommonPOSMapService
//    {
//        string LastActionResult { get; }
//        bool AutoMapPOSByPLU(string siteId);
//        bool AutoMapPOSByPLU(int networkObjectId, int MenuId);
//    }
//}
