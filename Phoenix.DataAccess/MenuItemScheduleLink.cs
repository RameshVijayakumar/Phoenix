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
    
    public partial class MenuItemScheduleLink
    {
        public MenuItemScheduleLink()
        {
            this.MenuItemCycleInSchedules = new HashSet<MenuItemCycleInSchedule>();
        }
    
        public int ItemScheduleLinkId { get; set; }
        public int ItemId { get; set; }
        public bool IsSelected { get; set; }
        public int NetworkObjectId { get; set; }
        public int Day { get; set; }
        public System.DateTime UpdatedDate { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual ICollection<MenuItemCycleInSchedule> MenuItemCycleInSchedules { get; set; }
        public virtual NetworkObject NetworkObject { get; set; }
        public virtual Item Item { get; set; }
    }
}