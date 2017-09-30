using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.Common
{
    public class AppConfiguration
    {
        public string ApiBaseUrl { get; set; }
        public string ApiClientID { get; set; }
        public string ApiClientSecert { get; set; }
        public string ApiTenant { get; set; }
        public string ApiResourceID { get; set; }
        public string ApplicationIrisId { get; set; }

        /// <summary>
        /// Reads azure table for configuration values and load them into member variables
        /// </summary>
        /// <returns>boolean status</returns>
        public bool LoadSSOConfig(string azureConnectionstring)
        {
            bool retVal = false;
            ConfigManager cfg = new ConfigManager();
            // read the azure config table
            if (cfg.Init(azureConnectionstring, MasterConfigKeys.ComponentPhoenix))
            {
                // load variables and validate

                string baseUrl = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIBaseAddress];
                string apiClientID = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIClientID];
                string apiClientSecert = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIClientSecert];
                string apiTenant = cfg.AppSettings[MasterConfigKeys.LoginSSOAPITenant];
                string apiResourceID = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIResourceID];
                string userpart = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIUserAccess];
                string networkspart = cfg.AppSettings[MasterConfigKeys.LoginSSOAPINetworks];
                string applicationId = cfg.AppSettings[MasterConfigKeys.LoginSSOAPIApplicationID];



                if (!string.IsNullOrEmpty(apiClientID) && !string.IsNullOrEmpty(apiClientSecert) && !string.IsNullOrEmpty(apiTenant) && !string.IsNullOrEmpty(apiResourceID))
                {
                    ApiBaseUrl = baseUrl;

                    ApiClientID = apiClientID;
                    ApiClientSecert = apiClientSecert;
                    ApiTenant = apiTenant;
                    ApiResourceID = apiResourceID;
                    ApplicationIrisId = applicationId;
                    retVal = true;
                }

            }
            return retVal;
        }

        /// <summary>
        /// Reads azure table for configuration values and load them into member variables
        /// </summary>
        /// <returns>boolean status</returns>
        public bool LoadMenuAPIConfig(string azureConnectionstring)
        {
            bool retVal = false;
            ConfigManager cfg = new ConfigManager();
            // read the azure config table
            if (cfg.Init(azureConnectionstring, MasterConfigKeys.ComponentODSManager))
            {
                // load variables and validate

                string baseUrl = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPIBaseAddress];
                string apiClientID = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPIClientID];
                string apiClientSecert = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPIClientSecert];
                string apiTenant = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPITenant];
                string apiResourceID = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPIResourceID];
                string userpart = cfg.AppSettings[MasterConfigKeys.POSMapMenuAPIMapPart];



                if (!string.IsNullOrEmpty(apiClientID) && !string.IsNullOrEmpty(apiClientSecert) && !string.IsNullOrEmpty(apiTenant) && !string.IsNullOrEmpty(apiResourceID))
                {
                    ApiBaseUrl = baseUrl;

                    ApiClientID = apiClientID;
                    ApiClientSecert = apiClientSecert;
                    ApiTenant = apiTenant;
                    ApiResourceID = apiResourceID;
                    retVal = true;
                }

            }
            return retVal;
        }
    }
}
