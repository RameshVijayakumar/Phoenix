using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.DataAccess
{
    public partial class Item
    {
        public bool IsOverride { get; set; }
        public bool IsAutoSelect { get; set; }
        public int SortOrder { get; set; }
        public bool hasChildren { get; set; }
        public int OrgItemId { get; set; }
    }

    public partial class Category
    {
        public bool IsOverride { get; set; }
        public int SortOrder { get; set; }
        public bool hasChildren { get; set; }
        public int OrgCategoryId { get; set; }
    }

    public partial class ItemCollection
    {
        public bool IsOverride { get; set; }
        public int SortOrder { get; set; }
        public bool hasChildren { get; set; }
        public int OrgCollectionId { get; set; }
    }

    public partial class Menu
    {
        public DateTime LastUpdatedDate { get; set; }
        public bool IsMenuOverriden { get; set; }
    }

    public partial class Schedule
    {
        public DateTime LastUpdatedDate { get; set; }
        public OverrideStatus OverrideStatus { get; set; }
        public bool IsOverride { get; set; }
        public int ScheduleOrginatedAt { get; set; }
        public int Priority { get; set; }
        public int ScheduleLinkedAt { get; set; }
    }

    public partial class SchCycle
    {
        public bool IsOverride { get; set; }
        public int CycleOrginatedAt { get; set; }
    }


    [Flags]
    public enum NetworkFeaturesSet
    {
        None =0,
        POSMapEnabled = 2,
        MasterItemDWColumnsEnabled = 4,
        IncludeInBrandsAPI = 8
    }
}
