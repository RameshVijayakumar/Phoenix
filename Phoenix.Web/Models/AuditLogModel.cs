using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class AuditLogModel
    {
        public int AuditLogId { get; set; }
        public int UserId { get; set; }
        public int? NetworkObjectId { get; set; }
        public string NetworkObjectName { get; set; }
        public string UserName { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Name { get; set; }
    }
}