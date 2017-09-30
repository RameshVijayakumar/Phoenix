using Microsoft.WindowsAzure;
using Phoenix.Common;
using Phoenix.RuleEngine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class AccountService
    {
        private AppConfiguration _config;
        private AuthenticationService _authenticationService;

        private string _lastActionResult;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        public AccountService()
        {
            _config = new AppConfiguration();
            if (_config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
            {
                _authenticationService = new AuthenticationService(_config);
            }
        }

        /// <summary>
        /// Get user details from SSO
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="applicationId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool GetUserDetails(string userName, out RuleEngine.UserInfo user)
        {
            user = new RuleEngine.UserInfo();
            bool retVal = false;
            try
            {
                retVal = _authenticationService.GetUserDetails(userName, out user);
            }
            catch(Exception ex)
            {
                Logger.WriteError(ex);
            }
            return retVal;
        }

        /// <summary>
        /// Get User Network access
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<NetworkPermissions> GetUserNetworkAccess(string userName)
        {
            var networks = new List<NetworkPermissions>();
            try
            {
                var status = System.Net.HttpStatusCode.OK;
                networks = _authenticationService.GetUserNetworkAccess(userName, out status);                
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return networks;
        }
    }
}