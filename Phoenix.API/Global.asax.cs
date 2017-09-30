using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Phoenix.API
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            //BundleConfig.RegisterBundles(BundleTable.Bundles);

            // create SerializerSettings for lowercase conversion and assign to global configuration
            var config = GlobalConfiguration.Configuration;
            var settings = new JsonSerializerSettings();
            settings.ContractResolver = new LowercaseContractResolver();
            config.Formatters.JsonFormatter.SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
            config.Formatters.JsonFormatter.SerializerSettings = settings;

            // remove xml reply
            GlobalConfiguration.Configuration.Formatters.XmlFormatter.SupportedMediaTypes.Clear();

            HttpApplicationState appState = HttpContext.Current.Application;
            Assembly asm = null;
            if (!appState.AllKeys.Contains("appversion"))
            {
                asm = Assembly.GetAssembly(typeof(Phoenix.API.Controllers.HomeController));

                appState.Lock();
#if DEBUG
                appState.Add("appversion", asm.GetName().Version.ToString() + " DEBUG");
                HibernatingRhinos.Profiler.Appender.EntityFramework.EntityFrameworkProfiler.Initialize();
#else
                appState.Add("appversion", asm.GetName().Version.ToString());
#endif
                appState.UnLock();
            }

            if (!appState.AllKeys.Contains("lastbuild"))
            {
                if (asm == null)
                {
                    Assembly.GetAssembly(typeof(Phoenix.API.Controllers.HomeController));
                }

                appState.Lock();
                appState.Add("lastbuild", System.IO.File.GetLastWriteTime(asm.Location).ToString("MM/dd/yyyy"));
                appState.UnLock();

            }

            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            RouteTable.Routes.IgnoreRoute("{resource}.ico");

            RegisterApis(GlobalConfiguration.Configuration);
        }

        public static void RegisterApis(HttpConfiguration config)
        {
            config.MessageHandlers.Add(new ApiUsageLogger());
        }
    }


    /// <summary>
    /// ContractResolver which is injected to the JSON serialization process to convert
    /// property names to lower case
    /// </summary>
    public class LowercaseContractResolver : DefaultContractResolver
    {
        protected override string ResolvePropertyName(string propertyName)
        {
            return propertyName.ToLower();
        }
    }
}
