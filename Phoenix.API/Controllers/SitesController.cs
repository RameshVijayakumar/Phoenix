using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Phoenix.API.Models;
using System.Security.Claims;

namespace Phoenix.API.Controllers
{
    public class SitesController : ApiController
    {
        private INetworkObjectService _siteService;

        public SitesController()
        {
            //TODO: inject this interface
            _siteService = new NetworkObjectService();
            _siteService.ClientID = string.Empty;
            foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            {
                if (existingClaim.Type == "appid")
                {
                    _siteService.ClientID = existingClaim.Value;
                }

            }
        }

        // GET api/sites
        /// <summary>
        /// Gets all available sites.
        /// </summary>
        /// <returns>List of SiteListModel objects</returns>
        [Authorize]
        public SiteListModel Get(long brand)
        {
            HttpStatusCode status = HttpStatusCode.OK;
            var model = _siteService.GetAllSitesInBrand(brand, out status);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _siteService.LastActionResult));
            }
            return model;
        }

        // GET api/sites/80E6E93A-67F9-49B3-B8A0-174EEE7EE684
        /// <summary>
        /// Gets a site by it's id.
        /// </summary>
        /// <param name="id">Id that uniquely identifies a site.</param>
        /// <returns>SiteModel object</returns>
        [Authorize]
        public SiteModel Get(string id)
        {
            HttpStatusCode status = HttpStatusCode.OK;
            var model = _siteService.GetSite(id, out status);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _siteService.LastActionResult));
            }
            return model;
        }

        // POST sites
        /// <summary>
        /// Sync the Network objects with CMS sites
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        public HttpResponseMessage Post(NetworkPayloadInfo payload)
        {
            var retVal = HttpStatusCode.OK;
            retVal = _siteService.SyncSites(payload);
            if (retVal != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(retVal, _siteService.LastActionResult));
            }
            else
            {
                return new HttpResponseMessage { Content = new StringContent(_siteService.LastActionResult) };
            }
        }
    }
}
