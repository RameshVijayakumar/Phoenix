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
    
    public partial class TargetTagLink
    {
        public int TargetTagLinkId { get; set; }
        public int TargetId { get; set; }
        public int TagId { get; set; }
    
        public virtual MenuSyncTarget MenuSyncTarget { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
