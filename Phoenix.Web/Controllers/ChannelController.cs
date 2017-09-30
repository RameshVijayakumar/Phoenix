using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
    public class ChannelController : Controller
    {
        //
        // GET: /Channel/

        public ActionResult Index()
        {
            return View();
        }

    }
}
