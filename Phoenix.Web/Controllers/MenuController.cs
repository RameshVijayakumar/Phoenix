using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Phoenix.Web.Models;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using System.Web.Script.Serialization;
using Phoenix.Web.Models.Grid;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using SnowMaker;

namespace Phoenix.Web.Controllers
{

    [CustomAuthorize(Roles = "Viewer,Editor,Administrator,SuperAdministrator")]
    public class MenuController : PMBaseController
    {
        private IItemService _itemService;

        public IMenuService _menuService;

        public int NetworkObjectId
        {
            get
            {
                var netId = this.Request.QueryString["netId"];
                if (string.IsNullOrEmpty(netId)) netId = "0";
                return int.Parse(netId);
            }
        }

        public int MenuId
        {
            get
            {
                var menuId = this.Request.QueryString["menuId"];
                if (string.IsNullOrEmpty(menuId)) menuId = "0";
                return int.Parse(menuId);
            }
        }

        public MenuController(IMenuService menuService, IItemService itemService)
        {
            _menuService = menuService;
            _itemService = itemService;
            _menuService.Initialize(new ModelStateWrapper(this.ModelState));
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
            _menuService.NetworkObjectId = NetworkObjectId;

            _menuService.MenuId = MenuId;
            _menuService._ruleService.NetworkObjectId = NetworkObjectId;
        }

        #region Menu Management -- page processing
        /// <summary>
        /// List of Menus in a Network
        /// </summary>
        /// <returns></returns>
        public ActionResult MenuSelect()
        {
            this.ViewBag.MenuNames = string.Empty;
            MenuDataModel model = new MenuDataModel();
            return View(model);
        }

        /// <summary>
        /// View to edit a Menu
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult MenuEdit(int id)
        {
            int brandId = 0;
            string parentsBreadCrum = string.Empty;
            var network = _menuService.GetNetworkObject(NetworkObjectId, out brandId, out parentsBreadCrum);
            var menu = _menuService.GetMenu(id);

            this.ViewBag.menuId = id;
            this.ViewBag.networkId = NetworkObjectId;
            this.ViewBag.networkname = network.Name;
            this.ViewBag.menuTreeId = _menuService.GetMenuTreeId(MenuType.Menu, 0, id);
            this.ViewBag.brandId = brandId;
            this.ViewBag.networkIdMenuCreated = menu.NetworkObjectId;
            this.ViewBag.parentsBreadCrum = parentsBreadCrum;

            return View();
        }

        /// <summary>
        /// Populates Collection Type DDL
        /// </summary>
        /// <returns></returns>
        public JsonResult GetCollectionTypes()
        {
            var retVal = from CollectionTypeNames e in Enum.GetValues(typeof(CollectionTypeNames))
                         select new KeyValuePair<int, string>
                             (
                                 (int)Enum.Parse(typeof(CollectionTypeNames), e.ToString()),
                                 e.ToString()
                             );
            return Json(retVal, JsonRequestBehavior.AllowGet);
        }

        //bug 4737 - Add category type Reorder
        /// <summary>
        /// Populates Category Type DDL
        /// </summary>
        /// <returns></returns>
        public JsonResult GetCategoryTypes()
        {
            var retVal = from CategoryTypes e in Enum.GetValues(typeof(CategoryTypes))
                         select new KeyValuePair<int, string>
                             (
                                 (int)Enum.Parse(typeof(CategoryTypes), e.ToString()),
                                 e.ToString()
                             );
            return Json(retVal, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// List Menus available for a Network
        /// </summary>
        /// <param name="netId">netId</param>
        /// <returns></returns>
        public JsonResult GetMenuList(int netId)
        {
            List<MenuDataModel> retVal = _menuService.GetMenuList(netId);

            return Json(retVal, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get all the menus in given serilized networks list
        /// </summary>
        /// <param name="netIdsString"></param>
        /// <returns></returns>
        public JsonResult GetAllMenuList(string netIdsString)
        {
            List<int> mulitpleNetworkObjIds = null;
            if (string.IsNullOrWhiteSpace(netIdsString) == false)
            {
                var js = new JavaScriptSerializer();
                mulitpleNetworkObjIds = js.Deserialize<List<int>>(netIdsString);
            }
            return Json(_menuService.GetMenuListInSelectedNetworkTrees(mulitpleNetworkObjIds), JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Construct Menu Tree
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetHierarchicalTree(string id)
        {
            List<MenuService.MenuTreeItem> tree = new List<MenuService.MenuTreeItem>();
            _menuService.NetworkObjectId = NetworkObjectId;

            _menuService.GetHierarchicalMenuTree(id, tree);
            var jsonResult = Json(tree, JsonRequestBehavior.AllowGet);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">MenuId</param>
        /// <returns></returns>
        public JsonResult GetCategoryList(int id, int ovrridenId)
        {
            return Json(_menuService.GetCategoriesByMenu(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get all sub categories in a Cat
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetSubCategoryList(int id, int ovrridenId)
        {
            return Json(_menuService.GetSubCategories(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAllCategoriesList()
        {
            var qryResult = _menuService.GetAllCategoriesList();
            var res = new { total = _menuService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">CategoryId</param>
        /// <returns></returns>
        public JsonResult GetCategoryItemList(int id, int ovrridenId)
        {
            return Json(_menuService.GetItemsByCategory(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">ItemId</param>
        /// <returns></returns>
        public JsonResult GetItemCollectionList(int id, int ovrridenId)
        {
            return Json(_menuService.GetCollectionbyItem(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// List of Items in Collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetItemCollectionItemList(int id, int ovrridenId)
        {
            return Json(_menuService.GetItemsByCollection(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// List of Items in Collection
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetPrependItemList(int id, int ovrridenId)
        {
            return Json(_menuService.GetPrependItemsByItem(id, ovrridenId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets Item Schedule Summary for a Schedule in this Network
        /// </summary>
        /// <param name="id"></param>
        /// <param name="schId"></param>
        /// menuid
        /// networkid
        /// <returns></returns>
        public JsonResult GetItemSchDetailSummary(int id,int schId)
        {
            List<EntitySchSummary> data = new List<EntitySchSummary>();
            if(schId != 0)
            {
                data = _menuService.GetItemSchDetailSummary(id);
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets Category Schedule Summary for a Schedule in this Network
        /// </summary>
        /// <param name="id"></param>
        /// <param name="schId"></param>
        /// <returns></returns>
        public JsonResult GetCatSchDetailSummary(int id, int schId)
        {
            List<EntitySchSummary> data = new List<EntitySchSummary>();
            if(schId != 0)
            {
                data = _menuService.GetCatSchDetailSummary(id);
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Partial View for All the categories in a Menu
        /// </summary>
        /// <param name="id">CategoryId</param>
        /// <returns></returns>
        public ActionResult _CategorySummary(int id)
        {
            return PartialView();
        }

        /// <summary>
        /// Partial View for editing Category in a Menu
        /// </summary>
        /// <param name="id">MenuId</param>
        /// <returns></returns>
        public ActionResult _CategoryEdit(int id, int prntId,int typ)
        {
            if (id == 0)
            {
                CategoryModel catModel = new CategoryModel();
                return PartialView(catModel);
            }
            else
            {
                if (typ == (int)MenuType.Category)
                {
                    return PartialView(_menuService.GetCategory(id, prntId, NetworkObjectId));
                }
                else
                {
                    return PartialView(_menuService.GetSubCategory(id, prntId, NetworkObjectId));
                }
            }
        }

        /// <summary>
        /// Partial View for editing Item in a Menu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="catId"></param>
        /// <returns></returns>
        public ActionResult _ItemEdit(int id, int prntId = 0, int itemType = (int)MenuType.Item)
        {
            var mdl = _menuService.GetItemWithMaster(id,prntId,(MenuType)itemType);
            return PartialView(mdl);
        }

        /// <summary>
        /// Partial View editing Collection in a Menu
        /// </summary>
        /// <param name="id"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ActionResult _ItemCollectionEdit(int id, int itemId)
        {
            if (id == 0)
            {
                CollectionModel colModel = new CollectionModel();
                return PartialView(colModel);
            }
            else
            {
                var mdl = _menuService.GetItemCollection(id, itemId);
                return PartialView(mdl);
            }
        }

        /// <summary>
        /// Gets list of all Master collection that can be added to Item in current Menu
        /// </summary>
        /// <param name="parentId">pass parentId to exclude the collections that can't be added</param>
        /// MenuId
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public JsonResult GetMasterCollectionList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest)
        {
            var qryResult = _menuService.GetMasterCollectionList(parentId, prntType, grdRequest);
            var res = new { total = _menuService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Gets list of all Master Category List that can be added to another Category in current Menu
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public JsonResult GetMasterCategoryList(int? parentId, MenuType? prntType, KendoGridRequest grdRequest)
        {
            var qryResult = _menuService.GetMasterCategoryList(parentId, prntType,grdRequest);
            var res = new { total = _menuService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);

        }
        #endregion
        #region Menu Management -- Changes to Menu

        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult SaveCat(CategoryModel model, int prntCatId, int mnuId, int netId, MenuType catType)
        {
            _menuService.MenuId = mnuId;
            _menuService.NetworkObjectId = netId;
            var newCatId = _menuService.SaveCategory(model, prntCatId, catType);
            var refreshTree = false;
            if (ModelState.IsValid)
            {
                if (newCatId != model.CategoryId)
                {
                    refreshTree = true;
                    model.CategoryId = newCatId;
                }
                return new JsonResult { Data = new { refreshTree = refreshTree, status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, model = model }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            else
            {
                return new JsonResult { Data = new { refreshTree = refreshTree, status = false, lastActionResult = "EntityError", model = ModelState.ValidationErrors() }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult SaveItem(ItemModel model, int prntId, int mnuId, int netId, MenuType itmType)
        {
            _menuService.MenuId = mnuId;
            _menuService.NetworkObjectId = netId;
            var itemId = _menuService.SaveItem(model, prntId, itmType);
            var refreshTree = false;
            if (itemId != model.ItemId)
            {
                refreshTree = true;
                model.ItemId = itemId;
            }
            return new JsonResult { Data = new { refreshTree = refreshTree, status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, model = model }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult SaveCollection(CollectionModel model, int itemId, int mnuId, int netId)
        {
            _menuService.MenuId = mnuId;
            _menuService.NetworkObjectId = netId;
            var id = _menuService.SaveCollection(model, itemId);
            var refreshTree = false;
            if (id != model.CollectionId)
            {
                refreshTree = true;
                model.CollectionId = id;
            }
            return new JsonResult { Data = new { refreshTree = refreshTree, status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, model = model }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        
        /// <summary>
        /// Add sub categories to another category
        /// </summary>
        /// <param name="subCatIds">sub cats</param>
        /// <param name="catId">Parent category</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddSubCatstoCategory(string Ids, int prntId)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(Ids);
            var subCatsAdded = _menuService.AddSubCategoriestoCategory(ids, prntId);
            return new JsonResult { Data = new { items = subCatsAdded, status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add categories to menu
        /// </summary>
        /// <param name="subCatIds">sub cats</param>
        /// <param name="catId">Parent category</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddCatstoMenu(string Ids, int prntId)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(Ids);
            var catsAdded = _menuService.AddCategoriestoMenu(ids, prntId);
            return new JsonResult { Data = new { items = catsAdded.ToList(), status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add items to the category
        /// </summary>
        /// <param name="ids">items</param>
        /// <param name="catId">category</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddItemstoCategory(string Ids, int prntId)
        {
            var itmAdded = _menuService.AddItemstoCategory(Ids, prntId);

            return new JsonResult { Data = new { items = itmAdded.ToList(), updatedItems = itmAdded.Where(x => x.Value.CompareTo("AddedandItemUpdated") == 0).Select(x => x.Key).ToList(), status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        
        /// <summary>
        /// Add Collections to the Item
        /// </summary>
        /// <param name="ids">collections</param>
        /// <param name="itemId">item</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddCollectionstoItem(string Ids, int prntId)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(Ids);
            var colAdded = _menuService.AddCollectionstoItem(ids, prntId);
            return new JsonResult { Data = new { items = colAdded, status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add Collections to the Item
        /// </summary>
        /// <param name="ids">collections</param>
        /// <param name="itemId">item</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddItemstoCollection(string Ids, int prntId)
        {
            var itemsAdded = _menuService.AddItemstoCollection(Ids, prntId);
            return new JsonResult { Data = new { items = itemsAdded.ToList(), updatedItems = itemsAdded.Where(x => x.Value.CompareTo("AddedandItemUpdated") == 0).Select(x => x.Key).ToList(), status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add PrependItemd to the Item
        /// </summary>
        /// <param name="ids">prependitems</param>
        /// <param name="itemId">item</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddPrependItemstoItem(string Ids, int prntId)
        {
            var itemsAdded = _menuService.AddPrependItemstoItem(Ids, prntId);
            return new JsonResult { Data = new { items = itemsAdded.ToList(), updatedItems = itemsAdded.Where(x => x.Value.CompareTo("AddedandItemUpdated") == 0).Select(x => x.Key).ToList(), status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete category from the menu
        /// </summary>
        /// <param name="catId">category</param>
        /// <param name="menuid">menu</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteCategoryMenuLink(int catId, int menuId)
        {
            var deletedCatTreeId = _menuService.DeleteCategoryMenuLink(catId, menuId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedCatTreeId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete sub category from parent category
        /// </summary>
        /// <param name="subCatId"></param>
        /// <param name="prntCatId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteSubCategoryLink(int subCatId, int prntCatId)
        {
            var deletedCatTreeId = _menuService.DeleteSubCategoryLink(subCatId, prntCatId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedCatTreeId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete item from the category
        /// </summary>
        /// <param name="ids">item</param>
        /// <param name="catId">category</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteCatObject(int itemId, int catId)
        {
            var deletedItemId = _menuService.DeleteCategoryObject(itemId, catId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedItemId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete collection from the item
        /// </summary>
        /// <param name="colId">collection</param>
        /// <param name="itemId">item</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteItemCollection(int colId, int itemId)
        {
            var deletedTreeId = _menuService.DeleteItemCollection(colId, itemId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedTreeId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete collection from the item
        /// </summary>
        /// <param name="colId">collection</param>
        /// <param name="itemId">item</param>
        /// <param name="menuId">menu</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteItemCollectionItem(int itemId, int colId)
        {
            var deletedTreeId = _menuService.DeleteItemCollectionItem(itemId, colId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedTreeId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// delete prepend Item from the item
        /// </summary>
        /// <param name="itmId">prependItem</param>
        /// <param name="prependItemId">item</param>
        /// <param name="menuId">menu</param>
        /// <param name="netId">network</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeletePrependItem(int prependItemId, int itmId)
        {
            var deletedTreeId = _menuService.DeletePrependItem(prependItemId, itmId);
            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult, id = deletedTreeId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change the position of a categories in Menu for a given NW
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangeCatMenuLinkPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> cats = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (cats != null)
            {
                    _menuService.ChangeCatMenuLinkPositions(cats, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change the position of a subcategories in Category for a given NW
        /// </summary>
        /// <param name="models"></param>
        /// <param name="prntId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangeSubCatLinkPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> cats = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (cats != null)
            {
                _menuService.ChangeSubCatLinkPositions(cats, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change the position of items in a catefory for  a given NW
        /// </summary>
        /// <param name="models"></param>
        /// <param name="prntId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangeCatObjectPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> itmModels = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (itmModels != null)
            {
                _menuService.ChangeCatObjectPositions(itmModels, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change the position of collections in a item for a given NW
        /// </summary>
        /// <param name="models"></param>
        /// <param name="prntId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangeItemCollectionLinkPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> colModels = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (colModels != null)
            {
                _menuService.ChangeItemCollectionLinkPositions(colModels, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change position of given items in a collection for given NW
        /// </summary>
        /// <param name="models"></param>
        /// <param name="prntId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangeItemCollectionObjectPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> itmModels = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (itmModels != null)
            {
                _menuService.ChangeItemCollectionObjectPositions(itmModels, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change position of given prepend items in a Item for given NW
        /// </summary>
        /// <param name="models"></param>
        /// <param name="prntId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ChangePrependItemLinkPositions(string models, int prntId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<MenuService.MenuGridItem> itmModels = js.Deserialize<List<MenuService.MenuGridItem>>(models);
            if (itmModels != null)
            {
                _menuService.ChangePrependItemLinkPositions(itmModels, prntId);
            }

            return new JsonResult { Data = new { status = _menuService.LastActionResult == null || !_menuService.LastActionResult.Contains("failed"), lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Revert whole menu that is not create by the current network
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult RevertMenu(int menuId,int netId)
        {
            _menuService.MenuId = menuId;
            _menuService.NetworkObjectId = netId;
            var actionstatus = _menuService.RevertMenu();
            return new JsonResult { Data = new { status = actionstatus, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        //public ActionResult CopyMenu(int menuId, int netId)
        //{
        //    _menuService.MenuId = menuId;
        //    _menuService.NetworkObjectId = netId;
        //    _menuService.CopyMenu(menuId);
        //    return new JsonResult { Data = new { status = _menuService.LastActionResult != null && _menuService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _menuService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        //}


        /// <summary>
        /// Create New Menu at current network
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult CreateMenu([DataSourceRequest] DataSourceRequest request,MenuDataModel model)
        {
            ModelState.Remove("MenuId");
            model = _menuService.CreateMenu(model);
            if (ModelState.IsValid == false)
            {
                return Json(ModelState.ToDataSourceResult());
            }
            else
            {
                return Json(model);
            }
        }

        /// <summary>
        /// Update the Menu which is created at current network
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult UpdateMenu([DataSourceRequest] DataSourceRequest request, MenuDataModel model)
        {
            model = _menuService.SaveMenu(model);
            if (ModelState.IsValid == false)
            {
                return Json(ModelState.ToDataSourceResult());
            }
            else
            {
                return Json(model);
            }
        }

        /// <summary>
        /// Deletes the Menu which is created at current level
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DeleteMenu(string model, int netId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            MenuDataModel menuModel = js.Deserialize<MenuDataModel>(model);
            var msg = string.Empty;
            if (menuModel != null)
            {
                var result = _menuService.DeleteMenu(menuModel, netId);
                msg = _menuService.LastActionResult;
            }
            else
            {
                msg = "Operation failed.";
            }
            return new JsonResult { Data = new { status = !string.IsNullOrWhiteSpace(msg) && msg.Contains("failed") ? false : true, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        #endregion

    }
}
