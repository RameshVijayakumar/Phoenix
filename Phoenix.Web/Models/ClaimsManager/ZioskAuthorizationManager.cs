using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace Phoenix.Web.Models.ClaimsManager
{
    public class ZioskAuthorizationManager : ClaimsAuthorizationManager
    {
        public override bool CheckAccess(AuthorizationContext context)
        {
            bool retVal = false;

            string action = context.Action.First().Value;
            string resource = context.Resource.First().Value;

            ClaimsPrincipal cp = context.Principal;

            // unauthorized user?
            if (cp.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name) == null)
            {
                return false;
            }

            var userProfileString = cp.FindFirst(Constants.ZioskClaimTypes.Profile).Value;

            if (userProfileString == string.Empty)
            {
                // user does not registered in SSO Portal so he/she does not have profile yet
                return false;
            }

            Phoenix.RuleEngine.UserInfo userProfile = JsonConvert.DeserializeObject<Phoenix.RuleEngine.UserInfo>(userProfileString);

            #region 1/ Menu, Site, Schedule,Mapping

            // only authenticated user can access controllers

            string[] region1Resources = new string[] { "Menu", "Site", "Schedule", "Mapping" };

            if (action == "Show")
            {
                // if user has any UserProfile, then he is allowed to access 2 controllers
                string userName = cp.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name).Value;

                if (userProfile != null)
                {
                    if (userProfile.IsAuthorized) // active user
                        retVal = true;
                    else
                        retVal = false; // deleted user or inactive user
                }
                else
                {
                    retVal = false; // user not exist
                }

                return retVal;
            }

            #endregion 1/ Menu, Site, Schedule,Mapping

            #region 1/ MasterItems, POSItems, Admin

            // only authenticated user can access controllers

            string[] region2Resources = new string[] { "MasterItem", "POSItem", "Admin" };

            if (action == "Show" && region2Resources.Contains(resource))
            {
                // if user has any UserProfile, then he is allowed to access 2 controllers
                string userName = cp.FindFirst(System.IdentityModel.Claims.ClaimTypes.Name).Value;

                if (userProfile != null)
                {
                    if (userProfile.IsAuthorized) // active user
                        retVal = true;
                    else
                        retVal = false; // deleted user or inactive user
                }
                else
                {
                    retVal = false; // user not exist
                }

                return retVal;
            }

            #endregion 1/ Menu, Site, Schedule,Mapping


            return retVal;
        }
    }
}