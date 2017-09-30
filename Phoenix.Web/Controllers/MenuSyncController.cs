using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Phoenix.Web.Models;
using Phoenix.Web.Models.Grid;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class MenuSyncController : PMBaseController
    {
        //
        // GET: /MenuSync/
        private IMenuSyncService _menuSyncService;

        public MenuSyncController(IMenuSyncService menuSyncService)
        {
            _menuSyncService = menuSyncService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>  
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]     
        public ActionResult Index()
        {
            return View();
        }
        
        /// <summary>
        /// This method brings a list of all the targets for the specified networkObjectid
        /// </summary>
        /// <param name="targetId"></param>
        /// <returns></returns>
        [HttpGet]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetTargetList(int? networkObjectId, string netIdsString)
        {

            List<int> mulitpleNetworkObjIds = null;
            if (string.IsNullOrWhiteSpace(netIdsString) == false)
            {
                var js = new JavaScriptSerializer();
                mulitpleNetworkObjIds = js.Deserialize<List<int>>(netIdsString);
            }
            var list = _menuSyncService.GetTargetList(networkObjectId, mulitpleNetworkObjIds);
            if (list == null)
            {
                list = new List<MenuSyncTargetModel>();
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetTargetDetailList(string netIdsString, KendoGridRequest grdRequest)
        {
            var list = new List<MenuSyncTargetDetailModel>();

            List<int> mulitpleNetworkObjIds = null;
            if (string.IsNullOrWhiteSpace(netIdsString) == false)
            {
                var js = new JavaScriptSerializer();
                mulitpleNetworkObjIds = js.Deserialize<List<int>>(netIdsString);
            }
            list = _menuSyncService.GetTargetDetailList(mulitpleNetworkObjIds, grdRequest);
            
            var res = new { total = _menuSyncService.Count, data = list };
            return SerializeToMax(Json(res, JsonRequestBehavior.AllowGet));

        }  

        /// This method is used to save(create as well as update) targets. It is invoked from Menu Sync Target Manager page.
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]  
        public ActionResult SaveTarget([DataSourceRequest] DataSourceRequest request, MenuSyncTargetModel model)
        {
            if (ModelState.IsValid)
            {
                bool isTargetNotUnique = false;
                bool isURLNotUnique = false;

                _menuSyncService.CheckUniquenessOfReqdData(model, out isTargetNotUnique, out isURLNotUnique);

                if (isTargetNotUnique && isURLNotUnique)
                {
                    ModelState.AddModelError("TargetName", "Target Name must be unique");
                    ModelState.AddModelError("URL", "URL must be unique");
                    return Json(ModelState.ToDataSourceResult());
                }
                else if (isTargetNotUnique)
                {
                    ModelState.AddModelError("TargetName", "Target Name must be unique");
                    return Json(ModelState.ToDataSourceResult());
                }
                else if (isURLNotUnique)
                {
                    ModelState.AddModelError("URL", "URL must be unique");
                    return Json(ModelState.ToDataSourceResult());
                }
                else
                {
                    model = _menuSyncService.SaveTarget(model);
                    if (_menuSyncService.LastActionResult.Contains("failed"))
                    {
                        ModelState.AddModelError("TargetName", _menuSyncService.LastActionResult);
                        return Json(ModelState.ToDataSourceResult());
                    }
                    else
                    {
                        return Json(model);
                    }
                }
            }
            else
            {
                return Json(ModelState.ToDataSourceResult());
            }           
        }
             
        /// <summary>
        /// This method is used to delete list of targets
        /// </summary>
        /// <param name="targetIds"></param>
        /// <returns></returns>    
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]  
        public ActionResult DeleteTargets(string targets)
        {
            var actionStatus = _menuSyncService.DeleteTargets(targets);
            return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _menuSyncService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }      

        /// <summary>
        /// Send Sync message for selected targets
        /// </summary>
        /// <param name="networkObjectIds"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult Sync(string networkObjectIds, string targets)
        {
            var actionStatus = _menuSyncService.SyncTargets(networkObjectIds, targets);
            return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _menuSyncService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Delete Sync History of selected networks
        /// </summary>
        /// <param name="networkObjectIds"></param>
        /// <param name="targets"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult DeleteSyncHistory(string networkObjectDetails)
        {
            var actionStatus = _menuSyncService.DeleteSyncHistory(networkObjectDetails);
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _menuSyncService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        } 
 
        /// <summary>
        /// Refresh Targets from SSO
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult RefreshTargets()
        {
            var actionStatus = _menuSyncService.RefreshTargets();
            return new JsonResult { Data = new { status = actionStatus, lastActionResult = _menuSyncService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        
        }
    }
}


