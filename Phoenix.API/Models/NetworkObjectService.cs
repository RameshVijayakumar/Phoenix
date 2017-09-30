using Phoenix.API;
using Phoenix.DataAccess;
using Phoenix.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using Phoenix.RuleEngine;
using Microsoft.WindowsAzure;
using System.Diagnostics;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Class where business layer for SitesController resides.
    /// </summary>
    public class NetworkObjectService : INetworkObjectService
    {
        private IRepository _repository;
        private DbContext _context;

        private AppConfiguration _config;
        private AuthenticationService _authenticationService;
        private RuleService _ruleService;

        private string _lastActionResult;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        private string _clientId;
        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }
        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public NetworkObjectService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _config = new AppConfiguration();
            if (_config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
            {
                _authenticationService = new AuthenticationService(_config);
            }
            _ruleService = new RuleService(RuleService.CallType.API);
        }

        /// <summary>
        /// Get site Detailsin a Brand
        /// </summary>
        /// <param name="brandId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public SiteListModel GetAllSitesInBrand(long brandId, out HttpStatusCode status)
        {
            var returnModel = new SiteListModel();
            status = HttpStatusCode.OK;
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                List<SiteInfoModel> siteList = null;
                List<SiteModelMenu> menus = null;
                SiteInfoModel siteInfoModel = null;

                long brand = 0;
                if (long.TryParse(brandId.ToString(), out brand))
                {
                    var networkItem = _repository.GetQuery<NetworkObject>(x => x.IrisId == brand && x.NetworkObjectTypeId == NetworkObjectTypes.Brand && x.IsActive).FirstOrDefault();
                    if (networkItem != null)
                    {
                        if (_authenticationService.IsNetworkAccessible(ClientID, brand,out status))
                        {
                            Logger.WriteInfo(string.Format("Time taken - To check IsNetworkAccessible - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
                            // change command timeout for this statement
                            ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_context).ObjectContext.CommandTimeout = 120;
                            sw = Stopwatch.StartNew();

                            //get Target Channels
                            var clientTags = getTargetClientChannels();

                            var allSiteMenusInfo = (_context as ProductMasterContext).uspGetAllSiteMenusInfoInBrand(brandId).ToList();
                            //var allSiteServicesOffered = _repository.GetQuery<SerivceNetworkObjectLink>().Include("ServiceType").ToList();
                            if (allSiteMenusInfo != null && allSiteMenusInfo.Count() > 0)
                            {
                                siteList = new List<SiteInfoModel>();

                                var siteMenusGrps = allSiteMenusInfo.GroupBy(x => x.NetworkObjectId);
                                //foreach (var siteInfo in allSiteMenusInfo.Distinct())
                                foreach (var siteMenusGrp in siteMenusGrps.ToList())
                                {
                                    var siteInfo = siteMenusGrp.ToList();
                                    if (siteInfo != null && siteInfo.Any())
                                    {
                                        siteInfoModel = new SiteInfoModel();
                                        siteInfoModel.Id = siteInfo[0].NetworkObjectId.Value;
                                        siteInfoModel.SiteId = siteInfo[0].SiteId.ToString();
                                        siteInfoModel.StoreName = siteInfo[0].StoreName;
                                        siteInfoModel.StoreNumber = siteInfo[0].StoreNumber;
                                        siteInfoModel.IrisId = (long)siteInfo[0].IrisId;


                                        //var servicesOffered = allSiteServicesOffered.Where(x => x.NetworkObjectId == siteInfoModel.Id).ToList();

                                        //if (servicesOffered != null)
                                        //{
                                        //    foreach (var service in servicesOffered)
                                        //    {
                                        //        siteInfoModel.Services.Add(new SiteServiceModel
                                        //        {
                                        //            ServiceTypeName = service.ServiceType.ServiceName,
                                        //            Fee = service.Fee,
                                        //            MinOrder = service.MinOrder,
                                        //            EstimatedTime = service.EstimatedTime,
                                        //            AreaCovered = service.AreaCovered,
                                        //            TaxTypeId = service.TaxTypeId,
                                        //            ServiceTypeId = service.ServiceTypeId
                                        //        });
                                        //    }
                                        //}

                                        menus = new List<SiteModelMenu>();
                                        foreach (var siteMenu in siteMenusGrp)
                                        {
                                            var menuTagString = string.IsNullOrWhiteSpace(siteMenu.Tags) ? string.Empty : siteMenu.Tags;
                                            var menuTags = menuTagString.ToLower().Split(',').ToList();
                                            //Send only Menu having Matching Tags with Target
                                            if (siteMenu.MenuIrisId.HasValue && checkTag(clientTags, menuTags))
                                            {
                                                menus.Add(new SiteModelMenu
                                                {
                                                    MenuId = (long)siteMenu.MenuIrisId,
                                                    Name = siteMenu.MenuName,
                                                    LastUpdated = siteMenu.MENU_LastUpdatedDate
                                                });
                                            }
                                        }

                                        if (menus.Count == 0)
                                        {
                                            menus = null;
                                        }

                                        siteInfoModel.Menus = menus;
                                        siteInfoModel.LastMenuUpdate = menus != null ? menus.Max(x => x.LastUpdated) : DateTime.MinValue;
                                        siteList.Add(siteInfoModel);
                                    }
                                }
                            }
                            returnModel.Sites = siteList;

                            if (returnModel.Sites == null || returnModel.Sites.Any() == false)
                            {
                                status = HttpStatusCode.NotFound;
                                _lastActionResult = "No sites found under given brand.";
                            }
                        }
                        else
                        {
                            _lastActionResult = _authenticationService.LastActionResult;
                        }
                    }
                    else
                    {
                        status = HttpStatusCode.NotFound;
                        _lastActionResult = "Invalid brand id";
                    }
                }
                else
                {
                    status = HttpStatusCode.BadRequest;
                    _lastActionResult = "Invalid brand id.";
                }

                Logger.WriteInfo(string.Format("Time taken - To check Get all sites - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
            }
            catch (Exception e)
            {
                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Unexpected error occured.";
                //log exception
                Logger.WriteError(e);
            }
            return returnModel;
        }

        /// <summary>
        /// Gets site details
        /// </summary>
        /// <param name="siteIrisId">id of the site</param>
        /// <returns>SiteModel object</returns>
        public SiteModel GetSite(string siteId, out HttpStatusCode status)
        {
            var returnModel = new SiteModel();
            status = HttpStatusCode.OK;
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                List<SiteModelMenu> menus = null;
                long siteIrisId = 0;
                if (long.TryParse(siteId, out siteIrisId))
                {
                    var networkItem = _repository.GetQuery<NetworkObject>(x => x.IrisId == siteIrisId && x.NetworkObjectTypeId == NetworkObjectTypes.Site && x.IsActive).FirstOrDefault();
                    if (networkItem != null)
                    {
                        if (_authenticationService.IsNetworkAccessible(ClientID, siteIrisId,out status))
                        {
                            Logger.WriteInfo(string.Format("Time taken - To check IsNetworkAccessible - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
                            sw = Stopwatch.StartNew();

                            //get Target Channels
                            var clientTags = getTargetClientChannels();

                            var siteMenusInfo = (_context as ProductMasterContext).udfGetSiteMenusInfo(null, siteIrisId, true).ToList();
                            if (siteMenusInfo != null && siteMenusInfo.Count() > 0)
                            {
                                var distinctSiteInfo = siteMenusInfo.ElementAt(0);

                                returnModel.SiteId = distinctSiteInfo.SiteId.ToString();
                                returnModel.StoreName = distinctSiteInfo.StoreName;
                                returnModel.StoreNumber = distinctSiteInfo.StoreNumber;
                                returnModel.Group = distinctSiteInfo.GroupName;
                                returnModel.Street = distinctSiteInfo.Street;
                                returnModel.City = distinctSiteInfo.City;
                                returnModel.State = distinctSiteInfo.State;
                                returnModel.Zip = distinctSiteInfo.Zip;
                                returnModel.IrisId = (long)distinctSiteInfo.IrisId;
                                returnModel.Phone = distinctSiteInfo.Phone;
                                returnModel.Latitude = distinctSiteInfo.Latitude;
                                returnModel.Longitude = distinctSiteInfo.Longitude;
                                returnModel.SiteTimeZoneId = distinctSiteInfo.TimeZoneId;
                                returnModel.Cuisines = string.IsNullOrWhiteSpace(distinctSiteInfo.Cuisines) ? null : distinctSiteInfo.Cuisines.Split(',').ToList();
                                returnModel.LastSiteUpdated = distinctSiteInfo.Site_LastUpdatedDate;

                                var servicesOffered = _repository.GetQuery<SerivceNetworkObjectLink>(x => x.NetworkObjectId == distinctSiteInfo.NetworkObjectId).Include("ServiceType").ToList();

                                if (servicesOffered != null)
                                {
                                    foreach (var service in servicesOffered)
                                    {
                                        returnModel.Services.Add(new SiteServiceModel
                                        {
                                            ServiceTypeName = service.ServiceType.ServiceName,
                                            Fee = service.Fee,
                                            MinOrder = service.MinOrder,
                                            EstimatedTime = service.EstimatedTime,
                                            AreaCovered = service.AreaCovered,
                                            TaxTypeId = service.TaxTypeId,
                                            ServiceTypeId = service.ServiceTypeId
                                        });
                                    }
                                }

                                menus = new List<SiteModelMenu>();
                                foreach (var siteMenu in siteMenusInfo)
                                {
                                    var menuTagString = string.IsNullOrWhiteSpace(siteMenu.Tags) ? string.Empty : siteMenu.Tags;
                                    var menuTags = menuTagString.ToLower().Split(',').ToList();
                                    //Send only Menu having Matching Tags with Target
                                    if (siteMenu.MenuIrisId.HasValue && checkTag(clientTags, menuTags))
                                    {
                                        menus.Add(new SiteModelMenu
                                        {
                                            MenuId = (long)siteMenu.MenuIrisId,
                                            Name = siteMenu.MenuName,
                                            LastUpdated = siteMenu.MENU_LastUpdatedDate

                                        });
                                    }
                                }

                                if (menus.Count == 0)
                                {
                                    menus = null;
                                }

                                returnModel.Menus = menus;
                                returnModel.LastMenuUpdate = menus != null ? menus.Max(x => x.LastUpdated) : DateTime.MinValue;
                            }
                            else
                            {
                                status = HttpStatusCode.NotFound;
                                _lastActionResult = "No site found.";
                            }
                        }
                        else
                        {
                            _lastActionResult = _authenticationService.LastActionResult;
                        }
                    }
                    else
                    {
                        status = HttpStatusCode.NotFound;
                        _lastActionResult = "Invalid Site id";
                    }
                }
                else
                {
                    status = HttpStatusCode.BadRequest;
                    _lastActionResult = "Invalid Site id";
                }

                Logger.WriteInfo(string.Format("Time taken - To get site details - Time {0:0.000}s", sw.Elapsed.TotalSeconds));
            }
            catch (Exception e)
            {
                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Unexpected error occurred";
                //log exception
                Logger.WriteError(e);
            }
            return returnModel;
        }

        /// <summary>
        /// Get All brand details
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public HttpStatusCode GetAllBrands(out BrandListModel model)
        {
            HttpStatusCode retCode = HttpStatusCode.OK;
            model = new BrandListModel();
            try
            {
                //var brands = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == NetworkObjectTypes.Brand && n.FeaturesSet >= (int)NetworkFeaturesSet.IncludeInBrandsAPI).ToList();
                var accessibleBrandIds = _authenticationService.GetAllAccessibleNetwork(ClientID, NetworkObjectTypes.Brand, out retCode).Select(x => x.Id);
                var brands = _repository.GetQuery<NetworkObject>(n => n.NetworkObjectTypeId == NetworkObjectTypes.Brand && accessibleBrandIds.Contains(n.IrisId)).ToList();     
                model.Brands = new List<BrandModel>();
                foreach (var b in brands)
                {
                    model.Brands.Add(new BrandModel { IrisId = b.IrisId, Name = b.Name });
                }
                if (model.Brands.Any() == false)
                {
                    retCode = HttpStatusCode.NotFound;
                    _lastActionResult = string.Format("No brands found.");
                }
                else
                {
                    _lastActionResult = string.Format("Successfully retrived brands.");
                }
            }
            catch (Exception e)
            {
                retCode = HttpStatusCode.InternalServerError;
                _lastActionResult = e.Message;

                //log exception
                Logger.WriteError(e);
            }
            return retCode;
        }

        /// <summary>
        /// receive the sites hirerachy
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public HttpStatusCode SyncSites(NetworkPayloadInfo data)
        {
            var retCode = HttpStatusCode.OK;
            string msgId = null;
            _lastActionResult = string.Empty;
            try
            {
                Logger.WriteAudit("Processing request to synchorize the networks from CMS.");
                if (data != null)
                {
                    msgId = data.Id;

                    if (data.PreviousHash != data.Hash)
                    {
                        var Root = data.Payload;
                        if (Root != null)
                        {
                            var networkObjects = _repository.GetAll<NetworkObject>().ToList();

                            //Add to a list
                            var cmsNetworks = new List<RootInfo>();
                            cmsNetworks.Add(Root);

                            //Capture DataIds
                            var cmsDataIds = new List<string>();

                            //Sync all the networks
                            var syncOutput = new SyncInfo();
                            syncNetworks(null, NetworkObjectTypes.Root, cmsNetworks, networkObjects, ref cmsDataIds, ref syncOutput);

                            //Make the remaining Networks to Inactive
                            //Check if dataId is present and not in the list then add to inactive list. Even if dataId is not in present then add to Inactive list
                            var networksNotinCMS = networkObjects.Where(x => x.DataId == null ? 1 == 1 : !cmsDataIds.Contains(x.DataId.ToLower().ToString())).ToList();
                            networksNotinCMS.ForEach(x => x.IsActive = false);

                            _context.SaveChanges();

                            Logger.WriteAudit(string.Format("Synchorized the Networks. Updated: {0}, New: {1}, Deleted: {2}", syncOutput.Updated, syncOutput.Created, networksNotinCMS.Count()));
                            _lastActionResult = "Synchorized the Networks.";
                        }
                    }
                    else
                    {
                        _lastActionResult = "No changes to synchorize.";
                    }
                }
                else
                {
                    _lastActionResult = "No data to Synchorize.";
                }
            }
            catch (Exception e)
            {
                retCode = HttpStatusCode.InternalServerError;
                _lastActionResult = "Failed to synchorize the Networks. See logs for more details";

                //log exception
                Logger.WriteError(e);
            }
            finally
            {
                sendResponse(msgId, retCode);
                Logger.WriteAudit(_lastActionResult);
            }
            return retCode;
        }

        /// <summary>
        /// get Target's Channels
        /// </summary>
        /// <param name="siteNetworkObjectId"></param>
        /// <returns></returns>
        private List<string> getTargetClientChannels()
        {
            var tagsList = new List<string>();

            var target = _ruleService.GetTargetClientChannels(ClientID);
            if (target != null && target.Any())
            {
                tagsList = target.Select(x => x.TagName).ToList();
            }
            return tagsList;
        }

        /// <summary>
        /// Checks if tags are allowed to use entity
        /// </summary>
        /// <param name="tags">source tags</param>
        /// <param name="allowedTags">list of tags allowed</param>
        /// <returns></returns>
        private bool checkTag(List<string> tags, List<string> allowedTags)
        {
            bool retval = false;
            try
            {
                if (allowedTags.Count > 0)
                {
                    foreach (var tag in tags)
                    {
                        if (allowedTags.Contains(tag.ToLower()))
                        {
                            retval = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(string.Format("Tag check failed. Message:{0} StackTrace:{1}", ex.Message, ex.StackTrace));
            }
            return retval;
        }
        /// <summary>
        /// After Sites synchorised send the result back to the caller
        /// </summary>
        /// <param name="msgId"></param>
        /// <param name="retCode"></param>
        private void sendResponse(string msgId, HttpStatusCode retCode)
        {
            if (msgId != null)
            {
                var url = string.Format("{0}/{1}", ConfigurationManager.AppSettings.Get("BarcelonaBaseAddress").TrimEnd('/'), "sites/hierarchy/syncstatus");
                SyncStatusModel syncStatus = new SyncStatusModel
                {
                    Id = msgId,
                    Code = retCode == HttpStatusCode.OK ? "Success" : "Failure",
                    Msg = _lastActionResult
                };

                // Initiate HTTP POST                             
                HttpClient httpClient = new HttpClient();
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

                var response = httpClient.PostAsJsonAsync(url, syncStatus).Result;
                //Add the responsetext to _lastActionResult
                if (response.IsSuccessStatusCode)
                {
                    _lastActionResult += response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    _lastActionResult += ". Failed send response.";
                }
            }
        }
        /// <summary>
        /// Update the Networks to sync CMS networks
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="networkType"></param>
        /// <param name="cmsNetworks"></param>
        /// <param name="networkObjects"></param>
        private void syncNetworks(NetworkObject parent, NetworkObjectTypes networkType, dynamic cmsNetworks, List<NetworkObject> networkObjects, ref List<string> cmsDataIds, ref SyncInfo syncOutput)
        {
            var networksOfThisType = networkObjects.Where(x => x.NetworkObjectTypeId == networkType).ToList();
            //Loop through networks
            foreach (var cmsNetwork in cmsNetworks)
            {
                // Check if it is active
                if (cmsNetwork != null && cmsNetwork.IsActive && string.IsNullOrWhiteSpace(cmsNetwork.DataId) == false)
                {
                    var cmsDataId = cmsNetwork.DataId;
                    //Add Network if it is not Root and the particular dataId record is not present
                    var network = networksOfThisType.Where(x => (networkType == NetworkObjectTypes.Root ? 1 == 1 : (x.DataId != null && x.DataId.ToLower().CompareTo(cmsDataId.ToLower()) == 0))).FirstOrDefault();
                    if (network != null)
                    {//update if exists
                        //bug 4783
                        network.Name = (networkType == NetworkObjectTypes.Site ? cmsNetwork.Name + " " + cmsNetwork.StoreNumber.ToString() : cmsNetwork.Name);
                        network.DataId = cmsNetwork.DataId;
                        network.IsActive = true;
                        network.IrisId = cmsNetwork.IrisId;
                        if (networkType == NetworkObjectTypes.Site)
                        {
                            var siteId = new Guid(cmsNetwork.DataId);
                            var siteInfo = network.SiteInfoes.FirstOrDefault();
                            if (siteInfo != null)
                            {
                                siteInfo.SiteId = siteId;
                                siteInfo.StoreNumber = cmsNetwork.StoreNumber.ToString();
                                siteInfo.Address1 = cmsNetwork.Address1;
                                siteInfo.Address2 = cmsNetwork.Address2;
                                siteInfo.State = cmsNetwork.State;
                                siteInfo.City = cmsNetwork.City;
                                siteInfo.Zip = cmsNetwork.Zip;
                                siteInfo.LastUpdatedDate = DateTime.Now;
                            }
                            else
                            {
                                var newSiteInfo = new SiteInfo
                                {
                                    SiteId = siteId,
                                    NetworkObjectId = network.NetworkObjectId,
                                    StoreNumber = cmsNetwork.StoreNumber.ToString(),
                                    Address1 = cmsNetwork.Address1,
                                    Address2 = cmsNetwork.Address2,
                                    State = cmsNetwork.State,
                                    City = cmsNetwork.City,
                                    Zip = cmsNetwork.Zip,
                                    LastUpdatedDate = DateTime.Now
                                };
                                _repository.Add<SiteInfo>(newSiteInfo);
                            }
                        }
                        syncOutput.Updated++;
                    }
                    else
                    {//add if not exists
                        network = new NetworkObject
                        {
                            //bug 4783
                            Name = (networkType == NetworkObjectTypes.Site ? cmsNetwork.Name + " " + cmsNetwork.StoreNumber.ToString() : cmsNetwork.Name),
                            NetworkObjectTypeId = networkType,
                            IsActive = true,
                            DataId = cmsNetwork.DataId,
                            NetworkObject2 = parent,
                            IrisId = cmsNetwork.IrisId
                        };
                        if (networkType == NetworkObjectTypes.Site)
                        {
                            var siteId = new Guid(cmsNetwork.DataId);
                            network.SiteInfoes.Add(new SiteInfo
                            {
                                SiteId = siteId,
                                StoreNumber = cmsNetwork.StoreNumber.ToString(),
                                Address1 = cmsNetwork.Address1,
                                Address2 = cmsNetwork.Address2,
                                State = cmsNetwork.State,
                                City = cmsNetwork.City,
                                Zip = cmsNetwork.Zip,
                                LastUpdatedDate = DateTime.Now
                            });
                        }
                        _repository.Add<NetworkObject>(network);
                        syncOutput.Created++;
                    }

                    //syncChildren
                    dynamic children;
                    var childNetworkType = getNextNetworkObjectType(networkType, cmsNetwork, out children);
                    if (children != null && childNetworkType != null)
                    {
                        syncNetworks(network, childNetworkType, children, networkObjects, ref cmsDataIds, ref syncOutput);
                    }
                }
            }
            // Capture all the dataIds
            cmsDataIds.AddRange(getCMSDataIds(networkType, cmsNetworks));
        }

        /// <summary>
        /// get Ids from dynamic object
        /// </summary>
        /// <param name="networkType"></param>
        /// <param name="cmsNetworks"></param>
        /// <returns></returns>
        private List<string> getCMSDataIds(NetworkObjectTypes networkType, dynamic cmsNetworks)
        {
            List<string> dataIds = new List<string>();
            foreach (var cmsNetwork in cmsNetworks)
            {
                if (cmsNetwork.DataId != null)
                {
                    dataIds.Add(cmsNetwork.DataId.ToLower().ToString());
                }
            }
            return dataIds;
        }

        /// <summary>
        /// determine the next NetworkType to process
        /// </summary>
        /// <param name="networkType"></param>
        /// <param name="cmsNetwork"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        private NetworkObjectTypes? getNextNetworkObjectType(NetworkObjectTypes networkType, dynamic cmsNetwork, out dynamic children)
        {
            switch (networkType)
            {
                case NetworkObjectTypes.Root:
                    children = cmsNetwork.Brands;
                    return NetworkObjectTypes.Brand;

                case NetworkObjectTypes.Brand:
                    children = cmsNetwork.Franchises;
                    return NetworkObjectTypes.Franchise;

                case NetworkObjectTypes.Franchise:
                    children = cmsNetwork.Markets;
                    return NetworkObjectTypes.Market;

                case NetworkObjectTypes.Market:
                    children = cmsNetwork.Sites;
                    return NetworkObjectTypes.Site;

                case NetworkObjectTypes.Site:
                    children = null;
                    return null;
                default:
                    children = null;
                    return null;
            }
        }
    }

    /// <summary>
    /// Interface class that SiteInfoService class implements
    /// </summary>
    public interface INetworkObjectService
    {
        string ClientID { get; set; }
        string LastActionResult { get; }
        SiteListModel GetAllSitesInBrand(long brandId, out HttpStatusCode status);
        SiteModel GetSite(string id, out HttpStatusCode status);
        HttpStatusCode GetAllBrands(out BrandListModel model);
        HttpStatusCode SyncSites(NetworkPayloadInfo payload);
    }
}