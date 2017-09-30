using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.Web.Models;
using Phoenix.Web.Models.Grid;

namespace Phoenix.Web.Controllers
{
    public class DataMapController : PMBaseController
    {
        private IPOSMapService _posMapService;
        private IODSPOSService _oDSPOSServiceService;

        public DataMapController()
        {
            //TODO: inject this interface
            _posMapService = new POSMapService();
            _oDSPOSServiceService = new ODSPOSService();
        }

        #region pos mapping

        /// <summary>
        /// POS Mapping Page
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult POS()
        {
            return View();
        }

        /// <summary>
        /// POS Administartion page
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator,Editor,Viewer")]
        public ActionResult POSAdmin()
        {
            var model = new POSAdminModel();
            return View(model);
        }

        /// <summary>
        /// Get MenuItems in given menu for a specific network attached to given parenttype
        /// </summary>
        /// <param name="parentGroupId">Parent Type (categry/collection type)</param>
        /// <param name="networkObjectId">network Identifier</param>
        /// <param name="MenuId">Menu Identifier</param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("MenuItems")]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult MenuItems(int parentGroupId, int networkObjectId, int MenuId, KendoGridRequest grdRequest)
        {
            return Json(_posMapService.GetMenuItems(parentGroupId, networkObjectId, MenuId, grdRequest), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get ODSData for given network
        /// </summary>
        /// <param name="networkObjectId">Network Identifier</param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("OperationalPOSData")]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult OperationalPOSData(int networkObjectId)
        {
            return Json(_posMapService.GetOperationalPOSData(networkObjectId), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get POSItems list for given network
        /// </summary>
        /// <param name="networkObjectId">Network Identifier</param>
        /// <param name="includeMappped">Flag to included mapped POSItems or not </param>
        /// <returns></returns>
        [HttpGet]
        [ActionName("GetPOSDataList")]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetPOSDataList(int networkObjectId, bool includeMappped = true, KendoGridRequest grdRequest = null)
        {
            bool actionStatus = false;
            var poslist = _posMapService.GetPOSDataList(networkObjectId, includeMappped, grdRequest);
            
            var result = new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult, data = poslist, total = _posMapService.Count, pages = _posMapService.Pages }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

            return SerializeToMax(Json(result, JsonRequestBehavior.AllowGet));
        }
        
        /// <summary>
        /// Create/Edit POS Item Save
        /// </summary>
        /// <param name="posItemId">POSItem identifier</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("SavePOSItem")]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult SavePOSItem(POSDataModel positem)
        {
            bool actionStatus = false;
            var positemresult = _posMapService.SavePOSItem(positem,out actionStatus);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult, posItem = positemresult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Delete unmapped POSItem
        /// </summary>
        /// <param name="posItemId">POSItem identifier</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DeletePOSItem")]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult DeletePOSItem(int posItemId)
        {
            bool actionStatus = _posMapService.DeletePOSItem(posItemId);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add POSItem to MasterItem
        /// </summary>
        /// <param name="posItemId">POSItem identifier</param>
        /// <param name="itemId">Item identifier</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("AttachPOSItem")]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult AttachPOSItemToMasterItem(int posItemId, int itemId)
        {
            bool actionStatus = _posMapService.AttachPOSItemToMasterItem(posItemId, itemId);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Remove POSItem from MasterItem
        /// </summary>
        /// <param name="posItemId">POSItem identifier</param>
        /// <param name="itemId">Item identifier</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("RemovePOSItem")]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult RemovePOSItemFromMasterItem(int posItemId, int itemId)
        {
            bool actionStatus = _posMapService.RemovePOSItemFromMasterItem(posItemId, itemId);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Change POSItem selection in a Menu Item for requested Network
        /// </summary>
        /// <param name="posItemId"></param>
        /// <param name="itemId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("ChangePOSItem")]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult ChangePOSItemOfMenuItem(int posItemId, int itemId,int networkObjectId,int menuId)
        {
            bool actionStatus = _posMapService.ChangePOSItemOfMenuItem(posItemId, itemId, networkObjectId, menuId);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _posMapService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        //[HttpPost]
        //[ActionName("POSManualMap")]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult POSManualMap(int networkObjectId, int itemId, int odsPOSId, int MenuId)
        //{
        //    var result = _posMapService.MapPOSData(networkObjectId, itemId, odsPOSId, MenuId);

        //    return new JsonResult
        //    {
        //        Data = new { status = result, message = _posMapService.LastActionResult },
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet
        //    };
        //}
        //[HttpPost]
        //[ActionName("POSManualUnmap")]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult POSManualUnmap(int networkObjectId, int itemId, int MenuId)
        //{
        //    var result = _posMapService.UnmapPOSData(networkObjectId, itemId, MenuId);

        //    return new JsonResult
        //    {
        //        Data = new { status = result, message = _posMapService.LastActionResult },
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet
        //    };
        //}
        //[HttpPost]
        //[ActionName("AutoMapPOSByPLU")]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult AutoMapPOSByPLU(int networkObjectId, int MenuId)
        //{
        //    var result = _posMapService.AutoMapPOSByPLU(networkObjectId, MenuId);

        //    return new JsonResult
        //    {
        //        Data = new { status = result, message = _posMapService.LastActionResult },
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet
        //    };
        //}

        //[HttpPost]
        //[ActionName("AutoMapPOSBySite")]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult AutoMapPOSBySite(int networkObjectId, int refNetworkObjectId, int MenuId)
        //{
        //    var result = _posMapService.AutoMapPOSByReferenceSite(networkObjectId, refNetworkObjectId, MenuId);

        //    return new JsonResult
        //    {
        //        Data = new { status = result, message = _posMapService.LastActionResult },
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet
        //    };
        //}


        //[HttpPost]
        //[ActionName("ItemStatus")]
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult SetItemStatus(int itemId, int networkObjectId, int menuId, bool isEnabled)
        //{
        //    var result = _posMapService.SetItemStatus(itemId, networkObjectId, menuId, isEnabled);

        //    return new JsonResult
        //    {
        //        Data = new { status = result, message = _posMapService.LastActionResult },
        //        JsonRequestBehavior = JsonRequestBehavior.AllowGet
        //    };
        //}

        #endregion


        #region ODS POS Details

        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult odsposdetails()
        {
            return View();
        }

        /// <summary>
        /// GetSiteODSData : Returns the ODSPOS Data results for the grid - paged
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetSiteODSData(ODSPOSGridRequest grdRequest)
        {
            var qryResult = _oDSPOSServiceService.GetODSPOSDataList(grdRequest);
            var res = new { total = _oDSPOSServiceService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);
        }

        #endregion

    }
}
