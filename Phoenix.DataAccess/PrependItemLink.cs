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
    
    public partial class PrependItemLink
    {
        public int PrependItemLinkId { get; set; }
        public int PrependItemId { get; set; }
        public int ItemId { get; set; }
        public int MenuNetworkObjectLinkId { get; set; }
        public int SortOrder { get; set; }
        public OverrideStatus OverrideStatus { get; set; }
        public Nullable<int> OverrideParentPrependItemId { get; set; }
    
        public virtual MenuNetworkObjectLink MenuNetworkObjectLink { get; set; }
        public virtual Item Item { get; set; }
        public virtual Item OverrideParentPrependItem { get; set; }
        public virtual Item PrependItem { get; set; }
    }
}
