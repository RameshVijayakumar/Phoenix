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
    
    public partial class ItemDescription
    {
        public ItemDescription()
        {
            this.Items = new HashSet<Item>();
        }
    
        public int ItemDescriptionId { get; set; }
        public int ItemId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    
        public virtual ICollection<Item> Items { get; set; }
        public virtual Item Item { get; set; }
    }
}
