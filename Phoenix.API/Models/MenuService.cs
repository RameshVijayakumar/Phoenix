using Omu.ValueInjecter;
using Phoenix.API;
using Phoenix.DataAccess;
using Phoenix.Common;
using Phoenix.RuleEngine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Net;
using Microsoft.WindowsAzure;
using System.Diagnostics;

namespace Phoenix.API.Models
{
    //NOTE1: Change suggested by Prabhakar.Govindraj in email sent to Aman.Pahwa On 7/23/2013 (API Feedback). 
    //NOTE1: 1. Make all empty lists to null
    //"categories": null, (V2 in phase 1 is like this)

    /// <summary>
    /// Class where business layer for MenusController resides.
    /// </summary>
    public class MenuService : IMenuService
    {
        private IRepository _repository;
        private RuleService _menuRuleService;

        private IRepository _odsRepository;
        private DbContext _odsContext;


        private AppConfiguration _config;
        private AuthenticationService _authenticationService;

        private DbContext _context;
        private string _cdnEndpoint = null;
        private static string _IMAGES_CONTAINER = "mmsimages";
        private bool _POSMappingEnabled = false;
        private List<int> _parentNetworkObjectIds = new List<int>();
        private List<ODSPOSData> _odsDataList = null;
        private List<ItemPOSDataLink> _itemPOSDataList = null;

        //private bool _hasMappedChildren = false;
        private List<SchCycle> _scheduleCycleList = new List<SchCycle>();
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

        protected enum EntityType
        {
            Menus = 0,
            Categories = 1,
            Items = 2,
            Collections = 3,
            Assets = 4,
            Modifications = 5,
            Substitutions = 6,
            UpSells = 7,
            CrossSells = 8,
            Combos = 9,
            EndOfOrders = 10
        };

        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public MenuService()
        {
            //TODO: inject these interfaces
            _menuRuleService = new RuleService(RuleService.CallType.API);
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);

            _odsContext = new PhoenixODSContext();
            _odsRepository = new GenericRepository(_odsContext);

            _config = new AppConfiguration();
            if (_config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
            {
                _authenticationService = new AuthenticationService(_config);
            }
        }

        private IList<AssetCategoryLink> _assetCategoryLinkData = null;
        private IList<AssetCategoryLink> _assetCategoryLinkTable
        {
            get
            {
                if (_assetCategoryLinkData == null)
                {
                    _assetCategoryLinkData = _repository.GetQuery<AssetCategoryLink>().Include("Asset").ToList();
                }
                return _assetCategoryLinkData;
            }
        }

        private IList<AssetItemLink> _assetItemLinkData = null;
        private IList<AssetItemLink> _assetItemLinkTable
        {
            get
            {
                if (_assetItemLinkData == null)
                {
                    _assetItemLinkData = _repository.GetQuery<AssetItemLink>().Include("Asset").ToList();
                }
                return _assetItemLinkData;
            }
        }

        private IList<TagAssetLink> _tagAssetLinkData = null;
        private IList<TagAssetLink> _tagAssetLinkTable
        {
            get
            {
                if (_tagAssetLinkData == null)
                {
                    _tagAssetLinkData = _repository.GetQuery<TagAssetLink>().Include("Tag").ToList();
                }
                return _tagAssetLinkData;
            }
        }

        /// <summary>
        /// Gets the version of the menu
        /// </summary>
        /// <param name="id">Id of the menu for which version needs to be fetched.</param>
        /// <returns>MenuModel object</returns>
        public MenuModel GetMenuVersion(long id, out HttpStatusCode status)
        {
            MenuModel retModel = new MenuModel();
            status = HttpStatusCode.OK;
            try
            {
                var menu = _repository.GetQuery<Menu>(m => m.IrisId == id).FirstOrDefault();
                if (null != menu)
                {
                    // Get values like id and version.
                    retModel.InjectFrom(menu);
                }
                else
                {
                    status = HttpStatusCode.NotFound;
                    _lastActionResult = string.Format("No menu found with given id {0}", id);
                }
            }
            catch (Exception e)
            {
                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Unexpected error occured";
                //log exception
                Logger.WriteError(e);
            }
            return retModel;
        }

        /// <summary>
        /// To check whether site is valid or not get site details
        /// </summary>
        /// <param name="siteIrisId"></param>
        /// <param name="siteInfo"></param>
        /// <returns></returns>
        protected SiteInfoState GetSiteInfo(long siteIrisId, ref SiteInfo siteInfo)
        {
            if (siteIrisId != 0)
            {
                // get menu id for given site-id
                var network = _repository.GetQuery<NetworkObject>(s => s.IrisId == siteIrisId && s.NetworkObjectTypeId == NetworkObjectTypes.Site).Include("SiteInfoes").FirstOrDefault();
                if (network != null)
                {
                    siteInfo = network.SiteInfoes.FirstOrDefault();
                    if (siteInfo == null)
                        return SiteInfoState.InvalidId;

                    var status = HttpStatusCode.OK;
                    if (_authenticationService.IsNetworkAccessible(ClientID, siteIrisId,out status))
                    {
                        _POSMappingEnabled = checkSitePOSMapEnabled(siteInfo.NetworkObjectId);
                        return SiteInfoState.NoIssue;
                    }
                    else
                    {
                        return status == HttpStatusCode.Forbidden ? SiteInfoState.NotAccessible : SiteInfoState.Error;
                    }
                }
                else
                {
                    return SiteInfoState.NotFound;
                }
            }
            else
                return SiteInfoState.InvalidId;


        }

        /// <summary>
        /// check if Site has POSMapEnabled
        /// </summary>
        /// <param name="siteNetworkObjectId"></param>
        /// <returns></returns>
        private bool checkSitePOSMapEnabled(int siteNetworkObjectId)
        {
            var retVal = false;

            var brand = _menuRuleService.GetBrandNetworkObject(siteNetworkObjectId);
            if (brand != null)
            {
                //Check whether brand has POSEnabled
                var brandFeature = brand.FeaturesSet & (int)NetworkFeaturesSet.POSMapEnabled;
                if (brandFeature == (int)NetworkFeaturesSet.POSMapEnabled)
                {
                    retVal = true;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Gets the entire menu including the categories, items, collections, etc.
        /// </summary>
        /// <param name="siteIrisId"></param>
        /// <param name="version"></param>
        /// <param name="channel"></param>
        /// <returns>FullMenuModelsv2</returns>
        public dynamic GetFullMenu(long siteIrisId, string version, out HttpStatusCode status, string channel = null)
        {
            Stopwatch sw = Stopwatch.StartNew();
            status = HttpStatusCode.OK;
            dynamic retModels = null;
            dynamic retModel = null;
            switch (version)
            {
                case "V4":
                    retModels = new FullMenuModelsV4();
                    retModels.Version = "V4";
                    break;
                default:
                    break;
            }
            try
            {
                var menus = new List<Phoenix.DataAccess.Menu>();
                var networkObjId = -1;
                SiteInfo siteInfo = null;

                var retSIState = GetSiteInfo(siteIrisId, ref siteInfo);

                Logger.WriteInfo(string.Format("Time taken - To check IsNetworkAccessible - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
                sw = Stopwatch.StartNew();
                if (retSIState == SiteInfoState.NoIssue)
                {
                    networkObjId = siteInfo.NetworkObjectId;
                    _menuRuleService.NetworkObjectId = networkObjId;
                    
                    //get Target Channels
                    var clientTags = getTargetClientChannels();

                    // get all Menu links for this networkObject(Site)   
                    var sitemenuIds = new List<int>();
                    var siteMenusInfo = (_context as ProductMasterContext).udfGetSiteMenusInfo(networkObjId, null, true).ToList();
                    if (siteMenusInfo != null && siteMenusInfo.Count() > 0)
                    {
                        var distinctSiteInfo = siteMenusInfo.ElementAt(0);
                        foreach (var siteMenu in siteMenusInfo)
                        {
                            var menuTagString = string.IsNullOrWhiteSpace(siteMenu.Tags) ? string.Empty : siteMenu.Tags;
                            var menuTags = menuTagString.ToLower().Split(',').ToList();
                            //Send only Menu having Matching Tags with Target
                            if (siteMenu.MenuId.HasValue && checkTag(clientTags, menuTags))
                            {
                                sitemenuIds.Add(siteMenu.MenuId.Value);
                            }
                        }
                    }

                    //Fetch matching Menus Info
                    menus = _repository.GetQuery<Menu>(x => sitemenuIds.Contains(x.MenuId)).OrderBy(x => x.SortOrder).ToList();

                    if (menus != null && menus.Count > 0)
                    {
                        _scheduleCycleList = _menuRuleService.GetScheduleCycles(networkObjId);

                        //If a Channel exists apply filtering by Menu's internal name
                        if (channel != null)
                        {
                            var menuNames = channel.ToLower().Split(',');
                            menus = (from m in menus
                                     where menuNames.Contains(m.InternalName.ToLower())
                                     select m).ToList();
                        }

                        foreach (var menu in menus)
                        {
                            switch (version)
                            {
                                case "V4":
                                    retModel = new FullMenuModelV4();
                                    break;
                                default:
                                    break;
                            }
                            if (menu != null)
                            {
                                //Assign MenuId to ruleserivce to refresh the data
                                _menuRuleService.MenuId = menu.MenuId;
                                if (GetFullMenuModelV4(menu, retModel, networkObjId, siteInfo.NetworkObject.IrisId))
                                {
                                    retModels.Menus.Add(retModel);
                                }
                                else
                                {
                                    status = HttpStatusCode.NotFound;
                                    _lastActionResult = string.Format("No menu found for site {0} {1}, id {2}",
                                         siteInfo.NetworkObject.Name, siteInfo.StoreNumber, siteInfo.NetworkObject.IrisId);
                                }
                            }
                        }
                    }


                    // Please see the comment 'NOTE1' above in this file
                    if (retModels != null && retModels.Menus != null && retModels.Menus.Count == 0)
                    {
                        retModels.Menus = null;
                        status = HttpStatusCode.NotFound;
                        _lastActionResult = "No Menus found";
                    }
                }
                else
                {
                    switch (retSIState)
                    {
                        case SiteInfoState.InvalidId:
                            {
                                status = HttpStatusCode.BadRequest;
                                _lastActionResult = "Invalid site id";
                                break;
                            }
                        case SiteInfoState.NotFound:
                            {
                                status = HttpStatusCode.NotFound;
                                _lastActionResult = "No Site found";
                                break;
                            }
                        case SiteInfoState.NotAccessible:
                            {
                                status = HttpStatusCode.Forbidden;
                                _lastActionResult = "Access to this resource is forbidden";
                                break;
                            }
                        case SiteInfoState.Error:
                            {
                                status = HttpStatusCode.InternalServerError;
                                _lastActionResult = "Unexpected error occured.";
                                break;
                            }
                        default:
                            break;
                    }
                }

                Logger.WriteInfo(string.Format("Time taken - To get menu - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
            }
            catch (Exception e)
            {
                // Nullify the menus if there is any error.
                retModels.Menus = null;

                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Unexpected error occured.";
                //log exception
                Logger.WriteError(e);
            }
            return retModels;
        }
        
        #region V4 Methods

        /// <summary>
        /// Gets the entire menu in V4 structure including the categories, items, collections, etc.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="menuModel"></param>
        /// <param name="networkObjId"></param>
        /// <returns></returns>
        protected bool GetFullMenuModelV4(Phoenix.DataAccess.Menu menu, FullMenuModelV4 menuModel, int networkObjId, long siteIrisId)
        {
            bool blnValidResult = false;

            #region get pos data
            
            if (_POSMappingEnabled)
            {
                _odsDataList = _odsRepository.GetQuery<ODSPOSData>(p => p.IrisId == siteIrisId)
                    .ToList();
            }
            else
            {
                _odsDataList = new List<ODSPOSData>();
            }

            #endregion

            menuModel.MenuId = menu.MenuId;
            menuModel.InternalName = menu.InternalName;
            menuModel.IrisId = menu.IrisId;

            _parentNetworkObjectIds = _menuRuleService.GetNetworkParents(networkObjId);

            #region Get Item POS Data
            _itemPOSDataList = _repository.GetQuery<ItemPOSDataLink>(x => x.Item.MenuId == menu.MenuId && _parentNetworkObjectIds.Contains(x.NetworkObjectId)).Include("POSData").ToList();
            #endregion

            var children = getCategoryListV4(menu.MenuId, menu.MenuId, ref menuModel, EntityType.Menus, networkObjId);
            if (children != null && children.Any())
            {
                menuModel.Children.AddRange(children);
            }
            else
            {
                //Make all empty lists to null
                menuModel.Children = null;
            }
            //Make all empty lists to null
            if (menuModel.Categories != null && menuModel.Categories.Count == 0)
            {
                menuModel.Categories = null;
            }

            //Make all empty lists to null
            if (menuModel.Items != null && menuModel.Items.Count == 0)
            {
                menuModel.Items = null;
            }

            //Make all empty lists to null
            if (menuModel.Modifications != null && menuModel.Modifications.Count == 0)
            {
                menuModel.Modifications = null;
            }

            //Make all empty lists to null
            if (menuModel.Substitutions != null && menuModel.Substitutions.Count == 0)
            {
                menuModel.Substitutions = null;
            }

            //Make all empty lists to null
            if (menuModel.Upsells != null && menuModel.Upsells.Count == 0)
            {
                menuModel.Upsells = null;
            }

            //Make all empty lists to null
            if (menuModel.CrossSells != null && menuModel.CrossSells.Count == 0)
            {
                menuModel.CrossSells = null;
            }

            //Make all empty lists to null
            if (menuModel.Combos != null && menuModel.Combos.Count == 0)
            {
                menuModel.Combos = null;
            }

            //Make all empty lists to null
            if (menuModel.EndOfOrders != null && menuModel.EndOfOrders.Count == 0)
            {
                menuModel.EndOfOrders = null;
            }

            //Make all empty lists to null
            if (menuModel.Assets != null && menuModel.Assets.Count == 0)
            {
                menuModel.Assets = null;
            }

            menuModel.SpecialNotices = _menuRuleService.GetMenuSpecialNotices(menu.MenuId);

            blnValidResult = true;
            return blnValidResult;
        }

        /// <summary>
        /// Gets child categories under the category/Menu and add the categories to MenuModel.Categories
        /// </summary>
        /// <param name="parentId">parent Id(either MenuId or CategoryId)</param>
        /// <param name="menuId">Menu Id</param>
        /// <param name="menuModel">Menu model to find/add categories</param>
        /// <param name="parentType">Type of entity for which the list is requested (Category / Menu)</param>
        /// <param name="networkObjectId">NetworkObjectId</param>
        /// <returns>List of ChildModel objects</returns>
        private List<ChildModelV4> getCategoryListV4(int parentId, int menuId, ref FullMenuModelV4 menuModel, EntityType parentType, int networkObjectId = -1)
        {
            var catsInEntity = new List<ChildModelV4>();
            var categories = new List<Category>();
            if (parentType == EntityType.Categories)
            {
                categories = _menuRuleService.GetSubCategoryList(parentId, menuId);
            }
            else if (parentType == EntityType.Menus)
            {

                categories = _menuRuleService.GetCategoriesList(parentId);
            }

            if (categories != null && categories.Count > 0)
            {
                foreach (var category in categories)
                {
                    //These are always true if POSMappingEnabled is false. Other wise check whether to add the item to menu
                    bool isAnyValidItem = true;
                    bool isAnyValidSubCategory = true;

                    if (_POSMappingEnabled)
                    {
                        isAnyValidItem = isAnyValidSubCategory = false;
                    }
                    if (menuModel.Categories.Any(x => x.CategoryId == category.CategoryId) == false)
                    {
                        var childCategoryModel = new CategoryModelV4();

                        childCategoryModel.CategoryId = category.CategoryId;
                        childCategoryModel.IrisId = category.IrisId;
                        childCategoryModel.InternalName = category.InternalName;
                        childCategoryModel.DisplayName = category.DisplayName;
                        childCategoryModel.Schedule = getCatSchDetailListV4(category.CategoryId);
                        childCategoryModel.StartDate = category.StartDate;
                        childCategoryModel.EndDate = category.EndDate;
                        childCategoryModel.IsFeatured = category.IsFeatured;
                        childCategoryModel.ShowPrice = category.ShowPrice;
                        childCategoryModel.DeepLinkId = category.DeepLinkId;
                        childCategoryModel.CategoryType = ((CategoryTypes)category.CategoryTypeId).ToString();

                        var items = getItemListV4(category.OrgCategoryId, menuId, networkObjectId, ref menuModel, EntityType.Categories);
                        if (items != null && items.Count > 0)
                        {
                            isAnyValidItem = true;
                            childCategoryModel.Children.AddRange(items);
                        }
                        var childCategories = getCategoryListV4(category.OrgCategoryId, menuId, ref menuModel, EntityType.Categories, networkObjectId);
                        if (childCategories != null && childCategories.Any())
                        {
                            isAnyValidSubCategory = true;
                            childCategoryModel.Children.AddRange(childCategories);
                        }
                        var assets = getCategoryAssetListV4(category, ref menuModel);
                        if (assets != null && assets.Any())
                        {
                            childCategoryModel.Children.AddRange(assets);
                        }

                        //Make all empty lists to null
                        if (childCategoryModel.Children != null && childCategoryModel.Children.Count == 0)
                        {
                            childCategoryModel.Children = null;
                        }

                        if (isAnyValidItem || isAnyValidSubCategory)
                        {
                            childCategoryModel.Index = menuModel.Categories.Count();
                            menuModel.Categories.Add(childCategoryModel);
                        }
                    }

                    // add the index and type to children
                    if (menuModel.Categories.Any(x => x.CategoryId == category.CategoryId))
                    {
                        catsInEntity.Add(new ChildModelV4
                        {
                            Index = menuModel.Categories.Where(x => x.CategoryId == category.CategoryId).FirstOrDefault().Index,
                            Type = EntityType.Categories.ToString().ToLower()
                        });
                    }
                }
            }

            //Make all empty lists to null
            if (catsInEntity != null && catsInEntity.Count == 0)
            {
                catsInEntity = null;
            }
            return catsInEntity;
        }

        /// <summary>
        ///  Gets child items/prependItem under the category/item/collection and add the item to MenuModel.Items
        /// </summary>
        /// <param name="originalParentId">parent id (Either item i</param>
        /// <param name="menuId">Menu Id</param>
        /// <param name="menuModel">Menu model to find/add Items</param>
        /// <param name="parentType">Type of entity for which the list is requested (Category / Item / Collection)</param>
        /// <param name="networkObjectId">NetworkObjectId</param>
        /// <returns>List of ChildModel objects</returns>
        private dynamic getItemListV4(int originalParentId, int menuId, int networkObjectId, ref FullMenuModelV4 menuModel, EntityType parentType, CollectionTypeNames? parentSubType = null)
        {
            ////These are always true if POSMappingEnabled is false. Other wise check whether to add the item to menu
            //isAnyValidItem = _POSMappingEnabled ? false : true;

            dynamic itemsInEntity = null;
            var itemList = new List<Item>();
            if (parentType == EntityType.Categories)
            {
                itemsInEntity = new List<ChildModelV4>();
                itemList = _menuRuleService.GetItemList(originalParentId, menuId);
            }
            else if (parentType == EntityType.Collections)
            {
                itemsInEntity = new List<ItemChildModelV4>();
                itemList = _menuRuleService.GetCollectionItemList(originalParentId, menuId);
            }
            else if (parentType == EntityType.Items)
            {
                itemsInEntity = new List<ChildModelV4>();
                itemList = _menuRuleService.GetPrependItemList(originalParentId, menuId);
            }

            if (itemList != null)
            {
                foreach (var item in itemList)
                {
                    if (menuModel.Items.Any(x => x.ItemId == item.ItemId) == false)
                    {

                        var itemModel = new ItemModelV4();

                        itemModel.IrisId = item.IrisId;
                        itemModel.ItemId = item.ItemId;
                        itemModel.ItemName = item.ItemName;
                        itemModel.DisplayName = item.DisplayName;
                        itemModel.DisplayDescription = item.DisplayDescription;
                        itemModel.SendHierarchy = item.IsSendHierarchy;
                        itemModel.IsTopLevel = item.IsTopLevel;
                        itemModel.Combo = item.IsCombo;
                        itemModel.QuickOrder = item.QuickOrder;
                        itemModel.IsAvailable = item.IsAvailable;
                        //itemModel.ShowPrice = item.ShowPrice;
                        itemModel.IsAlcohol = item.IsAlcohol;
                        itemModel.DeepLinkId = item.DeepLinkId;
                        //itemModel.POSItemName = item.MenuItemName;
                        itemModel.ModifierFlag = item.ModifierFlag == null ? null : MapMFtoMFV3(item.ModifierFlag);
                        //bug 4729
                        itemModel.IsIncluded = item.IsIncluded;
                        itemModel.IsFeatured = item.IsFeatured;
                        itemModel.Feeds = item.Feeds;
                        itemModel.StartDate = item.StartDate == null ? string.Empty : item.StartDate.Value.ToString();
                        itemModel.EndDate = item.EndDate == null ? string.Empty : item.EndDate.Value.ToString();
                        // itemModel.Calories = item.Calories;
                        
                        // Work Item 193:When building the menu output for online ordering, use internal name from POS instead of menu template
                        itemModel.ItemName = item.ItemName;

                        var nutrition = item.Nutrition;
                        itemModel.Calories = (nutrition == null) ? null : (int?)nutrition.NetCalories;

                        if (item.StartDate != null)
                        {
                            itemModel.StartDate = item.StartDate.Value.ToString();
                        }

                        if (item.EndDate != null)
                        {
                            itemModel.EndDate = item.EndDate.Value.ToString();
                        }


                        //Check for the POS Mapping 
                        // get the POS applicable(pick last parent value) for this network - 
                        var posDataLink = _itemPOSDataList.Any(x => x.ItemId == item.ItemId && x.IsDefault && _parentNetworkObjectIds.Contains(x.NetworkObjectId)) ? _itemPOSDataList.Where(x => x.ItemId == item.ItemId && x.IsDefault && _parentNetworkObjectIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault() : null;
                        //var posDataLink = item.ItemPOSDataLinks.Any(x => x.IsDefault && _parentNetworkObjectIds.Contains(x.NetworkObjectId)) ? item.ItemPOSDataLinks.Where(x => x.IsDefault && _parentNetworkObjectIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault() : null;
                        ODSPOSData itemODSData = null;

                        // *do not* include items that are disabled
                        // if item is not enabled then this item is not to be included in the to-be returned Itemlist 
                        // PrependItems are always available
                        if (item.IsAvailable || parentType == EntityType.Items)
                        {
                            if (posDataLink != null && posDataLink.POSData != null)
                            {
                                itemModel.PLU = posDataLink.POSData.PLU;
                                itemModel.AlternatePLU = posDataLink.POSData.AlternatePLU;
                                itemModel.BasePrice = posDataLink.POSData.BasePrice.HasValue ? posDataLink.POSData.BasePrice.Value : 0;// (float)categoryObject.Item.BasePrice;
                                itemModel.IsAlcohol = posDataLink.POSData.IsAlcohol;
                                itemModel.IsModifier = posDataLink.POSData.IsModifier;

                                // Work Item 193:When building the menu output for online ordering, use internal name from POS instead of menu template
                                itemModel.POSItemName = posDataLink.POSData.POSItemName;

                                if (itemModel.PLU.HasValue && itemModel.PLU == 0)
                                {
                                    // for zero plu, include name in the search
                                    itemODSData = _odsDataList.Where(p => p.PLU == itemModel.PLU && p.ItemName == itemModel.POSItemName).FirstOrDefault();
                                }
                                else
                                {
                                    itemODSData = _odsDataList.Where(x => itemModel.PLU.HasValue && x.PLU == itemModel.PLU.Value).FirstOrDefault();
                                }
                            }


                            // Get prependItems for this item
                            itemModel.PrependItems = getItemListV4(item.OrgItemId, menuId, networkObjectId, ref menuModel, EntityType.Items);


                            var hasCollections = false;
                            var collections = getCollectionListV4(item.OrgItemId, menuId, networkObjectId, ref menuModel);
                            if (collections != null)
                            {
                                hasCollections = true;
                                itemModel.Children.AddRange(collections);
                            }
                            else if (itemModel.PLU != null)
                            {// Include if Item is Mapped but has no collections
                                hasCollections = true;
                            }

                            // *do not* include items that have ODS data in the menu template but are unmapped.
                            // *do* include items with PLU == 0 if they are unmapped or mapped
                            if ((_POSMappingEnabled == false) || (hasCollections || parentType != EntityType.Categories) && ((itemModel.PLU != null && itemODSData == null) == false || (itemModel.PLU == 0)))
                            //if (isMappedItem || hasCollections || ((itemModel.PLU != null && itemODSData == null) == false || (itemModel.PLU == 0)))
                            {   
                                if(itemODSData != null)
                                {
                                    if (parentType == EntityType.Collections && parentSubType == CollectionTypeNames.Combo)                                
                                    {
                                        itemModel.BasePrice = itemODSData.ComboPrice;
                                    }
                                    else
                                    {
                                        itemModel.BasePrice = itemODSData.BasePrice;
                                    }
                                    var taxBucketList = getTaxBucketList(itemODSData.TaxTypeIds);
                                    itemModel.TaxTypeId = taxBucketList;
                                }

                                //Overriden price takes higher priority than other prices
                                itemModel.IsPriceOverridden = item.IsPriceOverriden;
                                itemModel.BasePrice = item.IsPriceOverriden ? item.OverridenPrice : itemModel.BasePrice;

                                //Get Schedule for this item
                                itemModel.Schedule = getItemSchDetailListV4(item.ItemId);

                                var assets = getItemAssetListV4(item, ref menuModel);
                                if (assets != null)
                                {
                                    itemModel.Children.AddRange(assets);
                                }

                                //Make all empty lists to null
                                if (itemModel.Children != null && itemModel.Children.Count == 0)
                                {
                                    itemModel.Children = null;
                                }
                                // Add itemModel to the Menu.

                                //set Index of new item
                                itemModel.Index = menuModel.Items.Count();
                                menuModel.Items.Add(itemModel);
                            }
                        }
                    }

                    // add the index and type to children
                    if (menuModel.Items.Any(x => x.ItemId == item.ItemId))
                    {
                        if (parentType == EntityType.Collections)
                        {
                            itemsInEntity.Add(new ItemChildModelV4
                            {
                                Index = menuModel.Items.Where(x => x.ItemId == item.ItemId).FirstOrDefault().Index,
                                AutoSelect = item.IsAutoSelect,
                                Type = EntityType.Items.ToString().ToLower()
                            });
                        }
                        else
                        {
                            itemsInEntity.Add(new ChildModelV4
                            {
                                Index = menuModel.Items.Where(x => x.ItemId == item.ItemId).FirstOrDefault().Index,
                                Type = EntityType.Items.ToString().ToLower()
                            });
                        }
                    }
                }
            }
            //Make all empty lists to null
            if (itemsInEntity != null && itemsInEntity.Count == 0)
            {
                itemsInEntity = null;
            }
            return itemsInEntity;
        }

        /// <summary>
        /// Gets child collections under the item and add the collection to specific collection type of List in MenuModel
        /// </summary>
        /// <param name="originalItemId">parent id</param>
        /// <param name="menuId">Menu Id</param>
        /// <param name="networkObjectId">NetworkObjectId</param>
        /// <param name="menuModel">Menu model to find/add Collections</param>
        /// <returns>List of ChildModel objects</returns>
        private List<ChildModelV4> getCollectionListV4(int originalItemId, int menuId, int networkObjectId, ref FullMenuModelV4 menuModel)
        {
            var collectionsInItem = new List<ChildModelV4>();
            var itemCollectionList = _menuRuleService.GetCollectionList(originalItemId, menuId);
            try
            {
                if (itemCollectionList != null && itemCollectionList.Count() > 0)
                {
                    foreach (var itemCollection in itemCollectionList)
                    {
                        //NOTE: collections are split based on their type, separate  list is maintained fot each collection type. Hence search specific list to get the collection
                        var listToSearch = new List<CollectionModelV4>();
                        switch (itemCollection.CollectionTypeId)
                        {
                            case CollectionTypeNames.Modification:
                                listToSearch = menuModel.Modifications;
                                break;
                            case CollectionTypeNames.Substitution:
                                listToSearch = menuModel.Substitutions;
                                break;
                            case CollectionTypeNames.UpSell:
                                listToSearch = menuModel.Upsells;
                                break;
                            case CollectionTypeNames.CrossSell:
                                listToSearch = menuModel.CrossSells;
                                break;
                            case CollectionTypeNames.Combo:
                                listToSearch = menuModel.Combos;
                                break;
                            case CollectionTypeNames.EndOfOrder:
                                listToSearch = menuModel.EndOfOrders;
                                break;

                        }

                        if (listToSearch.Any(x => x.CollectionId == itemCollection.CollectionId) == false)
                        {
                            var collectionModel = new CollectionModelV4();

                            collectionModel.IrisId = itemCollection.IrisId;
                            collectionModel.CollectionId = itemCollection.CollectionId;
                            collectionModel.Type = itemCollection.CollectionTypeId.ToString();
                            collectionModel.InternalName = itemCollection.InternalName;
                            collectionModel.DisplayName = itemCollection.DisplayName;
                            collectionModel.Mandatory = itemCollection.IsMandatory;
                            collectionModel.Propagate = itemCollection.IsPropagate;
                            collectionModel.IsVisibleToGuest = itemCollection.IsVisibleToGuest;
                            collectionModel.ReplacesItem = itemCollection.ReplacesItem;
                            collectionModel.ShowPrice = itemCollection.ShowPrice;
                            collectionModel.MinQuantity = itemCollection.MinQuantity;
                            collectionModel.MaxQuantity = itemCollection.MaxQuantity;
                            
                            //Bug#7236 - All Children unmapped, parent still showing in JSON
                            //if there are no items under the collection then do not include that collection
                            //even if there are not items donot add collection
                            var itemsInCollection = getItemListV4(itemCollection.OrgCollectionId, menuId, networkObjectId, ref menuModel, EntityType.Collections, itemCollection.CollectionTypeId);
                            if (itemsInCollection != null && itemsInCollection.Count > 0)
                            {
                                collectionModel.Children.AddRange(itemsInCollection);

                                //NOTE: collections are split based on their type, separate  list is maintained fot each collection type. Hence add the model to specific type of list
                                switch (itemCollection.CollectionTypeId)
                                {
                                    case CollectionTypeNames.Modification:
                                        collectionModel.Index = menuModel.Modifications.Count();
                                        menuModel.Modifications.Add(collectionModel);
                                        break;
                                    case CollectionTypeNames.Substitution:
                                        collectionModel.Index = menuModel.Substitutions.Count();
                                        menuModel.Substitutions.Add(collectionModel);
                                        break;
                                    case CollectionTypeNames.UpSell:
                                        collectionModel.Index = menuModel.Upsells.Count();
                                        menuModel.Upsells.Add(collectionModel);
                                        break;
                                    case CollectionTypeNames.CrossSell:
                                        collectionModel.Index = menuModel.CrossSells.Count();
                                        menuModel.CrossSells.Add(collectionModel);
                                        break;
                                    case CollectionTypeNames.Combo:
                                        collectionModel.Index = menuModel.Combos.Count();
                                        menuModel.Combos.Add(collectionModel);
                                        break;
                                    case CollectionTypeNames.EndOfOrder:
                                        collectionModel.Index = menuModel.EndOfOrders.Count();
                                        menuModel.EndOfOrders.Add(collectionModel);
                                        break;
                                }
                            }
                        }

                        // add the index and type to children
                        //Make all empty lists to null
                        //Bug#7236 - All Children unmapped, parent still showing in JSON
                        //if there are no items under the collection then do not include that collection
                        //even if there are not items donot add collection
                        //NOTE: collections are split based on their type, separate  list is maintained fot each collection type. Hence search specific list to get the collection
                        var collectionToSearch = new CollectionModelV4();
                        var typename = EntityType.Collections; 
                        switch (itemCollection.CollectionTypeId)
                        {
                            case CollectionTypeNames.Modification:
                                collectionToSearch = menuModel.Modifications.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault(); ;
                                typename = EntityType.Modifications;
                                break;
                            case CollectionTypeNames.Substitution:
                                collectionToSearch = menuModel.Substitutions.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault();
                                typename = EntityType.Substitutions;
                                break;
                            case CollectionTypeNames.UpSell:
                                collectionToSearch = menuModel.Upsells.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault();
                                typename = EntityType.UpSells;
                                break;
                            case CollectionTypeNames.CrossSell:
                                collectionToSearch = menuModel.CrossSells.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault();
                                typename = EntityType.CrossSells;
                                break;
                            case CollectionTypeNames.Combo:
                                collectionToSearch = menuModel.Combos.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault();
                                typename = EntityType.Combos;
                                break;
                            case CollectionTypeNames.EndOfOrder:
                                collectionToSearch = menuModel.EndOfOrders.Where(x => x.CollectionId == itemCollection.CollectionId).FirstOrDefault();
                                typename = EntityType.EndOfOrders;
                                break;

                        }

                        if (collectionToSearch != null && collectionToSearch.Children.Any(x => x.Type == EntityType.Items.ToString().ToLower()))
                        {
                            collectionsInItem.Add(new ChildModelV4
                            {
                                Index = collectionToSearch.Index,
                                Type = typename.ToString().ToLower()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            //Make all empty lists to null
            if (collectionsInItem != null && collectionsInItem.Count == 0)
            {
                collectionsInItem = null;
            }
            return collectionsInItem;
        }

        /// <summary>
        /// Gets child assets of an Item  and add the asset to MenuModel.Assets
        /// </summary>
        /// <param name="item"></param>
        /// <param name="menuModel"></param>
        /// <returns>List of ChildModel objects</returns>
        private List<ChildModelV4> getItemAssetListV4(Item item, ref FullMenuModelV4 menuModel)
        {
            var assetsInitem = new List<ChildModelV4>();
            try
            {
                var assetItemLinks = _assetItemLinkTable.Where(p => p.ItemId == item.ParentItemId);
                if (assetItemLinks != null && assetItemLinks.Count() > 0)
                {
                    foreach (var assetItemLink in assetItemLinks)
                    {
                        if (menuModel.Assets.Any(x => x.AssetId == assetItemLink.Asset.AssetId) == false)
                        {
                            var assetModel = new AssetModelV4();

                            assetModel.AssetId = assetItemLink.Asset.AssetId;
                            assetModel.IrisId = assetItemLink.Asset.IrisId;
                            assetModel.FileHash = assetItemLink.Asset.FileHash;
                            assetModel.DimX = assetItemLink.Asset.DimX;
                            assetModel.DimY = assetItemLink.Asset.DimY;
                            assetModel.DimY = assetItemLink.Asset.DimY;
                            assetModel.Type = assetItemLink.Asset.AssetTypeId.ToString();
                            assetModel.URL = getAssetURL(assetItemLink.Asset.Blobname);
                            assetModel.Tags = getTagList(assetItemLink.Asset);

                            assetModel.Index = menuModel.Assets.Count();
                            menuModel.Assets.Add(assetModel);
                        }

                        // add the index and type to children
                        if (menuModel.Assets.Any(x => x.AssetId == assetItemLink.Asset.AssetId))
                        {
                            assetsInitem.Add(new ChildModelV4
                            {
                                Index = menuModel.Assets.Where(x => x.AssetId == assetItemLink.Asset.AssetId).FirstOrDefault().Index,
                                Type = EntityType.Assets.ToString().ToLower()
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            //Make all empty lists to null
            if (assetsInitem != null && assetsInitem.Count == 0)
            {
                assetsInitem = null;
            }
            return assetsInitem;
        }

        /// <summary>
        /// Gets child assets of an Category  and add the asset to MenuModel.Assets
        /// </summary>
        /// <param name="category"></param>
        /// <param name="menuModel"></param>
        /// <returns>List of ChildModel objects</returns>
        private List<ChildModelV4> getCategoryAssetListV4(Category category, ref FullMenuModelV4 menuModel)
        {
            var assetsInCategory = new List<ChildModelV4>();
            try
            {
                var assetCategoryLinks = _assetCategoryLinkTable.Where(p => p.CategoryId == category.OrgCategoryId);
                if (assetCategoryLinks != null && assetCategoryLinks.Count() > 0)
                {
                    foreach (var assetCategoryLink in assetCategoryLinks)
                    {
                        //If this asset is not yet added to MenuModel.Assets then add new
                        if (menuModel.Assets.Any(x => x.AssetId == assetCategoryLink.Asset.AssetId) == false)
                        {
                            var assetModel = new AssetModelV4();

                            assetModel.AssetId = assetCategoryLink.Asset.AssetId;
                            assetModel.IrisId = assetCategoryLink.Asset.IrisId;
                            assetModel.FileHash = assetCategoryLink.Asset.FileHash;
                            assetModel.DimX = assetCategoryLink.Asset.DimX;
                            assetModel.DimY = assetCategoryLink.Asset.DimY;
                            assetModel.Type = assetCategoryLink.Asset.AssetTypeId.ToString();
                            assetModel.URL = getAssetURL(assetCategoryLink.Asset.Blobname);
                            assetModel.Tags = getTagList(assetCategoryLink.Asset);

                            assetModel.Index = menuModel.Assets.Count();
                            menuModel.Assets.Add(assetModel);
                        }

                        // add the index and type to children
                        if (menuModel.Assets.Any(x => x.AssetId == assetCategoryLink.Asset.AssetId))
                        {
                            assetsInCategory.Add(new ChildModelV4
                            {
                                Index = menuModel.Assets.Where(x => x.AssetId == assetCategoryLink.Asset.AssetId).FirstOrDefault().Index,
                                Type = EntityType.Assets.ToString().ToLower()
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            //Make all empty lists to null
            if (assetsInCategory != null && assetsInCategory.Count == 0)
            {
                assetsInCategory = null;
            }
            return assetsInCategory;
        }

        /// <summary>
        /// Get ItemSchedule Information
        /// </summary>
        /// <param name="menuItemId"></param>
        /// <returns></returns>
        private ScheduleModelV2 getItemSchDetailListV4(int menuItemId)
        {
            if (menuItemId == 0)
            {
                return null;
            }
            var itemScheduleModel = new ScheduleModelV2();

            var itemSchDetails = _menuRuleService.GetItemSchDetails(menuItemId);
            itemScheduleModel = mapItemSchDetailsToItemSchedule(itemSchDetails);
            return itemScheduleModel;
        }

        /// <summary>
        /// Get Category Schedule Informatiom
        /// </summary>
        /// <param name="menuCategoryId"></param>
        /// <returns></returns>
        private ScheduleModelV2 getCatSchDetailListV4(int menuCategoryId)
        {
            if (menuCategoryId == 0)
            {
                return null;
            }
            var catScheduleModel = new ScheduleModelV2();

            var catSchDetails = _menuRuleService.GetCatSchDetails(menuCategoryId);
            catScheduleModel = mapCatSchDetailsToCatSchedule(catSchDetails);
            return catScheduleModel;
        }

        /// <summary>
        /// Map Item Schedule details to ItemScheduleModel
        /// </summary>
        /// <param name="schDetails"></param>
        /// <returns></returns>
        private ScheduleModelV2 mapItemSchDetailsToItemSchedule(List<MenuItemScheduleLink> schDetails)
        {
            var scheduleModel = new ScheduleModelV2();

            foreach (var schDetail in schDetails)
            {
                var schShowDetailModel = new ScheduleDetailModelV2();
                var schHideDetailModel = new ScheduleDetailModelV2();
                if (schDetail.IsSelected || schDetail.MenuItemCycleInSchedules.Where(x => x.IsShow && _scheduleCycleList.Contains(x.SchCycle)).Any())
                {
                    if (schDetail.MenuItemCycleInSchedules.Where(x => x.IsShow).Any())
                    {
                        schShowDetailModel.ScheduleSubDetails = schDetail.MenuItemCycleInSchedules.Where(x => x.IsShow && _scheduleCycleList.Contains(x.SchCycle)).Select(x => x.SchCycle.CycleName).ToList();
                    }
                    schShowDetailModel.DayOfWeek = ((System.DayOfWeek)schDetail.Day).ToString();
                    scheduleModel.Shows.Add(schShowDetailModel);
                }

                if (schDetail.MenuItemCycleInSchedules.Where(x => x.IsShow == false && _scheduleCycleList.Contains(x.SchCycle)).Any())
                {
                    schHideDetailModel.DayOfWeek = ((System.DayOfWeek)schDetail.Day).ToString();
                    schHideDetailModel.ScheduleSubDetails = schDetail.MenuItemCycleInSchedules.Where(x => x.IsShow == false && _scheduleCycleList.Contains(x.SchCycle)).Select(x => x.SchCycle.CycleName).ToList();
                    scheduleModel.Hides.Add(schHideDetailModel);
                }
            }
            return scheduleModel;
        }

        /// <summary>
        /// Map Category Schedule details to CategoryScheduleModel
        /// </summary>
        /// <param name="schDetails"></param>
        /// <returns></returns>
        private ScheduleModelV2 mapCatSchDetailsToCatSchedule(List<MenuCategoryScheduleLink> schDetails)
        {
            var scheduleModel = new ScheduleModelV2();

            foreach (var schDetail in schDetails)
            {
                var schShowDetailModel = new ScheduleDetailModelV2();
                var schHideDetailModel = new ScheduleDetailModelV2();
                if (schDetail.IsSelected || schDetail.MenuCategoryCycleInSchedules.Where(x => x.IsShow && _scheduleCycleList.Contains(x.SchCycle)).Any())
                {
                    if (schDetail.MenuCategoryCycleInSchedules.Where(x => x.IsShow).Any())
                    {
                        schShowDetailModel.ScheduleSubDetails = schDetail.MenuCategoryCycleInSchedules.Where(x => x.IsShow && _scheduleCycleList.Contains(x.SchCycle)).Select(x => x.SchCycle.CycleName).ToList();
                    }
                    schShowDetailModel.DayOfWeek = ((System.DayOfWeek)schDetail.Day).ToString();
                    scheduleModel.Shows.Add(schShowDetailModel);
                }

                if (schDetail.MenuCategoryCycleInSchedules.Where(x => x.IsShow == false && _scheduleCycleList.Contains(x.SchCycle)).Any())
                {
                    schHideDetailModel.DayOfWeek = ((System.DayOfWeek)schDetail.Day).ToString();
                    schHideDetailModel.ScheduleSubDetails = schDetail.MenuCategoryCycleInSchedules.Where(x => x.IsShow == false && _scheduleCycleList.Contains(x.SchCycle)).Select(x => x.SchCycle.CycleName).ToList();
                    scheduleModel.Hides.Add(schHideDetailModel);
                }
            }
            return scheduleModel;
        }
        #endregion

        /// <summary>
        /// Create AssetURL
        /// </summary>
        /// <param name="blobName"></param>
        /// <returns></returns>
        private string getAssetURL(string blobName)
        {
            try
            {
                if (string.IsNullOrEmpty(_cdnEndpoint))
                {
                    _cdnEndpoint = string.Format("{0}/{1}", ConfigurationManager.AppSettings.Get("CDNEndpoint").TrimEnd('/'), _IMAGES_CONTAINER);
                }

            }
            catch (Exception e)
            {
                throw e;
            }
            return string.Format("{0}/{1}", _cdnEndpoint, blobName);
        }

        /// <summary>
        /// Fetch Tags for an asset
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private List<string> getTagList(Asset asset, string version = "")
        {
            var tagAssetLinks = _tagAssetLinkTable.Where(p => p.AssetId == asset.AssetId);
            List<string> tagsModel = null;
            try
            {
                if (tagAssetLinks != null && tagAssetLinks.Count() > 0)
                {
                    tagsModel = new List<string>();
                    foreach (var tagAssetLink in tagAssetLinks)
                    {
                        tagsModel.Add(tagAssetLink.Tag.TagName);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            // Please see the comment 'NOTE1' above in this file
            if (tagAssetLinks != null && tagAssetLinks.Count() == 0)
            {
                tagAssetLinks = null;
            }
            return tagsModel;
        }

        /// <summary>
        /// Map ModifierFlag to Model
        /// </summary>
        /// <param name="modifierFlag"></param>
        /// <returns></returns>
        private ModifierFlagV3 MapMFtoMFV3(ModifierFlag modifierFlag)
        {
            var MFV3 = new ModifierFlagV3
            {
                Code = modifierFlag.Code,
                Name = modifierFlag.Name
            };
            return MFV3;
        }

        /// <summary>
        /// Convert TagBuket string into TaxTypeIds
        /// </summary>
        /// <param name="tBucketString"></param>
        /// <returns></returns>
        private List<int> getTaxBucketList(string tBucketString)
        {
            List<int> retVal = new List<int>();
            if (string.IsNullOrEmpty(tBucketString))
            {
                // invalid input string
                retVal.Add(0);
            }
            else
            {
                string[] tStringArray = tBucketString.Split(',');
                int tValue = 0;
                foreach (string tValueString in tStringArray)
                {
                    if (Int32.TryParse(tValueString, out tValue))
                    {
                        retVal.Add(tValue);
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// get Target's Channels
        /// </summary>
        /// <param name="siteNetworkObjectId"></param>
        /// <returns></returns>
        private List<string> getTargetClientChannels()
        {
            var tagsList = new List<string>();

            var target = _menuRuleService.GetTargetClientChannels(ClientID);
            if (target != null && target.Any())
            {
                tagsList = target.Select(x => x.TagName).ToList();
            }
            return tagsList;
        }

        /// <summary>
        /// Checks if tags are allowed to use entity
        /// </summary>
        /// <param name="tags">source tags</param>
        /// <param name="allowedTags">list of tags allowed</param>
        /// <returns></returns>
        private bool checkTag(List<string> tags, List<string> allowedTags)
        {
            bool retval = false;
            try
            {
                if (allowedTags.Count > 0)
                {
                    foreach (var tag in tags)
                    {
                        if (allowedTags.Contains(tag.ToLower()))
                        {
                            retval = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Tag check failed. Message:{0} StackTrace:{1}", ex.Message, ex.StackTrace));
            }
            return retval;
        }
    }

    /// <summary>
    /// Interface class that MenuService class implements
    /// </summary>
    public interface IMenuService
    {
        string LastActionResult { get; }
        string ClientID { get; set; }
        MenuModel GetMenuVersion(long id, out HttpStatusCode status);
        object GetFullMenu(long siteIrisId, string version, out HttpStatusCode status, string channel = null);
    }
}