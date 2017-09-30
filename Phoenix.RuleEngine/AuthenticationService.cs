using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Phoenix.RuleEngine
{
    public class AuthenticationService
    { //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used by the application to authenticate to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenant;
        private static string clientId;
        private static string appKey;
        private long applicationIrisId = 0;
        private const int MAX_RETRIES = 3;
        private const int API_RETRY_SLEEP_TIME = 1000; //(in milliseconds)

        //
        // To authenticate to the SSO, the client needs to know the service's App ID URI.
        // To contact the SSO we need it's URL as well.
        //
        private static string ssoResourceId;
        private static string ssoBaseAddress;

        private static HttpClient httpClient = new HttpClient();
        private static AuthenticationContext authContext = null;
        private static ClientCredential clientCredential = null;

        private string _lastActionResult;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }


        public AuthenticationService(AppConfiguration config)
        {

            tenant = config.ApiTenant;
            clientId = config.ApiClientID;
            appKey = config.ApiClientSecert;
            ssoResourceId = config.ApiResourceID;
            ssoBaseAddress = config.ApiBaseUrl;

            long.TryParse(config.ApplicationIrisId, out applicationIrisId);

            string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);

            authContext = new AuthenticationContext(authority);
            clientCredential = new ClientCredential(clientId, appKey);
        }

        /// <summary>
        /// Get user details from SSO
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="applicationId"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool GetUserDetails(string userName, out UserInfo user)
        {
            user = new UserInfo();
            bool retVal = false;
            try
            {
                var retryCount = 1;
                var token = GetAccessToken();

                // Add the access token to the authorization header of the request.
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                while (retryCount <= MAX_RETRIES)
                {
                    //Send GET Request
                    HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}/users/{1}/apps/{2}", ssoBaseAddress, userName, applicationIrisId)).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response to get user.
                        string s = response.Content.ReadAsStringAsync().Result;
                        user = JsonConvert.DeserializeObject<UserInfo>(s);
                        retVal = true;
                        break; //breaks while
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        var result = response.Content.ReadAsStringAsync().Result;
                        _lastActionResult = JsonConvert.DeserializeObject<ResponseMessage>(result).Message;
                        retVal = true;
                        break; //breaks while
                    }
                    else
                    {
                        _lastActionResult = string.Format("Failed to retrieve user details. Error:  {0}", response.ReasonPhrase);
                        Logger.WriteError(string.Format(" {0}. Retry Count {1}", _lastActionResult, retryCount));
                        System.Threading.Thread.Sleep(API_RETRY_SLEEP_TIME);
                        retryCount++;
                    }
                }
            }
            catch (Exception ex)
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
        public List<NetworkPermissions> GetUserNetworkAccess(string userName, out HttpStatusCode status)
        {
            var networks = new List<NetworkPermissions>();
            status = HttpStatusCode.OK;
            try
            {
                var retryCount = 1;
                var token = GetAccessToken();

                // Add the access token to the authorization header of the request.
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
                while (retryCount <= MAX_RETRIES)
                {
                    //Send GET request
                    HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}/users/{1}/apps/{2}/networks", ssoBaseAddress, userName, applicationIrisId)).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response to get Access levels.
                        string s = response.Content.ReadAsStringAsync().Result;
                        networks = JsonConvert.DeserializeObject<List<NetworkPermissions>>(s);
                        status = HttpStatusCode.OK;
                        break; //breaks while
                    }
                    else
                    {
                        _lastActionResult = string.Format("Failed to retrieve user network access. Error:  {0}", response.ReasonPhrase);
                        status = response.StatusCode;
                        Logger.WriteError(string.Format(" {0}. Retry Count {1}", _lastActionResult, retryCount));
                        System.Threading.Thread.Sleep(API_RETRY_SLEEP_TIME);
                        retryCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                status = HttpStatusCode.InternalServerError;
                Logger.WriteError(ex);
            }
            return networks;
        }

        /// <summary>
        /// Get an access token from Azure AD using client credentials.
        /// </summary>
        /// <returns></returns>
        public AuthenticationResult GetAccessToken()
        {

            AuthenticationResult result = null;
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    result = authContext.AcquireToken(ssoResourceId, clientCredential);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    _lastActionResult = (
                        String.Format("An error occurred while acquiring a token. Time: {0}, Error: {1}, Retry: {2}",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString()));
                    Logger.WriteError(_lastActionResult);
                }

            } while ((retry == true) && (retryCount < 3));

            return result;
        }


        /// <summary>
        /// Check if the given network is accessible by the user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="networkIrisId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool IsNetworkAccessible(string username, long networkIrisId, out HttpStatusCode status)
        {
            var retval = false;
            status = HttpStatusCode.OK;
            try
            {
                var networks = GetUserNetworkAccess(username, out status);
                if (status != HttpStatusCode.InternalServerError)
                {
                    if (networks != null && networks.Any())
                    {
                        var network = networks.Where(x => x.Id == networkIrisId).FirstOrDefault();
                        retval = network != null && network.HasAccess;
                    }
                    if (retval == false)
                    {
                        status = HttpStatusCode.Forbidden;
                        _lastActionResult = "Access to this resource is forbidden";
                    }
                    else
                    {
                        status = HttpStatusCode.OK;
                    }
                }
                else
                {
                    _lastActionResult = string.Format("Failed to retrieve site access.");
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format("Unable to fetch access level for given site");
                Logger.WriteError(ex);
                status = HttpStatusCode.InternalServerError;
            }
            return retval;
        }

        /// <summary>
        /// Get all networks accessible of given type
        /// </summary>
        /// <param name="username"></param>
        /// <param name="networkType"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public List<NetworkPermissions> GetAllAccessibleNetwork(string username, NetworkObjectTypes networkType, out HttpStatusCode status)
        {
            var accessibleNetworks = new List<NetworkPermissions>();
            status = HttpStatusCode.OK;
            try
            {
                var networks = GetUserNetworkAccess(username, out status);
                if (status != HttpStatusCode.InternalServerError)
                {
                    if (networks != null && networks.Any())
                    {
                        accessibleNetworks = networks.Where(x => x.NetworkType.Equals(networkType.ToString(), StringComparison.InvariantCultureIgnoreCase)).ToList();
                    }
                    if (accessibleNetworks.Any() == false)
                    {
                        status = HttpStatusCode.Forbidden;
                        _lastActionResult = "Access to this resource is forbidden";
                    }
                    else
                    {
                        status = HttpStatusCode.OK;
                    }
                }
                else
                {
                    _lastActionResult = string.Format("Failed to retrieve site access.");
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format("Unable to fetch access level for given site");
                Logger.WriteError(ex);
                status = HttpStatusCode.InternalServerError;
            }
            return accessibleNetworks;
        }

        /// <summary>
        /// Get All Clients
        /// </summary>
        /// <param name="username"></param>
        /// <param name="networkType"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public List<UserInfo> GetAllAccessibleClients(out HttpStatusCode status)
        {
            var clients = new List<UserInfo>();
            status = HttpStatusCode.OK;
            try
            {
                var token = GetAccessToken();

                // Add the access token to the authorization header of the request.
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

                //Send GET Request
                HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}/clients/all/apps/{1}", ssoBaseAddress, applicationIrisId)).Result;

                if (response.IsSuccessStatusCode)
                {
                    // Read the response to get user.
                    string s = response.Content.ReadAsStringAsync().Result;
                    clients = JsonConvert.DeserializeObject<List<UserInfo>>(s);
                    status = response.StatusCode;
                }
                else if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    _lastActionResult = JsonConvert.DeserializeObject<ResponseMessage>(result).Message;
                    status = response.StatusCode;
                }
                else
                {
                    _lastActionResult = string.Format("Failed to retrieve targets. Error:  {0}", response.ReasonPhrase);
                    Logger.WriteError(_lastActionResult);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format("Unable to fetch targets");
                Logger.WriteError(ex);
                status = HttpStatusCode.InternalServerError;
            }
            return clients;
        }
    }
}
