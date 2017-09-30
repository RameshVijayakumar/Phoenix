using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class MenuFeaturesController : Controller
    {
        //
        // GET: /MenuFeatures/

        public ActionResult Index()
        {
            return View();
        }
        
    }
}
