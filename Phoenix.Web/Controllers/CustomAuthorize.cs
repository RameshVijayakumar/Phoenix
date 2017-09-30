using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;

namespace Phoenix.Web.Controllers
{
    /// <summary>
    /// Implements custom authorization service
    /// </summary>
    public class CustomAuthorize : AuthorizeAttribute
    {
        // for future use
        //IAuthorizationService _authService { get; set; }

        /// <summary>
        /// This method always returns true in debug mode.
        /// It is overridden to enable code debugging without going through authorization services
        /// </summary>
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            bool retVal = false;
            try
            {
//#if DEBUG
//                retVal = true;
//#else
                //return _authService.Authorize(httpContext);
                retVal = base.AuthorizeCore(httpContext);
//#endif
            }
            catch (Exception ex)
            {
                
                var msg = string.Format("Caller:{0}, IsAuthenticated:{1}, URL:{2}", 
                    new StackFrame(1).GetMethod().Name,
                    httpContext.Request.IsAuthenticated,
                    httpContext.Request.Url);
                
                Logger.WriteError(msg, ex);
                
            }
            return retVal;
        }

    }
}