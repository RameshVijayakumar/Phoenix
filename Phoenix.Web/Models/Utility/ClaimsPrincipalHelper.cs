using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models.Utility
{
    public class ClaimPrincipalHelper
    {
        /// <summary>
        /// get user name like abc@zioskconnect.com if it exists. Else, return "server"
        /// </summary>
        /// <param name="currentClaimsPrincipal"></param>
        /// <returns></returns>
        public static string GetLoggedInUserName(System.Security.Claims.ClaimsPrincipal currentClaimsPrincipal)
        {
            string userName = "Unknown";

            if (currentClaimsPrincipal.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name) != null)
            {
                if (!string.IsNullOrEmpty(currentClaimsPrincipal.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name).Value))
                {
                    userName = currentClaimsPrincipal.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name).Value;
                }
            }

            return userName;
        }
    }
}