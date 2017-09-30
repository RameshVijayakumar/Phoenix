using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using Phoenix.Web.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Omu.ValueInjecter;
using SnowMaker;

namespace Phoenix.Web.Controllers
{
    public class LoadTestController : Controller
    {
        private IRepository _repository;
        private DbContext _context;
        private IMenuService _menuService;
        private RuleService _ruleService;

        private List<int> _categoriesOverrideninMenu;
        private List<int> _collectionsOverrideninMenu;
        private List<int> _itemsOverrideninMenu;

        public LoadTestController(IMenuService menuService)
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _menuService = menuService;
            _ruleService = new RuleService(RuleService.CallType.Web);
            _categoriesOverrideninMenu = new List<int>();
            _collectionsOverrideninMenu = new List<int>();
            _itemsOverrideninMenu = new List<int>();
        }
        //
        // GET: /LoadTest/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OverrideMenuinNetwork(int menuId, int networkId)
        {
            var retVal = false;
            try
            {
                log(string.Format("Started Overriding Menu : {1} in Network : {2} at {0}", DateTime.Now, menuId.ToString(), networkId.ToString()));
                _menuService.MenuId = menuId;
                _menuService.NetworkObjectId = networkId;
                _ruleService.NetworkObjectId = networkId;
                var parentNetworkNodes = _ruleService.GetNetworkParents(networkId);
                _categoriesOverrideninMenu = new List<int>();
                _itemsOverrideninMenu = new List<int>();
                _collectionsOverrideninMenu = new List<int>();

                List<string> children = new List<string>();
                children.Add("Category");
                children.Add("Category.CategoryObjects");
                children.Add("Category.SubCategoryLinks");

                var catsinMenu = _ruleService.GetCategoriesList(menuId);
                var _totalCategories = catsinMenu.Count();
                var _currentCat = 1;

                foreach (var cat in catsinMenu)
                {
                    retVal = OverrideCategory(cat, menuId, menuId, networkId, parentNetworkNodes, MenuType.Category);
                    if (retVal)
                    {
                        log(string.Format("Completed Overriding '{3}' {1} of {2} Categories at {0}", DateTime.Now, _currentCat.ToString(), _totalCategories.ToString(), cat.DisplayName));
                    }
                    //else
                    //{
                    //    break;
                    //}
                    _currentCat++;
                }
                if (retVal)
                {
                    log(string.Format("Completed Overriding Menu : {1} in Network : {2} at {0}", DateTime.Now, menuId.ToString(), networkId.ToString()));
                    logNetworkComplete(string.Format("Completed Overriding Menu : {1} in Network : {2} at {0}", DateTime.Now, menuId.ToString(), networkId.ToString()));
                }
                else
                {
                    log(string.Format("Already Menu : {1} in Network : {2} at {0}", DateTime.Now, menuId.ToString(), networkId.ToString()));
                }

                // create new context to prevent slowness 
                _context.Dispose();
                _context = new ProductMasterContext();
                return new JsonResult { Data = new { status = true, result = retVal }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                log(string.Format("Error:{0} {1}\nMenuId:{2}, NetworkId:{3}", ex.Message, ex.StackTrace, menuId, networkId));
                return new JsonResult { Data = new { status = false, errMsg = ex.Message, result = retVal }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        public ActionResult GetAllNetworks(int? id, string excludedNetworkIds, string includedNetworkIds)
        {
            try
            {
                log(string.Format("Started {0}", DateTime.Now));
                var js = new JavaScriptSerializer();
                var excludeids = string.IsNullOrWhiteSpace(excludedNetworkIds) ? null : js.Deserialize<int[]>(excludedNetworkIds);
                var includeids = string.IsNullOrWhiteSpace(includedNetworkIds) ? null : js.Deserialize<int[]>(includedNetworkIds);
                var query = _repository.GetQuery<NetworkObject>();
                if (id.HasValue)
                {
                    query = query.Where(x => x.NetworkObjectId == id);
                }
                if (excludeids != null && excludeids.Any())
                {
                    query = query.Where(x => !excludeids.Contains(x.NetworkObjectId));
                }
                if (includeids != null && includeids.Any())
                {
                    query = query.Where(x => includeids.Contains(x.NetworkObjectId));
                }
                var allNetworks = (from n in query
                                   select new { n.NetworkObjectId, n.NetworkObjectTypeId, n.Name }).OrderByDescending(x => x.NetworkObjectTypeId).ToList();

                return new JsonResult { Data = new { status = true, networks = allNetworks.ToList() }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                log(string.Format("Error:{0} {1}", ex.Message, ex.StackTrace));
                return new JsonResult { Data = new { status = false, errMsg = ex.Message, networks = string.Empty }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        private bool OverrideCategory(Category cat, int prntId, int menuId, int networkId, List<int> parentNetworkNodes, MenuType catType)
        {
            var retVal = false;
            //Override Category
            //CategoryModel catModel = _menuService.GetCategory(cat.CategoryId, prntId, networkId);
            CategoryModel catModel = new CategoryModel();
            MapCategorytoCategoryModel(cat, ref catModel);
            var ovrText = " LoadTest" + networkId.ToString();
            if (!catModel.DisplayName.Contains(ovrText) && !_categoriesOverrideninMenu.Contains(catModel.CategoryId))
            {
                catModel.DisplayName += ovrText;
                _menuService.SaveCategory(catModel, prntId, catType);
                _categoriesOverrideninMenu.Add(catModel.CategoryId);

                var entityId = cat.CategoryId;
                //Get Items n Sub Categories
                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == entityId && co.MenuId == prntId).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    entityId = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var sovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == entityId).FirstOrDefault();
                    if (sovrride != null && sovrride.OverrideParentSubCategoryId != null)
                        entityId = sovrride.OverrideParentSubCategoryId.Value;
                }

                //get the data from ruleEngine and convert to MenuTree Object
                var itemsinCat = _ruleService.GetItemList(entityId, prntId);
                foreach (var item in itemsinCat)
                {
                    OverrideItem(item, entityId, menuId, networkId, parentNetworkNodes, MenuType.Item);
                }

                //get the data from ruleEngine and convert to MenuTree Object
                var subCatsinCat = _ruleService.GetSubCategoryList(entityId, prntId);
                foreach (var subCat in subCatsinCat)
                {
                    OverrideCategory(subCat, entityId, menuId, networkId, parentNetworkNodes, MenuType.SubCategory);
                }

                retVal = true;
            }
            return retVal;
        }

        private bool OverrideItem(Item item, int prntId, int menuId, int networkId, List<int> parentNetworkNodes, MenuType itmType)
        {
            var retVal = false;
            //Override Item
            ItemModel mdl = new ItemModel(); //_menuService.GetItemWithMaster(item.ItemId, prntId, itmType);
            MapItemtoItemModel(item, ref mdl);
            var ovrText = " LoadTest" + networkId.ToString();
            if (!mdl.DisplayName.Contains(ovrText) && !_itemsOverrideninMenu.Contains(mdl.ItemId))
            {
                mdl.DisplayName += ovrText;
                _menuService.SaveItem(mdl, prntId, itmType);
                _itemsOverrideninMenu.Add(mdl.ItemId);

                var entityId = item.ItemId;
                //make sure this was not an override for another item
                var itemovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == entityId).FirstOrDefault();
                if (itemovrride != null && itemovrride.ParentItemId != null)
                    entityId = itemovrride.ParentItemId.Value;

                //make sure this was not an override for another Item
                var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == entityId).FirstOrDefault();
                if (collObjovrride != null && collObjovrride.ParentItemId != null)
                    entityId = collObjovrride.ParentItemId.Value;

                //get the data from ruleEngine and convert to MenuTree Object
                var colsinItem = _ruleService.GetCollectionList(entityId, menuId);
                foreach (var col in colsinItem)
                {
                    OverrideCollection(col, item.ItemId, menuId, networkId, parentNetworkNodes, MenuType.ItemCollection);
                }
                retVal = true;
            }
            return retVal;
        }

        private bool OverrideCollection(ItemCollection collection, int prntId, int menuId, int networkId, List<int> parentNetworkNodes, MenuType colType)
        {
            var retVal = false;
            //Override Collection
            CollectionModel mdl = new CollectionModel();//_menuService.GetItemCollection(collection.CollectionId, prntId);
            MapCollectiontoCollectionModel(collection, ref mdl);
            var ovrText = " LoadTest" + networkId.ToString();
            if (!mdl.DisplayName.Contains(ovrText) && !_collectionsOverrideninMenu.Contains(mdl.CollectionId))
            {
                mdl.DisplayName += ovrText;
                _menuService.SaveCollection(mdl, prntId);
                _collectionsOverrideninMenu.Add(mdl.CollectionId);

                var entityId = collection.CollectionId;
                //make sure this was not an override for another collection
                var colovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == entityId).FirstOrDefault();
                if (colovrride != null && colovrride.ParentCollectionId != null)
                    entityId = colovrride.ParentCollectionId.Value;

                //get the data from ruleEngine and convert to MenuTree Object
                var itemsinCollection = _ruleService.GetCollectionItemList(entityId, menuId);
                foreach (var item in itemsinCollection)
                {
                    OverrideItem(item, collection.CollectionId, menuId, networkId, parentNetworkNodes, MenuType.ItemCollectionItem);
                }

                retVal = true;
            }
            return retVal;
        }

        private void MapCategorytoCategoryModel(Category cat, ref CategoryModel catModel)
        {
            catModel.CategoryId = cat.CategoryId;
            catModel.CategoryTypeId = (int)cat.CategoryTypeId;
            catModel.DisplayName = cat.DisplayName;
            catModel.InternalName = cat.InternalName;
            catModel.IsFeatured = cat.IsFeatured;
            catModel.ShowPrice = cat.ShowPrice;
            catModel.SortOrder = cat.SortOrder;
            catModel.MenuId = cat.MenuId;
            catModel.DeepLinkId = cat.DeepLinkId;
            catModel.IsEndOfOrder = catModel.CategoryTypeId == 1 ? true : false;
            catModel.StartDate = cat.StartDate.HasValue ? (DateTime?)cat.StartDate.Value.ToLocalTime() : cat.StartDate;
            catModel.EndDate = cat.EndDate.HasValue ? (DateTime?)cat.EndDate.Value.ToLocalTime() : cat.EndDate;
        }

        private void MapCollectiontoCollectionModel(ItemCollection col, ref CollectionModel colModel)
        {
            colModel.CollectionId = col.CollectionId;
            colModel.CollectionTypeId = (int)col.CollectionTypeId;
            colModel.DisplayName = col.DisplayName;
            colModel.InternalName = col.InternalName;
            colModel.MinQuantity = col.MinQuantity;
            colModel.MaxQuantity = col.MaxQuantity;
            colModel.ShowPrice = col.ShowPrice;
            colModel.IsMandatory = col.IsMandatory;
            colModel.IsPropagate = col.IsPropagate;
            colModel.SortOrder = col.SortOrder;
            colModel.MenuId = col.MenuId;
        }

        private void MapItemtoItemModel(Item itm, ref ItemModel itmModel)
        {
            itmModel.InjectFrom(itm);
            //itmModel.ItemId = itm.ItemId;
            //itmModel.BasePLU = itm.BasePLU;
            //itmModel.SelectedPLU = itm.PLUItemLinks.Any(p => p.IsActive) ? itm.PLUItemLinks.Where(p => p.IsActive).FirstOrDefault().PLU : itmModel.BasePLU;
            //itmModel.PLUItemLinkId = itm.PLUItemLinks.Any(p => p.IsActive) ? itm.PLUItemLinks.Where(p => p.IsActive).FirstOrDefault().PLUItemLinkId : 0;
            //itmModel.ItemName = itm.ItemName;
            //itmModel.DisplayName = itm.DisplayName;
            //itmModel.DisplayDescription = itm.DisplayDescription;
            itmModel.SelectedDescription = itm.ItemDescriptions.Any(p => p.IsActive) ? itm.ItemDescriptions.Where(p => p.IsActive).FirstOrDefault().Description : itm.DisplayDescription;
            //itmModel.IsFeatured = itm.IsFeatured;
            //itmModel.QuickOrder = itm.QuickOrder;
            //itmModel.ShowPrice = itm.ShowPrice;
            //itmModel.IsModifier = itm.IsModifier;
            //itmModel.IsIncluded = itm.IsIncluded;
            //itmModel.IsEnabled = itm.IsEnabled;
            //itmModel.BasePrice = itm.BasePrice;
            //itmModel.DiscountedPrice = itm.DiscountedPrice;
            //itmModel.RecipeId = itm.RecipeId;
            //itmModel.CreatedDate = itm.CreatedDate;
            //itmModel.UpdatedDate = itm.UpdatedDate;
            //itmModel.DataId = itm.DataId;
            //itmModel.IsAvailable = itm.IsAvailable;
            itmModel.StartDate = itm.StartDate.HasValue ? (DateTime?)itm.StartDate.Value.ToLocalTime() : itm.StartDate;
            itmModel.EndDate = itm.EndDate.HasValue ? (DateTime?)itm.EndDate.Value.ToLocalTime() : itm.EndDate;
            //itmModel.ParentItemId = itm.ParentItemId;
            //itmModel.IsCombo = itm.IsCombo;
            itmModel.IsAutoSelect = itm.ItemCollectionObjects.Where(x => x.ItemId == itm.ItemId).Any() ? itm.ItemCollectionObjects.Where(x => x.ItemId == itm.ItemId).FirstOrDefault().IsAutoSelect : false;
            //itmModel.IsSubstitute = itm.IsSubstitute;
            //itmModel.IsTopLevel = itm.IsTopLevel;
            //itmModel.IsSendHierarchy = itm.IsSendHierarchy;
            //itmModel.PLUItemLinks = MapPluItemLinktoPluItemModelList(itm.PLUItemLinks.ToList());
        }
        private void log(string s)
        {
            System.IO.File.AppendAllText(Server.MapPath("~/LoadTestImportLog.txt"), string.Format("{0} \n", s));
        }
        private void logNetworkComplete(string s)
        {
            System.IO.File.AppendAllText(Server.MapPath("~/LoadTestNetworkMenuLog.txt"), string.Format("{0} \n", s));
        }
    }
}
