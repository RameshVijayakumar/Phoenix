using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Phoenix.Common;
using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Phoenix.Web.Models
{
    public class LogService
    {   
        public List<WadLogEntity> GetWADLogs()
        {
            List<WadLogEntity> logs = null;
            try
            {
                // get storage account object from cloud config string
                Microsoft.WindowsAzure.Storage.CloudStorageAccount csa = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString"));

                /*
                TableServiceContext tableServiceContext = new TableServiceContext(csa.TableEndpoint.ToString(), csa.Credentials);
                IQueryable<WadLogEntity> traceLogsTable = tableServiceContext.CreateQuery<WadLogEntity>("WADLogsTable");
                var selection = from row in traceLogsTable where row.Level == 5 && row.Timestamp >= DateTime.Now.AddDays(-1) select row;
                //row.PartitionKey.CompareTo("0" + DateTime.UtcNow.AddHours(-24.0).Ticks) >= 0 select row;
                CloudTableQuery<WadLogEntity> query = selection.AsTableServiceQuery<WadLogEntity>();
                IEnumerable<WadLogEntity> results = query.Execute();
                //var results = tableServiceContext.CreateQuery<TableServiceEntity>("WADLogsTable").Take(50);
                logs = results.ToList();
                */

                // get the table object to run query
                CloudTableClient tableClient = csa.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("WADLogsTable");

                // create a range query
                TableQuery<WadLogEntity> rangeQuery = new TableQuery<WadLogEntity>().Where(
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterConditionForInt("Level", QueryComparisons.Equal, 5),
                        TableOperators.And,
                        TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.Now.AddDays(-5))));
                
                // execute the query
                logs = table.ExecuteQuery(rangeQuery).ToList();
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return logs;
        }
    }
}