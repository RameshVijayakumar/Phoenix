using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Diagnostics;

namespace Phoenix.Web
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // set up transfer of diagnostic data to storage account 
            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // -- logs --
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1.0);


            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);
            return base.OnStart();
        }
    }
}