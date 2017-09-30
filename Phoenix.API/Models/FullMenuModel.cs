using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    public class FullMenuModelsV4
    {
        [JsonPropertyAttribute("Version")]
        // Version for the all menus.
        public string Version { get; set; }

        [JsonPropertyAttribute("menus")]
        public List<FullMenuModelV4> Menus { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FullMenuModelsV4()
        {
            Menus = new List<FullMenuModelV4>();
        }
    }

    /// <summary>
    /// Represents the entire menu including id, name, version, categories, items, collections, etc.
    /// </summary>
    public class FullMenuModelV4
    {
        public FullMenuModelV4()
        {
            Children = new List<ChildModelV4>();
            Categories = new List<CategoryModelV4>();
            Items = new List<ItemModelV4>();
            Modifications = new List<CollectionModelV4>();
            Substitutions = new List<CollectionModelV4>();
            Upsells = new List<CollectionModelV4>();
            CrossSells = new List<CollectionModelV4>();
            Combos = new List<CollectionModelV4>();
            EndOfOrders = new List<CollectionModelV4>();
            Assets = new List<AssetModelV4>();
            SpecialNotices = new List<string>();
        }
        // Id that uniquely identifies a menu
        [JsonIgnore]
        public int MenuId { get; set; }

        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }

        // Menu name.
        [JsonPropertyAttribute("Name")]
        // Internal name for the menu.
        public string InternalName { get; set; }

        public List<ChildModelV4> Children { get; set; }

        public List<CategoryModelV4> Categories { get; set; }

        public List<ItemModelV4> Items { get; set; }

        public List<CollectionModelV4> Modifications { get; set; }

        public List<CollectionModelV4> Substitutions { get; set; }
        
        public List<CollectionModelV4> Upsells { get; set; }

        public List<CollectionModelV4> CrossSells { get; set; }

        public List<CollectionModelV4> Combos { get; set; }

        public List<CollectionModelV4> EndOfOrders { get; set; }

        public List<AssetModelV4> Assets { get; set; }

        //List of Special Notices
        public List<string> SpecialNotices { get; set; }
    }

    public class ChildModelV4 : IChildModelV4
    {
        public int Index { get; set; }

        public string Type { get; set; }
    }

    public class ItemChildModelV4 : ChildModelV4, IChildModelV4
    {
        public bool AutoSelect { get; set; }
    }

    public interface IChildModelV4
    {
        int Index { get; set; }

        string Type { get; set; }
    }
}