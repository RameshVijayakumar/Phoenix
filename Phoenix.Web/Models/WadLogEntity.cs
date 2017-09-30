using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Data.Services.Common;

namespace Phoenix.Web.Models
{
    [DataServiceKey("PartitionKey", "RowKey")]
    public class WadLogEntity : TableEntity
    {
        public Int64 EventTickCount { get; set; }
        public String DeploymentId { get; set; }
        public String Role { get; set; }
        public String RoleInstance { get; set; }
        public Int32 Level { get; set; }
        public Int32 EventId { get; set; }
        public Int32 Pid { get; set; }
        public Int32 Tid { get; set; }
        public String Message { get; set; }
    }
}