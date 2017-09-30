using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.API.Models
{
    public class TargetStatusModel
    {
        public TargetStatusModel()
        {
            Sites = new List<TargetSiteStatusModel>();
        }

        public long Id {get; set;}
        public List<TargetSiteStatusModel> Sites { get; set; }
    }

    public class TargetSiteStatusModel 
    {
         public long Id {get; set;}
         public string Menu {get; set;}
         public string Status {get; set;}
         public string Message {get; set;}
    }
}