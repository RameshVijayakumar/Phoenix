using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Phoenix.API
{
    public class WebRole : RoleEntryPoint
    {
        public override bool OnStart()
        {
            // set up transfer of diagnostic data to storage account 
            DiagnosticMonitorConfiguration dmc = DiagnosticMonitor.GetDefaultInitialConfiguration();

            // -- logs --
            dmc.Logs.ScheduledTransferLogLevelFilter = LogLevel.Verbose;
            dmc.Logs.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);

            // -- perf counters --
            //dmc.PerformanceCounters.DataSources.Add(new PerformanceCounterConfiguration() { CounterSpecifier= @"\Processor(*)\% Processor Time", SampleRate = TimeSpan.FromSeconds(3) });
            //dmc.PerformanceCounters.DataSources.Add(new PerformanceCounterConfiguration() { CounterSpecifier= @"\ASP.NET Applications(*)\Request Execution Time", SampleRate = TimeSpan.FromSeconds(3) });
            //dmc.PerformanceCounters.DataSources.Add(new PerformanceCounterConfiguration() { CounterSpecifier= @"\ASP.NET Applications(*)\Requests Executing", SampleRate = TimeSpan.FromSeconds(3) });
            //dmc.PerformanceCounters.DataSources.Add(new PerformanceCounterConfiguration() { CounterSpecifier= @"\ASP.NET Applications(*)\Requests/Sec", SampleRate = TimeSpan.FromSeconds(3) });
            //dmc.PerformanceCounters.ScheduledTransferPeriod = TimeSpan.FromMinutes(1);

            DiagnosticMonitor.Start("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString", dmc);


            return base.OnStart();
        }

    }
}
