using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Phoenix.API.Models;
using System.Net;
using System.Net.Http;
using System.Security.Claims;

namespace Phoenix.API.Controllers
{
    public class MenusController : ApiController
    {
        private IMenuService _menuService;

        public MenusController()
        {
            //TODO: inject this interface
            _menuService = new MenuService(); 
            _menuService.ClientID = string.Empty;
            foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            {
                if (existingClaim.Type == "appid")
                {

                    _menuService.ClientID = existingClaim.Value;
                }

            }
        }
        
        /// <summary>
        /// Get First menu information of a Site
        /// </summary>
        /// <param name="id">SiteId</param>
        /// <returns></returns>

        [Authorize] 
        public void Get(string id)
        {
            var reason = "This API version deprecated. Please use latest versions V4: /menus/V4/{SiteId}"; // or V3: /menus/V3/{SiteId}";
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, reason));
        }

        /// <summary>
        /// GET: get specfic version of all the menus(and related information) of Site by passing ID and/or channel(which is comman seperated menu names) 
        /// </summary>
        /// <param name="id">SiteId</param>
        /// <param name="version">V4</param>
        /// <param name="channel">comman seperated menu names</param>
        /// <returns></returns>
        [Authorize] 
        public dynamic Get(long id, string version, string channel = null)
        {           
            HttpStatusCode status = HttpStatusCode.OK;
            var model = _menuService.GetFullMenu(id, version.ToUpper(), out status, channel);

           if (status != HttpStatusCode.OK)
           {
               throw new HttpResponseException(Request.CreateErrorResponse(status, _menuService.LastActionResult));
           }
           return model;
        }         
    }
}
