using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using System.IdentityModel.Services;


namespace Phoenix.Web.Filters
{
    /// <summary>
    /// Create a custom error filter, inherit from the HandleErrorAttribute class and override
    /// the  OnException()  method
    /// </summary>
    public class CustomHandleError : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null)
                base.OnException(filterContext);

            // log the error with message id to track
            var messageId = Guid.NewGuid().ToString();
            Logger.WriteError(filterContext.Exception, messageId);

            // set the view to be sent back
            ViewResult result = new ViewResult { ViewName = "~/Views/Shared/Error.cshtml" };
            result.ViewBag.ShowReturnToLogin = false;
            if (filterContext.Exception is System.Data.SqlClient.SqlException)
            {
                result.ViewBag.Message = "A server error occurred which requires you to please ";
                result.ViewBag.ShowReturnToLogin = true;
                try
                {
                    FederatedAuthentication.SessionAuthenticationModule.SignOut();
                    FederatedAuthentication.WSFederationAuthenticationModule.SignOut(); // to signout from all applications
                }
                catch (Exception ex)
                {
                    var innerMessageId = Guid.NewGuid().ToString();
                    Logger.WriteError(ex, innerMessageId);
                }

            }
            else
                result.ViewBag.Message = "A server error occurred. Please contact the administrator with this id: " + messageId;

            filterContext.Result = result;
            if (filterContext.HttpContext.IsCustomErrorEnabled)
            {
                filterContext.ExceptionHandled = true;
                base.OnException(filterContext);
            }
        }
    }
}