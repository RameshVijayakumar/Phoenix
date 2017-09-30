using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Phoenix.Web.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Phoenix.Web.Models.Grid;
using Phoenix.DataAccess;
using SnowMaker;
using Newtonsoft.Json;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class SiteController : PMBaseController
    {
        public SiteService _siteService { get { return base.service as SiteService; } }

        public SiteController(SiteService siteService)
            : base(siteService)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="SiteId"></param>
        ///// <returns></returns>
        //[CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        //public ActionResult Edit(string SiteId)
        //{
        //    SiteModel model = null;
        //    try
        //    {
        //        model = _siteService.GetSiteDetails(SiteId);
        //    }
        //    catch
        //    {
        //        // write error
        //    }

        //    return View("SiteDetail", model);
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult SiteDetail()
        {
            var model = new SiteModel();   
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult SiteEdit(long Id)
        {
            var statusMsg = TempData.ContainsKey("siteeditstatusMsg") ? Convert.ToString(TempData["siteeditstatusMsg"]) : null;
            if (!string.IsNullOrWhiteSpace(statusMsg))
            {
                this.ViewBag.statusMessage = statusMsg;
            }
            else
            {
                this.ViewBag.statusMessage = string.Empty;
            }
            TempData["siteeditstatusMsg"] = null;
            var model = _siteService.GetSiteDetails(Id);
            ViewBag.Cuisines = _siteService.GetAllCuisines();
            ViewBag.AllServices = _siteService.AllServices();
            ViewBag.AllServicesObj = JsonConvert.SerializeObject(ViewBag.AllServices);
            return View(model);
        }

        /// <summary>
        /// Get all System TimeZones
        /// </summary>
        /// <returns></returns>
        public ActionResult  GetTimeZones()
        {
            var list = TimeZoneInfo.GetSystemTimeZones().Select(t => new SelectListItem
            {
                Text = t.DisplayName,
                Value = t.Id
            }).ToList();

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Update Site
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult SiteEdit(SiteModel model)
        {
            TempData["siteeditstatusMsg"] = null;
            var actionStatus = false;
            ViewBag.Cuisines = _siteService.GetAllCuisines();
            ViewBag.AllServices = _siteService.AllServices();
            ViewBag.AllServicesObj = JsonConvert.SerializeObject(ViewBag.AllServices);
            try
            {
                ModelState.Clear();
                model.NetworkObject.Name = model.Name;
                //model.NetworkObject.ParentNetworkObjectId = model.ParentNetworkObjectId;
                model.SiteTimeZone = TimeZoneInfo.FindSystemTimeZoneById(model.SiteTimeZoneId);
                if (!TryValidateModel(model))
                {
                    //return new JsonResult { Data = new { Tag = "ValidationError", State = ModelState.SelectMany(x => x.Value.Errors.Select(z => z.ErrorMessage)) }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
                    return View(model);
                }

                if (model.ServicesOffered.Any(x => x.ToDelete == false))
                {
                    var servicesAdded = model.ServicesOffered.Where(x => x.ToDelete == false).Select(x => x.ServiceTypeId).Count();
                    var distinctservicesAdded = model.ServicesOffered.Where(x => x.ToDelete == false).Select(x => x.ServiceTypeId).Distinct().Count();

                    if (servicesAdded != distinctservicesAdded)
                    {
                        ModelState.AddModelError("ServicesOffered", "Multiple occurance of Service Types noticed. Please add a service type only once.");
                        return View(model);
                    }
                }
                //if (model.SiteId == Guid.Empty)
                //{// REMOVED ADD FUNCTIONALITY SO this will not occur
                //    if (model.NetworkObject.ParentNetworkObjectId != null)
                //    {
                //        actionStatus = _siteService.AddSite(model);
                //        this.ViewBag.statusMessage = _siteService.LastActionResult;

                //        if (actionStatus)
                //        {
                //            TempData["siteeditstatusMsg"] = _siteService.LastActionResult;
                //            return RedirectToAction("SiteEdit", "Site", new { id = model.SiteId });
                //        }
                //    }
                //    else
                //    {
                //        ModelState.AddModelError("Add Site", "Please Select the Franchise to add site.");
                //    }
                //}
                //else
                //{
                model = _siteService.UpdateSite(model, out actionStatus);
                this.ViewBag.statusMessage = _siteService.LastActionResult;
                if (actionStatus)
                {
                    TempData["siteeditstatusMsg"] = _siteService.LastActionResult;
                    return RedirectToAction("SiteEdit", "Site", new { id =  model.IrisId });
                }
                //}

                return View(model);
            }
            catch
            {
                return View(model);
            }
        }

        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult DeleteSite(string siteId)
        {
            var actionStatus = _siteService.DeleteSite(siteId);
            return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _siteService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GroupDetail()
        {
            return View();
        }

        /// <summary>
        /// Update Group
        /// </summary>
        /// <param name="model"></param>
        /// <param name="networkItemId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult GroupDetail([DataSourceRequest] DataSourceRequest request, GroupModel model)
        {
            var actionStatus = false;
            try
            {

                if (model.NetowrkObjectId == 0)
                {
                    actionStatus = _siteService.AddGroup(model);
                    this.HttpContext.Session["CurrentUser"] = null;
                    _siteService.Permissions = PermissionFactory.GetUserAccess(this.HttpContext, null,  true);
                }
                else
                {
                    actionStatus = _siteService.UpdateGroup(model);
                    this.HttpContext.Session["CurrentUser"] = null;
                    _siteService.Permissions = PermissionFactory.GetUserAccess(this.HttpContext, null,  true);
                }
                if (actionStatus)
                {
                    ModelState.AddModelError("GroupName", _siteService.LastActionResult);
                }
                return Json(ModelState.IsValid ? model : ModelState.ToDataSourceResult());
            }
            catch
            {
                return View();
            }
        }

        /// <summary>
        /// Open Group for Edit
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult GroupEdit(int networkObjectId)
        {
            GroupModel model = null;
            try
            {
                model = _siteService.GetGroupDetails(networkObjectId);
            }
            catch
            {
                // write error
                return View("GroupDetail");
            }

            return View("GroupDetail", model);
        }

        /// <summary>
        /// Delete a Group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult DeleteGroup(int groupId, string groupName)
        {
            var actionStatus = _siteService.DeleteGroup(groupId, groupName);
            return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _siteService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Grid method to get all sites in network hirearchy
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetSiteList(int? networkObjectId, KendoGridRequest request)
        {
            if (networkObjectId.HasValue)
            {
                var resultCount = 0;
                var result = _siteService.GetSitelist(networkObjectId, out resultCount, request);
                return Json(new { data = result, total = resultCount }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { data = new List<SiteModel>(), total = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Grid Methos to get all groups in network hirearchy
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetGroups(int? networkObjectId)
        {
            var list = new List<GroupModel>();

            if (networkObjectId.HasValue)
            {
                list = _siteService.GetGroups(networkObjectId);
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Tree method to get network tree
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult NetworkObjectTreeView(int? Id, bool includeaccess, int networkObjectType = 0, int includeUntilNWType = 5, bool includefeatures = false)
        {
            var data = _siteService.GetNetworkObjectData(Id, includeaccess, (NetworkObjectTypes)networkObjectType, (NetworkObjectTypes)includeUntilNWType, includefeatures);

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        //[CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        //public ActionResult GetNetworkObjectDataAndSelectedUserAccess(int? Id, bool includeaccess, int networkObjectType = 0, int? userIdForAccess = null)
        //{
        //    var data = _siteService.GetNetworkObjectDataAndSelectedUserAccess(Id, includeaccess, (NetworkObjectTypes)networkObjectType, userIdForAccess);

        //    return Json(data, JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult NetworkObjectHierarchy()
        //{
        //    var data = _siteService.GetNetworkObjectHierarchy();
        //    return Json(data, JsonRequestBehavior.AllowGet);
        //}
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetGroupType()
        {
            var data = _siteService.GetNetworkTypes();

            return Json(data, JsonRequestBehavior.AllowGet);

        }

        /// <summary>
        /// Get only brand level network tree
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="networkObjectType"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult BrandLeveTree(int? Id, int networkObjectType = 0)
        {
            var data = _siteService.BrandLeveTree(Id, (NetworkObjectTypes)networkObjectType);

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        #region NetworkLevel Operations
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult SiteActions()
        {
            return View();
        }
        #endregion
        /// <summary>
        /// Get list of market for a site to be moved to 
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetMarketListForSite(int networkObjectId)
        {
            var list = _siteService.GetMarketListForSite(networkObjectId);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Assign a session value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SetSessionVariable(string key, string value)
        {
            bool hasActionFailed = false;
            var errorMessage = string.Empty;
            try
            {
                System.Web.HttpContext.Current.Session[key] = value;
            }
            catch (Exception ex)
            {
                hasActionFailed = true;
                errorMessage = ex.StackTrace;
            }
            return new JsonResult { Data = new { status = !hasActionFailed, errorMesssage = errorMessage }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
