//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Phoenix.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class ServiceType
    {
        public ServiceType()
        {
            this.SerivceNetworkObjectLinks = new HashSet<SerivceNetworkObjectLink>();
        }
    
        public int ServiceTypeId { get; set; }
        public string ServiceName { get; set; }
    
        public virtual ICollection<SerivceNetworkObjectLink> SerivceNetworkObjectLinks { get; set; }
    }
}
