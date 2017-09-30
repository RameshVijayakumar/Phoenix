using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.API.Models
{

    public class MasterItemMappingModel
    {
        public long ItemId { get; set; }
        public int? PLU { get; set; }
        public List<MasterItemMap> Mappings { get; set; }
    }

    public class MasterItemMap
    {
        public long SiteId { get; set; }
        [JsonIgnore]
        public Guid SiteGuid { get; set; }
        public string Menu { get; set; }
        public int MapId { get; set; }
        public string MappedItem { get; set; }
        public int? MappedPLU { get; set; }
        public decimal? Price { get; set; }
    }

    public class MasterItemsModel
    {
        public List<MasterItemModel> Items { get; set; }
    }

    public class POSItemModel
    {
        [JsonIgnore]
        public int POSDataId { get; set; }
        [JsonPropertyAttribute("Name")]
        public string POSItemName { get; set; }
        public int? PLU { get; set; }
        public bool IsDefault { get; set; }
    }

    public class MasterItemModel
    {
        //public string ItemGUID { get; set; }
        public long ItemId { get; set; }
        public List<POSItemModel> POSItems { get; set; }
        public string MenuItemName { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class MasterItemDetailModel : MasterItemModel
    {
        public string ButtonText { get; set; }
        public string OrderName { get; set; }
        public bool IsEntreeAppetizer { get; set; }
        public bool IsBeverage { get; set; }
        public bool IsModifier { get; set; }
        public bool IsForceRecipe { get; set; }
        public bool IsPrintOnOrder { get; set; }
        public bool IsPrintRecipe { get; set; }
        public bool IsPrintOnReceipt { get; set; }
        public bool IsPrintOnSameLine { get; set; }
        public string ItemCategorization { get; set; }
        public string CookTime { get; set; }
        public string PrepOrder { get; set; }
        public string ItemSubType { get; set; }
    }
}