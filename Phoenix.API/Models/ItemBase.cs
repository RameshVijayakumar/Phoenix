using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{

    public class ScheduleDetailModel
    {
        public List<string> Sunday { get; set; }
        public List<string> Monday { get; set; }
        public List<string> Tuesday { get; set; }
        public List<string> Wednesday { get; set; }
        public List<string> Thursday { get; set; }
        public List<string> Friday { get; set; }
        public List<string> Saturday { get; set; }
    }

    /// <summary>
    /// Represents a base class for ItemInCategoryModel and ItemInCollectionModel classes.
    /// </summary>
    public class ItemBase
    {
        // Id that uniquely identifies the item in the database
        [JsonIgnore]
        [JsonPropertyAttribute("Id")]
        public int ItemId { get; set; }
        // Product Lookup Unit number .
        public int? PLU { get; set; }
        // Internal name for the item.
        [JsonPropertyAttribute("InternalName")]
        public string ItemName { get; set; }
        // User-friendly name of the item .
        public string DisplayName { get; set; }
        // User-friendly description of the item.
        [JsonPropertyAttribute("Description")]
        public string DisplayDescription { get; set; }
        // Indicates if this is a featured item.
        public bool IsFeatured { get; set; }
        // Price to be charged for the item.
        [JsonPropertyAttribute("Price")]
        public decimal BasePrice { get; set; }
        // StartDate Start when Item is to be available
        [JsonPropertyAttribute("StartDate")]
        public string StartDate { get; set; }
        // EndDate End when Item is to be available
        [JsonPropertyAttribute("EndDate")]
        public string EndDate { get; set; }

        // Multiple Tax ids for the item       
        public List<int> TaxTypeId { get; set; }
        // Net calories in the item.
        public int? Calories { get; set; }

        public bool IsModifier { get; set; }
    }

    public class ItemModelV1V2 : ItemBase
    {

        // A Schedule with this item.
        public ScheduleDetailModel Schedule { get; set; }

        // Collection of items associated with this item.
        public List<CollectionModelV1V2> Collections { get; set; }
    }

    public class ItemModelV3 : ItemBase
    {
        // Indicates if this is a substitute item.
        public bool IsSubstitute { get; set; }

        // Collection of items associated with this item.
        public List<CollectionModelV3> Collections { get; set; }
        public List<ItemModelV3> PrependItems { get; set; }

        #region V3 Attributes

        //Indicates to the item and children hierarchically
        public bool SendHierarchy { get; set; }

        //TBD: 
        //indicates the item must be priced and ordered as a top level item
        public bool IsTopLevel { get; set; }

        //item can be ordered without presenting collections
        public bool QuickOrder { get; set; }

        //indicates whether the item is a combo.
        public bool Combo { get; set; }

        public bool AutoSelect { get; set; }

        public bool IsAvailable { get; set; }

        //public bool ShowPrice { get; set; }

        public bool IsAlcohol { get; set; }

        public string DeepLinkId { get; set; }

        [JsonPropertyAttribute("AlternateID")]
        public string AlternatePLU { get; set; }

        [JsonPropertyAttribute("POSName")]
        public string MenuItemName { get; set; }

        public ModifierFlagV3 ModifierFlag { get; set; }

        //bug 4729
        public bool IsIncluded { get; set; }

        public bool IsPriceOverridden { get; set; }

        #endregion

        public List<ItemScheduleModelV1> Schedules { get; set; }
    }

    public class ModifierFlagV3
    {
        public string Name { get; set; }

        public int Code { get; set; }
    }

    public class ItemModelV4 : ItemBase
    {
        public ItemModelV4()
        {
            Children = new List<ChildModelV4>();
            PrependItems = new List<ChildModelV4>();
        }

        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }
        
        public int Index { get; set; }
        
        //Indicates to the item and children hierarchically
        public bool SendHierarchy { get; set; }

        //TBD: 
        //indicates the item must be priced and ordered as a top level item
        public bool IsTopLevel { get; set; }

        //item can be ordered without presenting collections
        public bool QuickOrder { get; set; }

        //indicates whether the item is a combo.
        public bool Combo { get; set; }

        public bool AutoSelect { get; set; }

        public bool IsAvailable { get; set; }

        //public bool ShowPrice { get; set; }

        public bool IsAlcohol { get; set; }

        public string DeepLinkId { get; set; }

        [JsonPropertyAttribute("AlternateID")]
        public string AlternatePLU { get; set; }

        [JsonPropertyAttribute("POSName")]
        public string POSItemName { get; set; }
        
        public ModifierFlagV3 ModifierFlag { get; set; }

        //bug 4729
        public bool IsIncluded { get; set; }

        public bool IsPriceOverridden { get; set; }

        public int Feeds { get; set; }

        [JsonIgnore]
        public List<ItemScheduleModelV1> Schedules { get; set; }

        public ScheduleModelV2 Schedule { get; set; }

        // Collections/assets of items associated with this item.
        public List<ChildModelV4> Children { get; set; }

        public List<ChildModelV4> PrependItems { get; set; }

    }
}