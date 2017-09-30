using System;
using System.Collections.Generic;
using System.IdentityModel.Services;
using System.IdentityModel.Services.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Phoenix.Web.Controllers
{
    [AllowAnonymous]
    public class SignOutController : Controller
    {
        //
        // GET: /SignOut/

        public ActionResult Index()
        {
            return View();
        }

        public void SignOut()
        {
            this.HttpContext.Session["CurrentUserId"] = null;
            this.HttpContext.Session["CurrentUser"] = null;
            //Removes all the keys and values from the session-state collection
            Session.Clear();

            WsFederationConfiguration fc =
                   FederatedAuthentication.FederationConfiguration.WsFederationConfiguration;

            string request = System.Web.HttpContext.Current.Request.Url.ToString();
            string wreply = request.Substring(0, request.Length - 7);

            SignOutRequestMessage soMessage =
                            new SignOutRequestMessage(new Uri(fc.Issuer), wreply);
            soMessage.SetParameter("wtrealm", fc.Realm);

            FederatedAuthentication.SessionAuthenticationModule.SignOut();
            FederatedAuthentication.WSFederationAuthenticationModule.SignOut(); // to signout from all applications

            Response.Redirect(soMessage.WriteQueryString());
        } 
    }
}
