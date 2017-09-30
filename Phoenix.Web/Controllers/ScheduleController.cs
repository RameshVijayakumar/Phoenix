using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using Phoenix.Web.Models;
using Phoenix.Web.Models.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using SnowMaker;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Viewer,Administrator,Editor,SuperAdministrator")]
    public class ScheduleController : PMBaseController
    {
        public IScheduleService _schService;


        //Always pass netId as parameter in query string to determine which network is making the change
        public int NetworkObjectId
        {
            get
            {
                var netId = this.Request.QueryString["netId"];
                if (netId == null) netId = "0";
                return int.Parse(netId);
            }
        }

        //To determine on which schedule the changes are happening
        public int ScheduleId
        {
            get
            {
                var schId = this.Request.QueryString["schId"];
                if (schId == null) schId = "0";
                return int.Parse(schId);
            }
        }

        public ScheduleController(IScheduleService scheduleService)
        {
            _schService = scheduleService;
        }

        //Assign the querystring parameters to the properties of the service
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
            _schService.NetworkObjectId = NetworkObjectId;

            _schService.ScheduleId = ScheduleId;

            _schService._ruleService.NetworkObjectId = NetworkObjectId;
        }

        /// <summary>
        /// View to see List of Schedules for all NWs (Schedule Browser)
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Ajax call which will get all the schedules in a network
        /// </summary>
        /// <param name="netId">Network Id</param>
        /// <returns></returns>
        public JsonResult GetScheduleList(int netId)
        {
            List<ScheduleModel> schList = new List<ScheduleModel>();
            if (netId == 0)
            {
                return Json(schList, JsonRequestBehavior.AllowGet);

            }

            schList = _schService.GetScheduleList(netId);
            return Json(schList, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get inactive schedules - not in use
        /// </summary>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public JsonResult GetInActiveScheduleList(KendoGridRequest grdRequest)
        {
            var qryResult = _schService.GetInActiveScheduleList(grdRequest);
            var res = new { total = _schService.Count, data = qryResult };
            return Json(res, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// AJAX call to get all cycles in system
        /// </summary>
        /// <returns></returns>
        public JsonResult GetScheduleCycles(int netId)
        {
            List<SchCycleModel> schCycles = new List<SchCycleModel>();
            schCycles = _schService.GetScheduleCycles(netId,true);

            return Json(schCycles, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Add deleted Schedules - Not in use
        /// </summary>
        /// <param name="schIds">stringfied ScheduleIds</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult AddInActiveSchedules(string schIds)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(schIds);
            var schAdded = _schService.AddInActiveSchedules(ids);
            return new JsonResult { Data = new { schedules = schAdded, status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Call to delete a Schedule
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public JsonResult DeleteSchedule(int id)
        {
            _schService.DeleteSchedule(id);
            return new JsonResult { Data = new { status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult}, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Call to disable a Schedule
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public JsonResult DisableSchedule(int id)
        {
            _schService.DisableSchedule(id);
            return new JsonResult { Data = new { status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        /// <summary>
        /// Call to enable a Schedule
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public JsonResult EnableSchedule(int id)
        {
            _schService.EnableSchedule(id);
            return new JsonResult { Data = new { status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Call to copy a Schedule
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public JsonResult CopySchedule(string model)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            ScheduleModel schModel = js.Deserialize<ScheduleModel>(model);
            _schService.CopySchedule(schModel);
            return new JsonResult { Data = new { status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Call to revert a Schedule
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public JsonResult RevertSchedule(int id)
        {
            _schService.RevertSchedule(id);
            return new JsonResult { Data = new { status = _schService.LastActionResult != null && _schService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _schService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Custom Grid method to move Schedule
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult MoveSchedule(string model, int netId, int newSortOrder, int oldSortOrder)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            ScheduleModel schModel = js.Deserialize<ScheduleModel>(model);
            var msg = string.Empty;
            var status = true;
            if (schModel != null)
            {
                status =_schService.MoveSchedule(schModel, netId, newSortOrder, oldSortOrder);
                msg = _schService.LastActionResult;
            }
            else
            {
                status = false;
                msg = "Operation failed.";
            }
            return new JsonResult { Data = new { status = status, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }
        /// <summary>
        /// View to edit Schedule and its details
        /// </summary>
        /// <param name="id">Schedule Id</param>
        /// <returns></returns>
        public ActionResult ScheduleEdit(int? id)
        {
            this.ViewBag.networkId = NetworkObjectId;
            int brandId = 0;
            string parentsBreadCrum = string.Empty;
            var network = _schService.GetNetworkObject(NetworkObjectId, out brandId, out parentsBreadCrum);
            //Fetch NetworkName tto display in the View
            this.ViewBag.networkname = network.Name;
            this.ViewBag.parentsBreadCrum = parentsBreadCrum;

            var statusMsg = TempData.ContainsKey("schstatusMsg") ? Convert.ToString(TempData["schstatusMsg"]) : null;
            if (!string.IsNullOrWhiteSpace(statusMsg))
            {
                this.ViewBag.statusMessage = statusMsg;
            }
            else
            {
                this.ViewBag.statusMessage = string.Empty;
            }
            var model = new ScheduleModel();
            model.IsSchNameEditable = true;
            if (id.HasValue && id != 0)
            {
                model = _schService.GetSchedule(id.Value, NetworkObjectId);
            }
            //Always indicate that Schedule should be saved befor opening create/edit mode
            model.SaveSchedule = true;
            //Get the Summary of SchDetails explictily 
            model.SchSummary = _schService.GetScheduleDetailSummary(id.Value, NetworkObjectId);
            return View(model);
        }

        /// <summary>
        /// Post Method to Save the edits of Schedule
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult ScheduleEdit(ScheduleModel model)
        {
            _schService.NetworkObjectId = Convert.ToInt32(Request.Form["networkId"]);

            TempData["schstatusMsg"] = null;
            //Re assign the viewbag properties to show them after Save
            this.ViewBag.networkId = _schService.NetworkObjectId;
            int brandId = 0;
            string parentsBreadCrum = string.Empty;
            var network = _schService.GetNetworkObject(_schService.NetworkObjectId, out brandId, out parentsBreadCrum);
            //Fetch NetworkName tto display in the View
            this.ViewBag.networkname = network.Name;
            this.ViewBag.parentsBreadCrum = parentsBreadCrum;
            this.ViewBag.statusMessage = string.Empty;
            //Save the schedule only when "Save" is clicked else just rebind the View
            if (model.SaveSchedule)
            {
                model = _schService.SaveSchedule(model);
                this.ViewBag.statusMessage = _schService.LastActionResult;
            }
            if (_schService.LastActionResult !=null && !_schService.LastActionResult.Contains("failed"))
            {
                TempData["schstatusMsg"] = _schService.LastActionResult;
                return RedirectToAction("ScheduleEdit", "Schedule", new { id = model.ScheduleId, netId = this.ViewBag.networkId });
            }
            else
            {
                return View(model);
            }
        }

        /// <summary>
        /// manage cycels view to edit/delete the cycles in schedule
        /// </summary>
        /// <returns></returns>
        public ActionResult ManageCycles()
        {
            return View();
        }

        /// <summary>
        /// Grid Method of Update a Cycle
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult UpdateCycle(SchCycleModel model)
        {
            ModelState.Remove("SchCycleId");
            ModelState.Remove("SortOrder");
            if (ModelState.IsValid)
            {
                model = _schService.UpdateCycle(model);

                if (_schService.LastActionResult.Contains("failed"))
                {
                    ModelState.AddModelError("CycleName", _schService.LastActionResult);
                    return Json(ModelState.ToDataSourceResult());
                }
                else
                {
                    return Json(model);
                }
            }
            else
            {
                return Json(ModelState.ToDataSourceResult());
            }
        }

        /// <summary>
        /// Custom Grid method to delete/disable Cycle
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DestroyCycle(string model, int netId)
        {
           System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
           SchCycleModel schCycleModel = js.Deserialize<SchCycleModel>(model);
           var msg = string.Empty;
           if (schCycleModel != null)
           {
               _schService.DestroyCycle(schCycleModel, netId);
               msg = _schService.LastActionResult;
           }
           else
           {
               msg = "Operation failed.";
           }
           return new JsonResult { Data = new { status = !string.IsNullOrWhiteSpace(msg) && msg.Contains("failed") ? false : true, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
       
        }

        /// <summary>
        /// Disable a cycle
        /// </summary>
        /// <param name="model"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult DisableCycle(string model, int netId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            SchCycleModel schCycleModel = js.Deserialize<SchCycleModel>(model);
            var msg = string.Empty;
            if (schCycleModel != null)
            {
                _schService.DisableCycle(schCycleModel, netId);
                msg = _schService.LastActionResult;
            }
            else
            {
                msg = "Operation failed.";
            }
            return new JsonResult { Data = new { status = !string.IsNullOrWhiteSpace(msg) && msg.Contains("failed") ? false : true, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }

        /// <summary>
        /// Custom Grid method to enable Cycle
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult EnableCycle(string model, int netId)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            SchCycleModel schCycleModel = js.Deserialize<SchCycleModel>(model);
            var msg = string.Empty;
            if (schCycleModel != null)
            {
                _schService.EnableCycle(schCycleModel, netId);
                msg = _schService.LastActionResult;
            }
            else
            {
                msg = "Operation failed.";
            }
            return new JsonResult { Data = new { status = !string.IsNullOrWhiteSpace(msg) && msg.Contains("failed") ? false : true, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }

        /// <summary>
        /// Custom Grid method to enable Cycle - For future use if necessary
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult MoveCycle(string model, int netId, int newSortOrder, int oldSortOrder)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            SchCycleModel schCycleModel = js.Deserialize<SchCycleModel>(model);
            var msg = string.Empty;
            if (schCycleModel != null)
            {
                _schService.MoveCycle(schCycleModel, netId, newSortOrder, oldSortOrder);
                msg = _schService.LastActionResult;
            }
            else
            {
                msg = "Operation failed.";
            }
            return new JsonResult { Data = new { status = !string.IsNullOrWhiteSpace(msg) && msg.Contains("failed") ? false : true, lastActionResult = msg }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };

        }

        /// <summary>
        /// Grid method to create a new Cycle
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Editor,Administrator,SuperAdministrator")]
        public ActionResult CreateCycle(SchCycleModel model)
        {
            ModelState.Remove("SchCycleId");
            ModelState.Remove("SortOrder");
            if (ModelState.IsValid)
            {
                model = _schService.CreateCycle(model);
                if (_schService.LastActionResult.Contains("failed"))
                {
                    ModelState.Clear();
                    ModelState.AddModelError("CycleName", _schService.LastActionResult);
                    return Json(ModelState.ToDataSourceResult());
                }
                else
                {
                    return Json(model);
                }
            }
            else
            {
                return Json(ModelState.ToDataSourceResult());
            }
        }
    }
}
