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
    
    public partial class AssetItemLink
    {
        public int AssetItemLinkId { get; set; }
        public Nullable<int> ItemId { get; set; }
        public int AssetId { get; set; }
    
        public virtual Asset Asset { get; set; }
        public virtual Item Item { get; set; }
    }
}
