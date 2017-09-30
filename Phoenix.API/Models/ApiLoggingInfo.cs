using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phoenix.API.Models
{
    public class ApiLoggingInfo
    {
        public string HttpMethod { get; set; }
        public string UriAccessed { get; set; }
        public string IPAddress { get; set; }
        public string ComputerName { get; set; }
        public string IPAddress2 { get; set; }
        public string BodyContent { get; set; }
    }
}
