using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;
using System.Diagnostics;
using Omu.ValueInjecter;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SnowMaker;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;

namespace Phoenix.Web.Models
{
    public class MenuService : IMenuService
    {
        private IRepository _repository;
        private DbContext _context;
        private IUniqueIdGenerator _irisIdGenerator;
        // public NetworkObjectTypes currNetworkType = NetworkObjectTypes.Site;
        public List<int> parentNetworkNodeIds = new List<int>();
        public List<int> childNetworkNodeIds = new List<int>();
        public List<int> parentMenuNetworkNodeIds = new List<int>();
        public List<int> childMenuNetworkNodeIds = new List<int>();
        private string _lastActionResult;

        public int Count { get; set; }
        public int MenuId { get; set; }
        public int NetworkObjectId { get; set; }

        private IAuditLogService _auditLogger;
        public RuleService _ruleService { get; set; }
        private IScheduleService _schService;
        private IItemService _itemService;
        private ICommonService _commonService;
        private ITagService _tagService;

        private List<SubCategoryLink> _subCategoryLinks;
        private List<CategoryMenuLink> _categoryMenuLinks;
        private List<CategoryObject> _categoryObjects;
        private List<ItemCollectionLink> _itemCollectionLinks;
        private List<ItemCollectionObject> _itemCollectionObjects;
        private List<MenuItemScheduleLink> _itemScheduleLinks;
        private List<MenuCategoryScheduleLink> _categoryScheduleLinks;
        private List<SchNetworkObjectLink> _scheduleNetworkLinks;
        private List<SpecialNoticeMenuLink> _specialNoticeMenuLinks;
        private List<PrependItemLink> _prependItemLinks;
        private List<MenuNetworkObjectLink> _mnuNetworkLinks;

        private IValidationDictionary _validatonDictionary;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        [Dependency]
        public IUniqueIdGenerator IrisIdGenerator
        {
            get { return _irisIdGenerator; }
            set { _irisIdGenerator = value; }
        }

        public class MenuTreeItem
        {
            public MenuTreeItem()
            {
                this.isAvail = true;
            }
            // Id that uniquely identifies the item in the database
            public string id { get; set; }
            public int entityid { get; set; }
            public int actualid { get; set; }
            // User-friendly name of the item .
            public string txt { get; set; }
            public int srt { get; set; }
            public string prnt { get; set; }
            public string img { get; set; }
            public MenuType typ { get; set; }
            public bool isOvr { get; set; }
            public bool isAvail { get; set; }
            public bool hasChildren { get; set; }
            // Collection of items associated with this item.
            //public List<MenuTreeItem> Items { get; set; }
        }

        public class MenuGridItem
        {
            // Id that uniquely identifies the item in the database
            public string TreeId { get; set; }
            public int entityid { get; set; }
            public int actualid { get; set; }
            public MenuType typ { get; set; }
            // User-friendly name of the item .
            public string DisplayName { get; set; }
            public string InternalName { get; set; }
            public bool IsFirst { get; set; }
            public bool IsLast { get; set; }
            public bool isOvr { get; set; }
            public int SortOrder { get; set; }
            public int? BasePLU { get; set; }
            public string AlternatePLU { get; set; }
            public bool IsFeatured { get; set; }
            public bool IsModifier { get; set; }
            public bool IsIncluded { get; set; }
            public bool HasImages { get; set; }
            public bool IsAutoSelect { get; set; }
            public OverrideStatus OverrideStatus { get; set; }
            public int SelectedPOSDataId { get; set; }
            public List<Dropdown> POSDataList { get; set; }
        }

        /// <summary>
        /// Initialize the validation dictionary
        /// </summary>
        /// <param name="validatonDictionary"></param>
        public void Initialize(IValidationDictionary validatonDictionary)
        {
            _validatonDictionary = validatonDictionary;
        }
        /// <summary>
        /// .ctor
        /// </summary>
        public MenuService(IItemService itemService, IScheduleService scheduleService)
        {
            parentNetworkNodeIds = new List<int>();
            childNetworkNodeIds = new List<int>();
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _ruleService = new RuleService(RuleService.CallType.Web,_context,_repository);
            _schService = scheduleService;
            _itemService = itemService;
            _commonService = new CommonService(_repository);
            _tagService = new TagService(_context,_repository);

            _subCategoryLinks = new List<SubCategoryLink>();
            _categoryMenuLinks = new List<CategoryMenuLink>();
            _categoryObjects = new List<CategoryObject>();
            _itemCollectionLinks = new List<ItemCollectionLink>();
            _itemCollectionObjects = new List<ItemCollectionObject>();
            _itemScheduleLinks = new List<MenuItemScheduleLink>();
            _scheduleNetworkLinks = new List<SchNetworkObjectLink>();
            _categoryScheduleLinks = new List<MenuCategoryScheduleLink>();
            _specialNoticeMenuLinks = new List<SpecialNoticeMenuLink>();
            _prependItemLinks = new List<PrependItemLink>();
            _mnuNetworkLinks = new List<MenuNetworkObjectLink>();
        }

        /// <summary>
        /// Get NetworkObject Details for given network
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public NetworkObject GetNetworkObject(int netId, out int brandId, out string parentsBreadCrum)
        {
            return _ruleService.GetNetworkObject(netId, out brandId, out parentsBreadCrum);
        }

        /// <summary>
        /// Gte Menu Details for given network
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public Menu GetMenu(int menuId)
        {
            _ruleService.MenuId = menuId;
            var menu = _ruleService.Menu;

            return menu;
        }

        /// <summary>
        /// Returns list of menus available for a given Network
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public List<MenuDataModel> GetMenuList(int netId)
        {
            var retVal = new List<MenuDataModel>();
            //If NetworkId is not passed then return empty list
            if (netId == 0)
            {
                return retVal;
            }

            //Get the availables from RuleEngine
            List<Phoenix.DataAccess.Menu> menuList = new List<Phoenix.DataAccess.Menu>();
            _ruleService.GetMenus(netId, menuList);

            foreach (var m in menuList)
            {
                var channelList = getMenuRelatedTags(m.MenuId, NetworkObjectId);
                //Populate the Model from Menu
                var menuDataModel = new MenuDataModel
                      {
                          MenuId = m.MenuId,
                          Description = m.Description,
                          InternalName = m.InternalName,
                          LastUpdateDate = m.LastUpdatedDate.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt"),
                          NetworkObjectId = m.NetworkObjectId,
                          IsMenuOverriden = m.IsMenuOverriden,
                          IsEditable = m.NetworkObjectId == netId ? true : false,
                          IsDeletable = m.NetworkObjectId == netId ? true : false,
                          Channels = channelList,
                          ChannelListName = string.Join(",", channelList.Select(x => x.TagName)),
                          ChannelIdList = string.Join(",", channelList.Select(x => x.TagId)),
                      };
                retVal.Add(menuDataModel);
            }
            return retVal;
        }

        /// <summary>
        /// Get all menus in the application under particular network
        /// </summary>
        /// <returns></returns>
        public List<MenuDropdown> GetMenuListInSelectedNetworkTrees(List<int> mulitpleNetworkIds)
        {
            var retVal = new List<MenuDropdown>();
            try
            {
                if (mulitpleNetworkIds != null)
                {

                    //get Site NetworkObjects under provided NetworkObjectIds
                    var networkParentsOfSelectedNetworks = (_context as ProductMasterContext).fnNetworkObjectParentsOfSelectedNetworks(string.Join(",", mulitpleNetworkIds)).Select(x => x.NetworkObjectId).ToList();


                    List<Menu> lst = _repository.GetQuery<Menu>(x => networkParentsOfSelectedNetworks.Contains(x.NetworkObjectId)).Distinct().ToList();
                    retVal = (from ml in lst.Distinct()
                              //Reorder the list with the Prompt sorted at the beginning
                              select new MenuDropdown
                              {
                                  Order = (ml.MenuId == -1) ? 0 : 1,
                                  MenuId = ml.MenuId,
                                  Name = ml.InternalName
                              }).OrderBy(mo => mo.Order).ThenBy(mo => mo.Name).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkIds"></param>
        /// <returns></returns>
        public List<Menu> GetMenusInNetworks(List<int> networkIds)
        {
            var retVal = new List<Menu>();
            try
            {
                List<Menu> lst = _ruleService.GetMenusInNetworks(networkIds);
                retVal = lst != null ? lst : retVal;
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retVal;
        }
        /// <summary>
        /// returns all MenuNames in form of a string
        /// </summary>
        /// <returns></returns>
        public string GetMenuNames(int netId)
        {
            //concatenate all the available Menu names for this Network
            var menuNames = string.Empty;
            parentNetworkNodeIds = _ruleService.GetNetworkParents(netId);
            childNetworkNodeIds = _ruleService.GetNetworkChilds(netId);
            menuNames = string.Join(",", _repository.GetQuery<Menu>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Select(x => x.InternalName.ToUpper()).ToArray());
            return menuNames;
        }

        /// <summary>
        /// Get Menu data onDemand - first level children of a node
        /// </summary>
        /// <param name="id"></param>
        /// <param name="tree"></param>
        public void GetHierarchicalMenuTree(string id, List<MenuTreeItem> tree)
        {
            try
            {
                //If nothing is passed in Id, then load the first nodes of the tree. - Here it is MenuName
                if (string.IsNullOrEmpty(id))
                {
                    var menu = _repository.GetQuery<Menu>(m => m.MenuId == MenuId).FirstOrDefault();

                    MenuTreeItem menuData = new MenuTreeItem();
                    menuData.id = GetMenuTreeId(MenuType.Menu, 0, menu.MenuId);
                    menuData.entityid = menu.MenuId;
                    menuData.actualid = menu.MenuId;
                    menuData.txt = menu.InternalName;
                    menuData.typ = MenuType.Menu;
                    menuData.img = getImagePath(MenuType.Menu);
                    menuData.hasChildren = true;

                    tree.Add(menuData);
                }
                // If "id" is passed then get children passed on type of node.
                else
                {
                    //From id get the elements of the node.
                    string[] partsofId = id.Split('_');
                    if (partsofId.Length == 3)
                    {
                        //First element is always NodeType
                        //Second is parent Id of element in Tree. If is 0, then it doesn't have any parent
                        //Third is ElementId for which children are being loaded
                        var typeofElement = partsofId[0];
                        var prntId = Convert.ToInt32(partsofId[1]);
                        var entityId = Convert.ToInt32(partsofId[2]);
                        var orginialEntityId = entityId;

                        //get parents
                        //parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                        //RuleService.NetworkObjectId = NetworkObjectId;

                        _context.Configuration.LazyLoadingEnabled = false;
                        switch (typeofElement)
                        {
                            //If the NodeTYpe is Menu then load it children (categories)
                            case "Menu":

                                //Get all categories for this menu
                                var categoryList = _ruleService.GetCategoriesList(MenuId);
                                tree.AddRange(MapCatsListtoTree(categoryList, id, orginialEntityId, MenuType.Category));
                                break;
                            case "Cat":

                                //make sure this was not an override for another category
                                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == entityId && co.MenuId == MenuId).FirstOrDefault();
                                if (ovrride != null && ovrride.ParentCategoryId != null)
                                {
                                    entityId = ovrride.ParentCategoryId.Value;
                                }
                                else
                                {
                                    //make sure this was not an override for another category
                                    var subCategoryovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == entityId).FirstOrDefault();
                                    if (subCategoryovrride != null && subCategoryovrride.OverrideParentSubCategoryId != null)
                                        entityId = subCategoryovrride.OverrideParentSubCategoryId.Value;
                                }

                                //1. Get the data for items in categry from ruleEngine and convert to List of MenuTree Objects
                                tree.AddRange(MapItemsListtoTree(_ruleService.GetItemList(entityId, MenuId), id, orginialEntityId));

                                //2. Get the data for sub categories from ruleEngine and convert to List of MenuTree Objects
                                tree.AddRange(MapCatsListtoTree(_ruleService.GetSubCategoryList(entityId, MenuId), id, orginialEntityId, MenuType.SubCategory));
                                break;
                            case "Itm":
                            case "ColItm":
                                //make sure this was not an override for another item
                                var itemovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == entityId).FirstOrDefault();
                                if (itemovrride != null && itemovrride.ParentItemId != null)
                                    entityId = itemovrride.ParentItemId.Value;

                                //make sure this was not an override for another Item
                                var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == entityId).FirstOrDefault();
                                if (collObjovrride != null && collObjovrride.ParentItemId != null)
                                    entityId = collObjovrride.ParentItemId.Value;

                                //get the data for collection in this item from ruleEngine and convert to List of MenuTree Objects
                                tree.AddRange(MapCollectionsListtoTree(_ruleService.GetCollectionList(entityId, MenuId), id, orginialEntityId));
                                break;
                            case "ItmCol":

                                //make sure this was not an override for another collection
                                var colovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == entityId).FirstOrDefault();
                                if (colovrride != null && colovrride.ParentCollectionId != null)
                                    entityId = colovrride.ParentCollectionId.Value;

                                //get the data for item in this collection from ruleEngine and convert to List of MenuTree Objects
                                tree.AddRange(MapCollectionItemsListtoTree(_ruleService.GetCollectionItemList(entityId, MenuId), id, orginialEntityId));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// Map Tree SubCats for Categories
        /// </summary>
        /// <param name="CatList"></param>
        /// <param name="parentTreeId"></param>
        /// <param name="entityId"></param>
        /// <param name="typ"></param>
        /// <returns></returns>
        private List<MenuTreeItem> MapCatsListtoTree(List<Category> CatList, string parentTreeId, int entityId, MenuType typ)
        {
            List<MenuTreeItem> catModels = new List<MenuTreeItem>();
            //define all the tree properties
            foreach (var cat in CatList)
            {
                MenuTreeItem catModel = new MenuTreeItem();
                catModel.prnt = parentTreeId;
                catModel.typ = typ;
                catModel.img = getImagePath(MenuType.Category, cat.CategoryTypeId.ToString());
                //Unique Id to identify an Item in Menu
                catModel.id = GetMenuTreeId(typ, entityId, cat.CategoryId);
                catModel.entityid = cat.CategoryId;
                catModel.actualid = cat.OrgCategoryId;
                catModel.txt = cat.DisplayName;
                catModel.srt = cat.SortOrder;
                catModel.isOvr = cat.IsOverride;
                //Set the property hasChildren 
                if (cat.hasChildren)
                {
                    catModel.hasChildren = true;
                }
                catModels.Add(catModel);
            }
            return catModels;
        }

        /// <summary>
        /// Map Tree Items for Categories
        /// </summary>
        /// <param name="Items"></param>
        /// <param name="parentTreeId"></param>
        /// <returns></returns>
        private List<MenuTreeItem> MapItemsListtoTree(List<Item> Items, string parentTreeId, int entityId)
        {
            List<MenuTreeItem> itmModels = new List<MenuTreeItem>();
            foreach (var itm in Items)
            {
                //Map all the item to MenuTreeItem Object
                MenuTreeItem itmModel = new MenuTreeItem();
                itmModel.prnt = parentTreeId;
                itmModel.typ = MenuType.Item;
                //Image is I.png so store I
                itmModel.img = getImagePath(MenuType.Item);
                //Create Unique Id to be identified in a tree
                itmModel.id = GetMenuTreeId(MenuType.Item, entityId, itm.ItemId);
                itmModel.entityid = itm.ItemId;
                itmModel.actualid = itm.OrgItemId;
                itmModel.txt = itm.DisplayName;
                itmModel.srt = itm.SortOrder;
                //Style changes if it is a override
                itmModel.isOvr = itm.IsOverride;
                itmModel.isAvail = itm.IsAvailable;
                //Set the haschildren property if RuleEngine returns that it has children
                if (itm.hasChildren)
                {
                    itmModel.hasChildren = true;
                }
                itmModels.Add(itmModel);
            }
            return itmModels;
        }

        /// <summary>
        /// Map Tree Collections for Items
        /// </summary>
        /// <param name="ItemCols"></param>
        /// <param name="parentTreeId"></param>
        /// <returns></returns>
        private List<MenuTreeItem> MapCollectionsListtoTree(List<ItemCollection> ItemCols, string parentTreeId, int entityId)
        {
            List<MenuTreeItem> itmColModels = new List<MenuTreeItem>();
            foreach (var itmCol in ItemCols)
            {
                //Map all the collections to MenuTreeItem Object
                MenuTreeItem itmCollectionModel = new MenuTreeItem();
                itmCollectionModel.prnt = parentTreeId;
                itmCollectionModel.typ = MenuType.ItemCollection;
                //Image is C.png so store C
                itmCollectionModel.img = getImagePath(MenuType.ItemCollection, itmCol.CollectionTypeId.ToString());
                //Create Unique Id to be identified in a tree
                itmCollectionModel.id = GetMenuTreeId(MenuType.ItemCollection, entityId, itmCol.CollectionId);
                itmCollectionModel.entityid = itmCol.CollectionId;
                itmCollectionModel.actualid = itmCol.OrgCollectionId;
                itmCollectionModel.txt = itmCol.DisplayName;
                itmCollectionModel.srt = itmCol.SortOrder;
                //Style changes if it is a override
                itmCollectionModel.isOvr = itmCol.IsOverride;
                //Set the haschildren property if RuleEngine returns that it has children
                if (itmCol.hasChildren)
                {
                    itmCollectionModel.hasChildren = true;
                }
                itmColModels.Add(itmCollectionModel);
            }
            return itmColModels;
        }

        /// <summary>
        /// Map tree Items for Collections
        /// </summary>
        /// <param name="CollectionItems"></param>
        /// <param name="parentTreeId"></param>
        /// <returns></returns>
        private List<MenuTreeItem> MapCollectionItemsListtoTree(List<Item> CollectionItems, string parentTreeId, int entityId)
        {
            List<MenuTreeItem> collectionItemModels = new List<MenuTreeItem>();
            foreach (var item in CollectionItems)
            {
                //Map all the item to MenuTreeItem Object
                MenuTreeItem collectionItemModel = new MenuTreeItem();
                collectionItemModel.prnt = parentTreeId;
                collectionItemModel.typ = MenuType.ItemCollectionItem;
                //Image is I.png so store I
                collectionItemModel.img = getImagePath(MenuType.ItemCollectionItem);
                //Create Unique Id to be identified in a tree
                collectionItemModel.id = GetMenuTreeId(MenuType.ItemCollectionItem, entityId, item.ItemId);
                collectionItemModel.entityid = item.ItemId;
                collectionItemModel.actualid = item.OrgItemId;
                collectionItemModel.txt = item.DisplayName;
                collectionItemModel.srt = item.SortOrder;
                //Style changes if it is a override
                collectionItemModel.isOvr = item.IsOverride;
                collectionItemModel.isAvail = item.IsAvailable;
                //Set the haschildren property if RuleEngine returns that it has children
                if (item.hasChildren)
                {
                    collectionItemModel.hasChildren = true;
                }
                collectionItemModels.Add(collectionItemModel);
            }
            return collectionItemModels;
        }

        /// <summary>
        /// Add cats to the Menu
        /// </summary>
        /// <param name="catIds"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<string> AddCategoriestoMenu(int[] catIds, int menuId)
        {
            List<string> addedCategoriesNames = new List<string>(), alreadyExistCats = new List<string>(), invalidCats = new List<string>(), addedCategoryIds = new List<string>(), errCats = new List<string>();
            string menuName = string.Empty, catName = string.Empty;
            var srtOrder = 1;
            try
            {
                if (catIds != null)
                {
                    //Load the Nodes of the current tree
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    var mnu = _repository.GetQuery<Menu>(a => a.MenuId == menuId).FirstOrDefault();
                    if (mnu != null)
                    {
                        // get all links at once
                        _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(co => parentNetworkNodeIds.Contains(co.NetworkObjectId) || childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == menuId).Include("NetworkObject").ToList();

                        var currentCatMnuLinks = _categoryMenuLinks.Where(co => co.MenuId == menuId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        var allCategoriesToAdd = _repository.GetQuery<Category>(x => catIds.Contains(x.CategoryId)).ToList();
                        //Add each item
                        foreach (var catId in catIds)
                        {
                            var cat = allCategoriesToAdd.Where(x => x.CategoryId == catId).FirstOrDefault();
                            bool hasCatMnuLinked = false;
                            bool isAddedBack = false;// Flag to check if this SubCat have delete override before and being added new

                            // if it is valid Category then perform the action
                            if (cat != null)
                            {
                                catName = cat.InternalName;
                                menuName = mnu.InternalName;
                                // Parent category Id if is overriden
                                int? prntCatId = null;

                                // If this Category is already present as link in any of the parent
                                if (currentCatMnuLinks != null && currentCatMnuLinks.Where(co => co.CategoryId == catId).Count() > 0)
                                {
                                    //Get the last modified record
                                    var lastcatMnuLink = currentCatMnuLinks.Where(co => co.CategoryId == catId || (co.ParentCategoryId.HasValue && co.ParentCategoryId == cat.OverrideCategoryId)).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).ThenByDescending(co => co.ParentCategoryId).FirstOrDefault();

                                    //IF Category is overriden by last parent to delete
                                    if (lastcatMnuLink.OverrideStatus == OverrideStatus.HIDDEN)
                                    {
                                        //To Make it active now add override record for the deleted record.
                                        hasCatMnuLinked = false;
                                        prntCatId = lastcatMnuLink.ParentCategoryId;

                                        // If this Category is overriden to be deleted by this n/w and now being added again - delete the delete override
                                        if (lastcatMnuLink.NetworkObjectId == NetworkObjectId)
                                        {
                                            _repository.Delete<CategoryMenuLink>(lastcatMnuLink);
                                            isAddedBack = true;
                                            hasCatMnuLinked = true;
                                        }
                                    }
                                    else
                                    {
                                        //Else, this Category is already present
                                        hasCatMnuLinked = true;
                                    }
                                }

                                //Insert New record if it is not present
                                //If it is added back, make sure the addtions at child are taken care
                                if (hasCatMnuLinked == false || isAddedBack)
                                {
                                    //if same Category is added by childs before. delete those records
                                    var childs = _categoryMenuLinks.Where(co => co.MenuId == menuId && co.CategoryId == cat.CategoryId && co.OverrideStatus != OverrideStatus.HIDDEN && childNetworkNodeIds.Contains(co.NetworkObjectId));
                                    foreach (var chld in childs)
                                    {
                                        _repository.Delete<CategoryMenuLink>(chld);
                                    }
                                    if (hasCatMnuLinked == false)
                                    {
                                        //Adding Category to Menu
                                        CategoryMenuLink catMnuLink = new CategoryMenuLink
                                        {
                                            MenuId = menuId,
                                            CategoryId = cat.CategoryId,
                                            NetworkObjectId = NetworkObjectId,
                                            OverrideStatus = OverrideStatus.ACTIVE,
                                            SortOrder = (currentCatMnuLinks == null || currentCatMnuLinks.Count() == 0) ? srtOrder : currentCatMnuLinks.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1,
                                            ParentCategoryId = prntCatId
                                        };
                                        //If this is first Category to current Menu maintain the SortOrder for next Categories
                                        if (currentCatMnuLinks == null || (currentCatMnuLinks != null && currentCatMnuLinks.Count() == 0))
                                        {
                                            srtOrder = srtOrder + 1;
                                        }
                                        _repository.Add<CategoryMenuLink>(catMnuLink);
                                    }
                                    if (!addedCategoryIds.Contains(cat.CategoryId.ToString()))
                                    {
                                        addedCategoriesNames.Add(cat.InternalName);
                                        addedCategoryIds.Add(cat.CategoryId.ToString());
                                    }
                                }
                                else
                                {//Already added Cats
                                    alreadyExistCats.Add(cat.InternalName);
                                }
                            }
                            else
                            {//Id is invalid
                                invalidCats.Add(cat.InternalName);
                            }
                        }

                        //Set Menu Lastupdated Date to Now
                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        //Display appropriate Status
                        if (addedCategoriesNames.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatMnuLinkAddT, string.Join(",", addedCategoriesNames), mnu.InternalName);
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityNameList: mnu.InternalName, operationDescription: string.Format(Constants.AuditMessage.CatMnuLinkAddT, string.Join(",", addedCategoriesNames), mnu.InternalName));
                        }
                        if (alreadyExistCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatMnuLinkExistT, string.Join(",", alreadyExistCats), mnu.InternalName);
                        }
                        if (invalidCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidCatT, string.Join(",", invalidCats));
                        }
                        if (errCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrSubCatLinkNotAddedT, string.Join(",", errCats), mnu.InternalName);
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidMnuT, mnu.InternalName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatMnuLinkAddT, catName, menuName);
                Logger.WriteError(ex.Message);
            }
            return addedCategoryIds;
        }

        /// <summary>
        /// Add sub cats to the category
        /// </summary>
        /// <param name="subCategoryIds"></param>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public List<string> AddSubCategoriestoCategory(int[] subCategoryIds, int categoryId)
        {
            List<string> addedSubCatNames = new List<string>(), alreadyExistSubCats = new List<string>(), invalidSubCats = new List<string>(), addedSubCatIds = new List<string>(), errSubCats = new List<string>();
            string catname = string.Empty, subCatname = string.Empty;
            var orgCatId = 0;
            var srtOrder = 1;
            try
            {
                if (subCategoryIds != null)
                {
                    //Load the Nodes of the current tree
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    orgCatId = categoryId;

                    //make sure this was not an override for another category
                    var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == categoryId && co.MenuId == MenuId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentCategoryId != null)
                    {
                        categoryId = ovrride.ParentCategoryId.Value;
                    }
                    else
                    {
                        var subCatovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == categoryId).FirstOrDefault(); //_repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == catId).FirstOrDefault();
                        if (subCatovrride != null && subCatovrride.OverrideParentSubCategoryId != null)
                            categoryId = subCatovrride.OverrideParentSubCategoryId.Value;
                    }

                    //Get all rows from required link tables in this specific network Tree.
                    _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(co => parentNetworkNodeIds.Contains(co.NetworkObjectId) || childNetworkNodeIds.Contains(co.NetworkObjectId) && co.CategoryId == orgCatId).Include("NetworkObject").Include("Category").ToList();

                    // Get the category to which subcategorues are being added and subcategory that are selected to add
                    var allCategories = _repository.GetQuery<Category>(a => a.CategoryId == categoryId || subCategoryIds.Contains(a.CategoryId)).ToList();
                    var parentCategory = allCategories.Where(x => x.CategoryId == categoryId).FirstOrDefault();
                    if (allCategories.Any(x => x.CategoryId == categoryId))
                    {
                        //Get parent Categories Ids to check eligibilty before proceeding to Add Sub Cat
                        var isNotTopCategory = true;
                        var parentCatIdsToRestrict = new List<int>();
                        var catIds = new List<int>();
                        catIds.Add(categoryId);
                        //AS we cannot add current Cat too add the current selected parent Cat to parents list
                        parentCatIdsToRestrict.Add(orgCatId);
                        parentCatIdsToRestrict.Add(categoryId);
                        do
                        {
                            // pass Id and find all Categories under it. 
                            //When the function returns it updates the flag "loop", next updated catIds that should be searched next and All the Categories using current subCat as child
                            //Loop through for each item until you reach top Item to find all parent level Categories
                            isNotTopCategory = getparentCategories(catIds, parentCatIdsToRestrict);
                        } while (isNotTopCategory);

                        // get all sub categories already present
                        var currentSubCategoryLinks = _subCategoryLinks.Where(co => co.CategoryId == categoryId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        var isAllowedToAdd = false;

                        //assign srtorder for next item. If there are no item then start from 1
                        srtOrder = (currentSubCategoryLinks == null || currentSubCategoryLinks.Count() == 0) ? srtOrder : currentSubCategoryLinks.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1;
                        //Add each item
                        foreach (var subCategoryId in subCategoryIds)
                        {
                            var subCategory = allCategories.Where(x => x.CategoryId == subCategoryId).FirstOrDefault();
                            if (allCategories.Any(x => x.CategoryId == subCategoryId))
                            {
                                subCatname = subCategory.InternalName;
                                catname = allCategories.Where(x => x.CategoryId == categoryId).FirstOrDefault().InternalName;

                                //Check for the eligibility to add SubCat
                                isAllowedToAdd = getEligibilityToAddSubCategory(subCategoryId, parentCatIdsToRestrict);
                                if (isAllowedToAdd)
                                {
                                    bool hasSubCategoryAlreadyLinked = false;
                                    bool isAddedBack = false;// Flag to check if this SubCat have delete override before and being added noew

                                    int? prntSubCatId = null;

                                    // If this SubCat is already present as link in any of the parent
                                    if (currentSubCategoryLinks != null && currentSubCategoryLinks.Where(co => co.SubCategoryId == subCategoryId).Count() > 0)
                                    {
                                        //Get the last modified record
                                        var lastSubCategoryLink = currentSubCategoryLinks.Where(co => co.SubCategoryId == subCategoryId || (co.OverrideParentSubCategoryId.HasValue && co.OverrideParentSubCategoryId == subCategory.OverrideCategoryId)).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).ThenByDescending(co => co.OverrideParentSubCategoryId).FirstOrDefault();

                                        //IF the link is that this SubCat is overriden by last parent to delete
                                        if (lastSubCategoryLink.OverrideStatus == OverrideStatus.HIDDEN)
                                        {
                                            //To Make it active now add override record for the deleted record.
                                            hasSubCategoryAlreadyLinked = false;
                                            prntSubCatId = lastSubCategoryLink.OverrideParentSubCategoryId;

                                            // If this SubCat is overriden to be deleted by this n/w and now being added again - delete the delete override
                                            if (lastSubCategoryLink.NetworkObjectId == NetworkObjectId)
                                            {
                                                _repository.Delete<SubCategoryLink>(lastSubCategoryLink);
                                                isAddedBack = true;
                                                hasSubCategoryAlreadyLinked = true;
                                            }
                                        }
                                        else
                                        {
                                            //Else, this SubCat is already present
                                            hasSubCategoryAlreadyLinked = true;
                                        }
                                    }

                                    //Insert New record if it is not present
                                    if (!hasSubCategoryAlreadyLinked || isAddedBack)
                                    {
                                        //if same SubCat is added by childs before. delete those records
                                        var similarLinksatChildNetworks = _subCategoryLinks.Where(co => co.CategoryId == categoryId && co.SubCategoryId == subCategory.CategoryId && co.OverrideStatus != OverrideStatus.HIDDEN && childNetworkNodeIds.Contains(co.NetworkObjectId));
                                        foreach (var chld in similarLinksatChildNetworks)
                                        {
                                            _repository.Delete<SubCategoryLink>(chld);
                                        }

                                        if (hasSubCategoryAlreadyLinked == false)
                                        {
                                            //Adding SubCat to Category
                                            SubCategoryLink subCategoryLinkToAdd = new SubCategoryLink
                                            {
                                                CategoryId = categoryId,
                                                SubCategoryId = subCategory.CategoryId,
                                                NetworkObjectId = NetworkObjectId,
                                                OverrideStatus = OverrideStatus.ACTIVE,
                                                SortOrder = srtOrder,
                                                OverrideParentSubCategoryId = prntSubCatId
                                            };
                                            //If this is first SubCat to current category maintain the SortOrder for next itemIds
                                            srtOrder = srtOrder + 1;

                                            _repository.Add<SubCategoryLink>(subCategoryLinkToAdd);

                                            ////If this SubCat is edited in any other category then use the edited subCategory in current network and child networks
                                            //childNetworkNodeIds.Add(NetworkObjectId);
                                            //var searchedNetworks = new List<int>();
                                            ////get if this SubCat is overriden by child networks in any category of current Menu.
                                            //var alreadyOverridenSubCats = _subCategoryLinks.Where(co => co.OverrideParentSubCategoryId == subCategoryId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.OverrideParentSubCategoryId != co.CategoryId && co.OverrideStatus != OverrideStatus.HIDDEN && co.Category.MenuId == MenuId).ToList();

                                            //foreach (var alreadyOverridenSubCat in alreadyOverridenSubCats)
                                            //{
                                            //    // If override is not yet added to this Cat in current NW
                                            //    if (!searchedNetworks.Contains(alreadyOverridenSubCat.NetworkObjectId))
                                            //    {
                                            //        //If this SubCat not already present for same category in current NW
                                            //        if (!alreadyOverridenSubCats.Where(x => x.NetworkObjectId == alreadyOverridenSubCat.NetworkObjectId).Any(x => x.CategoryId == categoryId))
                                            //        {
                                            //            SubCategoryLink editedSubCatLink = new SubCategoryLink
                                            //            {
                                            //                CategoryId = categoryId,
                                            //                SubCategoryId = alreadyOverridenSubCat.SubCategoryId,
                                            //                NetworkObjectId = alreadyOverridenSubCat.NetworkObjectId,
                                            //                OverrideStatus = OverrideStatus.ACTIVE,
                                            //                SortOrder = subCategoryLinkToAdd.SortOrder,
                                            //                OverrideParentSubCategoryId = alreadyOverridenSubCat.OverrideParentSubCategoryId
                                            //            };
                                            //            _repository.Add<SubCategoryLink>(editedSubCatLink);
                                            //        }
                                            //        searchedNetworks.Add(alreadyOverridenSubCat.NetworkObjectId);
                                            //    }
                                            //}

                                            ////For categories, check if this is a Main level category and it is edited there, if it edited in the main level then that edited category should be added as sub category.
                                            //var alreadyOverridenCMLs = _categoryMenuLinks.Where(co => co.ParentCategoryId == subCategoryId && childNetworkNodeIds.Contains(co.NetworkObjectId) && !searchedNetworks.Contains(co.NetworkObjectId) && co.ParentCategoryId != co.CategoryId && co.OverrideStatus != OverrideStatus.HIDDEN && co.Category.MenuId == MenuId).ToList();
                                            //searchedNetworks.Clear();
                                            //foreach (var alreadyOverridenCML in alreadyOverridenCMLs)
                                            //{
                                            //    // If override is not yet added to this Cat in current NW
                                            //    if (!searchedNetworks.Contains(alreadyOverridenCML.NetworkObjectId))
                                            //    {
                                            //        //If this SubCat not already present for same category in current NW
                                            //        if (!alreadyOverridenSubCats.Where(x => x.NetworkObjectId == alreadyOverridenCML.NetworkObjectId).Any(x => x.CategoryId == categoryId))
                                            //        {
                                            //            SubCategoryLink editedSubCatLink = new SubCategoryLink
                                            //            {
                                            //                CategoryId = categoryId,
                                            //                SubCategoryId = alreadyOverridenCML.CategoryId,
                                            //                NetworkObjectId = alreadyOverridenCML.NetworkObjectId,
                                            //                OverrideStatus = OverrideStatus.ACTIVE,
                                            //                SortOrder = subCategoryLinkToAdd.SortOrder,
                                            //                OverrideParentSubCategoryId = alreadyOverridenCML.ParentCategoryId
                                            //            };
                                            //            _repository.Add<SubCategoryLink>(editedSubCatLink);
                                            //        }
                                            //        searchedNetworks.Add(alreadyOverridenCML.NetworkObjectId);
                                            //    }
                                            //}
                                            //childNetworkNodeIds.Remove(NetworkObjectId);
                                        }

                                        // added to list of SubCats added
                                        if (!addedSubCatIds.Contains(subCategory.CategoryId.ToString()))
                                        {
                                            addedSubCatNames.Add(subCategory.InternalName);
                                            addedSubCatIds.Add(subCategory.CategoryId.ToString());
                                        }
                                    }
                                    else
                                    {//Already added Cats
                                        alreadyExistSubCats.Add(subCategory.InternalName);
                                    }
                                }
                                else
                                {//Not eligible to add
                                    errSubCats.Add(subCategory.InternalName);
                                }

                            }
                            else
                            {//Id is invalid
                                invalidSubCats.Add(subCategory.InternalName);
                            }
                        }

                        //Set Menu LastUpdated Date
                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        //Display respective messages
                        if (addedSubCatNames.Any())
                        {
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.SubCatLinkAddT, string.Join(",", addedSubCatNames), parentCategory.InternalName));
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.SubCatLinkAddT, string.Join(",", addedSubCatNames), parentCategory.InternalName);
                        }
                        if (alreadyExistSubCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.SubCatLinkExistT, string.Join(",", alreadyExistSubCats), parentCategory.InternalName);
                        }
                        if (invalidSubCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidCatT, string.Join(",", invalidSubCats));
                        }
                        if (errSubCats.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrSubCatLinkNotAddedT, string.Join(",", errSubCats), parentCategory.InternalName);
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidCatT, parentCategory.InternalName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSubCatLinkAddT, subCatname, catname);
                Logger.WriteError(ex.Message);
            }
            return addedSubCatIds;
        }

        /// <summary>
        /// Add items to the category
        /// </summary>
        /// <param name="ids">items</param>
        /// <param name="parentCatId">category</param>
        /// <returns></returns>
        public Dictionary<int, string> AddItemstoCategory(string selectedItemDetails, int parentCatId)
        {
            Dictionary<int, string> itemsAdded = new Dictionary<int, string>();
            List<string> addedItemNames = new List<string>(), alreadyExistItems = new List<string>(), invalidItems = new List<string>(), addeditemIds = new List<string>();
            string catname = string.Empty, itemname = string.Empty;
            var orgCatId = 0;
            var srtOrder = 1;
            try
            {
                List<CheckedItemModel> itemsSelected = null;
                if (string.IsNullOrWhiteSpace(selectedItemDetails) == false)
                {
                    itemsSelected = JsonConvert.DeserializeObject<List<CheckedItemModel>>(selectedItemDetails);
                }

                if (itemsSelected == null)
                {
                    itemsSelected = new List<CheckedItemModel>();
                }
                if (itemsSelected != null)
                {
                    //Load the Nodes of the current tree
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    orgCatId = parentCatId;

                    //Get all rows from required link tables in this specific network Tree.
                    _categoryObjects = _repository.GetQuery<CategoryObject>(co => parentNetworkNodeIds.Contains(co.NetworkObjectId) || childNetworkNodeIds.Contains(co.NetworkObjectId) && co.CategoryId == parentCatId).Include("NetworkObject").Include("Category").ToList();

                    //make sure this was not an override for another category
                    var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == parentCatId && co.MenuId == MenuId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentCategoryId != null)
                    {
                        parentCatId = ovrride.ParentCategoryId.Value;
                    }
                    else
                    {
                        //make sure this was not an override for another category
                        var subcatovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == parentCatId).FirstOrDefault();
                        if (subcatovrride != null && subcatovrride.OverrideParentSubCategoryId != null)
                            parentCatId = subcatovrride.OverrideParentSubCategoryId.Value;
                    }

                    var cat = _repository.GetQuery<Category>(a => a.CategoryId == parentCatId).FirstOrDefault();
                    if (cat != null)
                    {
                        // get all links
                        var catObjects = _categoryObjects.Where(co => co.CategoryId == parentCatId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        var itemIds = itemsSelected.Select(x => x.Id).ToList();
                        var allItemToAdds = _repository.GetQuery<Item>(i => itemIds.Contains(i.ItemId)).ToList();

                        //assign srtorder for next item. If there are no item then start from 1
                        srtOrder = (catObjects == null || catObjects.Count() == 0) ? srtOrder : catObjects.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1;
                        //Add each item
                        foreach (var itemSelected in itemsSelected)
                        {
                            var itemToAdd = allItemToAdds.Where(i => i.ItemId == itemSelected.Id).FirstOrDefault();
                            bool hasItemAlreadyLinked = false;
                            bool isAddedBack = false;// Flag to check if this Item have delete override before and being added noew
                            // if it valid item then perform the action
                            if (itemToAdd != null)
                            {
                                itemname = itemToAdd.ItemName;
                                catname = cat.InternalName;

                                int? prntItemId = null;

                                // If this item is already present as link in any of the parent
                                if (catObjects != null && catObjects.Where(co => co.ItemId == itemSelected.Id).Count() > 0)
                                {
                                    //Get the last modified record
                                    var lastcatObj = catObjects.Where(co => co.ItemId == itemSelected.Id || (co.ParentItemId.HasValue && co.ParentItemId == itemToAdd.OverrideItemId)).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).ThenByDescending(co => co.ParentItemId).FirstOrDefault();

                                    //IF the link to this item is overriden by last parent to delete
                                    if (lastcatObj.OverrideStatus == OverrideStatus.HIDDEN)
                                    {
                                        //To Make it active now add override record for the deleted record.
                                        hasItemAlreadyLinked = false;
                                        prntItemId = lastcatObj.ParentItemId;

                                        // If this item is overriden to be deleted by this n/w and now being added again - delete the delete override
                                        //var deletedLink = catObjects.Where(co => co.ItemId == itemId && co.NetworkObjectId == netId).FirstOrDefault();
                                        if (lastcatObj.NetworkObjectId == NetworkObjectId)
                                        {
                                            _repository.Delete<CategoryObject>(lastcatObj);
                                            isAddedBack = true;
                                            hasItemAlreadyLinked = true;
                                        }
                                    }
                                    else
                                    {
                                        //else, this item is already present
                                        hasItemAlreadyLinked = true;
                                    }
                                }

                                //Insert New record if it is not present
                                if (!hasItemAlreadyLinked || isAddedBack)
                                {
                                    //Update the displayName if it is empty. Before adding to Men
                                    if (string.IsNullOrWhiteSpace(itemToAdd.DisplayName))
                                    {
                                        itemToAdd.DisplayName = itemToAdd.ItemName;
                                        _repository.Update<Item>(itemToAdd);
                                        itemsAdded.Add(itemToAdd.ItemId, "AddedandItemUpdated");
                                    }

                                    //if same item is added by childs before. delete those records
                                    var similarLinkatChildNetworks = _categoryObjects.Where(co => co.CategoryId == parentCatId && co.ItemId == itemToAdd.ItemId && co.OverrideStatus != OverrideStatus.HIDDEN && childNetworkNodeIds.Contains(co.NetworkObjectId));
                                    foreach (var chld in similarLinkatChildNetworks)
                                    {
                                        _repository.Delete<CategoryObject>(chld);
                                    }

                                    if (hasItemAlreadyLinked == false)
                                    {
                                        var newItm = new Item();
                                        if (itemToAdd.ParentItemId.HasValue == false && itemToAdd.OverrideItemId.HasValue == false)
                                        {
                                            mapItemtoItem(itemToAdd, ref newItm, true,itemSelected.POSDataId);
                                            newItm.ItemId = 0;
                                            newItm.ParentItemId = itemToAdd.ItemId;
                                            newItm.IsAvailable = false; // Add item as not available by default
                                            newItm.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);
                                            newItm.UpdatedDate = newItm.CreatedDate = DateTime.UtcNow;
                                            newItm.NetworkObjectId = NetworkObjectId;
                                            newItm.MenuId = MenuId;
                                        }
                                        else
                                        {
                                            newItm = itemToAdd;
                                        }
                                        //Adding Item to Category
                                        CategoryObject categoryObjToAdd = new CategoryObject
                                        {
                                            CategoryId = parentCatId,
                                            Item = newItm,
                                            NetworkObjectId = NetworkObjectId,
                                            OverrideStatus = OverrideStatus.ACTIVE,
                                            SortOrder = srtOrder,
                                            ParentItemId = prntItemId
                                        };
                                        _repository.Add<CategoryObject>(categoryObjToAdd);

                                        //maintain the SortOrder for next itemIds
                                        srtOrder = srtOrder + 1;
                                    }
                                    // added to list of items added
                                    if (!itemsAdded.ContainsKey(itemToAdd.ItemId))
                                    {
                                        itemsAdded.Add(itemToAdd.ItemId, "Added");
                                    }
                                    addedItemNames.Add(itemToAdd.ItemName);
                                    addeditemIds.Add(itemToAdd.ItemName);
                                }
                                else
                                {
                                    alreadyExistItems.Add(itemToAdd.ItemName);
                                }
                            }
                            else
                            {
                                invalidItems.Add(itemToAdd.ItemName);
                            }
                        }

                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        if (addedItemNames.Any())
                        {
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatObjectLinkAddT, string.Join(",", addedItemNames), cat.InternalName));
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatObjectLinkAddT, string.Join(",", addedItemNames), cat.InternalName);
                        }
                        if (alreadyExistItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatObjectLinkExistT, string.Join(",", alreadyExistItems), cat.InternalName);
                        }
                        if (invalidItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, string.Join(",", invalidItems));
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidCatT, cat.InternalName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatObjectLinkAddT, itemname, catname);
                Logger.WriteError(ex);
            }
            return itemsAdded;
        }

        /// <summary>
        /// Add collection to the item
        /// </summary>
        /// <param name="ids">collections</param>
        /// <param name="parentItemId">item</param>
        /// <returns></returns>
        public List<string> AddCollectionstoItem(int[] collectionIds, int parentItemId)
        {
            string itemname = string.Empty, colname = string.Empty;
            List<string> addedColNames = new List<string>(), alreadyExistCols = new List<string>(), invalidCols = new List<string>(), errCols = new List<string>(), addedcolIds = new List<string>();
            var originalId = 0;
            var srtOrder = 0;
            var isAllowedToAdd = false;
            try
            {
                if (collectionIds != null)
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    originalId = parentItemId;


                    //make sure this was not an override for another Item
                    var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == parentItemId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentItemId != null)
                    {
                        parentItemId = ovrride.ParentItemId.Value;
                    }
                    else
                    {
                        //make sure this was not an override for another Item
                        var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == parentItemId).FirstOrDefault();
                        if (collObjovrride != null && collObjovrride.ParentItemId != null)
                            parentItemId = collObjovrride.ParentItemId.Value;
                    }
                    _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(co => parentNetworkNodeIds.Contains(co.NetworkObjectId) || childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemId == parentItemId).Include("NetworkObject").ToList();


                    var item = _repository.GetQuery<Item>(i => i.ItemId == parentItemId).FirstOrDefault();
                    if (item != null)
                    {
                        //Get parent Collection Ids to check eligibilty before proceeding to Add Collection
                        var loop = true;
                        var allParentCollectionIdsToItem = new List<int>();
                        var itemIds = new List<int>();
                        itemIds.Add(parentItemId);
                        do
                        {
                            // pass Item Id and find all collection under it. 
                            //When the function returns it updates the flag "loop", next updated collectionIds that should be searched next and All the collections using current Item as child
                            //Loop through for each item until you reach top Item to find all parent level collections
                            loop = getparentCollections(itemIds, allParentCollectionIdsToItem);
                        } while (loop);

                        //Get current collection links
                        var itmCollectionLinks = _itemCollectionLinks.Where(co => co.ItemId == parentItemId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        var allItemCollections = _repository.GetQuery<ItemCollection>(a => collectionIds.Contains(a.CollectionId)).ToList();
                        //assign srtorder for next item. If there are no item then start from 1
                        srtOrder = (itmCollectionLinks == null || itmCollectionLinks.Count() == 0) ? srtOrder : itmCollectionLinks.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1;

                        foreach (var collectionId in collectionIds)
                        {
                            //Check for the eligibility to add Collection
                            isAllowedToAdd = getEligibilityToAddCollection(collectionId, allParentCollectionIdsToItem);

                            var collectionToAdd = allItemCollections.Where(a => a.CollectionId == collectionId).FirstOrDefault();
                            if (isAllowedToAdd)
                            {
                                bool hasItemAlreadyLinked = false;
                                var isAddedBack = false;// Flag to check if this Item have delete override before and being added noew
                                // if it is valid collection then perform the action
                                if (collectionToAdd != null)
                                {
                                    itemname = item.ItemName;
                                    colname = collectionToAdd.InternalName;
                                    int? prntColId = null;
                                    //get this collection record
                                    if (itmCollectionLinks != null && itmCollectionLinks.Where(co => co.CollectionId == collectionId).Count() > 0)
                                    {
                                        var lastItemColl = itmCollectionLinks.Where(co => co.CollectionId == collectionId || (co.ParentCollectionId.HasValue && co.ParentCollectionId == collectionToAdd.OverrideCollectionId)).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                                        //IF it is overriden by last parent to delete
                                        if (lastItemColl.OverrideStatus == OverrideStatus.HIDDEN)
                                        {
                                            hasItemAlreadyLinked = false;
                                            prntColId = lastItemColl.ParentCollectionId;
                                            // If this is overriden to be deleted by this n/w and now being added again
                                            if (lastItemColl.NetworkObjectId == NetworkObjectId)
                                            {
                                                hasItemAlreadyLinked = true;
                                                isAddedBack = true;
                                                _repository.Delete<ItemCollectionLink>(lastItemColl);
                                            }
                                        }
                                        else
                                        {
                                            hasItemAlreadyLinked = true;
                                        }
                                    }
                                    //Insert New record if it is not present
                                    //If it is added back, make sure the addtions at child are taken care
                                    if (!hasItemAlreadyLinked || isAddedBack)
                                    {
                                        //if same item is added by childs before. delete those records
                                        var similarLinkatChildNetworks = _itemCollectionLinks.Where(co => co.CollectionId == collectionId && co.ItemId == parentItemId && co.OverrideStatus != OverrideStatus.HIDDEN && childNetworkNodeIds.Contains(co.NetworkObjectId));
                                        foreach (var chld in similarLinkatChildNetworks)
                                        {
                                            _repository.Delete<ItemCollectionLink>(chld);
                                        }
                                        if (hasItemAlreadyLinked == false)
                                        {
                                            ItemCollectionLink itmLink = new ItemCollectionLink
                                            {
                                                ItemId = parentItemId,
                                                CollectionId = collectionToAdd.CollectionId,
                                                NetworkObjectId = NetworkObjectId,
                                                OverrideStatus = OverrideStatus.ACTIVE,
                                                SortOrder = srtOrder,
                                                ParentCollectionId = prntColId
                                            };
                                            //main tain sort Order for next items
                                            srtOrder = srtOrder + 1;

                                            _repository.Add<ItemCollectionLink>(itmLink);
                                        }

                                        //add to the list of addeditems to notify UI to refresh
                                        if (!addedcolIds.Contains(collectionToAdd.CollectionId.ToString()))
                                        {
                                            addedColNames.Add(collectionToAdd.InternalName);
                                            addedcolIds.Add(collectionToAdd.CollectionId.ToString());
                                        }
                                    }
                                    else
                                    {
                                        alreadyExistCols.Add(collectionToAdd.InternalName);
                                    }
                                }
                                else
                                {
                                    invalidCols.Add(collectionToAdd.InternalName);
                                }
                            }
                            else
                            {
                                errCols.Add(collectionToAdd.InternalName);
                            }

                        }

                        //Update the Menu date
                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        //Set Messages to be displayed on UI
                        if (addedColNames.Any())
                        {
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemColLinkAddT, string.Join(",", addedColNames), item.ItemName));
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColLinkAddT, string.Join(",", addedColNames), item.ItemName);
                        }
                        if (alreadyExistCols.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColLinkExistT, string.Join(",", alreadyExistCols), item.ItemName);
                        }
                        if (invalidCols.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItmColT, string.Join(",", invalidCols));
                        }
                        if (errCols.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColLinkNotAddedT, string.Join(",", errCols), item.ItemName);
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, item.ItemName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColLinkAddT, colname, itemname);
                Logger.WriteError(ex);
            }
            return addedcolIds;
        }

        /// <summary>
        /// Add collection to the item
        /// </summary>
        /// <param name="ids">collections</param>
        /// <param name="parentColId">item</param>
        /// <returns></returns>
        public Dictionary<int, string> AddItemstoCollection(string selectedItemDetails, int parentColId)
        {
            Dictionary<int, string> items = new Dictionary<int, string>();
            List<string> addedItems = new List<string>(), alreadyExistItems = new List<string>(), invalidItems = new List<string>(), errItems = new List<string>(), addedItemIds = new List<string>();
            string itemname = string.Empty, colname = string.Empty;
            var orgColId = 0;
            var srtOrder = 1;
            try
            {
                List<CheckedItemModel> itemsSelected = null;
                if (string.IsNullOrWhiteSpace(selectedItemDetails) == false)
                {
                    itemsSelected = JsonConvert.DeserializeObject<List<CheckedItemModel>>(selectedItemDetails);
                }

                if (itemsSelected == null)
                {
                    itemsSelected = new List<CheckedItemModel>();
                }
                if (itemsSelected != null)
                {
                    //Get Current Tree Nodes
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    orgColId = parentColId;
                    _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(co => parentNetworkNodeIds.Contains(co.NetworkObjectId) || childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemCollection.MenuId == MenuId).Include("NetworkObject").Include("ItemCollection").ToList();

                    var isAllowedToAdd = true;

                    //make sure this was not an override for another collection
                    var ovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == parentColId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentCollectionId != null)
                        parentColId = ovrride.ParentCollectionId.Value;

                    var col = _repository.GetQuery<ItemCollection>(a => a.CollectionId == parentColId).FirstOrDefault();
                    if (col != null)
                    {
                        //Get Parent ItemIds to check the eligibility to Add Item
                        var loop = true;
                        var parentItems = new List<int>();
                        var collectionIds = new List<int>();
                        collectionIds.Add(orgColId);
                        collectionIds.Add(parentColId);
                        _commonService.ParentNetworkNodes = parentNetworkNodeIds;
                        do
                        {
                            // pass Col Id and find all Item using it. 
                            // When the function returns it updates the flag "loop", next updated collectionIds that should be searched next and All the items using current Collection as child
                            //Loop through for each collection until you reach top Collection to find all parent level Items
                            loop = _commonService.GetparentItemsFromCollection(collectionIds, parentItems);

                        } while (loop);

                        var itmColObjs = _itemCollectionObjects.Where(co => co.CollectionId == parentColId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        //assign srtorder for next item. If there are no item then start from 1
                        srtOrder = (itmColObjs == null || itmColObjs.Count() == 0) ? srtOrder : itmColObjs.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1;
                        var itemIds = itemsSelected.Select(x => x.Id).ToList();

                        var allItems = _repository.GetQuery<Item>(i => itemIds.Contains(i.ItemId)).ToList();
                        foreach (var itemSelected in itemsSelected)
                        {
                            //Check the eligiblity before added the Item
                            isAllowedToAdd = getEligibilityToAddItem(itemSelected.Id, parentItems);
                            var itemToAdd = allItems.Where(i => i.ItemId == itemSelected.Id).FirstOrDefault();

                            if (isAllowedToAdd)
                            {

                                bool hasItemLinked = false;
                                bool isAddedBack = false;// Flag to check if this Item have delete override before and being added noew
                                // if it is valid collection then perform the action
                                if (itemToAdd != null)
                                {
                                    itemname = itemToAdd.ItemName;
                                    colname = col.InternalName;

                                    int? prntItemId = null;
                                    if (itmColObjs != null && itmColObjs.Where(co => co.ItemId == itemSelected.Id).Count() > 0)
                                    {
                                        var lastItemColItem = itmColObjs.Where(co => co.ItemId == itemSelected.Id || (co.ParentItemId.HasValue && co.ParentItemId == itemToAdd.OverrideItemId)).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).ThenByDescending(co => co.ParentItemId).FirstOrDefault();

                                        //IF it is overriden by last parent to delete
                                        if (lastItemColItem.OverrideStatus == OverrideStatus.HIDDEN)
                                        {
                                            hasItemLinked = false;
                                            prntItemId = lastItemColItem.ParentItemId;
                                            // If this  is overriden to be deleted by this n/w and now being added again
                                            if (lastItemColItem.NetworkObjectId == NetworkObjectId)
                                            {
                                                _repository.Delete<ItemCollectionObject>(lastItemColItem);
                                                isAddedBack = true;
                                                hasItemLinked = true;
                                            }
                                        }
                                        else
                                        {
                                            hasItemLinked = true;
                                        }
                                    }

                                    //Insert New record if it is not present
                                    if (!hasItemLinked || isAddedBack)
                                    {
                                        //Update the displayName if it is empty. Before adding to Men
                                        if (string.IsNullOrWhiteSpace(itemToAdd.DisplayName))
                                        {
                                            itemToAdd.DisplayName = itemToAdd.ItemName;
                                            _repository.Update<Item>(itemToAdd);
                                            items.Add(itemToAdd.ItemId, "AddedandItemUpdated");
                                        }

                                        //if same item is added by childs before. delete those records
                                        var childs = _itemCollectionObjects.Where(co => co.ItemId == itemSelected.Id && co.CollectionId == parentColId && co.OverrideStatus != OverrideStatus.HIDDEN && childNetworkNodeIds.Contains(co.NetworkObjectId));
                                        foreach (var chld in childs)
                                        {
                                            _repository.Delete<ItemCollectionObject>(chld);
                                        }
                                        if (hasItemLinked == false)
                                        {
                                            //MMS-15: Add Ability to have multiple menu items per master item
                                            var newItm = new Item();
                                            if (itemToAdd.ParentItemId.HasValue == false && itemToAdd.OverrideItemId.HasValue == false)
                                            {
                                                mapItemtoItem(itemToAdd, ref newItm, true,itemSelected.POSDataId);
                                                newItm.ItemId = 0;
                                                newItm.ParentItemId = itemToAdd.ItemId;
                                                newItm.IsAvailable = false; // Add item as not available by default
                                                newItm.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);
                                                newItm.UpdatedDate = newItm.CreatedDate = DateTime.UtcNow;
                                                newItm.NetworkObjectId = NetworkObjectId;
                                                newItm.MenuId = MenuId;
                                            }
                                            else
                                            {
                                                newItm = itemToAdd;
                                            }

                                            ItemCollectionObject itmObj = new ItemCollectionObject
                                            {
                                                Item = newItm,
                                                CollectionId = col.CollectionId,
                                                NetworkObjectId = NetworkObjectId,
                                                OverrideStatus = OverrideStatus.ACTIVE,
                                                SortOrder = srtOrder,
                                                ParentItemId = prntItemId
                                            };

                                            //Maintain the sortOrder for next Items
                                            srtOrder = srtOrder + 1;

                                            _repository.Add<ItemCollectionObject>(itmObj);
                                        }
                                        //Added to a list to notify UI for refresh
                                        if (!items.ContainsKey(itemToAdd.ItemId))
                                        {
                                            items.Add(itemToAdd.ItemId, "Added");
                                        }
                                        addedItems.Add(itemToAdd.ItemName);
                                        addedItemIds.Add(itemToAdd.ItemId.ToString());
                                    }
                                    else
                                    {
                                        alreadyExistItems.Add(itemToAdd.ItemName);
                                    }
                                }
                                else
                                {
                                    if (itemToAdd != null)
                                    {
                                        invalidItems.Add(itemToAdd.ItemName);
                                    }
                                }
                            }
                            else
                            {
                                errItems.Add(itemToAdd.ItemName);
                            }

                        }

                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        if (addedItems.Any())
                        {
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemColObjectAddT, string.Join(",", addedItems), col.InternalName));
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColObjectAddT, string.Join(",", addedItems), col.InternalName);
                        }
                        if (alreadyExistItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColObjectExistT, string.Join(",", alreadyExistItems), col.InternalName);
                        }
                        if (invalidItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, string.Join(",", invalidItems));
                        }
                        if (errItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrItemColObjectNotAddedT, string.Join(",", errItems), col.InternalName);
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItmColT, col.InternalName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColObjectAddT, itemname, colname);
                Logger.WriteError(ex);
            }
            return items;
        }

        /// <summary>
        /// Add PrependItem to the item
        /// </summary>
        /// <param name="ids">prependItemd</param>
        /// <param name="colId">item</param>
        /// <returns></returns>
        public Dictionary<int, string> AddPrependItemstoItem(string selectedPrependItemDetails, int itmId)
        {
            Dictionary<int, string> itemsAdded = new Dictionary<int, string>();
            List<string> addedItemNames = new List<string>(), alreadyExistItems = new List<string>(), invalidItems = new List<string>(), errItems = new List<string>(), addeditemIds = new List<string>();
            string itmname = string.Empty, prependItemname = string.Empty;
            var orgItemId = 0;
            var srtOrder = 1;
            try
            {
                List<CheckedItemModel> prependItemsSelected = null;
                if (string.IsNullOrWhiteSpace(selectedPrependItemDetails) == false)
                {
                    prependItemsSelected = JsonConvert.DeserializeObject<List<CheckedItemModel>>(selectedPrependItemDetails);
                }

                if (prependItemsSelected == null)
                {
                    prependItemsSelected = new List<CheckedItemModel>();
                }
                if (prependItemsSelected != null)
                {
                    var mnuNetworkObjectLink = getMnuNwLinkAddIfNotexist(MenuId, NetworkObjectId);
                    //Load the Nodes of the current tree
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                    parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);
                    childMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(childNetworkNodeIds, MenuId);

                    orgItemId = itmId;

                    //Get all rows from required link tables in this specific network Tree.
                    _prependItemLinks = _repository.GetQuery<PrependItemLink>(co => parentMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId) || childMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId) && co.MenuNetworkObjectLink.MenuId == MenuId).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").Include("Item").ToList();

                    //make sure this was not an override for another Item
                    var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itmId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentItemId != null)
                    {
                        itmId = ovrride.ParentItemId.Value;
                    }
                    else
                    {
                        //make sure this was not an override for another Item
                        var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itmId).FirstOrDefault();
                        if (collObjovrride != null && collObjovrride.ParentItemId != null)
                            itmId = collObjovrride.ParentItemId.Value;
                    }

                    var isAllowedToAdd = true;

                    var item = _repository.GetQuery<Item>(a => a.ItemId == itmId).FirstOrDefault();
                    if (item != null)
                    {
                        //Get Parent ItemIds to check the eligibility to Add PrependItem
                        var loop = true;
                        var parentItemIdsToRestrict = new List<int>();
                        var itemIdsToGetRestrictIds = new List<int>();
                        itemIdsToGetRestrictIds.Add(itmId);
                        itemIdsToGetRestrictIds.Add(orgItemId);
                        _commonService.ParentNetworkNodes = parentNetworkNodeIds;
                        do
                        {
                            // pass item Id and find all Item using it as child. 
                            // When the function returns it updates the flag "loop", and updated itemIds that should be searched next and get the items using current item as collection object
                            //Loop through for each item until you reach top item to find all parent level Items
                            loop = _commonService.GetparentItemsFromItem(itemIdsToGetRestrictIds, parentItemIdsToRestrict);

                        } while (loop);

                        itemIdsToGetRestrictIds.Add(itmId);
                        itemIdsToGetRestrictIds.Add(orgItemId);

                        loop = true;
                        do
                        {
                            // pass item Id and find all Item using it as prepend. 
                            // When the function returns it updates the flag "loop", and updated itemIds that should be searched next and get the items using current item as prepend
                            //Loop through for each item until you reach top prenpenditem to find all parent level prependItems
                            loop = _commonService.GetparentPrependItemsFromItem(itemIdsToGetRestrictIds, parentItemIdsToRestrict);

                        } while (loop);
                        // get all links
                        var prependLinks = _prependItemLinks.Where(co => co.ItemId == itmId && parentMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId)).ToList();
                        var prependIds = prependItemsSelected.Select(x => x.Id).ToList();
                        var allPrependItemToAdds = _repository.GetQuery<Item>(i => prependIds.Contains(i.ItemId)).ToList();

                        //assign srtorder for next item. If there are no item then start from 1
                        srtOrder = (prependLinks == null || prependLinks.Count() == 0) ? srtOrder : prependLinks.Any(x => x.OverrideStatus != OverrideStatus.HIDDEN) ? prependLinks.OrderByDescending(c => c.SortOrder).FirstOrDefault().SortOrder + 1 : srtOrder;
                        //Add each item
                        foreach (var prepItemSelected in prependItemsSelected)
                        {
                            //Check the eligiblity before added the prependItem
                            isAllowedToAdd = getEligibilityToAddPrependItem(prepItemSelected.Id, parentItemIdsToRestrict);

                            if (isAllowedToAdd)
                            {
                                var prependItem = allPrependItemToAdds.Where(i => i.ItemId == prepItemSelected.Id).FirstOrDefault();
                                bool hasItemAlreadyLinked = false;
                                bool isAddedBack = false;// Flag to check if this Item have delete override before and being added noew
                                // if it valid item then perform the action
                                if (prependItem != null)
                                {
                                    prependItemname = prependItem.ItemName;
                                    itmname = item.ItemName;

                                    int? prntItemId = null;

                                    // If this item is already present as link in any of the parent
                                    if (prependLinks != null && prependLinks.Where(co => co.PrependItemId == prepItemSelected.Id).Count() > 0)
                                    {
                                        //Get the last modified record
                                        var lastprependitemLink = prependLinks.Where(co => co.PrependItemId == prepItemSelected.Id || (co.OverrideParentPrependItemId.HasValue && co.OverrideParentPrependItemId == prependItem.OverrideItemId)).OrderByDescending(co => co.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).ThenByDescending(co => co.OverrideParentPrependItemId).FirstOrDefault();

                                        //IF the link to this item is overriden by last parent to delete
                                        if (lastprependitemLink.OverrideStatus == OverrideStatus.HIDDEN)
                                        {
                                            //To Make it active now add override record for the deleted record.
                                            hasItemAlreadyLinked = false;
                                            prntItemId = lastprependitemLink.OverrideParentPrependItemId;

                                            // If this item is overriden to be deleted by this n/w and now being added again - delete the delete override
                                            //var deletedLink = catObjects.Where(co => co.ItemId == itemId && co.NetworkObjectId == netId).FirstOrDefault();
                                            if (lastprependitemLink.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId)
                                            {
                                                _repository.Delete<PrependItemLink>(lastprependitemLink);
                                                isAddedBack = true;
                                                hasItemAlreadyLinked = true;
                                            }
                                        }
                                        else
                                        {
                                            //else, this item is already present
                                            hasItemAlreadyLinked = true;
                                        }
                                    }

                                    //Insert New record if it is not present
                                    if (!hasItemAlreadyLinked || isAddedBack)
                                    {
                                        //Update the displayName if it is empty. Before adding to Men
                                        if (string.IsNullOrWhiteSpace(prependItem.DisplayName))
                                        {
                                            prependItem.DisplayName = prependItem.ItemName;
                                            _repository.Update<Item>(prependItem);
                                            itemsAdded.Add(prependItem.ItemId, "AddedandItemUpdated");
                                        }

                                        //if same item is added by childs before. delete those records
                                        var similarLinkatChildNetworks = _prependItemLinks.Where(co => co.ItemId == itmId && co.PrependItemId == prependItem.ItemId && co.OverrideStatus != OverrideStatus.HIDDEN && childMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId));
                                        foreach (var chld in similarLinkatChildNetworks)
                                        {
                                            _repository.Delete<PrependItemLink>(chld);
                                        }
                                        if (hasItemAlreadyLinked == false)
                                        {
                                            var newItm = new Item();
                                            if (prependItem.ParentItemId.HasValue == false && prependItem.OverrideItemId.HasValue == false)
                                            {
                                                mapItemtoItem(prependItem, ref newItm, true,prepItemSelected.POSDataId);
                                                newItm.ItemId = 0;
                                                newItm.ParentItemId = prependItem.ItemId;
                                                newItm.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);
                                                newItm.UpdatedDate = newItm.CreatedDate = DateTime.UtcNow;
                                                newItm.NetworkObjectId = NetworkObjectId;
                                                newItm.MenuId = MenuId;
                                            }
                                            else
                                            {
                                                newItm = prependItem;
                                            }
                                            //Adding Item to Category
                                            PrependItemLink prependLinkToAdd = new PrependItemLink
                                            {
                                                ItemId = itmId,
                                                PrependItem = newItm,
                                                MenuNetworkObjectLink = mnuNetworkObjectLink,
                                                OverrideStatus = OverrideStatus.ACTIVE,
                                                SortOrder = srtOrder,
                                                OverrideParentPrependItemId = prntItemId
                                            };
                                            //maintain the SortOrder for next itemIds
                                            srtOrder = srtOrder + 1;

                                            _repository.Add<PrependItemLink>(prependLinkToAdd);

                                        }// added to list of items added
                                        if (!itemsAdded.ContainsKey(prependItem.ItemId))
                                        {
                                            itemsAdded.Add(prependItem.ItemId, "Added");
                                        }
                                        addedItemNames.Add(prependItem.ItemName);
                                        addeditemIds.Add(prependItem.ItemName);
                                    }
                                    else
                                    {
                                        alreadyExistItems.Add(prependItem.ItemName);
                                    }
                                }
                                else
                                {
                                    invalidItems.Add(prependItem.ItemName);
                                }
                            }
                            else
                            {
                                errItems.Add(item.ItemName);
                            }
                        }

                        _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                        _context.SaveChanges();

                        if (addedItemNames.Any())
                        {
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.PrependItemLinkAddT, string.Join(",", addedItemNames), item.ItemName));
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.PrependItemLinkAddT, string.Join(",", addedItemNames), item.ItemName);
                        }
                        if (alreadyExistItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.PrependItemLinkExistT, string.Join(",", alreadyExistItems), item.ItemName);
                        }
                        if (invalidItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, string.Join(",", invalidItems));
                        }
                        if (errItems.Any())
                        {
                            _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrNotEligiblePrependItemT, string.Join(",", errItems), item.ItemName);
                        }
                    }
                    else
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? "" : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidItemT, item.ItemName);
                    }
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPrependItemLinkAddT, prependItemname, itmname);
                Logger.WriteError(ex);
            }
            return itemsAdded;
        }

        /// <summary>
        /// Changes the position of catgories in a given menu for current NW
        /// </summary>
        /// <param name="categoryGridItems"></param>
        /// <param name="menuId"></param>
        public void ChangeCatMenuLinkPositions(List<MenuGridItem> categoryGridItems, int menuId)
        {
            var categoryNames = new List<string>();
            var categoryIds = new List<string>();
            var ErrCatNames = new List<string>();
            var catName = string.Empty;
            try
            {
                //Get parent Networks
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(co => co.MenuId == menuId && parentNetworkNodeIds.Contains(co.NetworkObjectId) && co.OverrideStatus != OverrideStatus.HIDDEN).Include("Category").Include("NetworkObject").OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).ToList();

                foreach (var categoryGridItem in categoryGridItems)
                {
                    // get current cat position
                    var cml = _categoryMenuLinks.Where(co => co.CategoryId == categoryGridItem.entityid).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (cml != null)
                    {
                        // adding names to statusmessage
                        catName = cml.Category.InternalName;
                        categoryNames.Add(cml.Category.InternalName);
                        categoryIds.Add(cml.Category.CategoryId.ToString());

                        // If this link overriden at current NW , then just update the position
                        if (cml.NetworkObjectId == NetworkObjectId)
                        {
                            cml.SortOrder = categoryGridItem.SortOrder;
                        }
                        //Else create new link at current NW level. Make sure that if it is override maintain the parent
                        else if (cml.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = cml.SortOrder;
                            CategoryMenuLink newcml = new CategoryMenuLink
                            {
                                CategoryId = cml.CategoryId,
                                MenuId = cml.MenuId,
                                OverrideStatus = OverrideStatus.MOVED,
                                ParentCategoryId = cml.ParentCategoryId.HasValue ? cml.ParentCategoryId : cml.CategoryId,
                                NetworkObjectId = NetworkObjectId,
                                SortOrder = categoryGridItem.SortOrder
                            };
                            _repository.Add<CategoryMenuLink>(newcml);
                        }
                    }
                    else
                    {
                        // Add to error list
                        ErrCatNames.Add(categoryGridItem.InternalName);
                    }
                }
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //After all positions are modified, save the changes to DB
                _context.SaveChanges();
                //Update the statusMessages
                if (categoryNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatMoveT, string.Join(",", categoryNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatMoveT, categoryNames.ToString().TrimEnd(',')));
                }
                if (ErrCatNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrCatMoveT, string.Join(",", ErrCatNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrCatMoveT, catName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// CHange the position of subCats in a given cat for current NW
        /// </summary>
        /// <param name="subCategoryMenuGridItems"></param>
        /// <param name="prntCatId"></param>
        public void ChangeSubCatLinkPositions(List<MenuGridItem> subCategoryMenuGridItems, int prntCatId)
        {
            var catNames = new List<string>();
            var catIds = new List<string>();
            var ErrCatNames = new List<string>();
            var catName = string.Empty;
            try
            {
                //Get parent Networks
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == prntCatId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    prntCatId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var subCatgeoryovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == prntCatId).FirstOrDefault();
                    if (subCatgeoryovrride != null && subCatgeoryovrride.OverrideParentSubCategoryId != null)
                        prntCatId = subCatgeoryovrride.OverrideParentSubCategoryId.Value;
                }

                _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(co => co.CategoryId == prntCatId && parentNetworkNodeIds.Contains(co.NetworkObjectId) && co.OverrideStatus != OverrideStatus.HIDDEN).Include("SubCategory").Include("NetworkObject").ToList();

                foreach (var subCategoryGridItem in subCategoryMenuGridItems)
                {
                    // get current cat position
                    var scl = _subCategoryLinks.Where(co => co.SubCategoryId == subCategoryGridItem.entityid).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (scl != null)
                    {
                        // adding names to statusmessage
                        catName = scl.SubCategory.InternalName;
                        catNames.Add(scl.SubCategory.InternalName);
                        catIds.Add(scl.SubCategory.CategoryId.ToString());

                        // If this link overriden at current NW , then just update the position
                        if (scl.NetworkObjectId == NetworkObjectId)
                        {
                            scl.SortOrder = subCategoryGridItem.SortOrder;
                        }
                        //Else create new link at current NW level. Make sure that if it is override maintain the parent
                        else if (scl.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = scl.SortOrder;
                            SubCategoryLink newcml = new SubCategoryLink
                            {
                                CategoryId = scl.CategoryId,
                                SubCategoryId = scl.SubCategoryId,
                                OverrideStatus = OverrideStatus.MOVED,
                                OverrideParentSubCategoryId = scl.OverrideParentSubCategoryId.HasValue ? scl.OverrideParentSubCategoryId : scl.SubCategoryId,
                                NetworkObjectId = NetworkObjectId,
                                SortOrder = subCategoryGridItem.SortOrder
                            };
                            _repository.Add<SubCategoryLink>(newcml);
                        }
                    }
                    else
                    {
                        // Add to error list
                        ErrCatNames.Add(subCategoryGridItem.InternalName);
                    }
                }

                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //After all positions are modified, save the changes to DB
                _context.SaveChanges();
                //Update the statusMessages
                if (catNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.SubCatMoveT, string.Join(",", catNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.SubCatMoveT, catNames.ToString().TrimEnd(',')));
                }
                if (ErrCatNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrSubCatMoveT, string.Join(",", ErrCatNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrSubCatMoveT, catName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// CHange the position of items in a given cat for current NW
        /// </summary>
        /// <param name="collectionItemGridItems"></param>
        /// <param name="colId"></param>
        public void ChangeItemCollectionObjectPositions(List<MenuGridItem> collectionItemGridItems, int colId)
        {
            var itemNames = new List<string>();
            var itemIds = new List<string>();
            var ErritmNames = new List<string>();
            var itemName = string.Empty;
            try
            {
                //get parent Networks for current NW
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);


                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == colId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCollectionId != null)
                    colId = ovrride.ParentCollectionId.Value;

                _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(co => co.CollectionId == colId && co.OverrideStatus != OverrideStatus.HIDDEN && parentNetworkNodeIds.Contains(co.NetworkObjectId)).Include("Item").Include("NetworkObject").ToList();

                foreach (var collectionItemGridItem in collectionItemGridItems)
                {
                    //get the latest position of the item
                    var itemColObj = _itemCollectionObjects.Where(co => co.ItemId == collectionItemGridItem.entityid).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (itemColObj != null)
                    {
                        //Add name to modified list
                        itemName = itemColObj.Item.ItemName;
                        itemNames.Add(itemColObj.Item.ItemName);
                        itemIds.Add(itemColObj.Item.ItemId.ToString());
                        //if the item already overriden at current NW then just update the same link
                        if (itemColObj.NetworkObjectId == NetworkObjectId)
                        {
                            itemColObj.SortOrder = collectionItemGridItem.SortOrder;
                            _repository.Update<ItemCollectionObject>(itemColObj);
                        }
                        // Else create new link at current NW and make sure that if it is override the status of override is maintained
                        else if (itemColObj.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = itemColObj.SortOrder;
                            ItemCollectionObject newco = new ItemCollectionObject
                            {
                                CollectionId = itemColObj.CollectionId,
                                ItemId = itemColObj.ItemId,
                                OverrideStatus = OverrideStatus.MOVED,
                                ParentItemId = itemColObj.ParentItemId.HasValue ? itemColObj.ParentItemId : itemColObj.ItemId,
                                IsAutoSelect = itemColObj.IsAutoSelect,
                                NetworkObjectId = NetworkObjectId,
                                SortOrder = collectionItemGridItem.SortOrder
                            };
                            _repository.Add<ItemCollectionObject>(newco);
                        }
                    }
                    else
                    {
                        //Add to Error list if link was not found
                        ErritmNames.Add(collectionItemGridItem.InternalName);
                    }
                }
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //After modifying all positions, save the changes to DB
                _context.SaveChanges();
                //UPdate the statusMessage to be displayed on UI and inserted in AuditLog
                if (itemNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemColObjectMoveT, string.Join(",", itemNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemColObjectMoveT, string.Join(",", itemNames.ToArray())));
                }
                if (ErritmNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrItemColObjectMoveT, string.Join(",", ErritmNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrItemColObjectMoveT, itemName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Change the position of a collections in a Item for current NW
        /// </summary>
        /// <param name="collectionMenuGridItems"></param>
        /// <param name="itemId"></param>
        public void ChangeItemCollectionLinkPositions(List<MenuGridItem> collectionMenuGridItems, int itemId)
        {
            var collectionName = string.Empty;
            var collectionNames = new List<string>();
            var collectionIds = new List<string>();
            var ErrcolNames = new List<string>();
            try
            {
                //get parent NWs for current NW
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itemId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    itemId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itemId).FirstOrDefault();
                    if (collObjovrride != null && collObjovrride.ParentItemId != null)
                        itemId = collObjovrride.ParentItemId.Value;
                }

                _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(co => co.ItemId == itemId && co.OverrideStatus != OverrideStatus.HIDDEN && parentNetworkNodeIds.Contains(co.NetworkObjectId)).Include("ItemCollection").Include("NetworkObject").ToList();

                foreach (var collectionGridItem in collectionMenuGridItems)
                {
                    // get the current item
                    var itemColln = _itemCollectionLinks.Where(co => co.CollectionId == collectionGridItem.entityid).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (itemColln != null)
                    {
                        collectionName = itemColln.ItemCollection.InternalName;
                        collectionNames.Add(itemColln.ItemCollection.InternalName);
                        collectionIds.Add(itemColln.ItemCollection.CollectionId.ToString());
                        // change the SortOrder if link is at this NW level
                        if (itemColln.NetworkObjectId == NetworkObjectId)
                        {
                            itemColln.SortOrder = collectionGridItem.SortOrder;
                            _repository.Update<ItemCollectionLink>(itemColln);
                        }
                        //Else, Add new link for current item at this NM level
                        else if (itemColln.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = itemColln.SortOrder;
                            ItemCollectionLink newco = new ItemCollectionLink
                            {
                                CollectionId = itemColln.CollectionId,
                                ItemId = itemColln.ItemId,
                                OverrideStatus = OverrideStatus.MOVED,
                                ParentCollectionId = itemColln.ParentCollectionId.HasValue ? itemColln.ParentCollectionId : itemColln.CollectionId,
                                NetworkObjectId = NetworkObjectId,
                                SortOrder = collectionGridItem.SortOrder
                            };
                            _repository.Add<ItemCollectionLink>(newco);
                        }
                    }
                    else
                    {
                        //Add to Error List if link is not found
                        ErrcolNames.Add(collectionGridItem.InternalName);
                    }
                }
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //Save all the position changes at once
                _context.SaveChanges();
                //Update the StatusMessages
                if (collectionNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.ItemCollnMoveT, string.Join(",", collectionNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemCollnMoveT, collectionNames.ToString().TrimEnd(',')));
                }
                if (ErrcolNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrItemColObjectMoveT, string.Join(",", ErrcolNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrItemCollnMoveT, collectionName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Change the position on Items in a Category for current NW
        /// </summary>
        /// <param name="itemMenuGridItems"></param>
        /// <param name="catId"></param>
        public void ChangeCatObjectPositions(List<MenuGridItem> itemMenuGridItems, int catId)
        {
            var itemName = string.Empty;
            var itemNames = new List<string>();
            var itemIds = new List<string>();
            var ErritmNames = new List<string>();
            try
            {
                //get parent NWs
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == catId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    catId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var subcategoryovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == catId).FirstOrDefault();
                    if (subcategoryovrride != null && subcategoryovrride.OverrideParentSubCategoryId != null)
                        catId = subcategoryovrride.OverrideParentSubCategoryId.Value;
                }

                _categoryObjects = _repository.GetQuery<CategoryObject>(co => co.CategoryId == catId && co.OverrideStatus != OverrideStatus.HIDDEN && parentNetworkNodeIds.Contains(co.NetworkObjectId)).Include("Item").Include("NetworkObject").ToList();

                foreach (var itemGridItem in itemMenuGridItems)
                {
                    var catObj = _categoryObjects.Where(co => co.ItemId == itemGridItem.entityid).OrderByDescending(co => co.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (catObj != null)
                    {
                        //Add to modified list
                        itemName = catObj.Item.ItemName;
                        itemNames.Add(catObj.Item.ItemName);
                        itemIds.Add(catObj.Item.ItemId.ToString());

                        //Change the position of current link if it is overriden by current NW
                        if (catObj.NetworkObjectId == NetworkObjectId)
                        {
                            catObj.SortOrder = itemGridItem.SortOrder;
                            _repository.Update<CategoryObject>(catObj);
                        }
                        //otherwise add new link in linktable for changing position
                        else if (catObj.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = catObj.SortOrder;
                            CategoryObject newco = new CategoryObject
                            {
                                CategoryId = catObj.CategoryId,
                                ItemId = catObj.ItemId,
                                OverrideStatus = OverrideStatus.MOVED,
                                ParentItemId = catObj.ParentItemId.HasValue ? catObj.ParentItemId : catObj.ItemId,
                                NetworkObjectId = NetworkObjectId,
                                SortOrder = itemGridItem.SortOrder
                            };
                            _repository.Add<CategoryObject>(newco);
                        }
                    }
                    else
                    {
                        //Add to Error list
                        ErritmNames.Add(itemGridItem.InternalName);
                    }
                }
                //Update the Menu Lastupdated Date
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //Save all changes to DB at once
                _context.SaveChanges();
                //Update the status messages accordingly
                if (itemNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.CatObjMoveT, string.Join(",", itemNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatObjMoveT, string.Join(",", itemNames.ToArray())));
                }
                if (ErritmNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrCatObjMoveT, string.Join(",", ErritmNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatObjMoveT, itemName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Change the position on PrependItems in a Item for current NW
        /// </summary>
        /// <param name="prependItemMenuGridItems"></param>
        /// <param name="itemId"></param>
        public void ChangePrependItemLinkPositions(List<MenuGridItem> prependItemMenuGridItems, int itemId)
        {
            var itemName = string.Empty;
            var itemNames = new List<string>();
            var itemIds = new List<string>();
            var ErritmNames = new List<string>();
            try
            {
                var mnuNetworkObjectLink = getMnuNwLinkAddIfNotexist(MenuId, NetworkObjectId);
                //get parent NWs
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);

                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itemId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    itemId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itemId).FirstOrDefault();
                    if (collObjovrride != null && collObjovrride.ParentItemId != null)
                        itemId = collObjovrride.ParentItemId.Value;
                }

                _prependItemLinks = _repository.GetQuery<PrependItemLink>(co => co.ItemId == itemId && co.OverrideStatus != OverrideStatus.HIDDEN && parentMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId)).Include("Item").Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();

                foreach (var itemGridItem in prependItemMenuGridItems)
                {
                    var prependItemLink = _prependItemLinks.Where(co => co.PrependItemId == itemGridItem.entityid).OrderByDescending(co => co.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    if (prependItemLink != null)
                    {
                        //Add to modified list
                        itemName = prependItemLink.PrependItem.ItemName;
                        itemNames.Add(prependItemLink.PrependItem.ItemName);
                        itemIds.Add(prependItemLink.PrependItem.ItemId.ToString());

                        //Change the position of current link if it is overriden by current NW
                        if (prependItemLink.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId)
                        {
                            prependItemLink.SortOrder = itemGridItem.SortOrder;
                            _repository.Update<PrependItemLink>(prependItemLink);
                        }
                        //otherwise add new link in linktable for changing position
                        else if (prependItemLink.MenuNetworkObjectLink.NetworkObjectId != NetworkObjectId)
                        {
                            var currentSortOrder = prependItemLink.SortOrder;
                            PrependItemLink newLink = new PrependItemLink
                            {
                                ItemId = prependItemLink.ItemId,
                                PrependItemId = prependItemLink.PrependItemId,
                                OverrideStatus = OverrideStatus.MOVED,
                                OverrideParentPrependItemId = prependItemLink.OverrideParentPrependItemId.HasValue ? prependItemLink.OverrideParentPrependItemId : prependItemLink.PrependItemId,
                                MenuNetworkObjectLink = mnuNetworkObjectLink,
                                SortOrder = itemGridItem.SortOrder
                            };
                            _repository.Add<PrependItemLink>(newLink);
                        }
                    }
                    else
                    {
                        //Add to Error list
                        ErritmNames.Add(itemGridItem.InternalName);
                    }
                }
                //Update the Menu Lastupdated Date
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                //Save all changes to DB at once
                _context.SaveChanges();
                //Update the status messages accordingly
                if (itemNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.PrependItemLinkMoveT, string.Join(",", itemNames.ToArray()));
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.PrependItemLinkMoveT, itemNames.ToString().TrimEnd(',')));
                }
                if (ErritmNames.Any())
                {
                    _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrPrependItemLinkMoveT, string.Join(",", ErritmNames.ToArray()));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPrependItemLinkMoveT, itemName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Delete Collection in a Item
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public string DeleteItemCollection(int collectionId, int itemId)
        {
            var itemname = "";
            var collectionName = "";
            var collectionTreeId = "";
            var itemIdbeforeCalculatingOvr = 0;
            CleanUpDataModel orphanRecords = new CleanUpDataModel();
            try
            {
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                itemIdbeforeCalculatingOvr = itemId;
                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itemId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    itemId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collectionObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itemId).FirstOrDefault();
                    if (collectionObjovrride != null && collectionObjovrride.ParentItemId != null)
                        itemId = collectionObjovrride.ParentItemId.Value;
                }

                _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => x.ItemId == itemId && parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                var currentItemCollectionLinks = _itemCollectionLinks.Where(co => co.ItemId == itemId && co.CollectionId == collectionId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();

                itemname = _repository.GetQuery<Item>(i => i.ItemId == itemId).FirstOrDefault().ItemName;
                collectionName = _repository.GetQuery<ItemCollection>(c => c.CollectionId == collectionId).FirstOrDefault().InternalName;
                var addDeleteLink = true;
                if (currentItemCollectionLinks != null)
                {
                    var parColId = 0;
                    var isprntId = false;
                    parColId = collectionId;
                    while (!isprntId)
                    {
                        isprntId = getMasterParentColId(parColId, itemId, out parColId);
                    }

                    //if this is overriden at this network level
                    var overrdenItemLink = currentItemCollectionLinks.Where(co => co.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.ParentCollectionId).FirstOrDefault();
                    if (overrdenItemLink != null)
                    {
                        //this is overriden at this NW
                        if (overrdenItemLink.ParentCollectionId.HasValue)
                        {
                            //if this not originated here or not if it has override then original item shouldn't be visible
                            addDeleteLink = true;
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }

                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    orphanRecords.CollectionIds.AddRange(_itemCollectionLinks.Where(co => co.ParentCollectionId == parColId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemId == itemId && co.CollectionId != co.ParentCollectionId).Select(x => x.CollectionId).Distinct());
                    //get the ids for the records being deleted
                    var itmColLinkIdsToDelete = _itemCollectionLinks.Where(co => co.ParentCollectionId == parColId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemId == itemId).Select(x => x.CollectionLinkId).ToList();
                    _repository.Delete<ItemCollectionLink>(co => co.ParentCollectionId == parColId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemId == itemId);


                    //If we are not adding delete override then it means we are this collection is being removed from all Networks. Hence delete all overrides and link which is added the item intially to Menu
                    if (!addDeleteLink)
                    {
                        //this collection is not used anywhere
                        if (overrdenItemLink != null)
                        {
                            orphanRecords.CollectionIds.Add(overrdenItemLink.CollectionId);
                        }
                    }
                    else
                    {
                        var oldItemCollectionLink = currentItemCollectionLinks.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        ItemCollectionLink itmLink = new ItemCollectionLink
                        {
                            CollectionId = parColId,
                            ItemId = oldItemCollectionLink.ItemId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldItemCollectionLink.SortOrder,
                            ParentCollectionId = parColId
                        };
                        _repository.Add<ItemCollectionLink>(itmLink);
                    }
                    if (overrdenItemLink != null)
                    {
                        //if override at current parent it is not deleted then it will be deleted now
                        if (overrdenItemLink.CollectionId != parColId)
                        {
                            orphanRecords.CollectionIds.Add(overrdenItemLink.CollectionId);
                        }
                        // Delete the record added at this netid
                        _repository.Delete<ItemCollectionLink>(overrdenItemLink);
                    }
                    collectionTreeId = GetMenuTreeId(MenuType.ItemCollection, itemIdbeforeCalculatingOvr, collectionId);
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ItemColLinkDeleteT, collectionName, itemname);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemColLinkDeleteT, collectionName, itemname));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColObjectDeleteT, itemname, collectionName);
                }
                if (orphanRecords.CollectionIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColLinkDeleteT, collectionName, itemname);
                Logger.WriteError(ex);
            }
            return collectionTreeId;
        }

        /// <summary>
        /// Delete Item in a Collection
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="collectionId"></param>
        /// <returns></returns>
        public string DeleteItemCollectionItem(int itemId, int collectionId)
        {
            var collectionName = "";
            var itemname = "";
            var itemTreeId = "";
            var collectionIdbeforeCalculatingOvr = 0;
            CleanUpDataModel orphanRecords = new CleanUpDataModel();
            try
            {
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                collectionIdbeforeCalculatingOvr = collectionId;

                //make sure this was not an override for another item
                var ovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == collectionId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCollectionId != null)
                    collectionId = ovrride.ParentCollectionId.Value;

                _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => x.CollectionId == collectionId && parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                var currentItmColObjs = _itemCollectionObjects.Where(co => co.CollectionId == collectionId && co.ItemId == itemId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();

                collectionName = _repository.GetQuery<ItemCollection>(i => i.CollectionId == collectionId).FirstOrDefault().InternalName;
                itemname = _repository.GetQuery<Item>(c => c.ItemId == itemId).FirstOrDefault().ItemName;
                var addDeleteLink = true;
                if (currentItmColObjs != null)
                {
                    var parItemId = itemId;
                    var isprntId = false;

                    while (!isprntId)
                    {
                        isprntId = getMasterParentColItemId(parItemId, collectionId, out parItemId);
                    }
                    //if this is overriden/created at this network level
                    var overrdenItemObj = currentItmColObjs.Where(co => co.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.ParentItemId).FirstOrDefault();
                    if (overrdenItemObj != null)
                    {
                        if (overrdenItemObj.ParentItemId.HasValue)
                        {
                            //if this not originated here or not if it has override then original item shouldn't be visible
                            addDeleteLink = true;
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }
                    //This Item is being removed from all Networks. Hence delete all overrides and link which is added the item intially to Menu
                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    //When all the overrides are deleted. There could be some orphan records. 
                    orphanRecords.ItemIds.AddRange(_itemCollectionObjects.Where(co => co.ParentItemId == parItemId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.CollectionId == collectionId && co.ItemId != co.ParentItemId).Select(x => x.ItemId).Distinct());
                    _repository.Delete<ItemCollectionObject>(co => co.ParentItemId == parItemId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.CollectionId == collectionId);

                    //If we are not adding delete override
                    if (!addDeleteLink)
                    {
                        // Then it is creatd at this network
                        if (overrdenItemObj != null)
                            orphanRecords.ItemIds.Add(overrdenItemObj.ItemId);
                    }
                    //Just add a Delete override so that it will not be visible in networks below
                    else
                    {
                        var oldItemColObj = currentItmColObjs.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        ItemCollectionObject itmObj = new ItemCollectionObject
                        {
                            ItemId = parItemId,
                            CollectionId = oldItemColObj.CollectionId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldItemColObj.SortOrder,
                            IsAutoSelect = oldItemColObj.IsAutoSelect,
                            ParentItemId = parItemId
                        };
                        _repository.Add<ItemCollectionObject>(itmObj);
                    }
                    if (overrdenItemObj != null)
                    {
                        //if override at current parent it is not deleted then it will be deleted now
                        if (overrdenItemObj.ItemId != parItemId)
                        {
                            orphanRecords.ItemIds.Add(overrdenItemObj.ItemId);
                        }
                        // Delete the record added at this netid
                        _repository.Delete<ItemCollectionObject>(overrdenItemObj);
                    }
                    itemTreeId = GetMenuTreeId(MenuType.ItemCollectionItem, collectionIdbeforeCalculatingOvr, itemId);
                    //Update the Menu Lastupdated Date
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ItemColObjectDeleteT, itemname, collectionName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemColObjectDeleteT, itemname, collectionName));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColObjectDeleteT, itemname, collectionName);
                }
                if (orphanRecords.ItemIds.Any())
                {
                    var deleteorphanstask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteorphanstask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemColObjectDeleteT, itemname, collectionName);
                Logger.WriteError(ex);
            }
            return itemTreeId;
        }

        /// <summary>
        /// Delete PrependItem in a Item
        /// </summary>
        /// <param name="prependItemId"></param>
        /// <param name="itmId"></param>
        /// <returns></returns>
        public string DeletePrependItem(int prependItemId, int itmId)
        {
            var itemName = "";
            var prependItemname = "";
            var itemTreeId = "";
            var itemIdbeforeCalculatingOvr = 0;
            var orphanRecords = new CleanUpDataModel();
            try
            {
                #region Initialize
                var mnuNetworkObjectLink = getMnuNwLinkAddIfNotexist(MenuId, NetworkObjectId);

                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);
                childMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(childNetworkNodeIds, MenuId);

                itemIdbeforeCalculatingOvr = itmId;
                #endregion

                #region Check Override

                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itmId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    itmId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collectionObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itmId).FirstOrDefault();
                    if (collectionObjovrride != null && collectionObjovrride.ParentItemId != null)
                        itmId = collectionObjovrride.ParentItemId.Value;
                }
                #endregion

                _prependItemLinks = _repository.GetQuery<PrependItemLink>(x => x.ItemId == itmId && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) || childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();

                var currentPrependItmLinks = _prependItemLinks.Where(co => co.ItemId == itmId && co.PrependItemId == prependItemId && parentMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId)).ToList();

                itemName = _repository.GetQuery<Item>(i => i.ItemId == itmId).FirstOrDefault().ItemName;
                prependItemname = _repository.GetQuery<Item>(c => c.ItemId == prependItemId).FirstOrDefault().ItemName;
                var addDeleteLink = true;
                if (currentPrependItmLinks != null)
                {
                    #region Calculations
                    var parPrependItemId = prependItemId;
                    var isprntId = false;

                    while (!isprntId)
                    {
                        isprntId = getMasterParentPrepItemId(parPrependItemId, itmId, out parPrependItemId);
                    }
                    //if this is overriden at this network level
                    var overrdenPrependitemLink = currentPrependItmLinks.Where(co => co.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.OverrideParentPrependItemId).FirstOrDefault();
                    if (overrdenPrependitemLink != null)
                    {
                        if (overrdenPrependitemLink.OverrideParentPrependItemId.HasValue)
                        {
                            //if this not originated here or not if it has override then original item shouldn't be visible
                            addDeleteLink = true;
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }
                    #endregion
                    //This Item is being removed from all Networks. Hence delete all overrides and link which is added the item intially to Menu
                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    //When all the overrides are deleted. There could be some orphan records. 
                    orphanRecords.ItemIds.AddRange(_prependItemLinks.Where(co => co.OverrideParentPrependItemId == parPrependItemId && childMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId) && co.ItemId == itmId && co.PrependItemId != co.OverrideParentPrependItemId).Select(x => x.PrependItemId).Distinct());
                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    _repository.Delete<PrependItemLink>(co => co.OverrideParentPrependItemId == parPrependItemId && childMenuNetworkNodeIds.Contains(co.MenuNetworkObjectLinkId) && co.ItemId == itmId);

                    //If we are not adding delete override
                    if (!addDeleteLink)
                    {
                        #region Delete
                        if (overrdenPrependitemLink != null)
                            orphanRecords.ItemIds.Add(overrdenPrependitemLink.PrependItemId);
                        #endregion
                    }
                    //Just add a Delete override so that it will not be visible in networks below
                    else
                    {
                        #region Add Delete link
                        var oldPrependItemLink = currentPrependItmLinks.OrderByDescending(x => x.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        PrependItemLink prependItmLink = new PrependItemLink
                        {
                            PrependItemId = parPrependItemId,
                            ItemId = oldPrependItemLink.ItemId,
                            MenuNetworkObjectLink = mnuNetworkObjectLink,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldPrependItemLink.SortOrder,
                            OverrideParentPrependItemId = parPrependItemId
                        };
                        _repository.Add<PrependItemLink>(prependItmLink);
                        #endregion
                    }
                    if (overrdenPrependitemLink != null)
                    {
                        //if override at current parent it is not deleted then it will be deleted now
                        if (overrdenPrependitemLink.PrependItemId != parPrependItemId)
                        {
                            orphanRecords.ItemIds.Add(overrdenPrependitemLink.PrependItemId);
                        }
                        // Delete the record added at this netid
                        _repository.Delete<PrependItemLink>(overrdenPrependitemLink);
                    }
                    //Update the Menu Lastupdated Date
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.PrependItemLinkDeleteT, prependItemname, itemName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.PrependItemLinkDeleteT, prependItemname, itemName));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrPrependItemLinkDeleteT, prependItemname, itemName);
                }
                if (orphanRecords.ItemIds.Any())
                {
                    var deleteorphanstask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteorphanstask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrPrependItemLinkDeleteT, prependItemname, itemName);
                Logger.WriteError(ex);
            }
            return itemTreeId;
        }

        /// <summary>
        /// Remove item from the category
        /// </summary>
        /// <param name="ids">item</param>
        /// <param name="categoryId">category</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        public string DeleteCategoryObject(int itemId, int categoryId)
        {
            var itemname = "";
            var catname = "";
            var itmTreeid = "";
            var categoryIdbeforecalculatingOvr = 0;
            CleanUpDataModel orphanRecords = new CleanUpDataModel();

            try
            {
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                categoryIdbeforecalculatingOvr = categoryId;

                #region Check Override
                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == categoryId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    categoryId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var subCategoryovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == categoryId).FirstOrDefault();
                    if (subCategoryovrride != null && subCategoryovrride.OverrideParentSubCategoryId != null)
                        categoryId = subCategoryovrride.OverrideParentSubCategoryId.Value;
                }
                #endregion

                #region Initialize
                _categoryObjects = _repository.GetQuery<CategoryObject>(x => x.CategoryId == categoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                var currentCatObjects = _categoryObjects.Where(co => co.ItemId == itemId && co.CategoryId == categoryId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();

                itemname = _repository.GetQuery<Item>(i => i.ItemId == itemId).FirstOrDefault().ItemName;
                catname = _repository.GetQuery<Category>(c => c.CategoryId == categoryId).FirstOrDefault().InternalName;
                var addDeleteLink = true;
                #endregion

                if (currentCatObjects != null)
                {
                    #region Calculations

                    var parItemId = 0;
                    var isprntId = false;

                    parItemId = itemId;
                    while (!isprntId)
                    {
                        isprntId = getMasterParentCatItemId(parItemId, categoryId, out parItemId);
                    }

                    //if this is overriden at this network level
                    var overridencatobj = currentCatObjects.Where(co => co.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.ParentItemId).FirstOrDefault();
                    if (overridencatobj != null)
                    {
                        if (overridencatobj.ParentItemId.HasValue)
                        {
                            //if this not originated here or not if it has override then original item shouldn't be visible
                            addDeleteLink = true;
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }
                    #endregion

                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    //Fetch the orphanIds that should be cleandup
                    orphanRecords.ItemIds.AddRange(_categoryObjects.Where(co => co.ParentItemId == parItemId && co.CategoryId == categoryId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemId != co.ParentItemId).Select(x => x.ItemId).Distinct());
                    _repository.Delete<CategoryObject>(co => co.ParentItemId == parItemId && co.CategoryId == categoryId && childNetworkNodeIds.Contains(co.NetworkObjectId));

                    //If we are not adding delete override then it means we are this Item is being removed from all Networks. Hence delete all overrides and link which is added the item intially to Menu
                    if (!addDeleteLink)
                    {
                        #region Delete
                        if (!addDeleteLink)
                        {
                            // Then it is creatd at this network
                            if (overridencatobj != null)
                                orphanRecords.ItemIds.Add(overridencatobj.ItemId);
                        }
                        #endregion
                    }
                    else
                    {
                        #region Add link to delete
                        //var similarLinkbychildNetworks = _categoryObjects.Where(co => co.ParentItemId == parItemId && co.CategoryId == categoryId && childNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();
                        //foreach (var child in similarLinkbychildNetworks)
                        //{
                        //    // delete overrides should be delete when parent is deleted
                        //    if (child.OverrideStatus == OverrideStatus.HIDDEN)
                        //    {
                        //        _repository.Delete<CategoryObject>(child);
                        //    }
                        //    // sort overrides should be deleted when parent is deleted
                        //    else if (child.ParentItemId == child.ItemId)
                        //    {
                        //        _repository.Delete<CategoryObject>(child);
                        //    }
                        //}
                        var oldCatObj = currentCatObjects.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        CategoryObject catObj = new CategoryObject
                        {
                            CategoryId = oldCatObj.CategoryId,
                            ItemId = parItemId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldCatObj.SortOrder,
                            ParentItemId = parItemId
                        };
                        _repository.Add<CategoryObject>(catObj);
                        #endregion
                    }
                    if (overridencatobj != null)
                    {
                        //if override at current parent it is not deleted then it will be deleted now
                        if (overridencatobj.ItemId != parItemId)
                        {
                            orphanRecords.ItemIds.Add(overridencatobj.ItemId);
                        }
                        _repository.Delete<CategoryObject>(overridencatobj);
                    }
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    itmTreeid = GetMenuTreeId(MenuType.Item, categoryIdbeforecalculatingOvr, itemId);
                    _lastActionResult = string.Format(Constants.AuditMessage.CatObjectLinkDeleteT, itemname, catname);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatObjectLinkDeleteT, itemname, catname));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrCatObjectLinkDeleteT, itemname, catname);
                }

                //delete all the overrides
                if (orphanRecords.ItemIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatObjectLinkDeleteT, itemname, catname);
                Logger.WriteError(ex);
            }
            return itmTreeid;
        }

        /// <summary>
        /// Delete a Category in a Menu
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public string DeleteCategoryMenuLink(int categoryId, int menuId)
        {
            var menuname = "";
            var categoryname = "";
            var catTreeId = "";
            var orphanRecords = new CleanUpDataModel();
            try
            {
                #region Initialize
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(x => x.MenuId == menuId && parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                var currentCatMnuLinks = _categoryMenuLinks.Where(co => co.MenuId == menuId && co.CategoryId == categoryId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();

                menuname = _repository.GetQuery<Menu>(i => i.MenuId == menuId).FirstOrDefault().InternalName;
                categoryname = _repository.GetQuery<Category>(c => c.CategoryId == categoryId).FirstOrDefault().InternalName;
                var addDeleteLink = true;
                #endregion

                if (currentCatMnuLinks != null)
                {
                    #region Calculations
                    var parCatId = 0;
                    var isprntId = false;
                    parCatId = categoryId;
                    while (!isprntId)
                    {
                        isprntId = getMasterParentCatId(parCatId, menuId, out parCatId);
                    }

                    //if this is overriden at this network level
                    var overrdenCatMnuLink = currentCatMnuLinks.Where(co => co.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.ParentCategoryId).FirstOrDefault();
                    if (overrdenCatMnuLink != null)
                    {
                        if (overrdenCatMnuLink.ParentCategoryId.HasValue)
                        {
                            //if this not originated here or not if it has override then original item shouldn't be visible
                            addDeleteLink = true;
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }
                    #endregion
                    //Add the normal(edit) Overrides to Orphans List to Delete
                    orphanRecords.CategoryIds.AddRange(_categoryMenuLinks.Where(co => co.ParentCategoryId == parCatId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == MenuId && co.CategoryId != co.ParentCategoryId).Select(x => x.CategoryId).Distinct());
                    //get the ids for the records being deleted
                    var catMnuLinkIdsToDelete = _categoryMenuLinks.Where(co => co.ParentCategoryId == parCatId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == MenuId).Select(x => x.CategoryMenuLinkId).ToList();
                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    _repository.Delete<CategoryMenuLink>(co => co.ParentCategoryId == parCatId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == MenuId);

                    //If we are not adding delete override then it means we are this category is being removed from all Networks. Hence delete all overrides and link which is added the item intially to Menu
                    if (!addDeleteLink)
                    {
                        #region Delete
                        if (overrdenCatMnuLink != null)
                            orphanRecords.CategoryIds.Add(overrdenCatMnuLink.CategoryId);
                        #endregion
                    }
                    //Add delete override so that it will not be visible in below networks
                    else
                    {
                        #region Add Delete Link

                        var oldItemLink = currentCatMnuLinks.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        CategoryMenuLink cmLink = new CategoryMenuLink
                        {
                            CategoryId = parCatId,
                            MenuId = menuId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldItemLink.SortOrder,
                            ParentCategoryId = parCatId
                        };
                        _repository.Add<CategoryMenuLink>(cmLink);
                        #endregion
                    }
                    if (overrdenCatMnuLink != null)
                    {
                        //if override at current item it is not deleted then it will be deleted now
                        if (overrdenCatMnuLink.CategoryId != parCatId)
                        {
                            orphanRecords.CategoryIds.Add(overrdenCatMnuLink.CategoryId);
                        }
                        _repository.Delete<CategoryMenuLink>(overrdenCatMnuLink);
                    }
                    catTreeId = GetMenuTreeId(MenuType.Category, menuId, categoryId);
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.CatDeleteT, categoryname, menuname);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatDeleteT, categoryname, menuname));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrCatDeleteT, categoryname, menuname);
                }

                //delete all overrides
                if (orphanRecords.CategoryIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatDeleteT, categoryname, menuname);
                Logger.WriteError(ex);
            }
            return catTreeId;
        }

        /// <summary>
        /// Delete a Sub Category in a Category
        /// </summary>
        /// <param name="subCategoryId"></param>
        /// <param name="prntCategoryId"></param>
        /// <returns></returns>
        public string DeleteSubCategoryLink(int subCategoryId, int prntCategoryId)
        {
            var subCatname = "";
            var prntCatname = "";
            var catTreeid = "";
            var orgPCatId = 0;
            var orphanRecords = new CleanUpDataModel();

            try
            {
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                #region Check Override
                orgPCatId = prntCategoryId;
                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == prntCategoryId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    prntCategoryId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var subovrride = _subCategoryLinks.Where(co => co.SubCategoryId == prntCategoryId).FirstOrDefault();
                    if (subovrride != null && subovrride.OverrideParentSubCategoryId != null)
                        prntCategoryId = subovrride.OverrideParentSubCategoryId.Value;
                }
                #endregion

                _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => x.CategoryId == prntCategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var currentSubCatLinks = _subCategoryLinks.Where(co => co.SubCategoryId == subCategoryId && co.CategoryId == prntCategoryId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).ToList();

                subCatname = _repository.GetQuery<Category>(i => i.CategoryId == subCategoryId).FirstOrDefault().InternalName;
                prntCatname = _repository.GetQuery<Category>(c => c.CategoryId == prntCategoryId).FirstOrDefault().InternalName;
                var addDeleteLink = true;
                if (currentSubCatLinks != null)
                {
                    #region Initialize
                    var parSubCatId = 0;
                    var isprntId = false;

                    parSubCatId = subCategoryId;
                    while (!isprntId)
                    {
                        isprntId = getMasterParentSubCatId(parSubCatId, prntCategoryId, out parSubCatId);
                    }
                    #endregion

                    #region Calculations
                    SubCategoryLink orginalSubCatLink = null;
                    //if this is overriden at this network level
                    var overrdensubCatLink = currentSubCatLinks.Where(co => co.NetworkObjectId == NetworkObjectId).OrderByDescending(x => x.OverrideParentSubCategoryId).FirstOrDefault();
                    if (overrdensubCatLink != null)
                    {
                        if (overrdensubCatLink.OverrideParentSubCategoryId.HasValue)
                        {
                            //If the original subCat is also at the same level - then delete this overide record and original record - Edited subCat can be deleted now
                            orginalSubCatLink = _subCategoryLinks.Where(co => co.SubCategoryId == parSubCatId && co.CategoryId == prntCategoryId && co.NetworkObjectId == NetworkObjectId && co.OverrideParentSubCategoryId == null).FirstOrDefault();
                            if (orginalSubCatLink != null)
                            {
                                //if this is originated here
                                addDeleteLink = false;
                            }
                            else
                            {
                                //if this not originated here or not if it has override then original subCat shouldn't be visible
                                addDeleteLink = true;
                            }
                        }
                        else
                        {
                            //if this is originated here
                            addDeleteLink = false;
                        }
                    }
                    #endregion
                    // This subCat is being removed from the current category and all child Networks. Hence delete all overrides 
                    //Add the normal(edit) Overrides to Orphans List to Delete
                    orphanRecords.CategoryIds.AddRange(_subCategoryLinks.Where(co => co.OverrideParentSubCategoryId == parSubCatId && co.CategoryId == prntCategoryId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.SubCategoryId != co.OverrideParentSubCategoryId).Select(x => x.SubCategoryId).Distinct());
                    //get the ids for the records being deleted
                    var subCatLinkIdsToDelete = _subCategoryLinks.Where(co => co.OverrideParentSubCategoryId == parSubCatId && co.CategoryId == prntCategoryId && childNetworkNodeIds.Contains(co.NetworkObjectId)).Select(x => x.SubCategoryLinkId).ToList();
                    // If this has any overrides then delete them - delete overrides, sort overrides or normal overrides
                    _repository.Delete<SubCategoryLink>(co => co.OverrideParentSubCategoryId == parSubCatId && co.CategoryId == prntCategoryId && childNetworkNodeIds.Contains(co.NetworkObjectId));

                    //If we are not adding delete override
                    if (!addDeleteLink)
                    {
                        #region Delete
                        //remove link which is added the subCat intially to Cat
                        if (orginalSubCatLink != null)
                        {
                            subCatLinkIdsToDelete.Add(orginalSubCatLink.SubCategoryLinkId);
                            _repository.Delete<SubCategoryLink>(orginalSubCatLink);
                        }

                        //this cat is not used anywhere
                        if (overrdensubCatLink != null)
                        {
                            subCatLinkIdsToDelete.Add(overrdensubCatLink.SubCategoryLinkId);
                        }
                        //If this Category is not used anywhere else. And not included as sub category anywhere. Donot search on the records that r being deleted
                        if (_repository.GetQuery<CategoryMenuLink>(x => x.CategoryId == parSubCatId).Count() == 0 && _repository.GetQuery<SubCategoryLink>(x => x.SubCategoryId == parSubCatId && subCatLinkIdsToDelete.Contains(x.SubCategoryLinkId) == false).Count() == 0)
                        {
                            var cat = _repository.GetQuery<Category>(x => x.CategoryId == parSubCatId).FirstOrDefault();
                            _repository.Delete<CategoryObject>(co => co.CategoryId == cat.CategoryId);
                            _repository.Delete<SubCategoryLink>(co => co.CategoryId == cat.CategoryId);
                            _repository.Delete<MenuCategoryScheduleLink>(co => co.CategoryId == cat.CategoryId);
                            _repository.Delete<AssetCategoryLink>(co => co.CategoryId == cat.CategoryId);
                            _repository.Delete<Category>(cat);
                        }
                        #endregion
                    }
                    else
                    {
                        #region Add Link to Delete
                        var oldCatObj = currentSubCatLinks.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                        SubCategoryLink catObj = new SubCategoryLink
                        {
                            CategoryId = oldCatObj.CategoryId,
                            SubCategoryId = parSubCatId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = oldCatObj.SortOrder,
                            OverrideParentSubCategoryId = parSubCatId
                        };
                        _repository.Add<SubCategoryLink>(catObj);
                        #endregion

                    }
                    if (overrdensubCatLink != null)
                    {
                        //if override at current parent it is not deleted then it will be deleted now
                        if (overrdensubCatLink.SubCategoryId != parSubCatId)
                        {
                            orphanRecords.CategoryIds.Add(overrdensubCatLink.SubCategoryId);
                        }
                        _repository.Delete<SubCategoryLink>(overrdensubCatLink);
                    }
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                    catTreeid = GetMenuTreeId(MenuType.Category, orgPCatId, subCategoryId);
                    _lastActionResult = string.Format(Constants.AuditMessage.SubCatLinkDeleteT, subCatname, prntCatname);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.SubCatLinkDeleteT, subCatname, prntCatname));
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrSubCatLinkDeleteT, subCatname, prntCatname);
                }
                if (orphanRecords.CategoryIds.Any())
                {
                    var deleteOrphansTask = new Task(() => _commonService.DeleteOrphanEntitiesAsync(orphanRecords));
                    deleteOrphansTask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSubCatLinkDeleteT, subCatname, prntCatname);
                Logger.WriteError(ex.Message);
            }
            return catTreeid;
        }

        /// <summary>
        /// Save/Override an Item in Category/Collection
        /// </summary>
        /// <param name="itemModel"></param>
        /// <param name="prntId"></param>
        /// <param name="itemType"></param>
        /// <returns></returns>
        public int SaveItem(ItemModel itemModel, int prntId, MenuType itemType)
        {
            //generatehash for the data
            var itemJson = JsonConvert.SerializeObject(itemModel);
            //Create Hash to the specific data that is being sent.
            var updatedItemHash = HashGeneratorMD5.GetHash(itemJson);

            var addNewItemandLink = true;
            var itm = new Item();
            var newItm = new Item();
            try
            {
                if (updatedItemHash != itemModel.ItemDataHash || itemModel.IsScheduleModified)
                {
                    if (NetworkObjectId > 0)
                    {
                        parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                        childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                        parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);
                        childMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(childNetworkNodeIds, MenuId);
                    }

                    #region Check Override
                    //var newItemSortOrder = 0;
                    if (itemType == MenuType.Item)
                    {
                        //make sure this was not an override for another category
                        var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == prntId).FirstOrDefault();
                        if (ovrride != null && ovrride.ParentCategoryId != null)
                        {
                            prntId = ovrride.ParentCategoryId.Value;
                        }
                        else
                        {
                            //make sure this was not an override for another category
                            var subCatgeoryovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == prntId).FirstOrDefault();
                            if (subCatgeoryovrride != null && subCatgeoryovrride.OverrideParentSubCategoryId != null)
                                prntId = subCatgeoryovrride.OverrideParentSubCategoryId.Value;
                        }
                    }
                    else if (itemType == MenuType.ItemCollectionItem)
                    {
                        //make sure this was not an override for another collection
                        var ovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == prntId).FirstOrDefault();
                        if (ovrride != null && ovrride.ParentCollectionId != null)
                            prntId = ovrride.ParentCollectionId.Value;
                    }
                    else
                    {
                        //make sure this was not an override for another item
                        var ovrride = _repository.GetQuery<PrependItemLink>(co => co.ItemId == prntId).FirstOrDefault();
                        if (ovrride != null && ovrride.OverrideParentPrependItemId != null)
                            prntId = ovrride.OverrideParentPrependItemId.Value;
                    }
                    #endregion

                    //Not a new Item
                    if (itemModel.ItemId != 0)
                    {
                        #region Calculations
                        itm = _repository.GetQuery<Item>(x => x.ItemId == itemModel.ItemId).Include("MenuItemScheduleLinks").Include("MenuItemScheduleLinks.MenuItemCycleInSchedules").FirstOrDefault();

                        //If is added or modified at this network then edit same one else make a copy and add to the menu
                        //As per new BR rule - ParentitemId it is used to determine if it is override
                        if ((itm.NetworkObjectId == NetworkObjectId && itm.ParentItemId.HasValue == true))
                        {
                            addNewItemandLink = false;
                        }

                        #endregion
                    }
                    if (addNewItemandLink)
                    {

                        #region Initialize
                        //Load Tables
                        var mnuNwLink = getMnuNwLinkAddIfNotexist(MenuId, NetworkObjectId);
                        _categoryObjects = _repository.GetQuery<CategoryObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId).Include("NetworkObject").Include("Category").ToList();
                        _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId).Include("NetworkObject").Include("ItemCollection").ToList();
                        _prependItemLinks = _repository.GetQuery<PrependItemLink>(x => parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) || childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();
                        _mnuNetworkLinks = _repository.GetQuery<MenuNetworkObjectLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.MenuId == MenuId).ToList();

                        var parItmId = itemModel.ItemId;
                        var isprntId = false;
                        if (itemModel.ItemId != 0)
                        {
                            //Get orginial Id of item
                            while (!isprntId)
                            {
                                if (itemType == MenuType.Item)
                                {
                                    isprntId = getMasterParentCatItemId(parItmId, prntId, out parItmId);
                                }
                                else
                                {
                                    isprntId = getMasterParentColItemId(parItmId, prntId, out parItmId);
                                }
                            }
                        }
                        mapItemtoItem(itm, ref newItm);
                        mapItemModeltoItem(itemModel, ref newItm, itemModel.IsDWFieldsEnabled);
                        mapItemSchModeltoItemSch(itemModel, ref newItm, true);

                        newItm.ItemId = 0;
                        newItm.OverrideItemId = itm.OverrideItemId.HasValue ? itm.OverrideItemId : (itm.ParentItemId.HasValue ? (int?)itm.ItemId : null); // As an override version will be an override of Menu item not Master Item
                        newItm.ParentItemId = itm.ParentItemId.HasValue ? itm.ParentItemId : itm.ItemId; // As an override version will have Master item as parent Id
                        newItm.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName); // Create new guid for override too
                        newItm.UpdatedDate = newItm.CreatedDate = DateTime.UtcNow;
                        //assign selected description
                        newItm.SelectedDescriptionId = itemModel.ItemDescriptionId == 0 ? null : (int?)itemModel.ItemDescriptionId;
                        newItm.NetworkObjectId = NetworkObjectId;

                        #endregion

                        if (itemModel.ItemId != 0)
                        {
                            #region Add New and Update all occurances in Tree

                            //For New item add Selected POSdata
                            //If not placeholder item then POSDataId is set else null is inserted
                            var posDataId = itemModel.SelectedPOSDataId != 0 ? (int?)itemModel.SelectedPOSDataId : null;

                            newItm.ItemPOSDataLinks.Add(new ItemPOSDataLink
                            {
                                POSDataId = posDataId,
                                IsDefault = true,
                                ParentMasterItemId = newItm.ParentItemId,
                                NetworkObjectId = NetworkObjectId,
                                UpdatedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                            });

                            // get all references of this item and change the item
                            var catObjects = _categoryObjects.Where(x => x.ItemId == parItmId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId && x.OverrideStatus != OverrideStatus.HIDDEN);
                            var sortOverrideIds = _categoryObjects.Where(x => x.ItemId == parItmId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.CategoryId);
                            foreach (var catObj in catObjects.Where(x => !sortOverrideIds.Contains(x.CategoryId)))
                            {
                                var sortOrder = catObj.SortOrder;
                                var overridenByParent = _categoryObjects.Where(x => x.ParentItemId == parItmId && x.ItemId != parItmId && x.CategoryId == catObj.CategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenByParent != null)
                                {
                                    // get the sortorder if it changed and then overriden by parent
                                    sortOrder = overridenByParent.SortOrder;
                                    // If the override is by current network then delete old one as it will be replaced by new item. ITEM can be deleted here.
                                    if (overridenByParent.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<CategoryObject>(overridenByParent);
                                    }
                                }

                                //MMS - 243 - If item is deleted in other category donot add the new item to that category
                                var overridenAsHidden = _categoryObjects.Where(x => x.ParentItemId == parItmId && x.CategoryId == catObj.CategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenAsHidden != null)
                                {
                                    continue;
                                }

                                newItm.CategoryObjects.Add(new CategoryObject
                                {
                                    CategoryId = catObj.CategoryId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortOrder,
                                    ParentItemId = parItmId
                                });
                            }

                            ////If this was added here then add new link for edited item
                            ////if this is overriden this n/w level or added at this level // deleted "&& x.CategoryId == catId"
                            var sortOverrides = _categoryObjects.Where(x => x.ItemId == itemModel.ItemId && x.ParentItemId == parItmId && x.OverrideStatus == OverrideStatus.MOVED && sortOverrideIds.Contains(x.CategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId);
                            foreach (var sOvr in sortOverrides)
                            {
                                // if it is overriden at multiple level then we have pickup last overriden sort order
                                if (!newItm.CategoryObjects.Where(x => x.CategoryId == sOvr.CategoryId).Any())
                                {
                                    newItm.CategoryObjects.Add(new CategoryObject
                                    {
                                        CategoryId = sOvr.CategoryId,
                                        NetworkObjectId = NetworkObjectId,
                                        OverrideStatus = OverrideStatus.ACTIVE,
                                        SortOrder = sOvr.SortOrder,
                                        ParentItemId = parItmId
                                    });
                                    if (sOvr.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<CategoryObject>(sOvr);
                                    }
                                }
                            }

                            // get all references of sort order changes of below the tree and update to new item
                            var childSortOverrides = _categoryObjects.Where(x => x.ItemId == parItmId && x.ParentItemId == parItmId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId && x.OverrideStatus == OverrideStatus.MOVED);
                            foreach (var ovr in childSortOverrides)
                            {
                                newItm.CategoryObjects.Add(new CategoryObject
                                {
                                    CategoryId = ovr.CategoryId,
                                    NetworkObjectId = ovr.NetworkObjectId,
                                    OverrideStatus = ovr.OverrideStatus,
                                    SortOrder = ovr.SortOrder,
                                    ParentItemId = parItmId
                                });
                                _repository.Delete<CategoryObject>(ovr);
                            }
                            // get all references of this collection and change the collection
                            var colObjects = _itemCollectionObjects.Where(x => x.ItemId == parItmId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId && x.ParentItemId == null);
                            var sortColOverrideIds = _itemCollectionObjects.Where(x => x.ItemId == parItmId && x.ParentItemId == parItmId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.CollectionId);
                            foreach (var colObj in colObjects.Where(x => !sortColOverrideIds.Contains(x.CollectionId)))
                            {
                                var sortOrder = colObj.SortOrder;
                                var overridenByParent = _itemCollectionObjects.Where(x => x.ParentItemId == parItmId && x.ItemId != parItmId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.CollectionId == colObj.CollectionId && x.ItemCollection.MenuId == MenuId && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenByParent != null)
                                {
                                    // get the sortorder if it changed and then overriden by parent
                                    sortOrder = overridenByParent.SortOrder;
                                    // If the override is by current network then delete old one as it will be replaced by new item. ITEM can be deleted here.
                                    if (overridenByParent.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<ItemCollectionObject>(overridenByParent);
                                    }
                                }

                                //MMS - 243 - If item is deleted in other category donot add the new item to that category
                                var overridenAsHidden = _itemCollectionObjects.Where(x => x.ParentItemId == parItmId && x.CollectionId == colObj.CollectionId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenAsHidden != null)
                                {
                                    continue;
                                }
                                newItm.ItemCollectionObjects.Add(new ItemCollectionObject
                                {
                                    CollectionId = colObj.CollectionId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortOrder,
                                    ParentItemId = parItmId,
                                    IsAutoSelect = colObj.IsAutoSelect
                                });
                            }

                            //If this was added here then add new link for edited item
                            //if this is overriden this n/w level or added at this level
                            var sortColOverrides = _itemCollectionObjects.Where(x => x.ItemId == itemModel.ItemId && x.ParentItemId == parItmId && x.OverrideStatus == OverrideStatus.MOVED && sortColOverrideIds.Contains(x.CollectionId) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId);
                            foreach (var sOvr in sortColOverrides)
                            {
                                // Donot add if collection is already Added
                                if (!newItm.ItemCollectionObjects.Where(x => x.CollectionId == sOvr.CollectionId).Any())
                                {
                                    newItm.ItemCollectionObjects.Add(new ItemCollectionObject
                                    {
                                        CollectionId = sOvr.CollectionId,
                                        NetworkObjectId = NetworkObjectId,
                                        OverrideStatus = OverrideStatus.ACTIVE,
                                        SortOrder = sOvr.SortOrder,
                                        ParentItemId = parItmId,
                                        IsAutoSelect = sOvr.IsAutoSelect
                                    });
                                    if (sOvr.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<ItemCollectionObject>(sOvr);
                                    }
                                }
                            }

                            // get all references of sort order changes of below the tree and update to new collection
                            var childSortColOverrides = _itemCollectionObjects.Where(x => x.ItemId == parItmId && x.ParentItemId == parItmId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId && x.OverrideStatus == OverrideStatus.MOVED);
                            foreach (var ovr in childSortColOverrides)
                            {
                                newItm.ItemCollectionObjects.Add(new ItemCollectionObject
                                {
                                    CollectionId = ovr.CollectionId,
                                    NetworkObjectId = ovr.NetworkObjectId,
                                    OverrideStatus = ovr.OverrideStatus,
                                    SortOrder = ovr.SortOrder,
                                    ParentItemId = parItmId,
                                    IsAutoSelect = ovr.IsAutoSelect //itmModel.IsAutoSelect //AutoSelect reflect for sort order override items below the tree
                                });
                                _repository.Delete<ItemCollectionObject>(ovr);
                            }
                            // get all references of this collection and change the collection
                            var prependLinks = _prependItemLinks.Where(x => x.PrependItemId == parItmId && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideParentPrependItemId == null);
                            var sortPrepOverrideIds = _prependItemLinks.Where(x => x.PrependItemId == parItmId && x.OverrideParentPrependItemId == parItmId && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.ItemId);
                            foreach (var prependLink in prependLinks.Where(x => !sortColOverrideIds.Contains(x.ItemId)))
                            {
                                var sortOrder = prependLink.SortOrder;
                                var overridenByParent = _prependItemLinks.Where(x => x.OverrideParentPrependItemId == parItmId && x.PrependItemId != parItmId && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.ItemId == prependLink.ItemId && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenByParent != null)
                                {
                                    // get the sortorder if it changed and then overriden by parent
                                    sortOrder = overridenByParent.SortOrder;
                                    // If the override is by current network then delete old one as it will be replaced by new item. ITEM can be deleted here.
                                    if (overridenByParent.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<PrependItemLink>(overridenByParent);
                                    }
                                }

                                //MMS - 243 - If item is deleted in other category donot add the new item to that category
                                var overridenAsHidden = _prependItemLinks.Where(x => x.OverrideParentPrependItemId == parItmId && x.ItemId == prependLink.ItemId && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenAsHidden != null)
                                {
                                    continue;
                                }

                                newItm.PrependItemLinks2.Add(new PrependItemLink
                                {
                                    ItemId = prependLink.ItemId,
                                    MenuNetworkObjectLink = mnuNwLink,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortOrder,
                                    OverrideParentPrependItemId = parItmId
                                });
                            }

                            //If this was added here then add new link for edited item
                            //if this is overriden this n/w level or added at this level
                            var sortPrepOverrides = _prependItemLinks.Where(x => x.PrependItemId == itemModel.ItemId && x.OverrideParentPrependItemId == parItmId && x.OverrideStatus == OverrideStatus.MOVED && sortPrepOverrideIds.Contains(x.ItemId) && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).OrderByDescending(x => x.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId);
                            foreach (var sOvr in sortPrepOverrides)
                            {
                                // Donot add if collection is already Added
                                if (!newItm.PrependItemLinks2.Where(x => x.ItemId == sOvr.ItemId).Any())
                                {
                                    newItm.PrependItemLinks2.Add(new PrependItemLink
                                    {
                                        ItemId = sOvr.ItemId,
                                        MenuNetworkObjectLink = mnuNwLink,
                                        OverrideStatus = OverrideStatus.ACTIVE,
                                        SortOrder = sOvr.SortOrder,
                                        OverrideParentPrependItemId = parItmId
                                    });
                                    if (sOvr.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<PrependItemLink>(sOvr);
                                    }
                                }
                            }

                            // get all references of sort order changes of below the tree and update to new item
                            var childSortPrepOverrides = _prependItemLinks.Where(x => x.PrependItemId == parItmId && x.OverrideParentPrependItemId == parItmId && childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus == OverrideStatus.MOVED);
                            foreach (var ovr in childSortPrepOverrides)
                            {
                                newItm.PrependItemLinks2.Add(new PrependItemLink
                                {
                                    ItemId = ovr.ItemId,
                                    MenuNetworkObjectLink = mnuNwLink,
                                    OverrideStatus = ovr.OverrideStatus,
                                    SortOrder = ovr.SortOrder,
                                    OverrideParentPrependItemId = parItmId
                                });
                                _repository.Delete<PrependItemLink>(ovr);
                            }
                            #endregion
                        }
                        if (itemType == MenuType.ItemCollectionItem)
                        {
                            if (newItm.ItemCollectionObjects.Where(x => x.CollectionId == prntId && x.NetworkObjectId == NetworkObjectId).Any())
                            {
                                newItm.ItemCollectionObjects.Where(x => x.CollectionId == prntId && x.NetworkObjectId == NetworkObjectId).FirstOrDefault().IsAutoSelect = itemModel.IsAutoSelect;
                            }
                        }
                        _repository.Add<Item>(newItm);
                        _lastActionResult = string.Format(Constants.AuditMessage.ItemUpdateT, itemModel.ItemName);
                    }
                    else
                    {
                        if (itm != null)
                        {
                            #region Update
                            mapItemModeltoItem(itemModel, ref itm, itemModel.IsDWFieldsEnabled);
                            mapItemSchModeltoItemSch(itemModel, ref itm, false);
                            //assign selected description
                            itm.SelectedDescriptionId = itemModel.ItemDescriptionId == 0 ? null : (int?)itemModel.ItemDescriptionId;

                            if (itemType == MenuType.ItemCollectionItem)
                            {
                                var existingColItemLink = _repository.GetQuery<ItemCollectionObject>(x => x.ItemId == itemModel.ItemId && x.CollectionId == prntId && x.NetworkObjectId == NetworkObjectId).FirstOrDefault();
                                if (existingColItemLink != null)
                                {
                                    existingColItemLink.IsAutoSelect = itemModel.IsAutoSelect;
                                    _repository.Update<ItemCollectionObject>(existingColItemLink);
                                }
                            }

                            //If POSItem is changed
                            if (itemModel.PreviousPOSDataIdSelected != itemModel.SelectedPOSDataId)
                            {
                                //If not placeholder item then POSDataId is set else null is inserted
                                var posDataId = itemModel.SelectedPOSDataId != 0 ? (int?)itemModel.SelectedPOSDataId : null;

                                //delete earlier row
                                _repository.Delete<ItemPOSDataLink>(x => x.ItemId == itm.ItemId && x.NetworkObjectId == NetworkObjectId);

                                //Add new default value
                                itm.ItemPOSDataLinks.Add(new ItemPOSDataLink
                                {
                                    POSDataId = posDataId,
                                    IsDefault = true,
                                    ParentMasterItemId = itm.ParentItemId,
                                    NetworkObjectId = NetworkObjectId,
                                    UpdatedDate = DateTime.UtcNow,
                                    CreatedDate = DateTime.UtcNow,
                                });
                            }

                            itm.UpdatedDate = DateTime.UtcNow;
                            _repository.Update<Item>(itm);
                            _lastActionResult = string.Format(Constants.AuditMessage.ItemUpdateT, itemModel.ItemName);
                            #endregion
                        }
                        else
                        {
                            _lastActionResult = string.Format(Constants.StatusMessage.ErrItemAddT, itemModel.ItemName);
                        }
                    }
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                }
                else // if data is not change then check if POS selection is different
                {
                    addNewItemandLink = false;

                    //If POSItem is changed
                    if (itemModel.PreviousPOSDataIdSelected != itemModel.SelectedPOSDataId)
                    {
                        //If not placeholder item then POSDataId is set else null is inserted
                        var posDataId = itemModel.SelectedPOSDataId != 0 ? (int?)itemModel.SelectedPOSDataId : null;

                        itm = _repository.GetQuery<Item>(x => x.ItemId == itemModel.ItemId).FirstOrDefault();
                        var posItem = _repository.GetQuery<POSData>(o => o.POSDataId == itemModel.SelectedPOSDataId).FirstOrDefault();
                        if ((itemModel.SelectedPOSDataId == 0 || posItem != null) && itm != null)
                        {
                            var posname = itemModel.SelectedPOSDataId == 0 ? "Placeholder" : posItem.POSItemName;
                            //delete earlier row
                            _repository.Delete<ItemPOSDataLink>(x => x.ItemId == itemModel.ItemId && x.NetworkObjectId == NetworkObjectId);

                            _repository.Add<ItemPOSDataLink>(new ItemPOSDataLink
                            {
                                ItemId = itm.ItemId,
                                POSDataId = posDataId,
                                IsDefault = true,
                                ParentMasterItemId = itm.ParentItemId,
                                NetworkObjectId = NetworkObjectId,
                                UpdatedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                            });
                            _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                            _context.SaveChanges();
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.POSItemChangedT, posItem == null ? posname : posname + "-" + posItem.PLU, itm.ItemName));
                        }
                    }
                    _lastActionResult = string.Format(Constants.AuditMessage.ItemUpdateT, itemModel.ItemName);
                }

            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemAddT, itemModel.ItemName);
                Logger.WriteError(ex);
            }

            //Log
            if (!_lastActionResult.Contains("failed"))
            {
                if (itemModel.IsScheduleModified)
                {
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemSchDetailUpdate, itemModel.ItemName));
                }
                _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemUpdateT, itemModel.ItemName));
            }
            if (addNewItemandLink)
            {
                return newItm.ItemId;
            }
            else
            {
                return itemModel.ItemId;
            }
        }

        /// <summary>
        /// Save/Override/Create a NewCategory in Menu
        /// </summary>
        /// <param name="categoryModel"></param>
        /// <param name="parentCategoryId"></param>
        /// <returns></returns>
        public int SaveCategory(CategoryModel categoryModel, int parentCategoryId, MenuType catType)
        {
            var addNewCatandLink = true;
            var cat = new Category();
            var newCat = new Category();
            try
            {
                if (NetworkObjectId > 0)
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                }

                #region Check Override
                if (catType == MenuType.SubCategory)
                {
                    //make sure this parent was not an override for another Category
                    var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == parentCategoryId).FirstOrDefault();
                    if (ovrride != null && ovrride.ParentCategoryId != null)
                    {
                        parentCategoryId = ovrride.ParentCategoryId.Value;
                    }
                    else
                    {
                        //make sure this was not an override for another Category
                        var subCatovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == parentCategoryId).FirstOrDefault();
                        if (subCatovrride != null && subCatovrride.OverrideParentSubCategoryId != null)
                            parentCategoryId = subCatovrride.OverrideParentSubCategoryId.Value;
                    }
                }
                #endregion

                if (categoryModel.CategoryId != 0)
                {
                    cat = _repository.GetQuery<Category>(x => x.CategoryId == categoryModel.CategoryId).Include("MenuCategoryScheduleLinks").Include("MenuCategoryScheduleLinks.MenuCategoryCycleInSchedules").FirstOrDefault();
                }
                if (isCategoryDeepLinkIdUniqueinBrand(categoryModel.DeepLinkId, categoryModel.CategoryId, MenuId, parentNetworkNodeIds, cat.OverrideCategoryId))
                {
                    //Not a new Cat
                    if (categoryModel.CategoryId != 0)
                    {

                        #region Calculations to add override or update
                        //if (catType == MenuType.SubCategory)
                        //{
                        //    //if this is overriden this n/w level or added at this level
                        //    var existingSubCatLink = _repository.GetQuery<SubCategoryLink>(x => x.SubCategoryId == categoryModel.CategoryId && x.CategoryId == parentCategoryId && x.NetworkObjectId == NetworkObjectId).FirstOrDefault();

                        //    // it is override then update the existing subCat
                        //    if (existingSubCatLink != null && existingSubCatLink.OverrideStatus != OverrideStatus.HIDDEN)
                        //    {
                        //        // this is normal override not override for SortOrder
                        //        if (existingSubCatLink.OverrideParentSubCategoryId.HasValue && existingSubCatLink.OverrideStatus != OverrideStatus.MOVED)
                        //        {
                        //            addNewCatandLink = false;
                        //        }
                        //        //Added at this level - for categories and collections edit the cat created at this level
                        //        if (existingSubCatLink.OverrideParentSubCategoryId.HasValue == false)
                        //        {
                        //            addNewCatandLink = false;
                        //        }


                        //    }
                        //}
                        //else
                        //{
                        //    var existingCatMnuLink = _repository.GetQuery<CategoryMenuLink>(x => x.CategoryId == categoryModel.CategoryId && x.MenuId == MenuId && x.NetworkObjectId == NetworkObjectId).FirstOrDefault();
                        //    if (existingCatMnuLink != null && existingCatMnuLink.OverrideStatus != OverrideStatus.HIDDEN)
                        //    {
                        //        // this is normal override not override for SortOrder
                        //        if (existingCatMnuLink.ParentCategoryId.HasValue && existingCatMnuLink.OverrideStatus != OverrideStatus.MOVED)
                        //        {
                        //            addNewCatandLink = false;
                        //        }
                        //        //Added at this level - for categories and collections edit the cat created at this level
                        //        if (!existingCatMnuLink.ParentCategoryId.HasValue)
                        //        {
                        //            addNewCatandLink = false;
                        //        }
                        //    }
                        //}
                        #endregion

                        //if category is not created here then it is a override
                        if (cat.NetworkObjectId == NetworkObjectId)
                        {
                            addNewCatandLink = false;
                        }
                    }
                    if (addNewCatandLink)
                    {
                        #region Initialize
                        //Load Tables - Get all the netwrks links of this category under same parent
                        _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId).Include("NetworkObject").Include("Category").ToList();
                        _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.MenuId == MenuId).Include("NetworkObject").ToList();

                        var parSubCatId = categoryModel.CategoryId;
                        //Get orginial Id of subcategory being updated
                        if (categoryModel.CategoryId != 0)
                        {
                            var isprntId = false;
                            while (!isprntId)
                            {
                                if (catType == MenuType.SubCategory)
                                {
                                    isprntId = getMasterParentSubCatId(parSubCatId, parentCategoryId, out parSubCatId);
                                }
                                else
                                {
                                    isprntId = getMasterParentCatId(parSubCatId, MenuId, out parSubCatId);
                                }
                            }
                        }

                        mapCatModeltoCat(categoryModel, ref newCat);
                        mapCatSchModeltoCatSch(categoryModel, ref newCat, true);
                        newCat.CategoryId = 0;
                        newCat.OverrideCategoryId = categoryModel.CategoryId != 0 ? (int?)parSubCatId : null;
                        newCat.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);
                        newCat.NetworkObjectId = NetworkObjectId;
                        newCat.CreatedDate = DateTime.UtcNow;
                        newCat.UpdatedDate = DateTime.UtcNow;

                        if (categoryModel.CategoryId == 0)
                        {
                            newCat.MenuId = MenuId;
                        }

                        #endregion

                        if (categoryModel.CategoryId != 0)
                        {
                            #region Add as override and Update all occurances of this instance in Tree
                            // get all references of this category and change the category
                            var allSubCatLinks = _subCategoryLinks.Where(x => x.SubCategoryId == parSubCatId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideParentSubCategoryId == null && x.OverrideStatus != OverrideStatus.HIDDEN);
                            var sortOverrideIds = _subCategoryLinks.Where(x => x.SubCategoryId == parSubCatId && x.OverrideParentSubCategoryId == parSubCatId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.CategoryId);
                            foreach (var subCatLink in allSubCatLinks.Where(x => !sortOverrideIds.Contains(x.CategoryId)))
                            {
                                var sortOrder = subCatLink.SortOrder;
                                var overridenByParent = _subCategoryLinks.Where(x => x.OverrideParentSubCategoryId == parSubCatId && x.SubCategoryId != parSubCatId && x.CategoryId == parentCategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenByParent != null)
                                {
                                    // get the sortorder if it changed and then overriden by parent
                                    sortOrder = overridenByParent.SortOrder;
                                    if (overridenByParent.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<SubCategoryLink>(overridenByParent);
                                    }
                                }

                                //MMS - 243 - If item is deleted in other category donot add the new item to that category
                                var overridenAsHidden = _subCategoryLinks.Where(x => x.OverrideParentSubCategoryId == parSubCatId && x.CategoryId == parentCategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenAsHidden != null)
                                {
                                    continue;
                                }

                                newCat.SubCategoryLinks2.Add(new SubCategoryLink
                                {
                                    CategoryId = subCatLink.CategoryId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortOrder,
                                    OverrideParentSubCategoryId = parSubCatId
                                });
                            }

                            //If this was moved in any item by current NW
                            var sortOverrides = _subCategoryLinks.Where(x => x.SubCategoryId == categoryModel.CategoryId && x.OverrideParentSubCategoryId == categoryModel.CategoryId && x.OverrideStatus == OverrideStatus.MOVED && sortOverrideIds.Contains(x.CategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId);
                            foreach (var sOvr in sortOverrides)
                            {
                                if (!newCat.SubCategoryLinks2.Where(x => x.CategoryId == sOvr.CategoryId).Any())
                                {
                                    newCat.SubCategoryLinks2.Add(new SubCategoryLink
                                    {
                                        CategoryId = sOvr.CategoryId,
                                        NetworkObjectId = NetworkObjectId,
                                        OverrideStatus = OverrideStatus.ACTIVE,
                                        SortOrder = sOvr.SortOrder,
                                        OverrideParentSubCategoryId = parSubCatId
                                    });
                                    if (sOvr.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<SubCategoryLink>(sOvr);
                                    }
                                }
                            }
                            // get all references of sort order changes of below the tree and update to new collection
                            var childSortOverrides = _subCategoryLinks.Where(x => x.SubCategoryId == parSubCatId && x.OverrideParentSubCategoryId == parSubCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED);
                            foreach (var sortoverridelink in childSortOverrides)
                            {
                                newCat.SubCategoryLinks2.Add(new SubCategoryLink
                                {
                                    CategoryId = sortoverridelink.CategoryId,
                                    NetworkObjectId = sortoverridelink.NetworkObjectId,
                                    OverrideStatus = sortoverridelink.OverrideStatus,
                                    SortOrder = sortoverridelink.SortOrder,
                                    OverrideParentSubCategoryId = parSubCatId
                                });
                                _repository.Delete<SubCategoryLink>(sortoverridelink);
                            }
                            // get all references of this category and change the category
                            var allCatMnuLinks = _categoryMenuLinks.Where(x => x.CategoryId == parSubCatId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ParentCategoryId == null && x.OverrideStatus != OverrideStatus.HIDDEN);
                            var cmlSortOverrideIds = _categoryMenuLinks.Where(x => x.CategoryId == parSubCatId && x.ParentCategoryId == parSubCatId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.CategoryId);
                            foreach (var catMnuLink in allCatMnuLinks.Where(x => !cmlSortOverrideIds.Contains(x.CategoryId)))
                            {
                                var sortOrder = catMnuLink.SortOrder;
                                var overridenByParent = _categoryMenuLinks.Where(x => x.ParentCategoryId == parSubCatId && x.CategoryId != parSubCatId && x.MenuId == MenuId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenByParent != null)
                                {
                                    // get the sortorder if it changed and then overriden by parent
                                    sortOrder = overridenByParent.SortOrder;
                                    if (overridenByParent.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<CategoryMenuLink>(overridenByParent);
                                    }
                                }

                                //MMS - 243 - If item is deleted in other category donot add the new item to that category
                                var overridenAsHidden = _categoryMenuLinks.Where(x => x.ParentCategoryId == parSubCatId && x.MenuId == MenuId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                                if (overridenAsHidden != null)
                                {
                                    continue;
                                }

                                newCat.CategoryMenuLinks.Add(new CategoryMenuLink
                                {
                                    MenuId = MenuId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortOrder,
                                    ParentCategoryId = parSubCatId
                                });
                            }

                            //If this was moved in any item by current NW
                            var cmlSortOverrides = _categoryMenuLinks.Where(x => x.CategoryId == categoryModel.CategoryId && x.ParentCategoryId == categoryModel.CategoryId && x.OverrideStatus == OverrideStatus.MOVED && cmlSortOverrideIds.Contains(x.CategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId);
                            foreach (var sOvr in cmlSortOverrides)
                            {
                                if (!newCat.CategoryMenuLinks.Where(x => x.MenuId == MenuId).Any())
                                {
                                    newCat.CategoryMenuLinks.Add(new CategoryMenuLink
                                    {
                                        MenuId = MenuId,
                                        NetworkObjectId = NetworkObjectId,
                                        OverrideStatus = OverrideStatus.ACTIVE,
                                        SortOrder = sOvr.SortOrder,
                                        ParentCategoryId = parSubCatId
                                    });
                                    if (sOvr.NetworkObjectId == NetworkObjectId)
                                    {
                                        _repository.Delete<CategoryMenuLink>(sOvr);
                                    }
                                }
                            }

                            // get all references of sort order changes of below the tree and update to new collection
                            var childCMLSortOverrides = _categoryMenuLinks.Where(x => x.CategoryId == parSubCatId && x.ParentCategoryId == parSubCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED);
                            foreach (var catMnu in childCMLSortOverrides)
                            {
                                newCat.CategoryMenuLinks.Add(new CategoryMenuLink
                                {
                                    MenuId = MenuId,
                                    NetworkObjectId = catMnu.NetworkObjectId,
                                    OverrideStatus = catMnu.OverrideStatus,
                                    SortOrder = catMnu.SortOrder,
                                    ParentCategoryId = parSubCatId
                                });
                                _repository.Delete<CategoryMenuLink>(catMnu);
                            }
                            #endregion
                        }
                        else
                        {
                            #region Add New
                            if (catType == MenuType.SubCategory)
                            {
                                //If it is new subCat add it to Category
                                var subCats = _subCategoryLinks.Where(x => x.CategoryId == parentCategoryId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN && x.OverrideParentSubCategoryId == null);
                                var sortord = 0;
                                if (subCats.Any())
                                {
                                    sortord = subCats.Max(x => x.SortOrder);
                                }
                                newCat.SubCategoryLinks2.Add(new SubCategoryLink
                                {
                                    CategoryId = parentCategoryId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sortord + 1,
                                    OverrideParentSubCategoryId = null
                                });
                            }
                            else
                            {
                                var cats = _categoryMenuLinks.Where(x => x.MenuId == MenuId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN && x.ParentCategoryId == null);
                                var sortord = 0;
                                if (cats != null && cats.Count() > 0)
                                {
                                    sortord = cats.Max(x => x.SortOrder);
                                }

                                newCat.CategoryMenuLinks.Add(new CategoryMenuLink
                                {
                                    MenuId = MenuId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = categoryModel.CategoryId != 0 ? categoryModel.SortOrder : sortord + 1,
                                    ParentCategoryId = null
                                });
                            }
                            #endregion
                        }

                        _repository.Add<Category>(newCat);
                        if (categoryModel.CategoryId == 0)
                        {
                            _lastActionResult = string.Format(Constants.AuditMessage.SubCategoryCreateT, categoryModel.InternalName);
                        }
                        else
                        {
                            _lastActionResult = string.Format(Constants.AuditMessage.SubCategoryUpdateT, categoryModel.InternalName);
                        }
                    }
                    //If there is no need to add override then update exisiting subcat
                    else
                    {
                        #region Update
                        if (cat != null)
                        {
                            mapCatModeltoCat(categoryModel, ref cat);
                            mapCatSchModeltoCatSch(categoryModel, ref cat, false);
                            cat.UpdatedDate = DateTime.UtcNow;
                            _repository.Update<Category>(cat);
                            _lastActionResult = string.Format(Constants.AuditMessage.SubCategoryUpdateT, categoryModel.InternalName);
                        }
                        else
                        {
                            _lastActionResult = string.Format(Constants.StatusMessage.ErrSubCategorySaveT, categoryModel.InternalName);
                        }
                        #endregion
                    }
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                    _context.SaveChanges();
                }
                else
                {
                    _lastActionResult = Constants.StatusMessage.ErrCategoryUniqueDeepLinkT;
                    _validatonDictionary.AddError("DeepLinkId", _lastActionResult);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCatLinkAddT, categoryModel.InternalName);
                Logger.WriteError(ex);
            }
            //Update the log and return subcat ID
            if (addNewCatandLink)
            {
                if (!_lastActionResult.Contains("failed"))
                {
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: _lastActionResult);
                }
                return newCat.CategoryId;
            }
            else
            {
                if (!_lastActionResult.Contains("failed"))
                {
                    if (categoryModel.IsScheduleModified)
                    {
                        _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.CatSchDetailUpdate, categoryModel.InternalName));
                    }
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.SubCategoryUpdateT, categoryModel.InternalName));
                }
                return categoryModel.CategoryId;
            }
        }

        /// <summary>
        /// Save/Override/Create a Collection in Item
        /// </summary>
        /// <param name="colModel"></param>
        /// <param name="parentItemId"></param>
        /// <returns></returns>
        public int SaveCollection(CollectionModel colModel, int parentItemId)
        {
            var addNewColandLink = true;
            var itmCol = new ItemCollection();
            var newItmCol = new ItemCollection();
            try
            {
                if (NetworkObjectId > 0)
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                }
                var parItmColId = 0;
                var isprntId = false;

                #region Check Override
                //make sure this parent was not an override for another Item
                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == parentItemId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    parentItemId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == parentItemId).FirstOrDefault();
                    if (collObjovrride != null && collObjovrride.ParentItemId != null)
                        parentItemId = collObjovrride.ParentItemId.Value;
                }
                #endregion

                #region Calculations
                //Not a new Collection
                if (colModel.CollectionId != 0)
                {
                    itmCol = _repository.GetQuery<ItemCollection>(x => x.CollectionId == colModel.CollectionId).FirstOrDefault();

                    //if collection is not created here then it is a override
                    if (itmCol.NetworkObjectId == NetworkObjectId)
                    {
                        addNewColandLink = false;
                    }
                }
                #endregion

                if (addNewColandLink)
                {
                    #region Initialize
                    //Load table
                    _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == MenuId).Include("NetworkObject").Include("ItemCollection").ToList();

                    //Get the originalId of collection being updated
                    if (colModel.CollectionId != 0)
                    {
                        parItmColId = colModel.CollectionId;
                        while (!isprntId)
                        {
                            isprntId = getMasterParentColId(parItmColId, parentItemId, out parItmColId);
                        }
                    }

                    mapCollectionModeltoCollection(colModel, ref newItmCol);

                    newItmCol.CollectionId = 0;
                    newItmCol.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName); // Created new guid for override too
                    newItmCol.OverrideCollectionId = colModel.CollectionId != 0 ? (int?)parItmColId : null;
                    newItmCol.NetworkObjectId = NetworkObjectId;
                    newItmCol.CreatedDate = DateTime.UtcNow;
                    newItmCol.UpdatedDate = DateTime.UtcNow;

                    #endregion

                    if (colModel.CollectionId != 0)
                    {
                        #region Add as override and Update all occurances of this instance but complete network Tree
                        // get all references of this collection and change the collection
                        var itmCollectionLinks = _itemCollectionLinks.Where(x => x.CollectionId == parItmColId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ParentCollectionId == null && x.OverrideStatus != OverrideStatus.HIDDEN);
                        var sortOverrideIds = _itemCollectionLinks.Where(x => x.CollectionId == parItmColId && x.ParentCollectionId == parItmColId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Select(x => x.ItemId);
                        foreach (var itmColl in itmCollectionLinks.Where(x => !sortOverrideIds.Contains(x.ItemId)))
                        {
                            var sortOrder = itmColl.SortOrder;
                            var overridenByParent = _itemCollectionLinks.Where(x => x.ParentCollectionId == parItmColId && x.CollectionId != parItmColId && x.ItemId == parentItemId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                            if (overridenByParent != null)
                            {
                                // get the sortorder if it changed and then overriden by parent
                                sortOrder = overridenByParent.SortOrder;
                                if (overridenByParent.NetworkObjectId == NetworkObjectId)
                                {
                                    _repository.Delete<ItemCollectionLink>(overridenByParent);
                                }
                            }

                            //MMS - 243 - If item is deleted in other category donot add the new item to that category
                            var overridenAsHidden = _itemCollectionLinks.Where(x => x.ParentCollectionId == parItmColId && x.ItemId == parentItemId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.HIDDEN).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                            if (overridenAsHidden != null)
                            {
                                continue; // do not add edit override
                            }

                            newItmCol.ItemCollectionLinks.Add(new ItemCollectionLink
                            {
                                ItemId = itmColl.ItemId,
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = sortOrder,
                                ParentCollectionId = parItmColId
                            });
                        }

                        //If this was moved in any item by current NW
                        var sortOverrides = _itemCollectionLinks.Where(x => x.CollectionId == colModel.CollectionId && x.ParentCollectionId == colModel.CollectionId && x.OverrideStatus == OverrideStatus.MOVED && sortOverrideIds.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId);
                        foreach (var sOvr in sortOverrides)
                        {
                            if (!newItmCol.ItemCollectionLinks.Where(x => x.ItemId == sOvr.ItemId).Any())
                            {
                                newItmCol.ItemCollectionLinks.Add(new ItemCollectionLink
                                {
                                    ItemId = sOvr.ItemId,
                                    NetworkObjectId = NetworkObjectId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    SortOrder = sOvr.SortOrder,
                                    ParentCollectionId = parItmColId
                                });
                                if (sOvr.NetworkObjectId == NetworkObjectId)
                                {
                                    _repository.Delete<ItemCollectionLink>(sOvr);
                                }
                            }
                        }

                        // get all references of sort order changes of below the tree and update to new collection
                        var childSortOverrides = _itemCollectionLinks.Where(x => x.CollectionId == parItmColId && x.ParentCollectionId == parItmColId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED);
                        foreach (var itmColl in childSortOverrides)
                        {
                            newItmCol.ItemCollectionLinks.Add(new ItemCollectionLink
                            {
                                ItemId = itmColl.ItemId,
                                NetworkObjectId = itmColl.NetworkObjectId,
                                OverrideStatus = itmColl.OverrideStatus,
                                SortOrder = itmColl.SortOrder,
                                ParentCollectionId = parItmColId
                            });
                            _repository.Delete<ItemCollectionLink>(itmColl);
                        }
                        #endregion
                    }
                    else
                    {
                        #region Add New
                        //If it is new collection add it to Item
                        var collections = _itemCollectionLinks.Where(x => x.ItemId == parentItemId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN && x.ParentCollectionId == null);
                        var sortord = 0;
                        if (collections.Any())
                        {
                            sortord = collections.Max(x => x.SortOrder);
                        }
                        newItmCol.ItemCollectionLinks.Add(new ItemCollectionLink
                        {
                            ItemId = parentItemId,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.ACTIVE,
                            SortOrder = sortord + 1,
                            ParentCollectionId = null
                        });
                        #endregion
                    }

                    _repository.Add<ItemCollection>(newItmCol);
                    if (colModel.CollectionId == 0)
                    {
                        _lastActionResult = string.Format(Constants.AuditMessage.ItemCollectionCreateT, colModel.InternalName);
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.AuditMessage.ItemCollectionUpdateT, colModel.InternalName);
                    }
                }
                //If there is no need to add override then update exisiting collection
                else
                {
                    #region Update
                    if (itmCol != null)
                    {
                        mapCollectionModeltoCollection(colModel, ref itmCol);
                        itmCol.UpdatedDate = DateTime.UtcNow;
                        _repository.Update<ItemCollection>(itmCol);
                        _lastActionResult = string.Format(Constants.AuditMessage.ItemCollectionUpdateT, colModel.InternalName);
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrItemCollectionSaveT, colModel.InternalName);
                    }
                    #endregion
                }
                _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId);
                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrItemCollectionSaveT, colModel.InternalName);
                Logger.WriteError(ex);
            }
            //Update the log and return collection ID
            if (addNewColandLink)
            {
                if (!_lastActionResult.Contains("failed"))
                {
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: _lastActionResult);
                }
                return newItmCol.CollectionId;
            }
            else
            {
                if (!_lastActionResult.Contains("failed"))
                {
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.ItemCollectionUpdateT, colModel.InternalName));
                }
                return colModel.CollectionId;
            }
        }

        /// <summary>
        /// revert the menu at current NW ( only if it is not created at this Network)
        /// </summary>
        /// <returns></returns>
        public bool RevertMenu()
        {
            var menuName = string.Empty;
            bool retVal = false;
            try
            {
                var menu = _repository.GetQuery<Menu>(x => x.MenuId == MenuId).FirstOrDefault();
                if (menu != null && menu.NetworkObjectId != NetworkObjectId)
                {
                    menuName = menu.InternalName;
                    //_context.Configuration.AutoDetectChangesEnabled = false;
                    _repository.UnitOfWork.BeginTransaction();

                    #region Initialzations

                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                    parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);
                    childMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(childNetworkNodeIds, MenuId);
                    _categoryObjects = _repository.GetQuery<CategoryObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Category").ToList();
                    _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("ItemCollection").ToList();
                    _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Category").ToList();
                    _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Category").ToList();
                    _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("ItemCollection").ToList();
                    _prependItemLinks = _repository.GetQuery<PrependItemLink>(x => parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) || childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").ToList();
                    #endregion

                    #region Remove overriden links
                    // Handle overrides by child NWs on New entites add at this network Level 
                    //Items
                    var newItemIds = _categoryObjects.Where(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId && x.ParentItemId == null).Select(x => x.ItemId).Distinct().ToList();
                    newItemIds.AddRange(_itemCollectionObjects.Where(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection.MenuId == MenuId && x.ParentItemId == null).Select(x => x.ItemId).Distinct().ToList());
                    if (newItemIds.Any())
                    {
                        // Delete overrides of this new items by child network
                        _repository.Delete<CategoryObject>(x => x.ParentItemId.HasValue && childNetworkNodeIds.Contains(x.NetworkObjectId) && newItemIds.Contains(x.ParentItemId.Value));
                        _repository.Delete<ItemCollectionObject>(x => x.ParentItemId.HasValue && childNetworkNodeIds.Contains(x.NetworkObjectId) && newItemIds.Contains(x.ParentItemId.Value));
                    }

                    //Categories
                    var newCatIds = _categoryMenuLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.MenuId == MenuId && x.ParentCategoryId == null).Select(x => x.CategoryId).Distinct().ToList();
                    newCatIds.AddRange(_subCategoryLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId && x.OverrideParentSubCategoryId == null).Select(x => x.SubCategoryId).Distinct().ToList());
                    if (newCatIds.Any())
                    {
                        _repository.Delete<CategoryMenuLink>(x => x.ParentCategoryId.HasValue && childNetworkNodeIds.Contains(x.NetworkObjectId) && newCatIds.Contains(x.ParentCategoryId.Value));
                        _repository.Delete<SubCategoryLink>(x => x.OverrideParentSubCategoryId.HasValue && childNetworkNodeIds.Contains(x.NetworkObjectId) && newCatIds.Contains(x.OverrideParentSubCategoryId.Value));
                    }
                    var orphanCats = newCatIds;

                    //Collections
                    var newColIds = _itemCollectionLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection.MenuId == MenuId && x.ParentCollectionId == null).Select(x => x.CollectionId).Distinct().ToList();
                    if (newColIds.Any())
                    {
                        _repository.Delete<ItemCollectionLink>(x => x.ParentCollectionId.HasValue && childNetworkNodeIds.Contains(x.NetworkObjectId) && newColIds.Contains(x.ParentCollectionId.Value));
                    }
                    var orphanCols = newColIds;

                    #endregion

                    #region Remove overrdien Items
                    //Update the items overriden by current network are which are moved by Child NW
                    //Delete Orphan Entites (Items, Category and Collection)
                    //Items
                    var orphanCatObjs = _categoryObjects.Where(x => x.NetworkObjectId == NetworkObjectId && x.Category != null && x.Category.MenuId == MenuId && x.ParentItemId.HasValue && x.ParentItemId.Value != x.ItemId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();
                    var orphanColObjs = _itemCollectionObjects.Where(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection != null && x.ItemCollection.MenuId == MenuId && x.ParentItemId.HasValue && x.ParentItemId.Value != x.ItemId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();
                    var orphanPrepLinks = _prependItemLinks.Where(x => x.MenuNetworkObjectLink != null && x.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId && x.MenuNetworkObjectLink.MenuId == MenuId && x.OverrideParentPrependItemId.HasValue && x.OverrideParentPrependItemId.Value != x.PrependItemId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();

                    var orphanItems = orphanCatObjs.Select(x => x.ItemId).Distinct().ToList();
                    var orphanCatObjIds = orphanCatObjs.Select(x => x.CategoryObjectId).ToList();
                    orphanItems.AddRange(orphanColObjs.Select(x => x.ItemId).Distinct().ToList());
                    var orphanColObjIds = orphanColObjs.Select(x => x.CollectionObjectId).Distinct().ToList();
                    orphanItems.AddRange(orphanPrepLinks.Select(x => x.PrependItemId).Distinct().ToList());
                    var orphanPrepLinkIds = orphanPrepLinks.Select(x => x.PrependItemLinkId).Distinct().ToList();

                    foreach (var orphanItemId in orphanItems.Distinct())
                    {
                        if (_categoryObjects.Where(x => x.ItemId == orphanItemId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _categoryObjects.Where(x => x.ItemId == orphanItemId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.ItemId = x.ParentItemId.Value);
                        }
                        if (_itemCollectionObjects.Where(x => x.ItemId == orphanItemId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _itemCollectionObjects.Where(x => x.ItemId == orphanItemId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.ItemId = x.ParentItemId.Value);
                        }
                        if (_prependItemLinks.Where(x => x.PrependItemId == orphanItemId && childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _prependItemLinks.Where(x => x.PrependItemId == orphanItemId && childMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.PrependItemId = x.OverrideParentPrependItemId.Value);
                        }

                        //Delete if it is not used anywhere
                        if (!_categoryObjects.Where(x => x.ItemId == orphanItemId && !orphanCatObjIds.Contains(x.CategoryObjectId)).Any() && !_itemCollectionObjects.Where(x => x.ItemId == orphanItemId && !orphanColObjIds.Contains(x.CollectionObjectId)).Any() && !_prependItemLinks.Where(x => x.PrependItemId == orphanItemId && !orphanPrepLinkIds.Contains(x.PrependItemLinkId)).Any())
                        {
                            _repository.Delete<AssetItemLink>(x => x.ItemId == orphanItemId);
                            _repository.Delete<ItemDescription>(x => x.ItemId == orphanItemId);
                            _repository.Delete<MenuItemCycleInSchedule>(x => x.MenuItemScheduleLink.ItemId == orphanItemId);
                            _repository.Delete<MenuItemScheduleLink>(x => x.ItemId == orphanItemId);
                            _repository.Delete<ItemPOSDataLink>(x => x.ItemId == orphanItemId);
                            _repository.Delete<TempSchedule>(x => x.ItemId == orphanItemId);

                            _repository.Delete<Item>(x => x.ItemId == orphanItemId);
                        }
                    }
                    #endregion

                    #region Categories
                    //categories
                    var orphanCatMnus = _categoryMenuLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId && x.ParentCategoryId.HasValue && x.ParentCategoryId.Value != x.CategoryId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();
                    var orphanSubCats = _subCategoryLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId && x.OverrideParentSubCategoryId.HasValue && x.OverrideParentSubCategoryId.Value != x.SubCategoryId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();

                    orphanCats.AddRange(orphanCatMnus.Select(x => x.CategoryId).Distinct().ToList());
                    var orphanCatMnuIds = orphanCatMnus.Select(x => x.CategoryMenuLinkId).ToList();
                    orphanCats.AddRange(orphanSubCats.Select(x => x.SubCategoryId).Distinct().ToList());
                    var orphanSubCatIds = orphanSubCats.Select(x => x.SubCategoryLinkId).Distinct().ToList();

                    foreach (var orphanCatId in orphanCats.Distinct())
                    {
                        if (_categoryMenuLinks.Where(x => x.CategoryId == orphanCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _categoryMenuLinks.Where(x => x.CategoryId == orphanCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.CategoryId = x.ParentCategoryId.Value);
                        }
                        if (_subCategoryLinks.Where(x => x.SubCategoryId == orphanCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _subCategoryLinks.Where(x => x.SubCategoryId == orphanCatId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.SubCategoryId = x.OverrideParentSubCategoryId.Value);
                        }
                        //Delete if it is not used anywhere
                        if (!_categoryMenuLinks.Where(x => x.CategoryId == orphanCatId && !orphanCatMnuIds.Contains(x.CategoryMenuLinkId)).Any() && !_subCategoryLinks.Where(x => x.SubCategoryId == orphanCatId && !orphanSubCatIds.Contains(x.SubCategoryLinkId)).Any())
                        {
                            _repository.Delete<AssetCategoryLink>(x => x.CategoryId == orphanCatId);

                            var catschLinkIds = _repository.GetQuery<MenuCategoryScheduleLink>(x => x.CategoryId == orphanCatId).Select(x => x.CategoryScheduleLinkId).ToList();

                            _repository.Delete<MenuCategoryCycleInSchedule>(x => catschLinkIds.Contains(x.CategoryScheduleLinkId));
                            _repository.Delete<MenuCategoryScheduleLink>(x => x.CategoryId == orphanCatId);

                            _repository.Delete<CategoryObject>(x => x.CategoryId == orphanCatId);
                            _repository.Delete<SubCategoryLink>(x => x.CategoryId == orphanCatId);
                            _repository.Delete<Category>(x => x.CategoryId == orphanCatId);
                        }
                    }

                    #endregion

                    #region Collections
                    //collections
                    var orphanItemCols = _itemCollectionLinks.Where(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection.MenuId == MenuId && x.ParentCollectionId.HasValue && x.ParentCollectionId.Value != x.CollectionId && x.OverrideStatus == OverrideStatus.ACTIVE).ToList();

                    orphanCols.AddRange(orphanItemCols.Select(x => x.CollectionId).Distinct().ToList());

                    foreach (var orphanColId in orphanCols.Distinct())
                    {
                        if (_itemCollectionLinks.Where(x => x.CollectionId == orphanColId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).Any())
                        {
                            _itemCollectionLinks.Where(x => x.CollectionId == orphanColId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.MOVED).ToList().ForEach(x => x.CollectionId = x.ParentCollectionId.Value);
                        }
                        _repository.Delete<ItemCollectionLink>(x => x.CollectionId == orphanColId);
                        _repository.Delete<ItemCollectionObject>(x => x.CollectionId == orphanColId);
                        _repository.Delete<ItemCollection>(x => x.CollectionId == orphanColId);
                    }
                    #endregion

                    #region All Other links
                    //Delete all Links
                    _repository.Delete<ItemPOSDataLink>(x => x.NetworkObjectId == NetworkObjectId && x.Item.MenuId == MenuId);
                    _repository.Delete<ItemCollectionObject>(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection.MenuId == MenuId);
                    _repository.Delete<ItemCollectionLink>(x => x.NetworkObjectId == NetworkObjectId && x.ItemCollection.MenuId == MenuId);
                    _repository.Delete<CategoryObject>(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId);
                    _repository.Delete<SubCategoryLink>(x => x.NetworkObjectId == NetworkObjectId && x.Category.MenuId == MenuId && x.SubCategory.MenuId == MenuId);
                    _repository.Delete<CategoryMenuLink>(x => x.NetworkObjectId == NetworkObjectId && x.MenuId == MenuId);
                    _repository.Delete<SpecialNoticeMenuLink>(x => x.MenuNetworkObjectLink.MenuId == MenuId && x.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId);
                    _repository.Delete<PrependItemLink>(x => x.MenuNetworkObjectLink.MenuId == MenuId && x.MenuNetworkObjectLink.NetworkObjectId == NetworkObjectId);
                    #endregion

                    _context.SaveChanges();
                    retVal = true;
                    // Update Last Updated Date
                    _commonService.SetMenuNetworksDateUpdated(MenuId, NetworkObjectId, isRevertMenu: true);
                    _repository.UnitOfWork.CommitTransaction();
                    _lastActionResult = string.Format(Constants.AuditMessage.RevertMenuT, menuName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { MenuId }, operationDescription: string.Format(Constants.AuditMessage.RevertMenuT, MenuId.ToString()));
                }
            }
            catch (Exception ex)
            {
                _repository.UnitOfWork.RollBackTransaction();
                _lastActionResult = string.Format(Constants.StatusMessage.ErrRevertMenuT, menuName);
                Logger.WriteError(ex);
            }
            return retVal;

        }

        /// <summary>
        /// Copy any Menu at current Network Level
        /// </summary>
        /// <param name="menuToCopy"></param>
        /// <returns></returns>
        public MenuDataModel CopyMenu(MenuDataModel menuToCopy)
        {
            var menuName = string.Empty;
            try
            {
                var orgmenuId = menuToCopy.MenuId;
                NetworkObjectId = menuToCopy.NetworkObjectId;
                var menu = _repository.GetQuery<Menu>(x => x.MenuId == orgmenuId).FirstOrDefault();
                if (menu != null)
                {
                    #region Menu
                    menuName = menu.InternalName;

                    _context.Configuration.AutoDetectChangesEnabled = false;
                    _repository.UnitOfWork.BeginTransaction();

                    //var newName = menu.InternalName + "-Copy";
                    ////Make sure name is Unique
                    //if (IsMenuNameNotUnique(newName, 0,NetworkObjectId,true))
                    //{
                    //    var latestMenuName = _repository.GetQuery<Menu>(x => x.InternalName.Contains(newName)).OrderByDescending(x => x.MenuId).FirstOrDefault().InternalName;
                    //    newName = latestMenuName + "-Copy";
                    //}

                    var existingMenus = new List<Menu>();
                    _ruleService.GetMenus(NetworkObjectId, existingMenus);

                    //create Menu and MenuNetworkLink
                    var newMenu = new Menu
                    {
                        InternalName = menuToCopy.InternalName,
                        Description = menuToCopy.Description,
                        NetworkObjectId = NetworkObjectId,
                        SortOrder = existingMenus.Count() + 1,
                        IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName)
                    };
                    newMenu.MenuNetworkObjectLinks.Add(new MenuNetworkObjectLink
                    {
                        NetworkObjectId = NetworkObjectId,
                        IsPOSMapped = false,
                        IsMenuOverriden = false,
                        LastUpdatedDate = DateTime.UtcNow
                    });
                    _repository.Add<Menu>(newMenu);
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                    parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, orgmenuId);
                    #endregion

                    #region channels

                    if (string.IsNullOrWhiteSpace(menuToCopy.ChannelIdList) == false)
                    {
                        var channelIdsToAdd = menuToCopy.ChannelIdList.Split(',').ToList();
                        foreach (var menuChannel in channelIdsToAdd)
                        {
                            if (string.IsNullOrWhiteSpace(menuChannel) == false)
                            {
                                var channelId = int.Parse(menuChannel);
                                newMenu.MenuTagLinks.Add(new MenuTagLink
                                {
                                    NetworkObjectId = NetworkObjectId,
                                    TagId = channelId,
                                    ParentTagId = null,
                                    OverrideStatus = OverrideStatus.ACTIVE
                                });
                            }
                        }
                    }

                    #endregion

                    #region Initializations
                    _categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.MenuId == orgmenuId).Include("Category").ToList();
                    _subCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.SubCategory.MenuId == orgmenuId).Include("SubCategory").ToList();
                    _categoryObjects = _repository.GetQuery<CategoryObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == orgmenuId).Include("Category").Include("Item").Include("Item1").ToList();
                    _categoryScheduleLinks = _repository.GetQuery<MenuCategoryScheduleLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId)).ToList();
                    _itemScheduleLinks = _repository.GetQuery<MenuItemScheduleLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId)).ToList();
                    _itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == orgmenuId).Include("ItemCollection").Include("Item").ToList();
                    _itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.ItemCollection.MenuId == orgmenuId).Include("ItemCollection").Include("Item").ToList();
                    _specialNoticeMenuLinks = _repository.GetQuery<SpecialNoticeMenuLink>(x => parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();
                    _prependItemLinks = _repository.GetQuery<PrependItemLink>(x => parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();

                    var catsIncludedInMenu = (_context as ProductMasterContext).fnNetworkCategories(NetworkObjectId, menuToCopy.MenuId, false).ToList().Select(x => x.CategoryId).ToList();
                    var itemsIncludedInMenu = (_context as ProductMasterContext).fnNetworkItems(NetworkObjectId, menuToCopy.MenuId, false).ToList().Select(x => x.ItemId).ToList();
                    var collectionsIncludedInMenu = (_context as ProductMasterContext).fnNetworkCollections(NetworkObjectId, menuToCopy.MenuId, false).ToList().Select(x => x.CollectionId).ToList();

                    var catIdsTobeCopied = new List<int>();
                    var colIdsTobeCopied = new List<int>();
                    var itemIdsTobeCopied = new List<int>();
                    var overridenPrntCatIdCatIdMap = new Dictionary<int, int>();
                    var overridenPrntColIdColIdMap = new Dictionary<int, int>();
                    var overridenPrntItemIdItemIdMap = new Dictionary<int, int>();
                    var newCatMapping = new Dictionary<int, Category>();
                    var newColMapping = new Dictionary<int, ItemCollection>();
                    var newItemMapping = new Dictionary<int, Item>();
                    #endregion

                    #region calculations
                    //Get last overide of each cat when sorted by NW type
                    var firstsInCMLOverrides = from p in _categoryMenuLinks.Where(y => y.ParentCategoryId.HasValue)
                                               //join c in catsIncludedInMenu on p.CategoryId equals c.CategoryId
                                               group p by p.ParentCategoryId into grp
                                               select grp.OrderByDescending(a => a.NetworkObject.NetworkObjectTypeId).First();

                    var applicableCatMnuLinks = firstsInCMLOverrides.ToList();
                    //overridenPrntCatIdCatIdMap = firstsInCMLOverrides.ToDictionary(r => r.ParentCategoryId.Value, r => r.CategoryId);
                    foreach (var ovr in firstsInCMLOverrides)
                    {
                        if (!overridenPrntCatIdCatIdMap.ContainsKey(ovr.ParentCategoryId.Value))
                        {
                            overridenPrntCatIdCatIdMap.Add(ovr.ParentCategoryId.Value, ovr.CategoryId);
                        }
                    }
                    foreach (var notOvr in _categoryMenuLinks.Where(x => !x.ParentCategoryId.HasValue))
                    {
                        // Add cat that are not overriden
                        if (!firstsInCMLOverrides.Any(x => x.MenuId == notOvr.MenuId && x.ParentCategoryId == notOvr.CategoryId))
                        {
                            applicableCatMnuLinks.Add(notOvr);
                        }
                    }
                    //applicableCatMnuLinks.AddRange(_catMnuLinks.Where(x => !x.ParentCategoryId.HasValue && !applicableCatMnuLinks.Select(z => z.ParentCategoryId).Contains(x.CategoryId)));

                    //Get last overide of each subcat when sorted by NW type
                    var firstsInSCLOverrides = from p in _subCategoryLinks.Where(y => y.OverrideParentSubCategoryId.HasValue)
                                               //join c in catsIncludedInMenu on p.CategoryId equals c.CategoryId
                                               group p by new { p.CategoryId, p.OverrideParentSubCategoryId } into grp
                                               select grp.OrderByDescending(a => a.NetworkObject.NetworkObjectTypeId).First();
                    var applicableSubCatLinks = firstsInSCLOverrides.ToList();
                    foreach (var ovr in firstsInSCLOverrides)
                    {
                        if (!overridenPrntCatIdCatIdMap.ContainsKey(ovr.OverrideParentSubCategoryId.Value))
                        {
                            overridenPrntCatIdCatIdMap.Add(ovr.OverrideParentSubCategoryId.Value, ovr.SubCategoryId);
                        }
                    }
                    foreach (var notOvr in _subCategoryLinks.Where(x => !x.OverrideParentSubCategoryId.HasValue))
                    {
                        // Add subcat that are not overriden
                        if (!firstsInSCLOverrides.Any(x => x.CategoryId == notOvr.CategoryId && x.OverrideParentSubCategoryId == notOvr.SubCategoryId))
                        {
                            applicableSubCatLinks.Add(notOvr);
                        }
                    }
                    //applicableSubCatLinks.AddRange(_subCatLinks.Where(x => !x.OverrideParentSubCategoryId.HasValue && !applicableSubCatLinks.Select(z => z.OverrideParentSubCategoryId).Contains(x.SubCategoryId)));

                    //Get only CatIds that are used at this level.
                    catIdsTobeCopied = applicableCatMnuLinks.Select(x => x.CategoryId).Distinct().ToList();
                    catIdsTobeCopied.AddRange(applicableSubCatLinks.Select(x => x.SubCategoryId).Distinct().ToList());
                    catIdsTobeCopied = catIdsTobeCopied.Where(x => catsIncludedInMenu.Contains(x)).ToList();

                    //Get last overide of each item when sorted by NW type
                    var firstsInCatObjOverrides = from p in _categoryObjects.Where(y => y.ParentItemId.HasValue)
                                                  //join c in itemsIncludedInMenu on p.ItemId equals c.ItemId
                                                  group p by new { p.CategoryId, p.ParentItemId } into grp
                                                  select grp.OrderByDescending(a => a.NetworkObject.NetworkObjectTypeId).First();
                    var applicableCatObjs = firstsInCatObjOverrides.ToList();

                    foreach (var ovr in firstsInCatObjOverrides)
                    {
                        if (!overridenPrntItemIdItemIdMap.ContainsKey(ovr.ParentItemId.Value))
                        {
                            overridenPrntItemIdItemIdMap.Add(ovr.ParentItemId.Value, ovr.ItemId);
                        }
                    }
                    foreach (var notOvr in _categoryObjects.Where(x => !x.ParentItemId.HasValue))
                    {
                        // Add items that are not overriden
                        if (!firstsInCatObjOverrides.Any(x => x.CategoryId == notOvr.CategoryId && x.ParentItemId == notOvr.ItemId))
                        {
                            applicableCatObjs.Add(notOvr);
                        }
                    }

                    //AS Per new business rule - Master Items are not directly added to Menu. Menu Items ( copy of Master Item) are added. Hence copy all MenuItems and Overrides Items to New Menus
                    itemIdsTobeCopied = applicableCatObjs.Select(x => x.ItemId).ToList(); //firstsInCatObjOverrides.Select(x => x.ItemId).ToList();

                    //Get last overide of each item when sorted by NW type
                    var firstsInICLOverrides = from p in _itemCollectionLinks.Where(y => y.ParentCollectionId.HasValue)
                                               //join c in collectionsIncludedInMenu on p.CollectionId equals c.CollectionId
                                               group p by new { p.ItemId, p.ParentCollectionId } into grp
                                               select grp.OrderByDescending(a => a.NetworkObject.NetworkObjectTypeId).First();
                    var applicableItmColLinks = firstsInICLOverrides.ToList();

                    //overridenPrntColIdColIdMap = firstsInICLOverrides.ToDictionary(r => r.ParentCollectionId.Value, r => r.CollectionId);
                    foreach (var ovr in firstsInICLOverrides)
                    {
                        if (!overridenPrntColIdColIdMap.ContainsKey(ovr.ParentCollectionId.Value))
                        {
                            overridenPrntColIdColIdMap.Add(ovr.ParentCollectionId.Value, ovr.CollectionId);
                        }
                    }
                    foreach (var notOvr in _itemCollectionLinks.Where(x => !x.ParentCollectionId.HasValue))
                    {
                        // Add items that are not overriden
                        if (!firstsInICLOverrides.Any(x => x.ItemId == notOvr.ItemId && x.ParentCollectionId == notOvr.CollectionId))
                        {
                            applicableItmColLinks.Add(notOvr);
                        }
                    }
                    //applicableItmColLinks.AddRange(_itmCollLinks.Where(x => !x.ParentCollectionId.HasValue && !applicableItmColLinks.Select(z => z.ParentCollectionId).Contains(x.CollectionId)));

                    //Get only ColIds that are used at this level.
                    colIdsTobeCopied = applicableItmColLinks.Select(x => x.CollectionId).Distinct().ToList();
                    colIdsTobeCopied = colIdsTobeCopied.Where(x => collectionsIncludedInMenu.Contains(x)).ToList();

                    //Get last overide of each item when sorted by NW type
                    var firstsInColObjOverrides = from p in _itemCollectionObjects.Where(y => y.ParentItemId.HasValue)
                                                  //join c in itemsIncludedInMenu on p.ItemId equals c.ItemId
                                                  group p by new { p.CollectionId, p.ParentItemId } into grp
                                                  select grp.OrderByDescending(a => a.NetworkObject.NetworkObjectTypeId).First();
                    var applicableColObjs = firstsInColObjOverrides.ToList();
                    foreach (var ovr in firstsInColObjOverrides)
                    {
                        if (!overridenPrntItemIdItemIdMap.ContainsKey(ovr.ParentItemId.Value))
                        {
                            overridenPrntItemIdItemIdMap.Add(ovr.ParentItemId.Value, ovr.ItemId);
                        }
                    }
                    foreach (var notOvr in _itemCollectionObjects.Where(x => !x.ParentItemId.HasValue))
                    {
                        // Add items that are not overriden
                        if (!firstsInColObjOverrides.Any(x => x.CollectionId == notOvr.CollectionId && x.ParentItemId == notOvr.ItemId))
                        {
                            applicableColObjs.Add(notOvr);
                        }
                    }
                    //applicableColObjs.AddRange(_itmCollObjects.Where(x => !x.ParentItemId.HasValue && !applicableColObjs.Select(z => z.ParentItemId).Contains(x.ItemId)));

                    //AS Per new business rule - Master Items are not directly added to Menu. Menu Items ( copy of Master Item) are added. Hence copy all MenuItems and Overrides Items to New Menus
                    itemIdsTobeCopied.AddRange(applicableColObjs.Select(x => x.ItemId).ToList()); //itemIdsTobeCopied.AddRange(firstsInColObjOverrides.Select(x => x.ItemId).ToList());

                    //Get last overide of each itemSch when sorted by NW type
                    var firstsInSNMLOverrides = from p in _specialNoticeMenuLinks
                                                //where conditions or joins with other tables to be included here
                                                group p by new { p.NoticeId } into grp
                                                select grp.OrderByDescending(a => a.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).First();
                    var applicableSplNoticeLinks = firstsInSNMLOverrides.ToList();

                    //Get last overide of each prepItem when sorted by NW type
                    var firstsInPILOverrides = from p in _prependItemLinks.Where(y => y.OverrideParentPrependItemId.HasValue)
                                               //join c in itemsIncludedInMenu on p.PrependItemId equals c.ItemId
                                               group p by new { p.ItemId, p.OverrideParentPrependItemId } into grp
                                               select grp.OrderByDescending(a => a.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).First();
                    var applicablePrepItemLinks = firstsInPILOverrides.ToList();
                    foreach (var notOverridenPrependItemLink in _prependItemLinks.Where(x => !x.OverrideParentPrependItemId.HasValue))
                    {
                        // Add schLinks thats are not overriden
                        if (!firstsInPILOverrides.Any(x => x.ItemId == notOverridenPrependItemLink.ItemId && x.OverrideParentPrependItemId == notOverridenPrependItemLink.PrependItemId))
                        {
                            applicablePrepItemLinks.Add(notOverridenPrependItemLink);
                        }
                    }

                    //AS Per new business rule - Master Items are not directly added to Menu. Menu Items ( copy of Master Item) are added. Hence copy all MenuItems and Overrides Items to New Menus
                    itemIdsTobeCopied.AddRange(applicablePrepItemLinks.Select(x => x.PrependItemId).ToList());
                    itemIdsTobeCopied = itemIdsTobeCopied.Where(x => itemsIncludedInMenu.Contains(x)).ToList();

                    #endregion

                    #region Items
                    //Create all Items
                    //Get item details for the catId that are used at this level
                    var items = _repository.GetQuery<Item>(x => itemIdsTobeCopied.Contains(x.ItemId)).ToList();
                    foreach (var itm in items)
                    {
                        var newItem = new Item();
                        mapItemtoItem(itm, ref newItem);
                        newItem.ItemId = 0;
                        newItem.CreatedDate = newItem.UpdatedDate = DateTime.UtcNow;
                        newItem.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName); // Create new guid for override too
                        newItem.ParentItemId = itm.ParentItemId;
                        newItem.Menu = newMenu;

                        var defaultPOSLink = itm.ItemPOSDataLinks.Where(x => x.IsDefault && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if(defaultPOSLink != null)
                        {
                            newItem.ItemPOSDataLinks.Add(new ItemPOSDataLink
                            {
                                POSDataId = defaultPOSLink.POSDataId,
                                IsDefault = true,
                                ParentMasterItemId = itm.ParentItemId.HasValue ? itm.ParentItemId : itm.ItemId,
                                NetworkObjectId = NetworkObjectId,
                                UpdatedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow

                            });
                        }
                        ////Add Schedules
                        foreach (var itmSch in itm.MenuItemScheduleLinks)
                        {
                            var menuItemCycleInSchList = new List<MenuItemCycleInSchedule>();
                            if (itmSch.MenuItemCycleInSchedules != null)
                            {
                                foreach (var itemCycleSch in itmSch.MenuItemCycleInSchedules)
                                {
                                    menuItemCycleInSchList.Add(new MenuItemCycleInSchedule
                                    {
                                        SchCycleId = itemCycleSch.SchCycleId,
                                        IsShow = itemCycleSch.IsShow,
                                        CreatedDate = DateTime.Now,
                                        UpdatedDate = DateTime.Now
                                    });
                                }
                            }
                            newItem.MenuItemScheduleLinks.Add(new MenuItemScheduleLink
                            {
                                NetworkObjectId = NetworkObjectId,
                                Day = itmSch.Day,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now,
                                IsSelected = itmSch.IsSelected,
                                MenuItemCycleInSchedules = menuItemCycleInSchList
                            });
                        }
                        _repository.Add<Item>(newItem);
                        newItemMapping.Add(itm.ItemId, newItem);
                    }
                    #endregion

                    #region categories
                    //Create all categories
                    //Get Cat details for the catId that are used at this level
                    var cats = _repository.GetQuery<Category>(x => x.MenuId == orgmenuId && catIdsTobeCopied.Contains(x.CategoryId)).Include("AssetCategoryLinks").ToList();
                    foreach (var cat in cats)
                    {
                        var newCat = new Category();
                        mapCategorytoCategory(cat, ref newCat);
                        newCat.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName); // Create new guid for override too
                        newCat.MenuId = newMenu.MenuId;
                        newCat.NetworkObjectId = NetworkObjectId;
                        newCat.CreatedDate = DateTime.UtcNow;
                        newCat.UpdatedDate = DateTime.UtcNow;
                        ////Add Schedules
                        foreach (var catSch in cat.MenuCategoryScheduleLinks)
                        {
                            var menuCatCycleInSchList = new List<MenuCategoryCycleInSchedule>();
                            if (catSch.MenuCategoryCycleInSchedules != null)
                            {
                                foreach (var catCycleSch in catSch.MenuCategoryCycleInSchedules)
                                {
                                    menuCatCycleInSchList.Add(new MenuCategoryCycleInSchedule
                                    {
                                        SchCycleId = catCycleSch.SchCycleId,
                                        IsShow = catCycleSch.IsShow,
                                        CreatedDate = DateTime.Now,
                                        UpdatedDate = DateTime.Now
                                    });
                                }
                            }
                            newCat.MenuCategoryScheduleLinks.Add(new MenuCategoryScheduleLink
                            {
                                NetworkObjectId = NetworkObjectId,
                                Day = catSch.Day,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now,
                                IsSelected = catSch.IsSelected,
                                MenuCategoryCycleInSchedules = menuCatCycleInSchList
                            });
                        }
                        _repository.Add<Category>(newCat);
                        newCatMapping.Add(cat.CategoryId, newCat);
                    }

                    foreach (var catMnuLink in applicableCatMnuLinks)
                    {
                        if (catMnuLink.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newCatMnuLink = new CategoryMenuLink
                            {
                                Category = newCatMapping[catMnuLink.CategoryId],
                                MenuId = newMenu.MenuId,
                                NetworkObjectId = NetworkObjectId, //catMnuLink.NetworkObjectId,TIVE, 
                                SortOrder = catMnuLink.SortOrder,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                Category1 = null
                            };
                            _repository.Add<CategoryMenuLink>(newCatMnuLink);
                        }
                    }
                    foreach (var subCatLink in applicableSubCatLinks)
                    {
                        // If the Parent Cat is overriden then in orginial menu's parent Cat has children but in copied menu we have to add for new Cat
                        var prntCatId = overridenPrntCatIdCatIdMap.ContainsKey(subCatLink.CategoryId) ? overridenPrntCatIdCatIdMap[subCatLink.CategoryId] : subCatLink.CategoryId;
                        var subCatId = overridenPrntCatIdCatIdMap.ContainsKey(subCatLink.SubCategoryId) ? overridenPrntCatIdCatIdMap[subCatLink.SubCategoryId] : subCatLink.SubCategoryId;
                        if (newCatMapping.ContainsKey(prntCatId) && subCatLink.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newSubcatLink = new SubCategoryLink
                            {
                                Category = newCatMapping[prntCatId],
                                SubCategory = newCatMapping[subCatId],
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = subCatLink.SortOrder,
                                OverrideParentSubCategory = null
                            };
                            _repository.Add<SubCategoryLink>(newSubcatLink);
                        }
                    }
                    foreach (var catObj in applicableCatObjs)
                    {
                        // If the Parent Cat is overriden, in orginial menu's parent Cat has children. But in copied menu we have to add for new Cat
                        var prntCatId = overridenPrntCatIdCatIdMap.ContainsKey(catObj.CategoryId) ? overridenPrntCatIdCatIdMap[catObj.CategoryId] : catObj.CategoryId;
                        var itemId = overridenPrntItemIdItemIdMap.ContainsKey(catObj.ItemId) ? overridenPrntItemIdItemIdMap[catObj.ItemId] : catObj.ItemId;

                        if (newCatMapping.ContainsKey(prntCatId) && catObj.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newCatObj = new CategoryObject
                            {
                                Category = newCatMapping[prntCatId],
                                Item = newItemMapping[itemId],
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = catObj.SortOrder,
                                Item1 = null
                            };
                            _repository.Add<CategoryObject>(newCatObj);
                        }
                    }

                    #endregion

                    #region Collections
                    //Create all Collections
                    //Get Cat details for the catId that are used at this level
                    var cols = _repository.GetQuery<ItemCollection>(x => x.MenuId == orgmenuId && colIdsTobeCopied.Contains(x.CollectionId)).ToList();
                    foreach (var col in cols)
                    {
                        var newCol = new ItemCollection();
                        mapCollectiontoCollection(col, ref newCol);
                        newCol.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);
                        newCol.MenuId = newMenu.MenuId;
                        newCol.NetworkObjectId = NetworkObjectId;
                        newCol.CreatedDate = DateTime.UtcNow;
                        newCol.UpdatedDate = DateTime.UtcNow;
                        _repository.Add<ItemCollection>(newCol);
                        newColMapping.Add(col.CollectionId, newCol);
                    }

                    foreach (var itemColLink in applicableItmColLinks)
                    {
                        //If the ParentItemId is overriden the get the new item added for the child Item
                        var prntItemId = overridenPrntItemIdItemIdMap.ContainsKey(itemColLink.ItemId) ? overridenPrntItemIdItemIdMap[itemColLink.ItemId] : itemColLink.ItemId;

                        if (newColMapping.ContainsKey(itemColLink.CollectionId) && newItemMapping.ContainsKey(prntItemId) && itemColLink.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newitemColLink = new ItemCollectionLink
                            {
                                Item = newItemMapping[prntItemId],
                                ItemCollection = newColMapping[itemColLink.CollectionId],
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = itemColLink.OverrideStatus,
                                SortOrder = itemColLink.SortOrder,
                                ItemCollection1 = null
                            };
                            _repository.Add<ItemCollectionLink>(newitemColLink);
                        }
                    }
                    foreach (var colObj in applicableColObjs)
                    {
                        // If the Parent Col is overriden then in orginial menu's parent Col has children but in copied menu we have to add for new Col
                        var prntColId = overridenPrntColIdColIdMap.ContainsKey(colObj.CollectionId) ? overridenPrntColIdColIdMap[colObj.CollectionId] : colObj.CollectionId;
                        var itemId = overridenPrntItemIdItemIdMap.ContainsKey(colObj.ItemId) ? overridenPrntItemIdItemIdMap[colObj.ItemId] : colObj.ItemId;

                        if (newColMapping.ContainsKey(prntColId) && colObj.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newColObj = new ItemCollectionObject
                            {
                                ItemCollection = newColMapping[prntColId],
                                Item = newItemMapping[itemId],
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = colObj.OverrideStatus,
                                SortOrder = colObj.SortOrder,
                                Item1 = null
                            };
                            _repository.Add<ItemCollectionObject>(newColObj);
                        }
                    }
                    #endregion

                    #region Prependitems
                    //Copy PrependItemLinks
                    foreach (var prepItemLink in applicablePrepItemLinks)
                    {
                        //If the ParentItemId is overriden the get the new item added for the child Item
                        var prntItemId = overridenPrntItemIdItemIdMap.ContainsKey(prepItemLink.ItemId) ? overridenPrntItemIdItemIdMap[prepItemLink.ItemId] : prepItemLink.ItemId;
                        var prependItemId = overridenPrntItemIdItemIdMap.ContainsKey(prepItemLink.PrependItemId) ? overridenPrntItemIdItemIdMap[prepItemLink.PrependItemId] : prepItemLink.PrependItemId;

                        if (prepItemLink.OverrideStatus != OverrideStatus.HIDDEN)
                        {
                            var newprepItemLink = new PrependItemLink
                            {
                                Item = newItemMapping[prntItemId],
                                PrependItem = newItemMapping[prependItemId],
                                MenuNetworkObjectLink = newMenu.MenuNetworkObjectLinks.FirstOrDefault(),
                                OverrideStatus = prepItemLink.OverrideStatus,
                                SortOrder = prepItemLink.SortOrder,
                                OverrideParentPrependItem = null
                            };
                            _repository.Add<PrependItemLink>(newprepItemLink);
                        }
                    }
                    #endregion

                    #region Special Notices
                    //Copy SpecialNotices
                    foreach (var splNCLink in applicableSplNoticeLinks)
                    {
                        SpecialNoticeMenuLink newSplNCLink = new SpecialNoticeMenuLink
                        {
                            NoticeId = splNCLink.NoticeId,
                            MenuNetworkObjectLink = newMenu.MenuNetworkObjectLinks.FirstOrDefault(),
                            IsLinked = splNCLink.IsLinked
                        };
                        _repository.Add<SpecialNoticeMenuLink>(newSplNCLink);
                    }
                    #endregion

                    _context.SaveChanges();
                    _repository.UnitOfWork.CommitTransaction();
                    menuToCopy.MenuId = newMenu.MenuId;
                    _lastActionResult = string.Format(Constants.AuditMessage.CopyMenuT, menuName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Menu, entityId: new List<int> { newMenu.MenuId }, operationDescription: _lastActionResult, netObjectId: newMenu.NetworkObjectId);
                }
                else
                {
                    _repository.UnitOfWork.RollBackTransaction();
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrCopyMenuT, menuName);
                }
            }
            catch (Exception ex)
            {
                _repository.UnitOfWork.RollBackTransaction();
                _lastActionResult = string.Format(Constants.StatusMessage.ErrCopyMenuT, menuName);
                _validatonDictionary.AddError("InternalName", _lastActionResult);
                Logger.WriteError(ex);
            }
            return menuToCopy;
        }

        /// <summary>
        /// Copy/Update Menu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public MenuDataModel SaveMenu(MenuDataModel model)
        {
            if (IsMenuNameNotUnique(model.InternalName, model.MenuId, model.NetworkObjectId, model.IsActionMenuCopy))
            {
                _validatonDictionary.AddError("InternalName", "Menu Name must be unique. A Menu with same name already exist at different level or same level.");
            }
            else
            {
                if (model.IsActionMenuCopy)
                {
                    model = CopyMenu(model);
                }
                else
                {
                    model = UpdateMenu(model);
                }
            }
            return model;
        }

        /// <summary>
        /// Creates new Menu for NW
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public MenuDataModel CreateMenu(MenuDataModel model)
        {
            try
            {
                _repository.UnitOfWork.BeginTransaction();

                if (IsMenuNameNotUnique(model.InternalName, model.MenuId, model.NetworkObjectId, false))
                {
                    _validatonDictionary.AddError("InternalName", "Menu Name must be unique. A Menu with same name already exist at different level or same level.");
                }
                else
                {

                    var existingMenus = new List<Menu>();
                    _ruleService.GetMenus(model.NetworkObjectId, existingMenus);
                    var menu = new Menu
                    {
                        InternalName = string.IsNullOrWhiteSpace(model.InternalName) ? string.Empty : model.InternalName.Trim(),
                        Description = string.IsNullOrWhiteSpace(model.Description) ? string.Empty : model.Description.Trim(),
                        NetworkObjectId = model.NetworkObjectId,
                        SortOrder = existingMenus.Count + 1,
                        IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName)
                    };

                    menu.MenuNetworkObjectLinks.Add(new MenuNetworkObjectLink
                    {
                        NetworkObjectId = model.NetworkObjectId,
                        IsMenuOverriden = false,
                        LastUpdatedDate = DateTime.UtcNow,
                        IsPOSMapped = false
                    });

                    //Add Channels
                    updateMenuChannelsAtgivenNetwork(menu, model, model.NetworkObjectId);

                    _repository.Add<Menu>(menu);
                    _context.SaveChanges();

                    //// Add link after menu is saved
                    //_commonService.SetMenuNetworksDateUpdated(menu.MenuId, model.NetworkObjectId);
                    //_context.SaveChanges();

                    _repository.UnitOfWork.CommitTransaction();
                    //populate the model back
                    model.MenuId = menu.MenuId;
                    model.LastUpdateDate = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt");
                    model.IsEditable = model.IsDeletable = true;
                    model.IsMenuOverriden = false;
                    _lastActionResult = string.Format(Constants.AuditMessage.MenuCreate, model.InternalName);
                    _auditLogger.Write(OperationPerformed.Created, EntityType.Menu, entityNameList: model.InternalName);
                }
            }
            catch (Exception ex)
            {
                _repository.UnitOfWork.RollBackTransaction();
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMenuCreate, model.InternalName);
                _validatonDictionary.AddError("InternalName", _lastActionResult);
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        ///Updates the Menu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public MenuDataModel UpdateMenu(MenuDataModel model)
        {
            try
            {
                Menu menu = _repository.GetQuery<Menu>(x => x.MenuId == model.MenuId).FirstOrDefault();
                if (menu != null)
                {
                    var oldName = menu.InternalName;

                    //Menu Name/Description can updated only if it created at specific Network
                    if (menu.NetworkObjectId == model.NetworkObjectId)
                    {
                        menu.InternalName = string.IsNullOrWhiteSpace(model.InternalName) ? string.Empty : model.InternalName.Trim();
                        menu.Description = string.IsNullOrWhiteSpace(model.Description) ? string.Empty : model.Description.Trim();
                        
                        _repository.Update<Menu>(menu);
                    }
                    updateMenuChannelsAtgivenNetwork(menu, model, model.NetworkObjectId);
                    _commonService.SetMenuNetworksDateUpdated(menu.MenuId, model.NetworkObjectId,false);
                    _context.SaveChanges();
                    //populate the model back
                    model.LastUpdateDate = DateTime.UtcNow.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt");
                    model.OperationStatus = _lastActionResult = string.Format(Constants.AuditMessage.MenuUpdate, oldName) + _lastActionResult;
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.Menu, entityNameList: model.InternalName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrMenuUpdate, model.InternalName);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMenuUpdate, model.InternalName);
                _validatonDictionary.AddError("InternalName", _lastActionResult);
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        /// update the channels of Menu at given Network
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="model"></param>
        /// <param name="networkIdWhereChannelsUpdated"></param>
        private void updateMenuChannelsAtgivenNetwork(Menu menu, MenuDataModel model, int networkIdWhereChannelsUpdated)
        {
            var tagKeyName = TagKeys.Channel.ToString();
            List<string> addedTagNames = new List<string>(), removedTagNames = new List<string>();

            //Load the Nodes of the current tree
            NetworkObjectId = _ruleService.NetworkObjectId = networkIdWhereChannelsUpdated;

            var menuChannelLinks = _ruleService.GetMenuTagLinkList(menu.MenuId, networkIdWhereChannelsUpdated);
            var existingChannels = string.Join(",", menuChannelLinks.Select(x => x.TagId));
            
            //If the selection is modified then update the channels
            if (existingChannels.Equals(model.ChannelIdList, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                
                var allMenuTagLinks = _repository.GetQuery<MenuTagLink>(x => x.MenuId == menu.MenuId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var allTags = _repository.GetQuery<Tag>(x => x.TagKey.Equals(tagKeyName, StringComparison.InvariantCultureIgnoreCase) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                var tagIdsAdded = string.Empty;
                var tagIdsRemoved = string.Empty;
                _tagService.GetTagsAddedNRemoved(model.ChannelIdList, existingChannels, out tagIdsAdded, out tagIdsRemoved);

                var channelIdsToAdd = tagIdsAdded.Split(',').ToList();
                var channelIdsToRemove = tagIdsRemoved.Split(',').ToList();
                foreach (var channel in channelIdsToAdd)
                {
                    if (string.IsNullOrWhiteSpace(channel) == false)
                    {
                        var channelId = int.Parse(channel);
                        var tagToAdd = allTags.Where(x => x.Id == channelId).FirstOrDefault();
                        
                        //If tagExists
                        if (tagToAdd != null)
                        {
                            var channelDisabled = allMenuTagLinks.Where(x => x.TagId == channelId).OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.MenuTagLinkId).FirstOrDefault();

                            var addEnableLink = true;
                            int? prnttagId = null;
                            if (channelDisabled != null && channelDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                            {
                                prnttagId = channelDisabled.ParentTagId == null ? channelDisabled.TagId : channelDisabled.ParentTagId;

                                if (channelDisabled.NetworkObjectId == networkIdWhereChannelsUpdated)
                                {
                                    addEnableLink = false;
                                    //Need recheck whether it is disabled by parent network
                                    var tagStillDisabled = allMenuTagLinks.Where(x => x.ParentTagId == channelDisabled.ParentTagId && x.NetworkObjectId != networkIdWhereChannelsUpdated)
                                                                .OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.MenuTagLinkId).FirstOrDefault();
                                    if (tagStillDisabled != null && tagStillDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                                    {//Add active link again
                                        addEnableLink = true;
                                    }
                                    _repository.Delete<MenuTagLink>(channelDisabled);
                                }
                            }

                            if (addEnableLink)
                            {
                                //Add Active row to enable back Tag or Add Tag
                                MenuTagLink menuTagLinkAsActive = new MenuTagLink
                                {
                                    TagId = channelId,
                                    NetworkObjectId = networkIdWhereChannelsUpdated,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    ParentTagId = prnttagId,
                                    MenuId = menu.MenuId
                                };
                                _repository.Add<MenuTagLink>(menuTagLinkAsActive);
                            }
                            //NO - This Tag is being enabled from child Networks. Hence remove if it was added at child networks
                            //This Tag is being disabled from all child Networks. Hence delete all  overrides which is added intially to Menu
                            _repository.Delete<MenuTagLink>(co => co.TagId == channelId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == menu.MenuId);
                            addedTagNames.Add(tagToAdd.TagName);
                        }
                    }
                }

                foreach (var channel in channelIdsToRemove)
                {
                    if (string.IsNullOrWhiteSpace(channel) == false)
                    {
                        var channelId = int.Parse(channel);
                        var tagToRemoved = allTags.Where(x => x.Id == channelId).FirstOrDefault();
                        
                        //If tagExists
                        if (tagToRemoved != null)
                        {
                            var channelEnabled = menuChannelLinks.Where(x => x.TagId == channelId).FirstOrDefault();

                            var addDisableLink = true;
                            int? prnttagId = null;
                            if (channelEnabled != null && channelEnabled.OverrideStatus != OverrideStatus.HIDDEN)
                            {
                                prnttagId = channelEnabled.ParentTagId == null ? channelEnabled.TagId : channelEnabled.ParentTagId;

                                if (channelEnabled.NetworkObjectId == networkIdWhereChannelsUpdated)
                                {
                                    //If it is overriden to be active at this network or added at this network
                                    addDisableLink = false;
                                    //Need recheck whether it is enabled by parent network
                                    var tagStillDisabled = allMenuTagLinks.Where(x => x.TagId == channelEnabled.ParentTagId && x.NetworkObjectId != networkIdWhereChannelsUpdated)
                                                                .OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.MenuTagLinkId).FirstOrDefault();
                                    if (tagStillDisabled != null && tagStillDisabled.OverrideStatus != OverrideStatus.HIDDEN)
                                    {//Add disable link again
                                        addDisableLink = true;
                                    }
                                    _repository.Delete<MenuTagLink>(channelEnabled);
                                }

                                if (addDisableLink)
                                {
                                    //Add Disable row to enable back Tag or Add Tag
                                    MenuTagLink menuTagLinkAsDisable = new MenuTagLink
                                    {
                                        TagId = channelId,
                                        NetworkObjectId = networkIdWhereChannelsUpdated,
                                        OverrideStatus = OverrideStatus.HIDDEN,
                                        ParentTagId = prnttagId,
                                        MenuId = menu.MenuId
                                    };
                                    _repository.Add<MenuTagLink>(menuTagLinkAsDisable);
                                }
                            }
                            else
                            {
                                //Tag is not present or already disabled. Hence no need to do anything
                            }

                            //This Tag is being disabled from all child Networks. Hence delete all  overrides which is added intially to Menu
                            _repository.Delete<MenuTagLink>(co => co.TagId == channelId && childNetworkNodeIds.Contains(co.NetworkObjectId) && co.MenuId == menu.MenuId);

                            removedTagNames.Add(tagToRemoved.TagName);
                        }
                    }
                }

                _lastActionResult = string.Format(Constants.AuditMessage.EntityTagUpdated, string.Join(",", addedTagNames), string.Join(",", removedTagNames));
            }
        }

        /// <summary>
        /// Deletes the Menu
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool DeleteMenu(MenuDataModel model, int netId)
        {
            bool retValue = false;
            try
            {
                Menu menu = _repository.GetQuery<Menu>(x => x.MenuId == model.MenuId).FirstOrDefault();
                if (menu != null && model.NetworkObjectId == netId)
                {
                    _context.Configuration.AutoDetectChangesEnabled = false;
                    _repository.UnitOfWork.BeginTransaction();

                    var collectionIds = _repository.GetQuery<ItemCollection>(x => x.MenuId == model.MenuId).Select(x => x.CollectionId).ToList();
                    var catIds = _repository.GetQuery<Category>(x => x.MenuId == model.MenuId).Select(x => x.CategoryId).ToList();
                    var mnNWLinkIds = _repository.GetQuery<MenuNetworkObjectLink>(x => x.MenuId == model.MenuId).Select(x => x.MenuNetworkObjectLinkId).ToList();

                    var itemIdsInCurrentMenu = _repository.GetQuery<CategoryObject>(x => catIds.Contains(x.CategoryId)).Select(x => x.ItemId).ToList();
                    itemIdsInCurrentMenu.AddRange(_repository.GetQuery<ItemCollectionObject>(x => collectionIds.Contains(x.CollectionId)).Select(x => x.ItemId).ToList());
                    itemIdsInCurrentMenu.AddRange(_repository.GetQuery<PrependItemLink>(x => mnNWLinkIds.Contains(x.MenuNetworkObjectLinkId)).Select(x => x.PrependItemId).ToList());
                    itemIdsInCurrentMenu.AddRange(_repository.GetQuery<Item>(x => x.MenuId == model.MenuId).Select(x => x.ItemId).ToList());
                    itemIdsInCurrentMenu = itemIdsInCurrentMenu.Distinct().ToList();

                    //var itemOverrideIdsInCurrentMenu = _repository.GetQuery<Item>(x => itemIdsInCurrentMenu.Contains(x.ItemId) && x.ParentItemId != null).Select(x => x.ItemId).ToList();

                    _repository.Delete<ItemCollectionObject>(x => x.ItemCollection.MenuId == model.MenuId);
                    _repository.Delete<ItemCollectionLink>(x => x.ItemCollection.MenuId == model.MenuId);
                    _repository.Delete<CategoryObject>(x => x.Category.MenuId == model.MenuId);
                    _repository.Delete<SubCategoryLink>(x => x.Category.MenuId == model.MenuId);
                    _repository.Delete<CategoryMenuLink>(x => x.Category.MenuId == model.MenuId);
                    _repository.Delete<AssetCategoryLink>(x => x.CategoryId.HasValue && x.Category.MenuId == model.MenuId);


                    _repository.Delete<MenuCategoryCycleInSchedule>(x => x.MenuCategoryScheduleLink.Category.MenuId == model.MenuId);
                    _repository.Delete<MenuItemCycleInSchedule>(x => x.MenuItemScheduleLink.Item.MenuId == model.MenuId);
                    _repository.Delete<MenuCategoryScheduleLink>(x => x.Category.MenuId == model.MenuId);
                    _repository.Delete<MenuItemScheduleLink>(x => x.Item.MenuId == model.MenuId);
                    _repository.Delete<ItemCollection>(x => x.MenuId == model.MenuId);
                    _repository.Delete<Category>(x => x.MenuId == model.MenuId);

                    _repository.Delete<SpecialNoticeMenuLink>(x => x.MenuNetworkObjectLink.MenuId == model.MenuId);
                    _repository.Delete<PrependItemLink>(x => x.MenuNetworkObjectLink.MenuId == model.MenuId);

                    _repository.Delete<AssetItemLink>(x => x.ItemId.HasValue && x.Item.MenuId == model.MenuId);
                    _repository.Delete<ItemDescription>(x => x.Item.MenuId == model.MenuId);
                    _repository.Delete<CategoryObject>(x => itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<ItemCollectionObject>(x => itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<ItemCollectionLink>(x => itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<PrependItemLink>(x => itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<ItemPOSDataLink>(x => itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<Item>(x => x.ParentItemId != null && itemIdsInCurrentMenu.Contains(x.ItemId));
                    _repository.Delete<ImportMapping>(x => x.MenuNetworkObjectLink.MenuId == model.MenuId);
                    _repository.Delete<MenuTagLink>(x => x.MenuId == model.MenuId);
                    _repository.Delete<MenuNetworkObjectLink>(x => x.MenuId == model.MenuId);
                    _repository.Delete<Menu>(x => x.MenuId == model.MenuId);

                    _context.SaveChanges();
                    _repository.UnitOfWork.CommitTransaction();
                    _lastActionResult = string.Format(Constants.AuditMessage.MenuDelete, menu.InternalName);
                    _auditLogger.Write(OperationPerformed.Deleted, EntityType.Menu, entityNameList: model.InternalName);
                    retValue = true;
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrMenuDelete, model.InternalName);
                }
            }
            catch (Exception ex)
            {
                _repository.UnitOfWork.RollBackTransaction();
                _lastActionResult = string.Format(Constants.StatusMessage.ErrMenuDelete, model.InternalName);
                Logger.WriteError(ex);
            }
            return retValue;
        }

        /// <summary>
        /// Check if the menuName is unique or not
        /// </summary>
        /// <param name="menuName"></param>
        /// <param name="menuId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public bool IsMenuNameNotUnique(string menuName, int menuId, int networkObjectId, bool isActionCopy)
        {
            bool retVal = true;

            if (string.IsNullOrWhiteSpace(menuName))
            {
                retVal = false;
            }
            else
            {
                menuName = string.IsNullOrWhiteSpace(menuName) ? string.Empty : menuName.Trim();
                if (!parentNetworkNodeIds.Any())
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(networkObjectId);
                }
                if (!childNetworkNodeIds.Any())
                {
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(networkObjectId);
                }

                //For New and Copy search all menus
                if (menuId == 0 || isActionCopy)
                {
                    retVal = _repository.FindOne<Menu>(x => (parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)) && System.String.Compare(x.InternalName.ToLower(), menuName.ToLower(), System.StringComparison.Ordinal) == 0) != null;
                }
                else
                {
                    retVal = _repository.FindOne<Menu>(x => (parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)) && x.MenuId != menuId && System.String.Compare(x.InternalName.ToLower(), menuName.ToLower(), System.StringComparison.Ordinal) == 0) != null;
                }
            }

            return retVal;
        }

        /// <summary>
        /// return Item along with Master Item details - Used in Menu Item Edit
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns>Item</returns>
        public ItemModel GetItemWithMaster(int itemId, int collectionId = 0, MenuType itemType = MenuType.Item)
        {
            var children = new List<string>
            {
                "ItemDescriptions",
                "AssetItemLinks",
                "AssetItemLinks.Asset",
                "ItemPOSDataLinks",
                "ItemPOSDataLinks.POSData",
                "MasterItem",
                "MasterItem.AssetItemLinks",
                "MasterItem.AssetItemLinks.Asset",
                "MasterItem.ItemDescriptions",
                "MasterItem.ItemPOSDataLinks",
                "MasterItem.ItemPOSDataLinks.POSData"
            };
            var itmModel = _itemService.GetItem(itemId, children.ToArray(), NetworkObjectId);
            itmModel.ItemType = itemType;
            if (itemType == MenuType.ItemCollectionItem)
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == collectionId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCollectionId != null)
                    collectionId = ovrride.ParentCollectionId.Value;

                var collectionObjects = _repository.GetQuery<ItemCollectionObject>(x => x.ItemId == itemId && x.CollectionId == collectionId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject");
                if (collectionObjects.Any())
                {
                    itmModel.IsAutoSelect = collectionObjects.OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault().IsAutoSelect;
                }
            }
            var masterItemId = itemId;
            if (itmModel.ParentItemId != null)
            {
                masterItemId = itmModel.ParentItemId.Value;
            }

            var masterItemchildren = new List<string>
            {
                "ItemDescriptions",
                "AssetItemLinks",
                "AssetItemLinks.Asset",
                "ItemPOSDataLinks",
                "ItemPOSDataLinks.POSData"
            };
            itmModel.MasterItem = _itemService.GetItem(masterItemId, masterItemchildren.ToArray());
            itmModel.Cycles = _schService.GetScheduleCycles(NetworkObjectId, false);
            itmModel.ScheduleDetails = GetItemSchDetailSummary(itmModel.ItemId);

            //generatehash for the data
            var itemJson = JsonConvert.SerializeObject(itmModel);
            //Create Hash to the specific data that is being sent.
            itmModel.ItemDataHash = HashGeneratorMD5.GetHash(itemJson);
            return itmModel;
        }

        /// <summary>
        /// return Category in a Menu to view model for particular Network
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="MenuId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns>Category</returns>
        public CategoryModel GetCategory(int categoryId, int menuId, int networkObjectId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var catModel = new CategoryModel();

            try
            {
                //var cat = _repository.GetQuery<Category>(i => i.CategoryId == CategoryId).SingleOrDefault();
                var cml = _repository.GetQuery<CategoryMenuLink>(cm => cm.CategoryId == categoryId && cm.MenuId == menuId && parentNetworkNodeIds.Contains(cm.NetworkObjectId)).Include("Category").Include("NetworkObject").OrderByDescending(cm => cm.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                if (cml != null && cml.Category != null)
                {
                    mapCategorytoCategoryModel(cml, ref catModel);
                    _schService.NetworkObjectId = NetworkObjectId;
                    catModel.Cycles = _schService.GetScheduleCycles(NetworkObjectId);
                    catModel.ScheduleDetails = GetCatSchDetailSummary(catModel.CategoryId);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return catModel;
        }

        /// <summary>
        /// return SubCategory in a Cat to view model for particular Network
        /// </summary>
        /// <param name="subCategoryId"></param>
        /// <param name="categoryId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public CategoryModel GetSubCategory(int subCategoryId, int categoryId, int networkObjectId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var catModel = new CategoryModel();

            try
            {
                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == categoryId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    categoryId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var sovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == categoryId).FirstOrDefault();
                    if (sovrride != null && sovrride.OverrideParentSubCategoryId != null)
                        categoryId = sovrride.OverrideParentSubCategoryId.Value;
                }

                var scl = _repository.GetQuery<SubCategoryLink>(cm => cm.SubCategoryId == subCategoryId && cm.CategoryId == categoryId && parentNetworkNodeIds.Contains(cm.NetworkObjectId)).Include("SubCategory").Include("NetworkObject").OrderByDescending(cm => cm.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                if (scl != null && scl.SubCategory != null)
                {
                    mapSubCategorytoCategoryModel(scl, ref catModel);
                    _schService.NetworkObjectId = NetworkObjectId;
                    catModel.Cycles = _schService.GetScheduleCycles(NetworkObjectId);
                    catModel.ScheduleDetails = GetCatSchDetailSummary(catModel.CategoryId);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return catModel;
        }

        /// <summary>
        /// return collection Model to view model
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="itemId"></param>
        /// <returns>ItemCollection</returns>
        public CollectionModel GetItemCollection(int collectionId, int itemId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var colModel = new CollectionModel();

            try
            {
                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == itemId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    itemId = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == itemId).FirstOrDefault();
                    if (collObjovrride != null && collObjovrride.ParentItemId != null)
                        itemId = collObjovrride.ParentItemId.Value;
                }

                var icl = _repository.GetQuery<ItemCollectionLink>(cm => cm.CollectionId == collectionId && cm.ItemId == itemId && parentNetworkNodeIds.Contains(cm.NetworkObjectId)).Include("ItemCollection").Include("NetworkObject").OrderByDescending(cm => cm.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                if (icl.ItemCollection != null)
                {
                    mapCollectiontoCollectionModel(icl, ref colModel);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return colModel;
        }

        /// <summary>
        /// List of Items in a Category for grid
        /// </summary>
        /// <param name="orginialCategoryId"></param>
        /// <returns>Item List</returns>
        public List<MenuGridItem> GetItemsByCategory(int orginialCategoryId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var itemsInCategoryList = new List<MenuGridItem>();

            //Get parent level categories for this menu
            var prntCatObjs = _repository.GetQuery<CategoryObject>(co => co.CategoryId == orginialCategoryId && parentNetworkNodeIds.Contains(co.NetworkObjectId)).Include("NetworkObject").ToList();

            //attempt to improve performance
            var itemIds = prntCatObjs.Select(x => x.ItemId).Distinct().ToList();
            var items = _repository.GetQuery<Item>(x => itemIds.Contains(x.ItemId)).Include("ItemPOSDataLinks").Include("MasterItem.AssetItemLinks").ToList();

            //Get all overrides
            var itmoverrides = (from co in prntCatObjs.Where(m => m.ParentItemId != null)
                                select new ovr<CategoryObject> { OverrideStatus = co.OverrideStatus, Id = co.ParentItemId.Value, NetworkTypeId = co.NetworkObject.NetworkObjectTypeId, Value = co })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntCO in prntCatObjs.Where(x => x.ParentItemId == null))
            {
                var itemToAdd = new MenuGridItem();
                ////ignore overrides
                //if (prntCO.ParentItemId != null) continue;

                bool hadOverride = false;
                //Assign current link to output
                CategoryObject co = prntCO;

                if (itmoverrides.Any())
                {
                    //update the output based on business rules
                    co = _ruleService.GetOvrValue<CategoryObject, ovr<CategoryObject>>(prntCO.ItemId, itmoverrides, prntCO, out hadOverride);
                }
                // If the result is not null - implies it is not a delete override
                if (co != null)
                {
                    var itemData = items.FirstOrDefault(x => x.ItemId == co.ItemId);
                    var defaultPOSLink = itemData.ItemPOSDataLinks.Where(x => x.IsDefault && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                    //Map the MenuGridItem Model
                    itemToAdd.TreeId = GetMenuTreeId(MenuType.Item, ovrridenId, co.ItemId);
                    itemToAdd.entityid = co.ItemId;
                    itemToAdd.actualid = prntCO.ItemId;
                    itemToAdd.typ = MenuType.Item;
                    itemToAdd.DisplayName = itemData.DisplayName; // attempt to improve performance
                    itemToAdd.BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? (int?)defaultPOSLink.POSData.PLU : null;
                    itemToAdd.AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty;
                    itemToAdd.InternalName = itemData.ItemName;
                    itemToAdd.IsFeatured = itemData.IsFeatured;
                    itemToAdd.IsModifier = itemData.IsModifier;
                    itemToAdd.IsIncluded = itemData.IsIncluded;
                    itemToAdd.HasImages = itemData.MasterItem.AssetItemLinks.Any();
                    itemToAdd.SortOrder = co.SortOrder;
                    itemToAdd.isOvr = co.ParentItemId.HasValue && co.ParentItemId == co.ItemId ? false : hadOverride;
                    itemsInCategoryList.Add(itemToAdd);
                }
            }

            //re arrange the items
            itemsInCategoryList = itemsInCategoryList.OrderBy(l => l.SortOrder).ToList();
            if (itemsInCategoryList.Any())
            {
                itemsInCategoryList[0].IsFirst = true;
                itemsInCategoryList[itemsInCategoryList.Count() - 1].IsLast = true;
            }
            return itemsInCategoryList;
        }

        /// <summary>
        /// List of Categories in a Menu with item Count for grid
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns>List of Categories in a Menu</returns>
        public List<MenuGridItem> GetCategoriesByMenu(int menuId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var categoriesList = new List<MenuGridItem>();

            //get parent level items for current NW
            var catMnuLinks = _repository.GetQuery<CategoryMenuLink>(ci => ci.MenuId == menuId && parentNetworkNodeIds.Contains(ci.NetworkObjectId))
                                                .Include("Category").Include("NetworkObject").ToList();

            //Get all overrides for current NW level
            var catoverrides = (from icl in catMnuLinks.Where(m => m.ParentCategoryId != null)
                                select new ovr<CategoryMenuLink> { OverrideStatus = icl.OverrideStatus, Id = icl.ParentCategoryId.Value, NetworkTypeId = icl.NetworkObject.NetworkObjectTypeId, Value = icl })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntCatMnuLink in catMnuLinks.Where(m => m.ParentCategoryId == null))
            {
                //Intialise the cat to be added.
                var catToAdd = new MenuGridItem();
                bool hadOverride = false;
                CategoryMenuLink cml = prntCatMnuLink;

                if (catoverrides.Any())
                {
                    //update the value based on business rules
                    cml = _ruleService.GetOvrValue<CategoryMenuLink, ovr<CategoryMenuLink>>(prntCatMnuLink.CategoryId, catoverrides, prntCatMnuLink, out hadOverride);
                }
                if (cml != null)
                {
                    //Map the properties of Model
                    catToAdd.TreeId = GetMenuTreeId(MenuType.Category, menuId, cml.CategoryId);
                    catToAdd.entityid = cml.CategoryId;
                    catToAdd.isOvr = catToAdd.entityid == prntCatMnuLink.CategoryId ? false : hadOverride;
                    catToAdd.actualid = prntCatMnuLink.CategoryId;
                    catToAdd.typ = MenuType.Category;
                    catToAdd.DisplayName = cml.Category.DisplayName;
                    catToAdd.InternalName = cml.Category.InternalName;
                    catToAdd.SortOrder = cml.SortOrder;

                    categoriesList.Add(catToAdd);
                }
            }

            //re arrange the values based on sortorder
            categoriesList = categoriesList.OrderBy(l => l.SortOrder).ToList();
            if (categoriesList.Any())
            {
                categoriesList[0].IsFirst = true;
                categoriesList[categoriesList.Count() - 1].IsLast = true;
            }

            return categoriesList;
        }

        /// <summary>
        /// List of SubCategories in a Categories with Count for grid
        /// </summary>
        /// <param name="orginialParentCatId"></param>
        /// <returns></returns>
        public List<MenuGridItem> GetSubCategories(int orginialParentCatId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var subCategoriesList = new List<MenuGridItem>();

            //get parent level items for current NW
            var subCatLinks = _repository.GetQuery<SubCategoryLink>(x => x.CategoryId == orginialParentCatId && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.Category.MenuId == MenuId)
                                                .Include("SubCategory").Include("NetworkObject").ToList();

            //Get all overrides for current NW level
            var catoverrides = (from icl in subCatLinks.Where(m => m.OverrideParentSubCategoryId != null)
                                select new ovr<SubCategoryLink> { OverrideStatus = icl.OverrideStatus, Id = icl.OverrideParentSubCategoryId.Value, NetworkTypeId = icl.NetworkObject.NetworkObjectTypeId, Value = icl })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntSubCatLink in subCatLinks.Where(m => m.OverrideParentSubCategoryId == null))
            {
                //Intialise the cat to be added.
                var catToAdd = new MenuGridItem();
                bool hadOverride = false;
                SubCategoryLink scl = prntSubCatLink;

                if (catoverrides.Any())
                {
                    //update the value based on business rules
                    scl = _ruleService.GetOvrValue<SubCategoryLink, ovr<SubCategoryLink>>(prntSubCatLink.SubCategoryId, catoverrides, prntSubCatLink, out hadOverride);
                }
                if (scl != null)
                {
                    //Map the properties of Model

                    catToAdd.TreeId = GetMenuTreeId(MenuType.SubCategory, ovrridenId, scl.SubCategoryId);
                    catToAdd.entityid = scl.SubCategoryId;
                    catToAdd.actualid = prntSubCatLink.SubCategoryId;
                    catToAdd.typ = MenuType.SubCategory;
                    catToAdd.DisplayName = scl.SubCategory.DisplayName;
                    catToAdd.InternalName = scl.SubCategory.InternalName;
                    catToAdd.SortOrder = scl.SortOrder;
                    catToAdd.isOvr = catToAdd.entityid == prntSubCatLink.SubCategoryId ? false : hadOverride;

                    subCategoriesList.Add(catToAdd);
                }
            }

            //re arrange the values based on sortorder
            subCategoriesList = subCategoriesList.OrderBy(l => l.SortOrder).ToList();
            if (subCategoriesList.Any())
            {
                subCategoriesList[0].IsFirst = true;
                subCategoriesList[subCategoriesList.Count() - 1].IsLast = true;
            }

            return subCategoriesList;
        }

        /// <summary>
        /// List of Collections in a Item for grid
        /// </summary>
        /// <param name="orginialItemId"></param>
        /// <returns>Collection List</returns>
        public List<MenuGridItem> GetCollectionbyItem(int orginialItemId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var collectionsInItemList = new List<MenuGridItem>();

            //get parent level items for current NW
            var prntCollections = _repository.GetQuery<ItemCollectionLink>(co => co.ItemId == orginialItemId && parentNetworkNodeIds.Contains(co.NetworkObjectId) && co.ItemCollection.MenuId == MenuId)
                                                .Include("ItemCollection").Include("NetworkObject").ToList();

            //Get all overrides for current NW level
            var collectionOverrides = (from icl in prntCollections.Where(m => m.ParentCollectionId != null)
                                       select new ovr<ItemCollectionLink> { OverrideStatus = icl.OverrideStatus, Id = icl.ParentCollectionId.Value, NetworkTypeId = icl.NetworkObject.NetworkObjectTypeId, Value = icl })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntCol in prntCollections.Where(m => m.ParentCollectionId == null))
            {
                var itemToAdd = new MenuGridItem();

                bool hadOverride = false;
                //intitalise the item to be added
                ItemCollectionLink icl = prntCol;

                if (collectionOverrides.Any())
                {
                    //update the value based on business rules
                    icl = _ruleService.GetOvrValue<ItemCollectionLink, ovr<ItemCollectionLink>>(prntCol.CollectionId, collectionOverrides, prntCol, out hadOverride);
                }
                if (icl != null)
                {
                    //Map the properties of Model
                    itemToAdd.TreeId = GetMenuTreeId(MenuType.ItemCollection, ovrridenId, icl.CollectionId);
                    itemToAdd.entityid = icl.CollectionId;
                    itemToAdd.actualid = prntCol.CollectionId;
                    itemToAdd.typ = MenuType.ItemCollection;
                    itemToAdd.DisplayName = icl.ItemCollection.DisplayName;
                    itemToAdd.InternalName = icl.ItemCollection.InternalName;
                    itemToAdd.SortOrder = icl.SortOrder;
                    itemToAdd.isOvr = icl.ParentCollectionId.HasValue && icl.ParentCollectionId == icl.CollectionId ? false : hadOverride;

                    collectionsInItemList.Add(itemToAdd);
                }
            }

            //re arrange items based on sortorder
            collectionsInItemList = collectionsInItemList.OrderBy(l => l.SortOrder).ToList();
            if (collectionsInItemList.Any())
            {
                collectionsInItemList[0].IsFirst = true;
                collectionsInItemList[collectionsInItemList.Count() - 1].IsLast = true;
            }
            return collectionsInItemList;
        }

        /// <summary>
        /// List of Items in a Collection for grid
        /// </summary>
        /// <param name="orginialCollectionId"></param>
        /// <returns>Item List</returns>
        public List<MenuGridItem> GetItemsByCollection(int orginialCollectionId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);

            var itemsInCollectionList = new List<MenuGridItem>();

            // get parent/master level items based on current NW
            var prntItemCOs = _repository.GetQuery<ItemCollectionObject>(ci => ci.CollectionId == orginialCollectionId && parentNetworkNodeIds.Contains(ci.NetworkObjectId))
                                                .Include("NetworkObject").ToList();

            //attempt to improve performance
            var itemIds = prntItemCOs.Select(x => x.ItemId).Distinct().ToList();
            var items = _repository.GetQuery<Item>(x => itemIds.Contains(x.ItemId)).Include("ItemPOSDataLinks").Include("MasterItem.AssetItemLinks").ToList();

            //Get all overrides above this NW level
            var itmoverrides = (from ico in prntItemCOs.Where(m => m.ParentItemId != null)
                                select new ovr<ItemCollectionObject> { OverrideStatus = ico.OverrideStatus, Id = ico.ParentItemId.Value, NetworkTypeId = ico.NetworkObject.NetworkObjectTypeId, Value = ico })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntItmCO in prntItemCOs.Where(x => x.ParentItemId == null))
            {
                var itemToAdd = new MenuGridItem();

                bool hadOverride = false;
                ItemCollectionObject ico = prntItmCO;

                if (itmoverrides.Any())
                {
                    ico = _ruleService.GetOvrValue<ItemCollectionObject, ovr<ItemCollectionObject>>(prntItmCO.ItemId, itmoverrides, prntItmCO, out hadOverride);
                }
                if (ico != null)
                {
                    var itemData = items.FirstOrDefault(x => x.ItemId == ico.ItemId);
                    var defaultPOSLink = itemData.ItemPOSDataLinks.Where(x => x.IsDefault && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();

                    itemToAdd.TreeId = GetMenuTreeId(MenuType.ItemCollectionItem, ovrridenId, ico.ItemId);
                    itemToAdd.entityid = ico.ItemId;
                    itemToAdd.actualid = prntItmCO.ItemId;
                    itemToAdd.typ = MenuType.ItemCollectionItem;
                    itemToAdd.DisplayName = itemData.DisplayName;
                    itemToAdd.BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? (int?)defaultPOSLink.POSData.PLU : null;
                    itemToAdd.AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty;
                    itemToAdd.InternalName = itemData.ItemName;
                    itemToAdd.IsFeatured = itemData.IsFeatured;
                    itemToAdd.IsModifier = itemData.IsModifier;
                    itemToAdd.IsIncluded = itemData.IsIncluded;
                    itemToAdd.HasImages = itemData.MasterItem.AssetItemLinks.Any();
                    itemToAdd.IsAutoSelect = ico.IsAutoSelect;
                    itemToAdd.SortOrder = ico.SortOrder;
                    itemToAdd.isOvr = ico.ParentItemId.HasValue && ico.ParentItemId == ico.ItemId ? false : hadOverride;
                    itemsInCollectionList.Add(itemToAdd);
                }
            }

            itemsInCollectionList = itemsInCollectionList.OrderBy(l => l.SortOrder).ToList();
            if (itemsInCollectionList.Any())
            {
                itemsInCollectionList[0].IsFirst = true;
                itemsInCollectionList[itemsInCollectionList.Count() - 1].IsLast = true;
            }

            return itemsInCollectionList;
        }

        /// <summary>
        /// List of Items in a Collection for grid
        /// </summary>
        /// <param name="orginialItemId"></param>
        /// <returns>Item List</returns>
        public List<MenuGridItem> GetPrependItemsByItem(int orginialItemId, int ovrridenId)
        {
            parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
            parentMenuNetworkNodeIds = _ruleService.GetMenuNetworkLinkIds(parentNetworkNodeIds, MenuId);

            var prependItemsInItemList = new List<MenuGridItem>();

            // get parent/master level items based on current NW
            var prntPrepItmLinks = _repository.GetQuery<PrependItemLink>(ci => ci.ItemId == orginialItemId && parentMenuNetworkNodeIds.Contains(ci.MenuNetworkObjectLinkId))
                                                .Include("PrependItem").Include("PrependItem.ItemPOSDataLinks").Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").ToList();

            //Get all overrides above this NW level
            var itmoverrides = (from ico in prntPrepItmLinks.Where(m => m.OverrideParentPrependItemId != null)
                                select new ovr<PrependItemLink> { OverrideStatus = ico.OverrideStatus, Id = ico.OverrideParentPrependItemId.Value, NetworkTypeId = ico.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId, Value = ico })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

            foreach (var prntprepItmLink in prntPrepItmLinks.Where(x => x.OverrideParentPrependItemId == null))
            {
                var itemToAdd = new MenuGridItem();

                bool hadOverride = false;
                PrependItemLink ico = prntprepItmLink;

                if (itmoverrides.Any())
                {
                    ico = _ruleService.GetOvrValue<PrependItemLink, ovr<PrependItemLink>>(prntprepItmLink.PrependItemId, itmoverrides, prntprepItmLink, out hadOverride);
                }
                if (ico != null)
                {

                    var defaultPOSLink = ico.PrependItem.ItemPOSDataLinks.Where(x => x.IsDefault && parentNetworkNodeIds.Contains(x.NetworkObjectId)).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                    //itemToAdd.TreeId = getmenuTreeId(MenuType.Item, ovrridenId, ico.ItemId);
                    itemToAdd.entityid = ico.PrependItemId;
                    itemToAdd.actualid = prntprepItmLink.ItemId;
                    itemToAdd.typ = MenuType.PrependItem;
                    itemToAdd.DisplayName = ico.PrependItem.DisplayName;
                    itemToAdd.BasePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? (int?)defaultPOSLink.POSData.PLU : null;
                    itemToAdd.AlternatePLU = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.AlternatePLU : string.Empty;
                    itemToAdd.SelectedPOSDataId = defaultPOSLink != null && defaultPOSLink.POSData != null ? defaultPOSLink.POSData.POSDataId : 0;
                    itemToAdd.POSDataList = _commonService.MapPOSDataDropdown(ico.PrependItem);
                    itemToAdd.InternalName = ico.PrependItem.ItemName;
                    itemToAdd.SortOrder = ico.SortOrder;
                    itemToAdd.isOvr = ico.OverrideParentPrependItemId.HasValue && ico.OverrideParentPrependItemId == ico.ItemId ? false : hadOverride;
                    prependItemsInItemList.Add(itemToAdd);
                }
            }

            prependItemsInItemList = prependItemsInItemList.OrderBy(l => l.SortOrder).ToList();
            if (prependItemsInItemList.Any())
            {
                prependItemsInItemList[0].IsFirst = true;
                prependItemsInItemList[prependItemsInItemList.Count() - 1].IsLast = true;
            }

            return prependItemsInItemList;
        }

        /// <summary>
        /// Converts the Schedule List of an item to ItemSchedule Summary
        /// </summary>
        /// <param name="menuItemId"></param>
        /// <returns></returns>
        public List<EntitySchSummary> GetItemSchDetailSummary(int menuItemId)
        {
            var itemSchDetailSummaryList = new List<EntitySchSummary>();

            //Get Item Schedule details
            var itemSchLinks = _ruleService.GetItemSchDetails(menuItemId);

            // Reterive summary information per each cycle
            var activeCycles = _ruleService.GetScheduleCycles(NetworkObjectId, false);
            //Reterive summary information per each day
            foreach (var dayOfWeek in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>())
            {
                var summ = new EntitySchSummary {Id = 0, Day = dayOfWeek};
                if (itemSchLinks.Any())
                {
                    var schDetailOfDay = itemSchLinks.FirstOrDefault(x => x.Day == (int)dayOfWeek);
                    if (schDetailOfDay != null)
                    {
                        summ.Id = schDetailOfDay.ItemScheduleLinkId;
                        summ.IsSelected = schDetailOfDay.IsSelected;
                        if (schDetailOfDay.MenuItemCycleInSchedules != null)
                        {
                            foreach (var cycleInSchedule in schDetailOfDay.MenuItemCycleInSchedules.Where(x => activeCycles.Contains(x.SchCycle)))
                            {
                                summ.Cycles.Add(new CycleInSchedule
                                {
                                    Id = cycleInSchedule.ItemScheduleCycleId,
                                    LinkId = cycleInSchedule.ItemScheduleLinkId,
                                    SchCycleId = cycleInSchedule.SchCycleId,
                                    CycleName = cycleInSchedule.SchCycle.CycleName,
                                    IsShow = cycleInSchedule.IsShow
                                });
                            }
                        }
                    }
                }
                else
                {//if there is no schedule for any day then only IsSelected should be true for all records
                    summ.IsSelected = true;
                }
                itemSchDetailSummaryList.Add(summ);
            }
            return itemSchDetailSummaryList;
        }

        /// <summary>
        /// Converts CategoryScheduleDetails to ScheduleSummary
        /// </summary>
        /// <param name="menuCategoryId"></param>
        /// MenuId
        /// NetworkId
        /// <returns></returns>
        public List<EntitySchSummary> GetCatSchDetailSummary(int menuCategoryId)
        {
            var catSchDetailSummaryList = new List<EntitySchSummary>();

            //Get Item Schedule details
            var catSchLinks = _ruleService.GetCatSchDetails(menuCategoryId);

            // Reterive summary information per each cycle
            var activeCycles = _ruleService.GetScheduleCycles(NetworkObjectId, false);
            //Reterive summary information per each day
            foreach (var dayOfWeek in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>())
            {
                var summ = new EntitySchSummary {Id = 0, Day = dayOfWeek};
                if (catSchLinks != null && catSchLinks.Any())
                {
                    var schDetailOfDay = catSchLinks.FirstOrDefault(x => x.Day == (int)dayOfWeek);
                    if (schDetailOfDay != null)
                    {
                        summ.Id = schDetailOfDay.CategoryScheduleLinkId;
                        summ.IsSelected = schDetailOfDay.IsSelected;
                        if (schDetailOfDay.MenuCategoryCycleInSchedules != null)
                        {
                            foreach (var cycleInSchedule in schDetailOfDay.MenuCategoryCycleInSchedules.Where(x => activeCycles.Contains(x.SchCycle)))
                            {
                                summ.Cycles.Add(new CycleInSchedule
                                {
                                    Id = cycleInSchedule.CategoryScheduleCycleId,
                                    LinkId = cycleInSchedule.CategoryScheduleLinkId,
                                    SchCycleId = cycleInSchedule.SchCycleId,
                                    CycleName = cycleInSchedule.SchCycle.CycleName,
                                    IsShow = cycleInSchedule.IsShow
                                });
                            }
                        }
                    }
                }
                else
                {//if there is no schedule for any day then only IsSelected should be true for all records
                    summ.IsSelected = true;
                }
                catSchDetailSummaryList.Add(summ);
            }
            return catSchDetailSummaryList;
        }

        /// <summary>
        /// return collection list to view model
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest">grid filtering, sorting, paging class</param>
        /// <returns>UI version of Collection Model</returns>
        public List<CollectionModel> GetMasterCollectionList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest)
        {
            List<CollectionModel> collectionModelList = null;
            try
            {
                var gridCollection = new KendoGrid<ItemCollection>();
                var filtering = gridCollection.GetFiltering(grdRequest);
                var sorting = gridCollection.GetSorting(grdRequest);

                var itemIds = new List<int>();
                var excludeCollections = new List<int>();

                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                if (parentId.HasValue)
                {
                    itemIds.Add(parentId.Value);
                    var loop = true;
                    do
                    {
                        loop = getparentCollections(itemIds, excludeCollections);

                    } while (loop);

                    //if (prntType.HasValue)
                    //{
                    //    getSiblingCols(parentId.Value, excludeCollections, prntType.Value);
                    //}
                }

                var collectionIdsInMenu = (_context as ProductMasterContext).fnNetworkCollections(NetworkObjectId, MenuId, true).ToList().Select(x => x.OriginalCollectionId).Distinct();

                var qryList = (from i in _repository.GetQuery<ItemCollection>(x => x.MenuId == MenuId && collectionIdsInMenu.Contains(x.CollectionId) && !excludeCollections.Contains(x.CollectionId))
                               select new CollectionModel
                               {
                                   CollectionId = i.CollectionId,
                                   DisplayName = i.DisplayName,
                                   InternalName = i.InternalName
                               }).Where(filtering).OrderBy(sorting);


                Count = qryList.Count();
                collectionModelList = qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return collectionModelList;
        }

        /// <summary>
        /// return master cat list to view model
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest">grid filtering, sorting, paging class</param>
        /// <returns>UI version of Category Model</returns>
        public List<CategoryModel> GetMasterCategoryList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest)
        {
            List<CategoryModel> categoryModelList = null;
            try
            {
                var gridCat = new KendoGrid<Category>();
                var filtering = gridCat.GetFiltering(grdRequest);
                var sorting = gridCat.GetSorting(grdRequest);

                var catIds = new List<int>();
                var excludeCats = new List<int>();

                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                if (parentId.HasValue && prntType.HasValue)
                {
                    catIds.Add(parentId.Value);
                    if (prntType.Value != MenuType.Menu)
                    {
                        excludeCats.Add(parentId.Value);
                        var loop = true;
                        do
                        {
                            loop = getparentCategories(catIds, excludeCats);

                        } while (loop);
                    }

                    //getSiblingCats(parentId.Value, excludeCats, (MenuType)prntType);
                }

                var categoryIdsInMenu = (_context as ProductMasterContext).fnNetworkCategories(NetworkObjectId, MenuId, true).ToList().Where(x => x.OriginalCategoryId.HasValue).Select(x => x.OriginalCategoryId.Value).Distinct();


                var qryList = (from i in _repository.GetQuery<Category>(x => categoryIdsInMenu.Contains(x.CategoryId) && !excludeCats.Contains(x.CategoryId))
                               select new CategoryModel
                               {
                                   CategoryId = i.CategoryId,
                                   DisplayName = i.DisplayName,
                                   InternalName = i.InternalName
                               }).Where(filtering).OrderBy(sorting);


                Count = qryList.Count();
                categoryModelList = qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize).ToList();
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex.Message);
            }
            return categoryModelList;
        }

        /// <summary>
        /// Get All categories for this network - Used in AssetManager
        /// </summary>
        /// param NetworkObjectId
        /// <returns>List of CategoryModels</returns>
        public List<CategoryModel> GetAllCategoriesList()
        {
            var catModelList = new List<CategoryModel>();
            var children = new List<string>();
            try
            {
                children.Add("Category");

                //Get Menus present in this network
                var menuList = new List<Menu>();
                _ruleService.GetAllMenusInNetworkTree(NetworkObjectId, menuList);

                var menuIds = menuList.Select(x => x.MenuId).Distinct();

                var allcatList = _repository.GetQuery<Category>(x => menuIds.Contains(x.MenuId)).ToList();

                //get categories by Calculating overrides
                foreach (var menu in menuList.Distinct())
                {
                    var catList = allcatList.Where(x => x.MenuId == menu.MenuId);
                    // If the cat is overriden then remove the overriden cat
                    var excludeCatIds = catList.Where(x => x.OverrideCategoryId != null).Select(x => x.CategoryId).Distinct();
                    catList = catList.Where(x => !excludeCatIds.Contains(x.CategoryId)).ToList();

                    //Map to model list
                    foreach (var i in catList.Distinct())
                    {
                        catModelList.Add(new CategoryModel
                        {
                            CategoryId = i.CategoryId,
                            DisplayName = i.DisplayName,
                            InternalName = i.InternalName,
                            MenuName = menu.InternalName
                        });
                    }
                }

                Count = catModelList.Count();
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return catModelList;
        }

        #region Private Methods

        /// <summary>
        /// Get Menu channels at selected nework
        /// </summary>
        /// <param name="menu"></param>
        /// <returns></returns>
        private List<TagModel> getMenuRelatedTags(int menuId, int networkObjectId)
        {
            var channels = new List<TagModel>();

            var menuTagLinks = _ruleService.GetMenuTagLinkList(menuId, networkObjectId);

            var channelTagKey = TagKeys.Channel.ToString().ToLower();
            foreach (var menuTagLink in menuTagLinks.Where(x => x.SelectedTag != null && x.SelectedTag.TagKey == channelTagKey))
            {
                if (menuTagLink.SelectedTag != null)
                {
                    channels.Add(new TagModel
                    {
                        TagId = menuTagLink.SelectedTag.Id,
                        TagName = menuTagLink.SelectedTag.TagName,
                        IsActive = menuTagLink.OverrideStatus != OverrideStatus.HIDDEN,
                    });
                }
            }

            return channels;
        }

        /// <summary>
        /// Check uniqueness for category deeplink
        /// </summary>
        /// <param name="deepLinkId"></param>
        /// <param name="categoryId"></param>
        /// <param name="menuId"></param>
        /// <param name="parentnetworkIds"></param>
        /// <param name="parentCategoryId"></param>
        /// <returns></returns>
        private bool isCategoryDeepLinkIdUniqueinBrand(string deepLinkId, int categoryId, int menuId, List<int> parentnetworkIds, int? parentCategoryId = null)
        {
            bool retVal = true;
            try
            {
                if (string.IsNullOrWhiteSpace(deepLinkId) == false)
                {
                    if (categoryId == 0)
                    {
                        retVal = _repository.FindOne<Category>(x => x.MenuId == menuId && parentnetworkIds.Contains(x.NetworkObjectId) && x.DeepLinkId.ToLower().CompareTo(deepLinkId.ToLower()) == 0) == null ? true : false;
                    }
                    else
                    {
                        retVal = _repository.FindOne<Category>(x => x.MenuId == menuId && parentnetworkIds.Contains(x.NetworkObjectId) && x.CategoryId != categoryId && (parentCategoryId.HasValue == false || x.CategoryId != parentCategoryId) && x.DeepLinkId.ToLower().CompareTo(deepLinkId.ToLower()) == 0) == null ? true : false;
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
        /// Map Collection to Another Collection - Used for Copy
        /// </summary>
        /// <param name="col"></param>
        /// <param name="newCol"></param>
        private void mapCollectiontoCollection(ItemCollection col, ref ItemCollection newCol)
        {
            newCol.CollectionId = col.CollectionId;
            newCol.CollectionTypeId = col.CollectionTypeId;
            newCol.IrisId = col.IrisId;
            newCol.DisplayName = col.DisplayName;
            newCol.InternalName = col.InternalName;
            newCol.MinQuantity = col.MinQuantity;
            newCol.MaxQuantity = col.MaxQuantity;
            newCol.ShowPrice = col.ShowPrice;
            newCol.IsMandatory = col.IsMandatory;
            newCol.IsPropagate = col.IsPropagate;
            newCol.IsVisibleToGuest = col.IsVisibleToGuest;
            newCol.ReplacesItem = col.ReplacesItem;
        }

        /// <summary>
        /// Map a Category to another Category - Used for override
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="newCat"></param>
        private void mapCategorytoCategory(Category cat, ref Category newCat)
        {
            newCat.CategoryTypeId = cat.CategoryTypeId;
            newCat.DisplayName = cat.DisplayName;
            newCat.InternalName = cat.InternalName;
            newCat.IsFeatured = cat.IsFeatured;
            newCat.ShowPrice = cat.ShowPrice;
            newCat.StartDate = cat.StartDate;
            newCat.EndDate = cat.EndDate;
            newCat.DeepLinkId = cat.DeepLinkId;
        }

        /// <summary>
        /// Map a Item to another Item - Used for override
        /// </summary>
        /// <param name="itm"></param>
        /// <param name="newItem"></param>
        /// <param name="appendDefaultPOSToItem"></param>
        private void mapItemtoItem(Item itm, ref Item newItem, bool appendDefaultPOSToItem = false , int? posDataId = 0)
        {
            newItem.ItemName = itm.ItemName;
            newItem.DisplayDescription = itm.DisplayDescription;
            newItem.DisplayName = itm.DisplayName;
            newItem.IsFeatured = itm.IsFeatured;
            newItem.ShowPrice = itm.ShowPrice;
            newItem.IsPriceOverriden = itm.IsPriceOverriden;
            newItem.DiscountedPrice = itm.DiscountedPrice;
            newItem.RecipeId = itm.RecipeId;
            newItem.NutritionId = itm.NutritionId;
            newItem.QuickOrder = itm.QuickOrder;
            newItem.IsAvailable = itm.IsAvailable;
            newItem.IsModifier = itm.IsModifier;
            newItem.IsSubstitute = itm.IsSubstitute;
            newItem.IsSendHierarchy = itm.IsSendHierarchy;
            newItem.IsTopLevel = itm.IsTopLevel;
            newItem.IsCombo = itm.IsCombo;
            newItem.StartDate = itm.StartDate;
            newItem.EndDate = itm.EndDate;
            newItem.NetworkObjectId = itm.NetworkObjectId;
            newItem.IsIncluded = itm.IsIncluded;
            newItem.ItemName = itm.ItemName;
            newItem.ButtonText = itm.ButtonText;
            newItem.PrintOnOrder = itm.PrintOnOrder;
            newItem.PrintOnReceipt = itm.PrintOnReceipt;
            newItem.PrintOnSameLine = itm.PrintOnSameLine;
            newItem.PrintRecipe = itm.PrintRecipe;
            newItem.ForceRecipe = itm.ForceRecipe;
            newItem.IsBeverage = itm.IsBeverage;
            newItem.IsEntreeApp = itm.IsEntreeApp;
            newItem.IsEnabled = itm.IsEnabled;
            newItem.ParentItemId = itm.ParentItemId;
            newItem.DisplayDescription = itm.DisplayDescription;
            newItem.IsCore = itm.IsCore;
            newItem.IsAlcohol = itm.IsAlcohol;
            newItem.DeepLinkId = itm.DeepLinkId;
            newItem.ModifierFlagId = itm.ModifierFlagId;
            newItem.CookTime = itm.CookTime;
            newItem.PrepOrderTime = itm.PrepOrderTime;
            newItem.DWItemCategorizationKey = itm.DWItemCategorizationKey;
            newItem.DWItemId = itm.DWItemId;
            newItem.DWItemSubTypeKey = itm.DWItemSubTypeKey;
            newItem.RequestedBy = itm.RequestedBy;
            newItem.Feeds = itm.Feeds;

            if (appendDefaultPOSToItem)
            {
                ItemPOSDataLink defaultPOS = null;
                if (posDataId.HasValue && posDataId != 0)
                {
                    defaultPOS = itm.ItemPOSDataLinks.FirstOrDefault(x => x.POSDataId == posDataId && x.ParentMasterItemId == null);
                }
                else
                {
                    defaultPOS = itm.ItemPOSDataLinks.Any(x => x.IsDefault) ? itm.ItemPOSDataLinks.FirstOrDefault(x => x.IsDefault) : null;
                }
                if (defaultPOS != null)
                {
                    //Add new default value
                    newItem.ItemPOSDataLinks.Add(new ItemPOSDataLink
                    {
                        POSDataId = defaultPOS.POSDataId,
                        IsDefault = true,
                        ParentMasterItemId = itm.ParentItemId.HasValue ? itm.ParentItemId : itm.ItemId,
                        NetworkObjectId = NetworkObjectId,
                        UpdatedDate = DateTime.UtcNow,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }
        }

        /// <summary>
        /// Map Category Model to Database Object Category
        /// </summary>
        /// <param name="catModel"></param>
        /// <param name="cat"></param>
        private void mapCatModeltoCat(CategoryModel catModel, ref Category cat)
        {
            cat.DisplayName = string.IsNullOrWhiteSpace(catModel.DisplayName) ? string.Empty : catModel.DisplayName.Trim();
            cat.InternalName = string.IsNullOrWhiteSpace(catModel.InternalName) ? string.Empty : catModel.InternalName.Trim();
            cat.IsFeatured = catModel.IsFeatured;
            cat.ShowPrice = catModel.ShowPrice;
            cat.MenuId = catModel.MenuId;
            cat.DeepLinkId = catModel.DeepLinkId;
            cat.CategoryTypeId = (CategoryTypes)catModel.CategoryTypeId;
            cat.StartDate = catModel.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(catModel.StartDate.Value) : catModel.StartDate;
            cat.EndDate = catModel.EndDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(catModel.EndDate.Value) : catModel.EndDate;
        }

        private void mapCatSchModeltoCatSch(CategoryModel catModel, ref Category catToUpdate, bool isCatBeingCopied)
        {
            
            if (isCatBeingCopied)
            {//If it is being copied, add all the new values to new item
                foreach (var catSchModel in catModel.ScheduleDetails)
                {
                    if (catSchModel.IsSelected || catSchModel.Cycles.Any(x => x.IsShow.HasValue))
                    {// the day is vaild if it is either selected r there are any day parts
                        var dayPartsInDay = catSchModel.Cycles.Where(x => x.IsShow.HasValue).Select(dayPartModel => new MenuCategoryCycleInSchedule
                        {
                            SchCycleId = dayPartModel.SchCycleId, IsShow = dayPartModel.IsShow != null && dayPartModel.IsShow.Value, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now,
                        }).ToList();
                        catToUpdate.MenuCategoryScheduleLinks.Add(new MenuCategoryScheduleLink
                        {
                            IsSelected = catSchModel.IsSelected,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now,
                            NetworkObjectId = NetworkObjectId,
                            Day = (int)catSchModel.Day,
                            MenuCategoryCycleInSchedules = dayPartsInDay
                        });
                    }
                }
            }
            else if (catModel.IsScheduleModified)
            {// otherwise update the existing item
                foreach (var catSchModel in catModel.ScheduleDetails)
                {
                    if (catSchModel.IsSelected || catSchModel.Cycles.Any(x => x.IsShow.HasValue))
                    {// the day is vaild if it is either selected r there are any day parts                    
                        if (catToUpdate.MenuCategoryScheduleLinks.Any(x => x.Day == (int)catSchModel.Day))
                        {//update the day

                            var catSchToUpdate = catToUpdate.MenuCategoryScheduleLinks.FirstOrDefault(x => x.Day == (int)catSchModel.Day);
                            catSchToUpdate.IsSelected = catSchModel.IsSelected;
                            catSchToUpdate.UpdatedDate = DateTime.Now;
                            foreach (var dayPartModel in catSchModel.Cycles)
                            {
                                if (catSchToUpdate.MenuCategoryCycleInSchedules.Any(x => x.SchCycleId == (int)dayPartModel.SchCycleId))
                                {
                                    var dayPartToUpdate = catSchToUpdate.MenuCategoryCycleInSchedules.FirstOrDefault(x => x.SchCycleId == (int)dayPartModel.SchCycleId);
                                    if (dayPartModel.IsShow.HasValue)
                                    {// update the day part 
                                        dayPartToUpdate.IsShow = dayPartModel.IsShow.Value;
                                        dayPartToUpdate.UpdatedDate = DateTime.Now;
                                    }
                                    else
                                    {//Remove if it is now cleared
                                        _repository.Delete<MenuCategoryCycleInSchedule>(dayPartToUpdate);
                                    }
                                }
                                else if (dayPartModel.IsShow.HasValue)
                                {// Add the  daypart
                                    catSchToUpdate.MenuCategoryCycleInSchedules.Add(new MenuCategoryCycleInSchedule
                                    {
                                        SchCycleId = dayPartModel.SchCycleId,
                                        IsShow = dayPartModel.IsShow.Value,
                                        CreatedDate = DateTime.Now,
                                        UpdatedDate = DateTime.Now,
                                    });
                                }
                            }
                        }
                        else
                        {// Add the day for this cat
                            var dayPartsInDay = new List<MenuCategoryCycleInSchedule>();
                            foreach (var dayPartModel in catSchModel.Cycles.Where(x => x.IsShow.HasValue))
                            {//Add all dayparts
                                dayPartsInDay.Add(new MenuCategoryCycleInSchedule
                                {
                                    SchCycleId = dayPartModel.SchCycleId,
                                    IsShow = dayPartModel.IsShow.Value,
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now,
                                });
                            }
                            catToUpdate.MenuCategoryScheduleLinks.Add(new MenuCategoryScheduleLink
                            {//Add day
                                IsSelected = catSchModel.IsSelected,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now,
                                NetworkObjectId = NetworkObjectId,
                                Day = (int)catSchModel.Day,
                                MenuCategoryCycleInSchedules = dayPartsInDay
                            });
                        }
                    }
                    else
                    {
                        //delete the CatSch if it is already present now unselected
                        if (catToUpdate.MenuCategoryScheduleLinks.Any(x => x.Day == (int)catSchModel.Day))
                        {
                            var catSchToDelete = catToUpdate.MenuCategoryScheduleLinks.FirstOrDefault(x => x.Day == (int)catSchModel.Day);
                            _repository.Delete<MenuCategoryCycleInSchedule>(x => x.CategoryScheduleLinkId == catSchToDelete.CategoryScheduleLinkId);
                            _repository.Delete<MenuCategoryScheduleLink>(catSchToDelete);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Map Item Model to DB Object Item
        /// </summary>
        /// <param name="itmModel"></param>
        /// <param name="itm"></param>
        private void mapItemModeltoItem(ItemModel itmModel, ref Item itm, bool isDWFieldsEnabled = false)
        {
            //itm.BasePLU = itmModel.BasePLU;
            itm.DisplayName = string.IsNullOrWhiteSpace(itmModel.DisplayName) ? string.Empty : itmModel.DisplayName.Trim();
            itm.ItemName = string.IsNullOrWhiteSpace(itmModel.ItemName) ? string.Empty : itmModel.ItemName.Trim();
            //itm.ItemDescriptionId = itmModel.ItemDescriptionId == 0 ? null : itmModel.ItemDescriptionId;
            itm.IsFeatured = itmModel.IsFeatured;
            itm.ShowPrice = itmModel.ShowPrice;
            itm.IsPriceOverriden = itmModel.IsPriceOverriden;
            itm.OverridenPrice = itmModel.OverridenPrice;
            itm.QuickOrder = itmModel.QuickOrder;
            itm.ShowPrice = itmModel.ShowPrice;
            itm.IsAvailable = itmModel.IsAvailable;
            itm.IsModifier = itmModel.IsModifier;
            itm.IsSubstitute = itmModel.IsSubstitute;
            itm.IsSendHierarchy = itmModel.IsSendHierarchy;
            itm.IsTopLevel = itmModel.IsTopLevel;
            itm.IsCombo = itmModel.IsCombo;
            itm.StartDate = itmModel.StartDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(itmModel.StartDate.Value) : itmModel.StartDate;
            itm.EndDate = itmModel.EndDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(itmModel.EndDate.Value) : itmModel.EndDate;
            itm.NetworkObjectId = itmModel.NetworkObjectId;
            itm.IsIncluded = itmModel.IsIncluded;
            itm.ModifierFlagId = itmModel.ModifierFlagId;
            itm.MenuId = MenuId;

            //EDM
            if (isDWFieldsEnabled)
            {
                itm.ButtonText = itmModel.ButtonText;
                itm.PrintOnOrder = itmModel.PrintOnOrder;
                itm.PrintOnReceipt = itmModel.PrintOnReceipt;
                itm.PrintOnSameLine = itmModel.PrintOnSameLine;
                itm.PrintRecipe = itmModel.PrintRecipe;
                itm.ForceRecipe = itmModel.ForceRecipe;
                itm.IsBeverage = itmModel.IsBeverage;
                itm.IsEntreeApp = itmModel.IsEntreeApp;
                itm.IsCore = itmModel.IsCore;
                itm.CookTime = itmModel.CookTime;
                itm.PrepOrderTime = itmModel.PrepOrderTime;
                itm.DWItemCategorizationKey = itmModel.DWItemCategorizationKey;
                itm.DWItemSubTypeKey = itmModel.DWItemSubTypeKey;
            }
        }

        private void mapItemSchModeltoItemSch(ItemModel itmModel, ref Item itmToUpdate, bool isItemBeingCopied)
        {
            if (isItemBeingCopied)
            {//If it is being copied, add all the new values to new item
                foreach (var itemSchModel in itmModel.ScheduleDetails)
                {
                    if (itemSchModel.IsSelected || itemSchModel.Cycles.Any(x => x.IsShow.HasValue))
                    {// the day is vaild if it is either selected r there are any day parts
                        var dayPartsInDay = itemSchModel.Cycles.Where(x => x.IsShow.HasValue).Select(dayPartModel => new MenuItemCycleInSchedule
                        {
                            SchCycleId = dayPartModel.SchCycleId, IsShow = dayPartModel.IsShow.Value, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now,
                        }).ToList();
                        itmToUpdate.MenuItemScheduleLinks.Add(new MenuItemScheduleLink
                        {
                            IsSelected = itemSchModel.IsSelected,
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now,
                            NetworkObjectId = NetworkObjectId,
                            Day = (int)itemSchModel.Day,
                            MenuItemCycleInSchedules = dayPartsInDay
                        });
                    }
                }
            }
            else if (itmModel.IsScheduleModified)
            {// otherwise update the existing item
                foreach (var itemSchModel in itmModel.ScheduleDetails)
                {
                    if (itemSchModel.IsSelected || itemSchModel.Cycles.Any(x => x.IsShow.HasValue))
                    {// the day is vaild if it is either selected r there are any day parts                    
                        if (itmToUpdate.MenuItemScheduleLinks.Any(x => x.Day == (int)itemSchModel.Day))
                        {//update the day

                            var itemSchToUpdate = itmToUpdate.MenuItemScheduleLinks.FirstOrDefault(x => x.Day == (int)itemSchModel.Day);
                            itemSchToUpdate.IsSelected = itemSchModel.IsSelected;
                            itemSchToUpdate.UpdatedDate = DateTime.Now;
                            foreach (var dayPartModel in itemSchModel.Cycles)
                            {
                                if (itemSchToUpdate.MenuItemCycleInSchedules.Any(x => x.SchCycleId == (int)dayPartModel.SchCycleId))
                                {
                                    var dayPartToUpdate = itemSchToUpdate.MenuItemCycleInSchedules.FirstOrDefault(x => x.SchCycleId == (int)dayPartModel.SchCycleId);
                                    if (dayPartModel.IsShow.HasValue)
                                    {// update the day part 
                                        dayPartToUpdate.IsShow = dayPartModel.IsShow.Value;
                                        dayPartToUpdate.UpdatedDate = DateTime.Now;
                                    }
                                    else
                                    {//Remove if it is now cleared
                                        _repository.Delete<MenuItemCycleInSchedule>(dayPartToUpdate);
                                    }
                                }
                                else if (dayPartModel.IsShow.HasValue)
                                {// Add the  daypart
                                    itemSchToUpdate.MenuItemCycleInSchedules.Add(new MenuItemCycleInSchedule
                                    {
                                        SchCycleId = dayPartModel.SchCycleId,
                                        IsShow = dayPartModel.IsShow.Value,
                                        CreatedDate = DateTime.Now,
                                        UpdatedDate = DateTime.Now,
                                    });
                                }
                            }
                        }
                        else
                        {// Add the day for this item
                            var dayPartsInDay = itemSchModel.Cycles.Where(x => x.IsShow.HasValue).Select(dayPartModel => new MenuItemCycleInSchedule
                            {
                                SchCycleId = dayPartModel.SchCycleId, IsShow = dayPartModel.IsShow.Value, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now,
                            }).ToList();
                            itmToUpdate.MenuItemScheduleLinks.Add(new MenuItemScheduleLink
                            {//Add day
                                IsSelected = itemSchModel.IsSelected,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now,
                                NetworkObjectId = NetworkObjectId,
                                Day = (int)itemSchModel.Day,
                                MenuItemCycleInSchedules = dayPartsInDay
                            });
                        }
                    }
                    else
                    {
                        //delete the ItemSch if it is already present now unselected
                        if (itmToUpdate.MenuItemScheduleLinks.Any(x => x.Day == (int)itemSchModel.Day))
                        {
                            var itemSchToDelete = itmToUpdate.MenuItemScheduleLinks.FirstOrDefault(x => x.Day == (int)itemSchModel.Day);
                            _repository.Delete<MenuItemCycleInSchedule>(x => x.ItemScheduleLinkId == itemSchToDelete.ItemScheduleLinkId);
                            _repository.Delete<MenuItemScheduleLink>(itemSchToDelete);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Map Collection Model to DB Object Collectiom
        /// </summary>
        /// <param name="colModel"></param>
        /// <param name="col"></param>
        private void mapCollectionModeltoCollection(CollectionModel colModel, ref ItemCollection col)
        {
            col.DisplayName = string.IsNullOrWhiteSpace(colModel.DisplayName) ? string.Empty : colModel.DisplayName.Trim();
            col.InternalName = string.IsNullOrWhiteSpace(colModel.InternalName) ? string.Empty : colModel.InternalName.Trim();
            col.MinQuantity = colModel.MinQuantity;
            col.MaxQuantity = colModel.MaxQuantity;
            col.ShowPrice = colModel.ShowPrice;
            col.IsMandatory = colModel.IsMandatory;
            col.IsPropagate = colModel.IsPropagate;
            col.IsVisibleToGuest = colModel.IsVisibleToGuest;
            col.ReplacesItem = colModel.ReplacesItem;
            col.CollectionTypeId = (CollectionTypeNames)colModel.CollectionTypeId;
            col.MenuId = MenuId;
        }

        /// <summary>
        /// Map database Object category to Model
        /// </summary>
        /// <param name="cml"></param>
        /// <param name="catModel"></param>
        private void mapCategorytoCategoryModel(CategoryMenuLink cml, ref CategoryModel catModel)
        {
            catModel.IrisId = cml.Category.IrisId;
            catModel.CategoryId = cml.Category.CategoryId;
            catModel.CategoryTypeId = (int)cml.Category.CategoryTypeId;
            catModel.DisplayName = cml.Category.DisplayName;
            catModel.InternalName = cml.Category.InternalName;
            catModel.IsFeatured = cml.Category.IsFeatured;
            catModel.ShowPrice = cml.Category.ShowPrice;
            catModel.SortOrder = cml.SortOrder;
            catModel.MenuId = cml.Category.MenuId;
            catModel.DeepLinkId = cml.Category.DeepLinkId;
            catModel.NetworkObjectId = cml.Category.NetworkObjectId;
            catModel.IsEndOfOrder = catModel.CategoryTypeId == 1 ? true : false;
            catModel.StartDate = cml.Category.StartDate.HasValue ? (DateTime?)cml.Category.StartDate.Value.ToLocalTime() : cml.Category.StartDate;
            catModel.EndDate = cml.Category.EndDate.HasValue ? (DateTime?)cml.Category.EndDate.Value.ToLocalTime() : cml.Category.EndDate;
            catModel.OverrideCategoryId = cml.Category.OverrideCategoryId;
        }

        /// <summary>
        /// Map database Object Subcategory to Model
        /// </summary>
        /// <param name="scl"></param>
        /// <param name="catModel"></param>
        private void mapSubCategorytoCategoryModel(SubCategoryLink scl, ref CategoryModel catModel)
        {
            catModel.IrisId = scl.SubCategory.IrisId;
            catModel.CategoryId = scl.SubCategory.CategoryId;
            catModel.CategoryTypeId = (int)scl.SubCategory.CategoryTypeId;
            catModel.DisplayName = scl.SubCategory.DisplayName;
            catModel.InternalName = scl.SubCategory.InternalName;
            catModel.IsFeatured = scl.SubCategory.IsFeatured;
            catModel.ShowPrice = scl.SubCategory.ShowPrice;
            catModel.SortOrder = scl.SortOrder;
            catModel.MenuId = scl.SubCategory.MenuId;
            catModel.DeepLinkId = scl.SubCategory.DeepLinkId;
            catModel.NetworkObjectId = scl.SubCategory.NetworkObjectId;
            catModel.OverrideCategoryId = scl.SubCategory.OverrideCategoryId;
            catModel.IsEndOfOrder = catModel.CategoryTypeId == 1 ? true : false;
            catModel.StartDate = scl.SubCategory.StartDate.HasValue ? (DateTime?)scl.SubCategory.StartDate.Value.ToLocalTime() : scl.SubCategory.StartDate;
            catModel.EndDate = scl.SubCategory.EndDate.HasValue ? (DateTime?)scl.SubCategory.EndDate.Value.ToLocalTime() : scl.SubCategory.EndDate;
        }

        /// <summary>
        /// Map database object Collection to its Model
        /// </summary>
        /// <param name="icl"></param>
        /// <param name="colModel"></param>
        private void mapCollectiontoCollectionModel(ItemCollectionLink icl, ref CollectionModel colModel)
        {
            colModel.IrisId = icl.ItemCollection.IrisId;
            colModel.CollectionId = icl.ItemCollection.CollectionId;
            colModel.CollectionTypeId = (int)icl.ItemCollection.CollectionTypeId;
            colModel.DisplayName = icl.ItemCollection.DisplayName;
            colModel.InternalName = icl.ItemCollection.InternalName;
            colModel.MinQuantity = icl.ItemCollection.MinQuantity;
            colModel.MaxQuantity = icl.ItemCollection.MaxQuantity;
            colModel.ShowPrice = icl.ItemCollection.ShowPrice;
            colModel.IsMandatory = icl.ItemCollection.IsMandatory;
            colModel.IsPropagate = icl.ItemCollection.IsPropagate;
            colModel.IsVisibleToGuest = icl.ItemCollection.IsVisibleToGuest;
            colModel.SortOrder = icl.SortOrder;
            colModel.ReplacesItem = icl.ItemCollection.ReplacesItem;
            colModel.MenuId = icl.ItemCollection.MenuId;
            colModel.NetworkObjectId = icl.ItemCollection.NetworkObjectId;
        }

        /// <summary>
        /// Look up the items as get Orginial Collection Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="colId"></param>
        /// <param name="itemId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentColId(int colId, int itemId, out int pId)
        {
            //int? prntId = _repository.GetQuery<ItemCollectionLink>(cm => cm.CollectionId == colId && parentNetworkNodeIds.Contains(cm.NetworkObjectId)).Include("NetworkObject").OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault().ParentCollectionId;
            var itemCollectionLink = _itemCollectionLinks.FirstOrDefault(cm => cm.CollectionId == colId && cm.ItemId == itemId);
            if (itemCollectionLink != null)
            {
                int? prntId = itemCollectionLink.ParentCollectionId;
                if (!prntId.HasValue)
                {
                    pId = colId;
                    return true;
                }
                else
                {
                    if (colId != prntId.Value)
                    {
                        pId = prntId.Value;
                        return false;
                    }
                    else
                    {
                        pId = prntId.Value;
                        return true;
                    }
                }
            }
            pId = colId;
            return true;
        }

        /// <summary>
        /// Look up the items as get Orginial Category Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="menuId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentCatId(int catId, int menuId, out int pId)
        {
            var categoryMenuLink = _categoryMenuLinks.FirstOrDefault(cm => cm.CategoryId == catId && cm.MenuId == menuId);
            if (categoryMenuLink != null)
            {
                int? prntId = categoryMenuLink.ParentCategoryId;

                if (!prntId.HasValue)
                {
                    pId = catId;
                    return true;
                }
                else
                {
                    pId = prntId.Value;
                    return false;
                }
            }
            pId = catId;
            return true;
        }

        /// <summary>
        ///  Look up to get Orginial SubCategory Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="subCatId"></param>
        /// <param name="catId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentSubCatId(int subCatId, int catId, out int pId)
        {
            var subCategoryLink = _subCategoryLinks.FirstOrDefault(cm => cm.SubCategoryId == subCatId && cm.CategoryId == catId);
            if (subCategoryLink != null)
            {
                int? prntId = subCategoryLink.OverrideParentSubCategoryId;

                if (!prntId.HasValue)
                {
                    pId = subCatId;
                    return true;
                }
                else
                {
                    pId = prntId.Value;
                    return false;
                }
            } 
            pId = subCatId;
            return true;
        }

        /// <summary>
        ///  Look up to get Orginial SubCategory Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="parPrependItemId"></param>
        /// <param name="itmId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentPrepItemId(int parPrependItemId, int itmId, out int pId)
        {
            var prependItemLink = _prependItemLinks.FirstOrDefault(cm => cm.PrependItemId == parPrependItemId && cm.ItemId == itmId);
            if (prependItemLink != null)
            {
                int? prntId = prependItemLink.OverrideParentPrependItemId;

                if (!prntId.HasValue)
                {
                    pId = parPrependItemId;
                    return true;
                }
                else
                {
                    pId = prntId.Value;
                    return false;
                }
            } 
            pId = parPrependItemId;
            return true;
        }

        /// <summary>
        /// Look up the items as get Orginial Item Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="catId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentCatItemId(int itemId, int catId, out int pId)
        {
            var firstOrDefault = _categoryObjects.FirstOrDefault(cm => cm.ItemId == itemId && cm.CategoryId == catId);
            if (firstOrDefault != null)
            {
                int? prntId = firstOrDefault.ParentItemId;

                if (!prntId.HasValue)
                {
                    pId = itemId;
                    return true;
                }
                else
                {
                    pId = prntId.Value;
                    return false;
                }
            } 
            pId = itemId;
            return true;
        }

        /// <summary>
        /// Look up the items as get Orginial Item Id before Overriding (a recursive call)
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="colId"></param>
        /// <param name="pId"></param>
        /// <returns></returns>
        private bool getMasterParentColItemId(int itemId, int colId, out int pId)
        {
            var itemCollectionObject = _itemCollectionObjects.FirstOrDefault(cm => cm.ItemId == itemId && cm.CollectionId == colId);
            if (itemCollectionObject != null)
            {
                int? prntId = itemCollectionObject.ParentItemId;

                if (!prntId.HasValue)
                {
                    pId = itemId;
                    return true;
                }
                else
                {
                    pId = prntId.Value;
                    return false;
                }
            }
            pId = itemId;
            return true;
        }

        /// <summary>
        /// Check Eligiblity whether an Item can be added to collection to overcome StackOverflow recursion exception
        /// </summary>
        /// <param name="itmId"></param>
        /// <param name="parentItems"></param>
        /// <returns></returns>
        private bool getEligibilityToAddItem(int itmId, List<int> parentItems)
        {
            var itemIds = new List<int>();
            var childItems = new List<int>();
            var isEligible = true;
            var loop = true;

            //recursive call to get all child Items
            itemIds.Add(itmId);
            do
            {
                loop = getchildItems(itemIds, childItems);

            } while (loop);
            //Add currrent item to childs list
            childItems.Add(itmId);

            //If any of the child is present in parent then it is not eligible
            foreach (var child in childItems)
            {
                if (parentItems.Contains(child))
                {
                    isEligible = false;
                    break;
                }
            }
            return isEligible;
        }

        /// <summary>
        /// Check Eligiblity whether an PrependItem can be added to Item to overcome StackOverflow recursion exception
        /// </summary>
        /// <param name="prependItmId"></param>
        /// <param name="parentItemIdsToRestrict"></param>
        /// <returns></returns>
        private bool getEligibilityToAddPrependItem(int prependItmId, List<int> parentItemIdsToRestrict)
        {
            var prependItemIds = new List<int>();
            var childItems = new List<int>();
            var isEligible = true;
            var loop = true;

            //recursive call to get all child Items
            prependItemIds.Add(prependItmId);
            do
            {
                loop = getchildPrependItems(prependItemIds, childItems);

            } while (loop);
            //Add currrent item to childs list
            childItems.Add(prependItmId);

            //If any of the child is present in parent then it is not eligible
            foreach (var child in childItems)
            {
                if (parentItemIdsToRestrict.Contains(child))
                {
                    isEligible = false;
                    break;
                }
            }
            return isEligible;
        }

        /// <summary>
        /// Check Eligiblity whether an Collection can be added to Item to overcome StackOverflow recursion exception
        /// </summary>
        /// <param name="colId"></param>
        /// <param name="prntCollectionIdsofItem"></param>
        /// <returns></returns>
        private bool getEligibilityToAddCollection(int colId, List<int> prntCollectionIdsofItem)
        {
            var collectionIds = new List<int>();
            var childCols = new List<int>();
            var isEligible = true;
            var loop = true;

            //recursive call to get all child collections
            collectionIds.Add(colId);
            do
            {
                loop = getchildCollections(collectionIds, childCols);

            } while (loop);

            //Add current id to childs list
            childCols.Add(colId);
            //If any of the child is present in parent then it is not eligible
            foreach (var child in childCols)
            {
                if (prntCollectionIdsofItem.Contains(child))
                {
                    isEligible = false;
                    break;
                }
            }
            return isEligible;
        }

        /// <summary>
        /// Check Eligiblity whether an SubCategory can be added to Category to overcome StackOverflow recursion exception
        /// </summary>
        /// <param name="subCatId"></param>
        /// <param name="prntSubCategoriesofCat"></param>
        /// <returns></returns>
        private bool getEligibilityToAddSubCategory(int subCatId, List<int> prntSubCategoriesofCat)
        {
            var subCatIds = new List<int>();
            var childsubCats = new List<int>();
            var isEligible = true;
            var loop = true;

            // recursive call to get all child SubCats
            subCatIds.Add(subCatId);
            do
            {
                loop = getchildSubCategories(subCatIds, childsubCats);

            } while (loop);
            childsubCats.Add(subCatId);

            //If any of the child is present in parent then it is not eligible
            foreach (var child in childsubCats)
            {
                if (prntSubCategoriesofCat.Contains(child))
                {
                    isEligible = false;
                    break;
                }
            }
            return isEligible;
        }

        /// <summary>
        /// get all parent collections for a Item
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="parentCollections"></param>
        /// <returns></returns>
        private bool getparentCollections(List<int> itemIds, List<int> parentCollections)
        {
            var repeatLoop = false;
            //get parent collections of the given list of Items in the current Network Tree
            var links = _itemCollectionObjects.Where(x => itemIds.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            var collectionIdsFound = links.Select(itmCol => itmCol.CollectionId).ToList();
            if (collectionIdsFound.Any())
            {
                // add the list of parent Ids in every loop
                parentCollections.AddRange(collectionIdsFound);
                var newCollectionItems = _itemCollectionLinks.Where(x => collectionIdsFound.Contains(x.CollectionId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.CollectionId);

                var newItemIds = _itemCollectionLinks.Where(x => newCollectionItems.Contains(x.CollectionId) && !itemIds.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId).ToList();

                // clear the list all add new values for next search
                itemIds.Clear();
                itemIds.AddRange(newItemIds);

                if (itemIds.Any())
                {
                    repeatLoop = true;
                }
            }
            return repeatLoop;
        }

        /// <summary>
        /// get all child Items for an Item that is supposed be added
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="childItems"></param>
        /// <returns></returns>
        private bool getchildItems(List<int> itemIds, List<int> childItems)
        {
            var returnVal = false;

            //get child collections of the given list of Items in the current Network Tree
            var collectionIds = _itemCollectionLinks.Where(x => itemIds.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.CollectionId).ToList();

            var itmColLinks = _itemCollectionObjects.Where(x => collectionIds.Contains(x.CollectionId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            var allItems = (from itmCol in itmColLinks where !itemIds.Contains(itmCol.ItemId) select itmCol.ItemId).ToList();

            // add the list of child Ids in every loop
            childItems.AddRange(allItems);
            var newItemIds = _itemCollectionLinks.Where(x => allItems.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId).ToList();

            // clear the list all add new values for next search
            itemIds.Clear();
            itemIds = newItemIds;

            if (itemIds.Any())
            {
                returnVal = true;
            }
            return returnVal;
        }

        /// <summary>
        /// get all child prepend Items for an Item that is supposed be added
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="childPrependItems"></param>
        /// <returns></returns>
        private bool getchildPrependItems(List<int> itemIds, List<int> childPrependItems)
        {
            var repeatLoop = false;
            var prependItemIdsFound = new List<int>();

            //get prepend Items of the given list of Items in the current Network Tree
            var prependItemLinks = _prependItemLinks.Where(x => itemIds.Contains(x.ItemId) && parentMenuNetworkNodeIds.Contains(x.MenuNetworkObjectLinkId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList();

            foreach (var prependItem in prependItemLinks)
            {
                if (!prependItemIdsFound.Contains(prependItem.PrependItemId) && !itemIds.Contains(prependItem.PrependItemId))
                {
                    prependItemIdsFound.Add(prependItem.PrependItemId);
                }

                var ovrPrependItemId = prependItem.OverrideParentPrependItemId.HasValue ? prependItem.OverrideParentPrependItemId.Value : 0;
                if (ovrPrependItemId != 0 && !prependItemIdsFound.Contains(ovrPrependItemId) && !itemIds.Contains(ovrPrependItemId))
                {
                    prependItemIdsFound.Add(ovrPrependItemId);
                }
            }

            if (prependItemIdsFound.Any())
            {
                // add the list of child Ids in every loop
                childPrependItems.AddRange(prependItemIdsFound);
                // clear the list all add new values for next search
                itemIds.Clear();
                itemIds = prependItemIdsFound;
                repeatLoop = true;
            }
            return repeatLoop;

        }

        /// <summary>
        /// get all child collections for a collection that is supposed be added
        /// </summary>
        /// <param name="collectionIds"></param>
        /// <param name="childCollectionIds"></param>
        /// <returns></returns>
        private bool getchildCollections(List<int> collectionIds, List<int> childCollectionIds)
        {
            var repeatLoop = false;

            //get child Items of the given list of Collections in the current Network Tree
            var itemIds = _itemCollectionObjects.Where(x => collectionIds.Contains(x.CollectionId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId).ToList();

            //get child collections of the above found list of Items in the current Network Tree
            var links = _itemCollectionLinks.Where(x => itemIds.Contains(x.ItemId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            var collectionIdsFound = (from itmCol in links where !collectionIds.Contains(itmCol.CollectionId) select itmCol.CollectionId).ToList();
            if (collectionIdsFound.Any())
            {
                // add the list of child Ids in every loop
                childCollectionIds.AddRange(collectionIdsFound);
                var newcollectionIdsForNextSearch = _itemCollectionObjects.Where(x => collectionIdsFound.Contains(x.CollectionId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.CollectionId).ToList();

                // clear the list all add new values for next search
                collectionIds.Clear();
                collectionIds = newcollectionIdsForNextSearch;

                if (collectionIds.Any())
                {
                    repeatLoop = true;
                }
            }
            return repeatLoop;
        }

        /// <summary>
        /// get all parent collections for a Cat
        /// </summary>
        /// <param name="catIds"></param>
        /// <param name="parentCategories"></param>
        /// <returns></returns>
        private bool getparentCategories(List<int> catIds, List<int> parentCategories)
        {
            var repeatLoop = false;
            var currentPrntCategoriesFound = new List<int>();
            List<SubCategoryLink> links;
            links = _subCategoryLinks.Any() ? _subCategoryLinks.Where(x => catIds.Contains(x.SubCategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList() : _repository.GetQuery<SubCategoryLink>(x => catIds.Contains(x.SubCategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList();
            foreach (var subCat in links)
            {
                if (!parentCategories.Contains(subCat.CategoryId) && !currentPrntCategoriesFound.Contains(subCat.CategoryId))
                {
                    currentPrntCategoriesFound.Add(subCat.CategoryId);
                }
                if (subCat.OverrideParentSubCategoryId.HasValue && !parentCategories.Contains(subCat.OverrideParentSubCategoryId.Value) && !currentPrntCategoriesFound.Contains(subCat.OverrideParentSubCategoryId.Value))
                {
                    currentPrntCategoriesFound.Add(subCat.OverrideParentSubCategoryId.Value);
                }
            }
            if (currentPrntCategoriesFound.Any())
            {
                // add the list of child Ids in every loop
                parentCategories.AddRange(currentPrntCategoriesFound);
                // clear the list all add new values for next search
                catIds.Clear();
                catIds.AddRange(currentPrntCategoriesFound);
                repeatLoop = true;
            }
            return repeatLoop;
        }

        /// <summary>
        /// get all child cats for a subCat that is supposed be added
        /// </summary>
        /// <param name="subCatIds"></param>
        /// <param name="childSubCategories"></param>
        /// <returns></returns>
        private bool getchildSubCategories(List<int> subCatIds, List<int> childSubCategories)
        {
            var repeatLoop = false;
            var currentChildSubCats = new List<int>();
            //get all subCats for given Cat
            var childSubCats = _subCategoryLinks.Where(x => subCatIds.Contains(x.CategoryId) && parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList();

            foreach (var childSubCat in childSubCats)
            {
                var subCatId = childSubCat.SubCategoryId;
                //do not add if the category was present in searched cats or added to subCats already
                if (!currentChildSubCats.Contains(subCatId) && !childSubCategories.Contains(subCatId))
                {
                    currentChildSubCats.Add(subCatId);
                }

                var ovrSubCatId = childSubCat.OverrideParentSubCategoryId.HasValue ? childSubCat.OverrideParentSubCategoryId.Value : 0;
                if (ovrSubCatId != 0 && !currentChildSubCats.Contains(ovrSubCatId) && !childSubCategories.Contains(ovrSubCatId))
                {
                    currentChildSubCats.Add(ovrSubCatId);
                }
            }
            if (currentChildSubCats.Any())
            {
                // add the list of child Ids in every loop
                childSubCategories.AddRange(currentChildSubCats);
                // clear the list all add new values for next search
                subCatIds.Clear();
                subCatIds.AddRange(currentChildSubCats);
                repeatLoop = true;
            }
            return repeatLoop;
        }

        /// <summary>
        /// Find sibling categories of given Category to restrict from picker popup
        /// </summary>
        /// <param name="catId"></param>
        /// <param name="siblingCategoryIds"></param>
        /// <param name="parentType"></param>
        private void getSiblingCats(int catId, List<int> siblingCategoryIds, MenuType parentType)
        {
            if (parentType != MenuType.Menu)
            {
                var links = _repository.GetQuery<SubCategoryLink>(x => x.CategoryId == catId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.SubCategoryId == deletedbyParent.CategoryId).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.SubCategoryId);
                        }
                    }
                }

                siblingCategoryIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.CategoryId)).Select(x => x.SubCategoryId));
                siblingCategoryIds.AddRange(links.Where(x => x.OverrideParentSubCategoryId.HasValue && !donotExcludeList.Contains(x.CategoryId)).Select(x => x.OverrideParentSubCategoryId.Value));
            }
            else
            {
                var links = _repository.GetQuery<CategoryMenuLink>(x => x.MenuId == MenuId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.CategoryId == deletedbyParent.CategoryId).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.CategoryId);
                        }
                    }
                }

                siblingCategoryIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.CategoryId)).Select(x => x.CategoryId));
                siblingCategoryIds.AddRange(links.Where(x => x.ParentCategoryId.HasValue && !donotExcludeList.Contains(x.ParentCategoryId.Value)).Select(x => x.ParentCategoryId.Value));
            }
        }

        /// <summary>
        /// Find sibling collections to restrict from picker popup
        /// </summary>
        /// <param name="id"></param>
        /// <param name="siblingCollectionIds"></param>
        /// <param name="parentType"></param>
        public void getSiblingCols(int id, List<int> siblingCollectionIds, MenuType parentType)
        {
            if (parentType == MenuType.Item)
            {

                var links = _repository.GetQuery<ItemCollectionLink>(x => x.ItemId == id && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.CollectionId == deletedbyParent.CollectionId).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.CollectionId);
                        }
                    }
                }

                siblingCollectionIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.CollectionId)).Select(x => x.CollectionId));
                siblingCollectionIds.AddRange(links.Where(x => x.ParentCollectionId.HasValue && !donotExcludeList.Contains(x.ParentCollectionId.Value)).Select(x => x.ParentCollectionId.Value));
            }
        }

        /// <summary>
        /// Gets the MnuNetworkLink and adds the link if it is not present. NOTE: this is not saved to database until the caller function calls saveChanges.
        /// </summary>
        /// <param name="mnuId"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        private MenuNetworkObjectLink getMnuNwLinkAddIfNotexist(int mnuId, int netId)
        {
            var mnuNetworkObjectLink = _repository.GetQuery<MenuNetworkObjectLink>(x => x.MenuId == mnuId && x.NetworkObjectId == netId).FirstOrDefault();
            if (mnuNetworkObjectLink == null)
            {
                mnuNetworkObjectLink = new MenuNetworkObjectLink
                {
                    MenuId = mnuId,
                    NetworkObjectId = netId,
                    IsMenuOverriden = true, // This is true because, if Menu is created at the current it will have mnuNWLink. as the link is not present this menu is not created at current Network
                    IsPOSMapped = false, //this is false because, if link is not present then there is no POSData
                    LastUpdatedDate = DateTime.UtcNow
                };
                _repository.Add<MenuNetworkObjectLink>(mnuNetworkObjectLink);
            }
            return mnuNetworkObjectLink;
        }

        /// <summary>
        /// Generate Unique Id for Tree
        /// </summary>
        /// <param name="typ"></param>
        /// <param name="prntId"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetMenuTreeId(MenuType typ, int prntId, int id)
        {
            string treeId;
            switch (typ)
            {
                case MenuType.Menu:
                    treeId = "Menu_" + prntId + "_" + id;
                    break;
                case MenuType.Category:
                case MenuType.SubCategory:
                    treeId = "Cat_" + prntId + "_" + id;
                    break;
                case MenuType.Item:
                    treeId = "Itm_" + prntId + "_" + id;
                    break;
                case MenuType.ItemCollection:
                    treeId = "ItmCol_" + prntId + "_" + id;
                    break;
                case MenuType.ItemCollectionItem:
                    treeId = "ColItm_" + prntId + "_" + id;
                    break;
                default:
                    treeId = "";
                    break;
            }
            return treeId;
        }

        /// <summary>
        /// Get Image Path for Tree Elements
        /// </summary>
        /// <param name="menuTypes"></param>
        /// <returns></returns>
        private string getImagePath(MenuType menuTypes, string subType = null)
        {
            string retVal = string.Empty;
            switch (menuTypes)
            {
                case MenuType.Menu:
                    retVal = Constants.MenuTypeImages.Menu;
                    break;
                case MenuType.SubCategory:
                case MenuType.Category:
                    retVal = Constants.MenuTypeImages.Category;
                    if (string.IsNullOrWhiteSpace(subType) == false)
                    {
                        switch (subType.ToLower())
                        {
                            case "endoforder":
                                retVal = Constants.MenuTypeImages.EndOfOrderCategory;
                                break;
                            case "reorder":
                                retVal = Constants.MenuTypeImages.ReOrderCategory;
                                break;
                        }
                    }
                    break;

                case MenuType.Item:
                    retVal = Constants.MenuTypeImages.Item;
                    break;

                case MenuType.ItemCollection:
                    retVal = Constants.MenuTypeImages.ItemCollection;

                    if (string.IsNullOrWhiteSpace(subType) == false)
                    {
                        switch (subType.ToLower())
                        {
                            case "combo":
                                retVal = Constants.MenuTypeImages.ComboCollection;
                                break;
                            case "endoforder":
                                retVal = Constants.MenuTypeImages.EndOfOrderCollection;
                                break;
                            case "modification":
                                retVal = Constants.MenuTypeImages.ModificationCollection;
                                break;
                            case "substitution":
                                retVal = Constants.MenuTypeImages.SubstitutionCollection;
                                break;
                            case "upsell":
                                retVal = Constants.MenuTypeImages.UpSellCollection;
                                break;
                            case "crosssell":
                                retVal = Constants.MenuTypeImages.ItemCollection;
                                break;
                        }
                    }
                    break;

                case MenuType.ItemCollectionItem:
                    retVal = Constants.MenuTypeImages.ItemCollectionItem;
                    break;
            }
            return retVal;
        }

        #endregion
    }

    public interface IMenuService
    {

        RuleService _ruleService { get; set; }

        string LastActionResult { get; }
        int Count { get; set; }
        int MenuId { get; set; }
        int NetworkObjectId { get; set; }


        void Initialize(IValidationDictionary validationDictionary);
        Menu GetMenu(int menuId);
        //void GetMenuTree(List<MenuService.MenuTreeItem> trecat);
        string GetMenuNames(int netId);
        void GetHierarchicalMenuTree(string id, List<MenuService.MenuTreeItem> tree);
        NetworkObject GetNetworkObject(int netId, out int brandId, out string parentsBreadCrum);
        List<MenuDataModel> GetMenuList(int netId);

        List<string> AddCategoriestoMenu(int[] catIds, int menuId);
        Dictionary<int, string> AddItemstoCategory(string selectedItemDetails, int catId);
        List<string> AddCollectionstoItem(int[] colIds, int itemId);
        Dictionary<int, string> AddItemstoCollection(string selectedItemDetails, int colId);
        List<string> AddSubCategoriestoCategory(int[] subCatIds, int catId);
        Dictionary<int, string> AddPrependItemstoItem(string selectedItemDetails, int itmId);

        string DeleteCategoryObject(int itemId, int catId);
        string DeleteItemCollection(int colId, int itemId);
        string DeleteCategoryMenuLink(int catId, int menuId);
        string DeleteItemCollectionItem(int itemId, int colId);
        string DeleteSubCategoryLink(int subCatId, int prntCatId);
        string DeletePrependItem(int prependItemId, int itmId);

        MenuDataModel CreateMenu(MenuDataModel model);
        MenuDataModel UpdateMenu(MenuDataModel model);
        bool DeleteMenu(MenuDataModel model, int netId);
        bool RevertMenu();
        MenuDataModel CopyMenu(MenuDataModel menuToCopy);

        //int SaveCategory(CategoryModel catModel, int mid);
        int SaveItem(ItemModel itmModel, int prntId, MenuType itmType);
        int SaveCollection(CollectionModel colModel, int itemId);
        //int SaveItemCollectionItem(ItemModel itmModel, int colId);
        int SaveCategory(CategoryModel catModel, int catId, MenuType catType);

        void ChangeCatMenuLinkPositions(List<MenuService.MenuGridItem> catModels, int menuId);
        void ChangeCatObjectPositions(List<MenuService.MenuGridItem> itmModels, int catId);
        void ChangeItemCollectionLinkPositions(List<MenuService.MenuGridItem> colModels, int itemId);
        void ChangeItemCollectionObjectPositions(List<MenuService.MenuGridItem> itmModels, int colId);
        void ChangeSubCatLinkPositions(List<MenuService.MenuGridItem> cats, int prntCatId);
        void ChangePrependItemLinkPositions(List<MenuService.MenuGridItem> itmModels, int prntItemId);

        string GetMenuTreeId(MenuType typ, int prntId, int id);

        ItemModel GetItemWithMaster(int itemId, int collectionId = 0, MenuType ItemType = MenuType.Item);
        CategoryModel GetCategory(int CategoryId, int menuId, int netId);
        CollectionModel GetItemCollection(int CollectionId, int ItemId);
        CategoryModel GetSubCategory(int subCategoryId, int catId, int netId);

        List<MenuService.MenuGridItem> GetItemsByCategory(int orgCategoryId, int ovrridenId);
        List<MenuService.MenuGridItem> GetSubCategories(int orgParentCatId, int ovrridenId);
        List<MenuService.MenuGridItem> GetCategoriesByMenu(int MenuId, int ovrridenId);
        List<MenuService.MenuGridItem> GetCollectionbyItem(int orgItemId, int ovrridenId);
        List<MenuService.MenuGridItem> GetItemsByCollection(int orgCollectionId, int ovrridenId);
        List<MenuService.MenuGridItem> GetPrependItemsByItem(int orgItemId, int ovrridenId);

        List<EntitySchSummary> GetItemSchDetailSummary(int itemId);
        List<EntitySchSummary> GetCatSchDetailSummary(int catId);

        List<CollectionModel> GetMasterCollectionList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest);
        List<CategoryModel> GetMasterCategoryList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest);
        List<CategoryModel> GetAllCategoriesList();

        bool IsMenuNameNotUnique(string menuName, int menuId, int networkObjectId, bool isActionCopy);

        List<MenuDropdown> GetMenuListInSelectedNetworkTrees(List<int> mulitpleNetworkIds);

        List<Menu> GetMenusInNetworks(List<int> networkIds);

        MenuDataModel SaveMenu(MenuDataModel model);
    }
}
