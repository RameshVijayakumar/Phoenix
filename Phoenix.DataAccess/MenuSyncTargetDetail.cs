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
    
    public partial class MenuSyncTargetDetail
    {
        public int MenuSyncTargetDetailId { get; set; }
        public long SyncIrisId { get; set; }
        public string TargetName { get; set; }
        public int NetworkObjectId { get; set; }
        public string MenuName { get; set; }
        public string SyncStatus { get; set; }
        public string SyncMessage { get; set; }
        public System.DateTime RequestedTime { get; set; }
        public Nullable<System.DateTime> ResponseTime { get; set; }
    
        public virtual NetworkObject NetworkObject { get; set; }
    }
}
