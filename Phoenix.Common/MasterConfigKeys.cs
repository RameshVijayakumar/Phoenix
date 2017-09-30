using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.Common
{
    public class MasterConfigKeys
    {
        public const string ComponentPhoenix = "Phoenix";

        public const string LoginSSOAPIClientID = "Login.SSOAPI.ClientID";
        public const string LoginSSOAPIClientSecert = "Login.SSOAPI.ClientSecert";
        public const string LoginSSOAPITenant = "Login.SSOAPI.Tenant";
        public const string LoginSSOAPIResourceID = "Login.SSOAPI.ResourceID";
        public const string LoginSSOAPIBaseAddress = "Login.SSOAPI.BaseAddress";
        public const string LoginSSOAPIUserAccess = "Login.SSOAPI.UserAccess";
        public const string LoginSSOAPINetworks = "Login.SSOAPI.Networks";
        public const string LoginSSOAPIApplicationID = "Login.SSOAPI.ApplicationIrisId";

        public const string ComponentODSManager = "ODSManager";

        public const string POSMapMenuAPIClientID = "POSMap.MenuAPI.ClientID";
        public const string POSMapMenuAPIClientSecert = "POSMap.MenuAPI.ClientSecert";
        public const string POSMapMenuAPITenant = "POSMap.MenuAPI.Tenant";
        public const string POSMapMenuAPIResourceID = "POSMap.MenuAPI.ResourceID";
        public const string POSMapMenuAPIBaseAddress = "POSMap.MenuAPI.BaseAddress";
        public const string POSMapMenuAPIMapPart = "POSMap.MenuAPI.MapPart";
    }

    public class AzureConstants
    {
        public const string DiagnosticsConnectionString = "Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString";
    }
}
