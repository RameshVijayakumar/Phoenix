using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Phoenix.Web.Models;
using Phoenix.RuleEngine;
using System.Security.Claims;
using System.Web.Script.Serialization;

namespace Phoenix.Web.Controllers
{
     public class PMBaseController : Controller
    {
        public IUserPermissions service { get; set; }

        public PMBaseController()
        {
           
        }

        public PMBaseController(IUserPermissions _service)
        {
            service = _service;
        }

        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (service != null)
            {
                service.Permissions = PermissionFactory.GetUserAccess(requestContext.HttpContext, null, true);
            }
        }


        public ActionResult SerializeToMax(JsonResult jsonResult)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = Int32.MaxValue, RecursionLimit = 100 };

            return new ContentResult()
            {
                Content = serializer.Serialize(jsonResult.Data),
                ContentType = "application/json",
            };
        }
    }


    //NOTE: Could not include this method in the Base Controller as AccountController would be required to have 
    // the initialize and therefore the service
    public static class PermissionFactory
    {
        private static string CacheKeyPrefix = "PMCache_";
        public static void RemoveUserAccess(HttpContextBase context, int? userId, string ProviderUserId, bool LoggedUser)
        {
            if (context == null)
            {
                return ;
            }
            string cacheKey = CacheKeyPrefix + (LoggedUser ? "CurrentUser" : userId.ToString());
            if (context.Cache[cacheKey] != null)
            {
                context.Cache.Remove(cacheKey);
            }
        }

        //Pull UserPermissions from Cache
        //NOTE: Use cache since crosses all applications.
        public static List<NetworkPermissions> GetUserAccess(HttpContextBase context, string userId, bool LoggedUser)
        {
            IAdminService up = null;
            if ((LoggedUser == true && context == null) || (LoggedUser == false && string.IsNullOrWhiteSpace(userId)))
            {
                return null;
            }


            //Base Current User permission on Session
            if (LoggedUser)
            {
                if (context.Session["CurrentUser"] == null)
                {
                    if (userId == null && context.Session["CurrentUserId"]!=null)
                    {
                        userId = (string)context.Session["CurrentUserId"];
                    }
                    if (userId == null )
                    {
                        userId = System.Security.Claims.ClaimsPrincipal.Current.Identity.Name;

                        if (userId == null)
                        {
                            return null;
                        }
                    }

                    up = new AdminService();

                    //Need to keep the UserId for times when the current user is modifying his access and we need to look it up again.
                    context.Session["CurrentUserId"] = up.LoadAccess(userId);
                    context.Session["CurrentUser"] = (up as IUserPermissions).Permissions;
                }
                return context.Session["CurrentUser"] as List<NetworkPermissions>;
            }

            //Use Cache to cross application for editing other Users Permissions
            //string cacheKey = CacheKeyPrefix + userId.ToString();
            //if (context.Cache[cacheKey] == null)
            //{
            //    if (userId == null && ProviderUserId ==null)
            //    {
            //        return null;
            //    }

            //    IAdminService up = new AdminService();
            //    up.LoadAccess(userId, ProviderUserId);
            //    context.Cache.Insert(cacheKey, (up as IUserPermissions).Permissions, null, DateTime.Now.AddMinutes(5), System.Web.Caching.Cache.NoSlidingExpiration);
            //}
            //return context.Cache[cacheKey] as List<Phoenix.DataAccess.usp_GetUserPermissions_Result>;
            if (userId == null)
            {
                return null;
            }

            up = new AdminService();
            up.LoadAccess(userId);
            return (up as IUserPermissions).Permissions;
        }
    }
}
