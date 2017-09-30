using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Phoenix.Web.Filters;
using Phoenix.Web.Models;
using System.Data.Entity;
using Phoenix.DataAccess;
using System.Collections.Specialized;
using System.Web.Configuration;
using Phoenix.Web.Models.Grid;


namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
    public class AdminController : PMBaseController
    {
        public AdminService _accountService { get { return base.service as AdminService; } }
        public LogService _logService;
        private IAuditLogService _auditLogService;

        public AdminController()
            : base(new AdminService())
        {
            _logService = new LogService();
            _auditLogService = new AuditLogService();
        }

        #region Admin Screen

        public ActionResult AuditLog()
        {
            return View();
        }

        public ActionResult AppLog()
        {
            return View();
        }

        public JsonResult GetAuditLogs(string noofDays, KendoGridRequest grdRequest)
        {
            //Get the Roles for the selected User
            //var data = _accountService.GetAuditLogs(id);
            var resultCount = 0;
            var result = _auditLogService.GetAll(noofDays, out resultCount, grdRequest);
            return Json(new { data = result, total = resultCount }, JsonRequestBehavior.AllowGet);           
        }

        public JsonResult GetWADLogs(int? id)
        {
            var data = _logService.GetWADLogs();
            return Json(data, JsonRequestBehavior.AllowGet);

        }

        #endregion

    }


}

