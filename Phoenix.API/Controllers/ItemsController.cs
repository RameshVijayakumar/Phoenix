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
    public class ItemsController : ApiController
    {
        private IMasterItemService _mastItemService;

        public ItemsController()
        {
            //TODO: inject this interface
            _mastItemService = new MasterItemService();
            _mastItemService.ClientID = string.Empty;
            foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            {
                if (existingClaim.Type == "appid")
                {

                    _mastItemService.ClientID = existingClaim.Value;
                }

            }
        }

        [Authorize]
        public MasterItemsModel Get(string brand)
        {
            MasterItemsModel model = null;
            HttpStatusCode status = _mastItemService.GetItems(brand, out model);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _mastItemService.LastActionResult));
            }
            return model;
        }


        [Authorize]
        public MasterItemDetailModel Get(long id)
        {
            MasterItemDetailModel model = null;
            HttpStatusCode status = _mastItemService.GetItemDetails(id, out model);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _mastItemService.LastActionResult));
            }
            return model;
        }

        [HttpGet]
        [Authorize]
        public MasterItemMappingModel Mappings(long id, long siteId, string menuIds = "")
        {
            MasterItemMappingModel model = null;
            HttpStatusCode status = _mastItemService.GetItemMapping(id, siteId, menuIds, out model);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _mastItemService.LastActionResult));
            }
            return model;
        }


    }
}
