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
    
    public partial class SubCategoryLink
    {
        public int SubCategoryLinkId { get; set; }
        public int SubCategoryId { get; set; }
        public int CategoryId { get; set; }
        public int NetworkObjectId { get; set; }
        public int SortOrder { get; set; }
        public OverrideStatus OverrideStatus { get; set; }
        public Nullable<int> OverrideParentSubCategoryId { get; set; }
    
        public virtual Category Category { get; set; }
        public virtual Category OverrideParentSubCategory { get; set; }
        public virtual Category SubCategory { get; set; }
        public virtual NetworkObject NetworkObject { get; set; }
    }
}