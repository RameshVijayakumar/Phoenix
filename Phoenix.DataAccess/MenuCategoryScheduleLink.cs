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
    
    public partial class MenuCategoryScheduleLink
    {
        public MenuCategoryScheduleLink()
        {
            this.MenuCategoryCycleInSchedules = new HashSet<MenuCategoryCycleInSchedule>();
        }
    
        public int CategoryScheduleLinkId { get; set; }
        public int CategoryId { get; set; }
        public bool IsSelected { get; set; }
        public int NetworkObjectId { get; set; }
        public int Day { get; set; }
        public System.DateTime UpdatedDate { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual Category Category { get; set; }
        public virtual ICollection<MenuCategoryCycleInSchedule> MenuCategoryCycleInSchedules { get; set; }
        public virtual NetworkObject NetworkObject { get; set; }
    }
}