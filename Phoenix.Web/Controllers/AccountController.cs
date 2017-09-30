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
using System.Net;
using Phoenix.Common;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using System.IdentityModel.Services;
using System.Text;
using System.IdentityModel.Services.Configuration;

namespace Phoenix.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        //
        // GET: /Account/KeepSessionAlive

        public ActionResult KeepSessionAlive()
        {
            return new JsonResult { Data = new { result = "Success" }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
