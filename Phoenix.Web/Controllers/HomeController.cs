using Phoenix.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IdentityModel.Services;
using System.IdentityModel.Services.Configuration;
using System.Data.Entity;
using System.Collections.Specialized;
using System.Web.Configuration;
using System.Security.Claims;
using Phoenix.RuleEngine;
using Phoenix.DataAccess;

namespace Phoenix.Web.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private AccountService _accountService;

        public HomeController()
        {
            _accountService = new AccountService();
        }
        public ActionResult Index()
        {
            
            if (User.Identity.IsAuthenticated == false)
            {
                ViewBag.Message = "Please log in to use this site";
            }
            else
            {
                ViewBag.Message = "Welcome to Menu Management";
                this.HttpContext.Session[Constants.SessionValue.CURRENT_USER_NAME] = User.Identity.Name;
                wAADLogin();
            }

            return View();
        }

        public ActionResult ErrorTest()
        {
            throw new Exception("TEST: Someting went wrong!");
        }
        /// <summary>
        /// Check the user access level and authorization level
        /// </summary>
        private void wAADLogin()
        {

            var userName = User.Identity.Name;

            ////Remove old roles if any
            //foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            //{
            //    if (existingClaim.Type == ClaimTypes.Role)
            //    {
            //        ((ClaimsIdentity)User.Identity).RemoveClaim(existingClaim);
            //    }

            //}

            var user = new Phoenix.RuleEngine.UserInfo();
            if (_accountService.GetUserDetails(userName, out user))
            {

                // Check if user already exists
                if (user != null && user.IsAuthorized == true)
                {
                    this.HttpContext.Session["ProductMasterSessionCheck"] = true;

                    //var permissions = PermissionFactory.GetUserAccess(this.HttpContext, userName, true);
                    //setIsBrandLevelAdmin(permissions);
                }
                else
                {
                    //ClaimsPrincipal cp = ClaimsPrincipal.Current;
                    //string fullname = string.Format("{0} {1}", cp.FindFirst(ClaimTypes.GivenName).Value,
                    //cp.FindFirst(ClaimTypes.Surname).Value);

                    this.HttpContext.Session["ProductMasterSessionCheck"] = true;

                    ViewBag.Message = "You are not Authorized.";
                }

                ////Get role assignments
                //foreach (string role in user.Roles)
                //{
                //    //Store the user's application roles as claims of type Role
                //    ((ClaimsIdentity)User.Identity).AddClaim(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "Phoenix.Web"));
                //}

                foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
                {
                    if (existingClaim.Type == Constants.ZioskClaimTypes.IsBrandLevelAdmin)
                    {
                        AdminService.IsBrandLevelAdmin = Convert.ToBoolean(existingClaim.Value);
                    }

                    if (existingClaim.Type == Constants.ZioskClaimTypes.HighestLevelAccess)
                    {
                        AdminService.HighestLevelAccess = (NetworkObjectTypes)Enum.Parse(typeof(NetworkObjectTypes), existingClaim.Value);
                    }

                }
            }
            else
            {
                ViewBag.Message = string.Empty;
                ViewBag.ErrorMessage = "Error occured while Authorizing";
            }
            
        }

        ///// <summary>
        ///// Check the whether the user is brand levelAdmin or not
        ///// </summary>
        ///// <param name="permissions"></param>
        //private void setIsBrandLevelAdmin(List<NetworkPermissions> permissions)
        //{
        //    AdminService.IsBrandLevelAdmin = false;
        //    AdminService.HighestLevelAccess = Phoenix.DataAccess.NetworkObjectTypes.Site;
        //    if (permissions != null && permissions.Any())
        //    {
        //        Phoenix.DataAccess.NetworkObjectTypes highestType = NetworkItemHelper.GetNetworkObjectType(permissions.FirstOrDefault().NetworkType);
        //        if (highestType == Phoenix.DataAccess.NetworkObjectTypes.Brand || highestType == Phoenix.DataAccess.NetworkObjectTypes.Root)
        //        {
        //            AdminService.IsBrandLevelAdmin = true;
        //        }
        //        AdminService.HighestLevelAccess = highestType;
        //    }

        //    ((ClaimsIdentity)User.Identity).AddClaim(new Claim(Constants.ZioskClaimTypes.IsBrandLevelAdmin, AdminService.IsBrandLevelAdmin.ToString(), ClaimValueTypes.Boolean, "Phoenix.Web"));
        //    ((ClaimsIdentity)User.Identity).AddClaim(new Claim(Constants.ZioskClaimTypes.HighestLevelAccess, AdminService.HighestLevelAccess.ToString(), ClaimValueTypes.Boolean, "Phoenix.Web"));
        //}

    }
}
