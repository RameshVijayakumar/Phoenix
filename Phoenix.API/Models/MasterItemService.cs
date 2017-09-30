using Microsoft.WindowsAzure;
using Phoenix.API;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using Omu.ValueInjecter;

namespace Phoenix.API.Models
{
    public class MasterItemService : IMasterItemService
    {
        private IRepository _repository;
        private DbContext _context;

        private AppConfiguration _config;
        private AuthenticationService _authenticationService;

        private string _lastActionResult;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        private string _clientId;
        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public MasterItemService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);

            _config = new AppConfiguration();
            if (_config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
            {
                _authenticationService = new AuthenticationService(_config);
            }
        }

        /// <summary>
        /// Fetches list of master items (parentid is not null and not overridden)
        /// for a given brand
        /// </summary>
        /// <param name="brandId">Ntworkobject Id of the brand</param>
        /// <param name="model">Return model</param>
        /// <returns></returns>
        public HttpStatusCode GetItems(string brandIdText, out MasterItemsModel model)
        {
            HttpStatusCode retCode = HttpStatusCode.OK;
            model = new MasterItemsModel();

            try
            {
                long brandIrisId = 0;
                if (long.TryParse(brandIdText, out brandIrisId))
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    var networkItem = _repository.GetQuery<NetworkObject>(x => x.IrisId == brandIrisId && x.NetworkObjectTypeId == NetworkObjectTypes.Brand && x.IsActive).FirstOrDefault();
                    if (networkItem != null)
                    {
                        if (_authenticationService.IsNetworkAccessible(ClientID, brandIrisId,out retCode))
                        {
                            // fetch from db
                            var items = _repository.GetQuery<Item>(i => i.NetworkObjectId == networkItem.NetworkObjectId/*brand.NetworkObjectId*/ && i.OverrideItemId == null && i.ParentItemId == null)
                                                                    .Include("ItemPOSDataLinks").Include("ItemPOSDataLinks.POSData").ToList();

                            // convert to model
                            var item = from i in items
                                       select new MasterItemModel
                                       {
                                           ItemId = i.IrisId,
                                           LastUpdated = i.UpdatedDate,
                                           MenuItemName = i.ItemName,
                                           POSItems = mapItemPOSLinktoPOSModelList(i.ItemPOSDataLinks.ToList())
                                       };

                            model.Items = new List<MasterItemModel>(item);
                            _lastActionResult = string.Format("Generated in {0:.00}s", sw.Elapsed.TotalSeconds);
                        }
                        else
                        {
                            _lastActionResult = _authenticationService.LastActionResult;
                        }
                    }
                    else
                    {
                        retCode = HttpStatusCode.BadRequest;
                        _lastActionResult = "Invalid brand-id";
                    }
                }
                else
                {
                    retCode = HttpStatusCode.BadRequest;
                    _lastActionResult = "Invalid id";

                }
            }
            catch (Exception e)
            {
                _lastActionResult = string.Format("Unexpcted error occurred. {0}", e.Message);
                retCode = HttpStatusCode.InternalServerError;
                //log exception
                Logger.WriteError(e);
            }
            return retCode;
        }
        /// <summary>
        /// Gets item details for a given item
        /// </summary>
        /// <param name="brandId"></param>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public HttpStatusCode GetItemDetails(long id, out MasterItemDetailModel model)
        {
            HttpStatusCode retCode = HttpStatusCode.OK;
            model = null;
            try
            {
                // get the item from db
                var item = _repository.GetQuery<Item>(i => i.IrisId == id).Include("ItemPOSDataLinks").Include("ItemPOSDataLinks.POSData").FirstOrDefault();
                if (item != null)
                {
                    var defaultPOS = item.ItemPOSDataLinks.Where(x => x.IsDefault).FirstOrDefault();
                    // map db entity to local model
                    model = new MasterItemDetailModel
                    {
                        ButtonText = item.ButtonText,
                        IsBeverage = item.IsBeverage,
                        IsEntreeAppetizer = item.IsEntreeApp,
                        IsForceRecipe = item.ForceRecipe,
                        IsModifier = defaultPOS != null && defaultPOS.POSData != null ?defaultPOS.POSData.IsModifier : false,
                        MenuItemName = defaultPOS != null && defaultPOS.POSData != null ? defaultPOS.POSData.MenuItemName : string.Empty,
                        IsPrintOnOrder = item.PrintOnOrder,
                        IsPrintOnReceipt = item.PrintOnReceipt,
                        IsPrintOnSameLine = item.PrintOnSameLine,
                        IsPrintRecipe = item.PrintRecipe,
                        ItemId = item.IrisId,
                        LastUpdated = item.UpdatedDate,
                        OrderName = item.ItemName,
                        POSItems = mapItemPOSLinktoPOSModelList(item.ItemPOSDataLinks.ToList()),
                        CookTime = item.DWItemCookTime != null ? item.DWItemCookTime.Description: string.Empty,
                        PrepOrder = item.DWItemLookup != null ? item.DWItemLookup.Sequence.ToString() + " - " + item.DWItemLookup.Name : string.Empty,
                        ItemCategorization = item.DWItemCategorization != null ? item.DWItemCategorization.Name : string.Empty,
                        ItemSubType = item.DWItemSubType != null ? item.DWItemSubType.Name : string.Empty,

                    };
                }
                else
                {
                    retCode = HttpStatusCode.NotFound;
                    _lastActionResult = "Item not found.";
                }
            }
            catch (Exception e)
            {
                _lastActionResult = string.Format("Unexpcted error occurred. {0}", e.Message);
                retCode = HttpStatusCode.InternalServerError;
                //log exception
                Logger.WriteError(e);
            }
            return retCode;
        }

        public HttpStatusCode GetItemMapping(long itemIrisId, long networkIrisId, string menuIrisIdsText, out MasterItemMappingModel model)
        {
            HttpStatusCode retCode = HttpStatusCode.OK;
            model = null;
            try
            {

                // locate the item
                var item = _repository.GetQuery<Item>(i => i.IrisId == itemIrisId).FirstOrDefault();
                if (item != null)
                {

                    model = new MasterItemMappingModel
                    {
                        ItemId = item.IrisId,
                        //PLU = item.BasePLU,
                        Mappings = new List<MasterItemMap>()
                    };

                    //get all overrides of the item to find mapping
                    var overridenItemIds = _repository.GetQuery<Item>(i => i.OverrideItemId == item.ItemId).Select(x => x.ItemId).ToList();
                    var itemIdsToSearch = new List<int>();
                    itemIdsToSearch.Add(item.ItemId);
                    if (overridenItemIds != null)
                    {
                        itemIdsToSearch.AddRange(overridenItemIds);
                    }

                    // get all menus liked to this networkobject site
                    var menuSiteLinksQuery = _repository.GetQuery<MenuNetworkObjectLink>(l => l.NetworkObject != null && l.NetworkObject.IrisId == networkIrisId)
                        .Include("Menu").Include("NetworkObject").Include("NetworkObject.SiteInfoes");

                    // split menu ids from text
                    List<long> menuIds = null;
                    if (!string.IsNullOrEmpty(menuIrisIdsText))
                    {
                        menuIds = menuIrisIdsText.Split(',').Select(n => long.Parse(n)).ToList();
                        if (menuIds.Count > 0)
                        {
                            // filter menu ids 
                            menuSiteLinksQuery = _repository.GetQuery<MenuNetworkObjectLink>(l => l.NetworkObject != null && l.Menu != null && l.NetworkObject.IrisId == networkIrisId && menuIds.Contains(l.Menu.IrisId))
                                .Include("Menu").Include("NetworkObject").Include("NetworkObject.SiteInfoes");
                        }
                    }

                    foreach (var menuSiteLink in menuSiteLinksQuery.ToList())
                    {
                        if (menuSiteLink != null && menuSiteLink.IsPOSMapped)
                        {
                            // get all pos mappings for item and site-menu link
                            var mapping = _repository.GetQuery<ItemPOSDataLink>(p => itemIdsToSearch.Contains(p.ItemId) && p.NetworkObjectId == menuSiteLink.NetworkObjectId)
                                .FirstOrDefault();

                            if (mapping != null && mapping.POSData != null)
                            {
                                MasterItemMap map = new MasterItemMap
                                {
                                    MapId = mapping.POSDataId.Value,
                                    MappedItem = mapping.POSData.POSItemName,
                                    MappedPLU = mapping.POSData.PLU,
                                    Menu = menuSiteLink.Menu.InternalName,
                                    Price = mapping.POSData.BasePrice,
                                    SiteGuid = menuSiteLink.NetworkObject.SiteInfoes.First().SiteId,
                                    SiteId = menuSiteLink.NetworkObject.IrisId
                                };
                                model.Mappings.Add(map);
                            }
                        }
                    }
                }
                else
                {
                    _lastActionResult = "Item not found";
                    retCode = HttpStatusCode.NotFound;
                }
            }
            catch (Exception e)
            {
                _lastActionResult = string.Format("Unexpcted error occurred. {0}", e.Message);
                retCode = HttpStatusCode.InternalServerError;
                //log exception
                Logger.WriteError(e);
            }
            return retCode;
        }

        private List<POSItemModel> mapItemPOSLinktoPOSModelList(List<ItemPOSDataLink> ItemPOSLinks)
        {
            List<POSItemModel> POSModels = new List<POSItemModel>();

            foreach (var link in ItemPOSLinks)
            {
                if (link.POSData != null)
                {
                    POSItemModel posItemModel = new POSItemModel();
                    posItemModel.InjectFrom(link.POSData);
                    posItemModel.IsDefault = link.IsDefault;

                    POSModels.Add(posItemModel);
                }
            }
            return POSModels;
        }
    }

    public interface IMasterItemService
    {
        string LastActionResult { get; }
        string ClientID { get; set; }
        HttpStatusCode GetItems(string brandId, out MasterItemsModel model);
        HttpStatusCode GetItemDetails(long id, out MasterItemDetailModel model);
        HttpStatusCode GetItemMapping(long itemId, long networkObjectId, string menuIdsText, out MasterItemMappingModel model);
    }
}