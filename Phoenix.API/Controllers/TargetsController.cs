using Phoenix.API.Models;
using System;
using System.Collections.Generic;
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
    public class TargetsController : ApiController
    {
        private ITargetService _targetService;

        public TargetsController()
        {
            //TODO: inject this interface
            _targetService = new TargetService();
            _targetService.ClientID = string.Empty;
            foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            {
                if (existingClaim.Type == "appid")
                {
                    _targetService.ClientID = existingClaim.Value;
                }

            }
        }

        [System.Web.Http.HttpPost]
        [Authorize]
        public HttpResponseMessage Status(TargetStatusModel status)
        {
            var retVal = HttpStatusCode.OK;
            retVal = _targetService.UpdateStatus(status);
            if (retVal != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(retVal, _targetService.LastActionResult));
            }
            else
            {
                return new HttpResponseMessage { Content = new StringContent(_targetService.LastActionResult) };
            }
        }

    }
}
