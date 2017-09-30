using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Phoenix.Web.Models;
using Phoenix.DataAccess;
using Phoenix.Common;
using System.Data.Entity;
using Phoenix.Web.Models.Grid;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;

using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using SnowMaker;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Viewer,Administrator,Editor,SuperAdministrator")]
    public class ItemController : PMBaseController
    {
        private IItemService _itemService;
        private IScheduleService _scheduleService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
            _scheduleService = new ScheduleService();
        }


        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
        }

        /// <summary>
        /// View to See MasterItem List
        /// </summary>
        /// <returns></returns>
        public ActionResult ItemList()
        {
            return View();
        }


        /// <summary>
        /// POS Items page
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult POSItems()
        {
            var model = new POSAdminModel();
            return View(model);
        }

        /// <summary>
        /// Gets all Master Items and Edited(newly added becoz of overriding) Items List
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public ActionResult GetItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, KendoGridRequest grdRequest)
        {
            List<ItemModel> itemList = new List<ItemModel>();
            if (brandId != 0)
            {
                itemList = _itemService.GetItemList(brandId, parentId, excludeNoPLUItems, prntType, netId, excludeDeactivated, isGrid, includeItemExtraProperties, grdRequest);
            }
            var res = new { total = _itemService.Count, data = itemList, pageSize = grdRequest.PageSize };
            
            return SerializeToMax(Json(res, JsonRequestBehavior.AllowGet));

        }

        /// <summary>
        /// Gets all POS Items and
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public ActionResult GetPOSItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, bool? isGrid, bool? includeItemExtraProperties, string gridType, KendoGridRequest grdRequest)
        {
            var onlyfewReturned = false;
            var qryResult = _itemService.GetPOSItemList(brandId, parentId, excludeNoPLUItems, prntType, netId, excludeDeactivated, isGrid, includeItemExtraProperties, gridType, grdRequest, out onlyfewReturned);
            var res = new { total = _itemService.Count, data = qryResult, pageSize = grdRequest.PageSize, onlyfewReturned = onlyfewReturned };

            return SerializeToMax(Json(res, JsonRequestBehavior.AllowGet));

        }

        /// <summary>
        /// Gets all Edited(newly added becoz of overriding) Items List of given Network
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public JsonResult GetOverriddenItemList(int brandId, int? parentId, bool? excludeNoPLUItems, MenuType? prntType, int? netId, bool? excludeDeactivated, int? menuId, KendoGridRequest grdRequest)
        {
            var qryResult = _itemService.GetOverriddenItemList(brandId, parentId, excludeNoPLUItems, prntType, netId, excludeDeactivated,menuId, grdRequest);
            var res = new { total = _itemService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Retreive the List of Items for given ItemIds
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public ActionResult GetSelectedItemList(string itemIds)
        {
            List<ItemModel> itemList = new List<ItemModel>();
            var js = new JavaScriptSerializer();
            if (!string.IsNullOrWhiteSpace(itemIds))
            {
                var ids = js.Deserialize<int[]>(itemIds);
                itemList = _itemService.GetSelectedItemList(ids);
            }

            return Json(itemList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get Item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult GetItemDetails(int id)
        {
            ItemModel itemModel = new ItemModel();
            if (id != 0)
            {
                List<string> children = new List<string>();
                children.Add("AssetItemLinks");
                children.Add("AssetItemLinks.Asset");
                children.Add("ItemDescriptions");
                children.Add("ItemPOSDataLinks");
                children.Add("ItemPOSDataLinks.POSData");
                itemModel = _itemService.GetItem(id, children.ToArray());
            }

            return Json(itemModel, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// View to Edit Master item
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult MasterItemEdit(int id, int brand)
        {
            var statusMsg = TempData.ContainsKey("statusMsg") ? Convert.ToString(TempData["statusMsg"]) : null;
            if (!string.IsNullOrWhiteSpace(statusMsg))
            {
                this.ViewBag.statusMessage = statusMsg;
            }
            else
            {
                this.ViewBag.statusMessage = string.Empty;
            }
            TempData["statusMsg"] = null;

            if (id == 0)
            {
                var model = new ItemModel();
                model.cDN = _itemService.cDN;
                model.IsDWFieldsEnabled = _itemService.CheckDWFieldsEnabled(brand); 
                return View(model);
            }
            List<string> children = new List<string>();
            children.Add("AssetItemLinks");
            children.Add("AssetItemLinks.Asset");
            children.Add("ItemDescriptions");
            children.Add("ItemPOSDataLinks");
            children.Add("ItemPOSDataLinks.POSData");
            var mdl = _itemService.GetItem(id, children.ToArray());
            mdl.IsFeatured = mdl.IsFeatured;

            ////Set AssetBrandId to show Assets added by that brand
            //mdl.AssetBrandId = _itemService.GetFirstBrand();
            return View(mdl);
        }

        /// <summary>
        /// Saves MasterItem
        /// </summary>
        /// <param name="itmModel"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult MasterItemEdit(ItemModel itmModel)
        {
            TempData["statusMsg"] = null;
            var createNew = itmModel.CreateNewAfterSave;
            //Check for uniqueness of PLU if PLU is changes
            var uniqueErrMsg = string.Empty;
            var entityWithError = string.Empty;

            //if (_itemService.IsPLUUniqueinBrand(itmModel.BasePLU,itmModel.AlternatePLU, itmModel.ItemId,out uniqueErrMsg,out entityWithError, itmModel.NetworkObjectId) == false)
            //{
            //    ModelState.AddModelError(entityWithError, uniqueErrMsg);
            //}

            //Check for uniqueness of DeepLinkId
            if (string.IsNullOrEmpty(itmModel.DeepLinkId) == false && !_itemService.IsDeepLinkIdUniqueinBrand(itmModel.DeepLinkId, itmModel.ItemId, itmModel.NetworkObjectId))
            {
                ModelState.AddModelError("DeepLinkId", "Deep Link Id must be unique");
            }

            //Check for uniqueness of AlternatePLU
            //if (string.IsNullOrEmpty(itmModel.AlternatePLU) == false && !_itemService.IsAltPLUUniqueinBrand(itmModel.AlternatePLU, itmModel.ItemId, itmModel.NetworkObjectId))
            //{
            //    ModelState.AddModelError("AlternatePLU", "Alternate Id must be unique");
            //}
            if (ModelState.IsValid)
            {
                itmModel = _itemService.SaveMasterItem(itmModel);
                this.ViewBag.statusMessage = _itemService.LastActionResult;


                if ((_itemService.LastActionResult != null && !_itemService.LastActionResult.Contains("failed")))
                {
                    TempData["statusMsg"] = _itemService.LastActionResult;
                    if (createNew)
                    {
                        return RedirectToAction("MasterItemEdit", "Item", new { id = 0 , brand = itmModel.NetworkObjectId });
                    }
                    else
                    {
                        return RedirectToAction("MasterItemEdit", "Item", new { id = itmModel.ItemId, brand = itmModel.NetworkObjectId });
                    }
                }
                else
                {
                    return View(itmModel);
                }
            }
            else
            {
                this.ViewBag.statusMessage = string.Format(Constants.StatusMessage.ErrItemAddT, itmModel.ItemName) + ModelState.ValidationErrorMessages();
                return View(itmModel);
            }

        }

        /// <summary>
        /// Deletes Master Item and all Links to it
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult DeleteMasterItem(int itemId)
        {
            var deletedItemId = _itemService.DeleteMasterItem(itemId);
            return new JsonResult { Data = new { status = _itemService.LastActionResult != null && _itemService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _itemService.LastActionResult, id = deletedItemId }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Deactivates Item and removes it from all menus or Activates the Item
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult UpdateMasterItemStatus(int itemId, bool updateStatus)
        {
            _itemService.UpdateMasterItemStatus(itemId, updateStatus);
            return new JsonResult { Data = new { status = _itemService.LastActionResult != null && _itemService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _itemService.LastActionResult}, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        #region Not Used
        ///// <summary>
        ///// Gets list of PLU as a select List for DropDown
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //public ActionResult GetPLUs(int id)
        //{
        //    var data = _itemService.GetPLUs(id);

        //    return Json(data, JsonRequestBehavior.AllowGet);

        //}

        ///// <summary>
        ///// Gets all PLUs of an Item
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //public ActionResult GetItemPLUs(int id)
        //{
        //    var data = _itemService.GetItemPLUs(id);

        //    return Json(data, JsonRequestBehavior.AllowGet);

        //}

        ///// <summary>
        ///// Gets alls Descritpions of an Item
        ///// </summary>
        ///// <param name="id"></param>
        ///// <returns></returns>
        //public ActionResult GetItemDescriptions(int id)
        //{
        //    var data = _itemService.GetItemDescriptions(id);

        //    return Json(data, JsonRequestBehavior.AllowGet);

        //}

        //Not used calls
        ///// <summary>
        ///// AJAX call to Create a new Description(always adds a additional desc) - Not used
        ///// </summary>
        ///// <param name="models"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult ItemDescriptionCreate(IEnumerable<ItemDescriptionModel> models)
        //{
        //    if (models != null)
        //    {
        //        foreach (var model in models)
        //        {
        //            model.ItemDescriptionId = _itemService.AddItemDescription(model);
        //        }
        //    }
        //    return Json(models.ToList());
        //}

        ///// <summary>
        ///// AJAX call to update a Description of Item - Not used
        ///// </summary>
        ///// <param name="models"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult ItemDescriptionUpdate(IEnumerable<ItemDescriptionModel> models)
        //{
        //    if (models != null && ModelState.IsValid)
        //    {
        //        foreach (var model in models)
        //        {
        //            _itemService.UpdateItemDescription(model);
        //        }
        //    }

        //    return Json(new { status = true }, "text/plain");
        //    //return Json(null);
        //}

        ///// <summary>
        ///// AJAX call to delete a Description( only deletes Additional Description) - Not used
        ///// </summary>
        ///// <param name="models"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult ItemDescriptionDestroy(IEnumerable<ItemDescriptionModel> models)
        //{
        //    if (models != null)
        //    {
        //        foreach (var model in models)
        //        {
        //            _itemService.DeleteItemDescription(model);
        //        }
        //    }

        //    return Json(new { status = true }, "text/plain");
        //    //return Json(null);
        //}        
        #endregion

        /// <summary>
        /// Call to fill the RequestedBy DropdownList in Master Item Edit
        /// </summary>
        /// <returns></returns>
        public JsonResult GetItemRequestedBy()
        {
            var data = _itemService.GetItemRequestedBy();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Call to fill the Item Lookup DropdownList in Master Item Edit
        /// </summary>
        /// <param name="typeId"></param>
        /// <param name="abbr"></param>
        /// <returns></returns>
        public JsonResult GetItemLookups(int typeId, string abbr)
        {
            var data = _itemService.GetItemLookups(typeId, abbr);

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Call to fill the Item Cook Time DropdownList in Master Item Edit
        /// </summary>
        /// <returns></returns>
        public JsonResult GetItemCookTimes()
        {
            var data = _itemService.GetItemCookTimes();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Call to fill the Cost Category DropdownList in Master Item Edit
        /// </summary>
        /// <returns></returns>
        public JsonResult GetItemCategorizations()
        {
            var data = _itemService.GetItemCategorizations();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Call to fill the Sub Category DropdownList in Master Item Edit
        /// </summary>
        /// <returns></returns>
        public JsonResult GetItemSubTypes()
        {
            var data = _itemService.GetItemSubTypes();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Updates the DisplyName of given items
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult UpdateItemsDisplayName(IEnumerable<ItemModel> models)
        {
            if (models != null)
            {
                foreach (var model in models)
                {
                    _itemService.UpdateItemDisplayName(model);
                }
            }

            return Json(new { status = true }, "text/plain");
        }
    }
}
