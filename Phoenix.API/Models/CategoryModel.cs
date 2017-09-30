using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    public class CategoryModel
    {
        [JsonIgnore]
        public int CategoryId { get; set; }

        public string InternalName { get; set; }

        public string DisplayName { get; set; }

        public bool IsFeatured { get; set; }

        public bool ShowPrice { get; set; }

        [JsonPropertyAttribute("Type")]
        public string CategoryType { get; set; }
    }

    /// <summary>
    /// Represents a category in the menu.
    /// </summary>
    public class CategoryModelV1V2 : CategoryModel
    {
        // Assets linked to categories
        public List<AssetModelV1V2> Assets { get; set; }

        // List of items within the category.
        public List<ItemInCategoryModelV1V2> Items { get; set; }
        // List of child categories within the category.
        public List<CategoryModelV1V2> Categories { get; set; }
    }

    public class CategoryModelV3 : CategoryModel
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Assets linked to categories
        public List<AssetModelV3> Assets { get; set; }

        // List of items within the category.
        public List<ItemInCategoryModelV3> Items { get; set; }

        // List of child categories within the category.
        public List<CategoryModelV3> Categories { get; set; }

        public List<CategoryScheduleModelV1> Schedules { get; set; }
    }

    public class CategoryModelV4 : CategoryModel
    {
        public CategoryModelV4()
        {
            Children = new List<ChildModelV4>();
        }

        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }
        
        public int Index { get; set; }

        public string DeepLinkId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // All children linked to categories
        public List<ChildModelV4> Children { get; set; }

        public ScheduleModelV2 Schedule { get; set; }
    }
}