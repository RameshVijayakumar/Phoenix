using System;
using System.Collections.Generic;
using System.IdentityModel.Services;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Reflection;
using System.Security.Claims;
using Phoenix.Web.Models.ClaimsManager;
using System.Threading;
using Microsoft.Practices.Unity;
using System.Configuration;
using Phoenix.DataAccess;
using System.Data.Entity;
using Phoenix.Web.Models;
using Microsoft.WindowsAzure;
using SnowMaker;
using Phoenix.Web.Unity;
using System.Security.Cryptography;

namespace Phoenix.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            IdentityConfig.ConfigureIdentity();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            configureUnity();

            getBuildVersion();
        }

        /// <summary>
        /// On Application Error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Application_Error(object sender, EventArgs e)
        {
            var error = Server.GetLastError();
            var cryptoEx = error as CryptographicException;
            if (cryptoEx != null)
            {
                FederatedAuthentication.SessionAuthenticationModule.SignOut();
                FederatedAuthentication.WSFederationAuthenticationModule.SignOut();
                Server.ClearError();
            }
        }

        /// <summary>
        /// set DI
        /// </summary>
        private void configureUnity()
        {
            //Create UnityContainer           
            IUnityContainer container = new UnityContainer();

            //#region EF database context

            //var productMasterContextConnectionString = ConfigurationManager.ConnectionStrings["ProductMasterContext"].ConnectionString;
            //var phoenixODSContextConnectionString = ConfigurationManager.ConnectionStrings["PhoenixODSContext"].ConnectionString;

            //container.RegisterType<DbContext, ProductMasterContext>(new HttpContextLifetimeManager<DbContext>(), new InjectionConstructor(productMasterContextConnectionString));
            //container.RegisterType<DbContext, PhoenixODSContext>(new HttpContextLifetimeManager<DbContext>(), new InjectionConstructor(phoenixODSContextConnectionString));


            //#endregion

            #region other objects
            // create Iris Id generator singleton object
            string azureConnectionStr = CloudConfigurationManager.GetSetting("DataConnectionString");
            BlobOptimisticDataStore store = new BlobOptimisticDataStore(Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(azureConnectionStr), Constants.IrisConstants.IrisIdContainerName);
            var generator = new UniqueIdGenerator(store);
            generator.BatchSize = 200;
            generator.MaxWriteAttempts = 5;
            container.RegisterInstance<IUniqueIdGenerator>(generator);

            #endregion

            #region services
            
            // from Menu
            container.RegisterType<IMenuService, MenuService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<IItemService, ItemService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<IScheduleService, ScheduleService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<IAssetService, AssetService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<ISiteService, SiteService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<IMenuSyncService, MenuSyncService>(new InjectionProperty("IrisIdGenerator", generator));
            container.RegisterType<ITagService, TagService>();

            #endregion

            //#region repositories

            //container.RegisterType<IRepository, GenericRepository>(new HttpContextLifetimeManager<IRepository>(), new InjectionConstructor(typeof(DbContext)));

            //#endregion

            //Set container for Controller Factory
            MvcUnityContainer.Container = container;

            //Set for Controller Factory
            ControllerBuilder.Current.SetControllerFactory(typeof(UnityControllerFactory));
        }

        /// <summary>
        /// get global build version
        /// </summary>
        private void getBuildVersion()
        {
            HttpApplicationState appState = HttpContext.Current.Application;
            Assembly asm = null;
            if (!appState.AllKeys.Contains("appversion"))
            {
                asm = Assembly.GetAssembly(typeof(Phoenix.Web.Controllers.HomeController));

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
                    Assembly.GetAssembly(typeof(Phoenix.Web.Controllers.HomeController));
                }

                appState.Lock();
                appState.Add("lastbuild", System.IO.File.GetLastWriteTime(asm.Location).ToString("MM/dd/yyyy"));
                appState.UnLock();

            }
        }

        private void WSFederationAuthenticationModule_RedirectingToIdentityProvider(object sender, RedirectingToIdentityProviderEventArgs e)
        {
            if (!String.IsNullOrEmpty(IdentityConfig.Realm))
            {
                e.SignInRequestMessage.Realm = IdentityConfig.Realm;
            }
        }

        protected void Application_PostAuthenticateRequest()
        {
            ClaimsPrincipal currentPrincipal = ClaimsPrincipal.Current;
            ZioskAuthenticationManager customClaimsTransformer = new ZioskAuthenticationManager();
            ClaimsPrincipal tranformedClaimsPrincipal = customClaimsTransformer.Authenticate(string.Empty, currentPrincipal);
            Thread.CurrentPrincipal = tranformedClaimsPrincipal;
            HttpContext.Current.User = tranformedClaimsPrincipal;
        }
    }
}
