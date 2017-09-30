using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Phoenix.API
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "VersionApi",
                routeTemplate: "{controller}/{version}/{id}/{channel}",
                defaults: new { id = RouteParameter.Optional, channel = RouteParameter.Optional }
            );


            config.Routes.MapHttpRoute(
                name: "ActionApi",
                routeTemplate: "{controller}/{action}/{menuId}",
                defaults: new { menuId = RouteParameter.Optional }

            );
        }
    }
}
