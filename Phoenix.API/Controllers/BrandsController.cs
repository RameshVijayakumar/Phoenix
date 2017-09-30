using Phoenix.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

namespace Phoenix.API.Controllers
{
    public class BrandsController : ApiController
    {
        private INetworkObjectService _siteService;

        public BrandsController()
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

        // GET api/brands
        [Authorize] 
        public BrandListModel Get()
        {

            BrandListModel model = null;
            HttpStatusCode status = _siteService.GetAllBrands(out model);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _siteService.LastActionResult));
            }
            return model;
        }
    }
}
