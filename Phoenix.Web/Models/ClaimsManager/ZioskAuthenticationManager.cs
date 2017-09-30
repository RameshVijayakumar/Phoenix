using Newtonsoft.Json;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using Phoenix.Web.Controllers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;

namespace Phoenix.Web.Models.ClaimsManager
{
    public class ZioskAuthenticationManager : ClaimsAuthenticationManager
    {
        private AccountService _accountService;
        public ZioskAuthenticationManager()
        {
            _accountService = new AccountService();
        }
        /// <summary>
        /// Build custom claims based on AD Azure incomingClaimsPrincipal
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="incomingClaimsPrincipal"></param>
        /// <returns></returns>
        public override ClaimsPrincipal Authenticate(string resourceName, ClaimsPrincipal incomingClaimsPrincipal)
        {
            if (!incomingClaimsPrincipal.Identity.IsAuthenticated)
            {
                return base.Authenticate(resourceName, incomingClaimsPrincipal);
            }

            var transformedPrincipal = this.CreateUserPrincipal(incomingClaimsPrincipal);
            this.CreateSession(transformedPrincipal);

            return transformedPrincipal;
        }

        private ClaimsPrincipal CreateUserPrincipal(ClaimsPrincipal incomingClaimsPrincipal)
        {
            List<Claim> claims = new List<Claim>();
            var userName = incomingClaimsPrincipal.Identity.Name;
            var incomingClaimsIdentity = (System.Security.Claims.ClaimsIdentity)incomingClaimsPrincipal.Identity;
            var firstName = incomingClaimsPrincipal.FindFirst(System.IdentityModel.Claims.ClaimTypes.GivenName).Value;
            var lastName = incomingClaimsPrincipal.FindFirst(System.IdentityModel.Claims.ClaimTypes.Surname).Value;


            claims.Add(new Claim(ClaimTypes.Name, userName));
            claims.Add(new Claim(ClaimTypes.GivenName, firstName));
            claims.Add(new Claim(ClaimTypes.Surname, lastName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userName));

            var user = new Phoenix.RuleEngine.UserInfo();
            var userProfileClaim = incomingClaimsPrincipal.FindFirst(Constants.ZioskClaimTypes.Profile);

            if (userProfileClaim == null || (userProfileClaim != null && string.IsNullOrWhiteSpace(userProfileClaim.Value)))
            {
                //Get User info from SSO
                _accountService.GetUserDetails(userName, out user);

                var permissions = PermissionFactory.GetUserAccess(null, userName, false);
                setIsBrandLevelAdmin(permissions);

                claims.Add(new Claim(Constants.ZioskClaimTypes.IsBrandLevelAdmin, AdminService.IsBrandLevelAdmin.ToString()));
                claims.Add(new Claim(Constants.ZioskClaimTypes.HighestLevelAccess, AdminService.HighestLevelAccess.ToString()));
            }
            else
            {
                //If claims already fetched from database
                user = JsonConvert.DeserializeObject<UserInfo>(userProfileClaim.Value);

            }



            // Check if user exists n authorized
            if (user != null && user.IsAuthorized == true)
            {
                claims.Add(new Claim(Constants.ZioskClaimTypes.Profile, JsonConvert.SerializeObject(user)));

                // roles
                if (user.Roles != null)
                {
                    //claims.Add(new Claim(Constants.ZioskClaimTypes.Roles, JsonConvert.SerializeObject(user.Roles)));

                    //Get role assignments
                    foreach (string role in user.Roles)
                    {
                        //Store the user's application roles as claims of type Role
                        claims.Add(new Claim(ClaimTypes.Role, role, ClaimValueTypes.String, "Phoenix.Web"));
                    }
                }
                else
                {
                    claims.Add(new Claim(Constants.ZioskClaimTypes.Roles, string.Empty));
                }

                //other permissions
                var brandLevelAdminClaim = incomingClaimsPrincipal.FindFirst(Constants.ZioskClaimTypes.IsBrandLevelAdmin);
                if(brandLevelAdminClaim != null && string.IsNullOrWhiteSpace(brandLevelAdminClaim.Value) == false)
                {
                    AdminService.IsBrandLevelAdmin = Convert.ToBoolean(brandLevelAdminClaim.Value);
                }

                var highestLevelAccessClaim = incomingClaimsPrincipal.FindFirst(Constants.ZioskClaimTypes.HighestLevelAccess);
                if (highestLevelAccessClaim != null && string.IsNullOrWhiteSpace(highestLevelAccessClaim.Value) == false)
                {
                    AdminService.HighestLevelAccess = (NetworkObjectTypes)Enum.Parse(typeof(NetworkObjectTypes), highestLevelAccessClaim.Value);
                }

            }
            else
            {
                // user does not have profile
                claims.Add(new Claim(Constants.ZioskClaimTypes.Profile, string.Empty));
            }
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "ZioskClaimsPrincipal"));
        }

        private void CreateSession(ClaimsPrincipal transformedPrincipal)
        {
            var sessionSecurityToken = new SessionSecurityToken(transformedPrincipal, TimeSpan.FromHours(8));

            if (FederatedAuthentication.SessionAuthenticationModule != null && FederatedAuthentication.SessionAuthenticationModule.ContainsSessionTokenCookie(HttpContext.Current.Request.Cookies))
            {
                return;
            }
            FederatedAuthentication.SessionAuthenticationModule.WriteSessionTokenToCookie(sessionSecurityToken);
            Thread.CurrentPrincipal = transformedPrincipal;
            FederatedAuthentication.SessionAuthenticationModule.SessionSecurityTokenReceived += SessionAuthenticationModule_SessionSecurityTokenReceived;
        }

        void SessionAuthenticationModule_SessionSecurityTokenReceived(object sender, SessionSecurityTokenReceivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("SessionAuthenticationModule_SessionSecurityTokenReceived");
        }

        /// <summary>
        /// Check the whether the user is brand levelAdmin or not
        /// </summary>
        /// <param name="permissions"></param>
        private void setIsBrandLevelAdmin(List<NetworkPermissions> permissions)
        {
            AdminService.IsBrandLevelAdmin = false;
            AdminService.HighestLevelAccess = Phoenix.DataAccess.NetworkObjectTypes.Site;
            if (permissions != null && permissions.Any())
            {
                Phoenix.DataAccess.NetworkObjectTypes highestType = NetworkItemHelper.GetNetworkObjectType(permissions.FirstOrDefault().NetworkType);
                if (highestType == Phoenix.DataAccess.NetworkObjectTypes.Brand || highestType == Phoenix.DataAccess.NetworkObjectTypes.Root)
                {
                    AdminService.IsBrandLevelAdmin = true;
                }
                AdminService.HighestLevelAccess = highestType;
            }
}
    }
}