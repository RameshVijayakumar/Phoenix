using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents a collection of items, which can be Modification, Substitution or Cross-sell.
    /// </summary>
    public class CollectionModel
    {
        [JsonIgnore]
        // Id that uniquely identifies a collection of items
        public int CollectionId { get; set; }
        // Type of collection.
        public string Type { get; set; }
        // Internal name for the collection.
        public string InternalName { get; set; }
        // User-friendly name of the collection.
        public string DisplayName { get; set; }
        // Indicates if items in the collection should be show with or without prices.
        public bool ShowPrice { get; set; }
        // The minimum number of items in this collection. 
        public int MinQuantity { get; set; }
        // The maximum number of items in this collection.
        public int MaxQuantity { get; set; }       
    }

    public class CollectionModelV1V2 : CollectionModel
    {
        // List of items within the collection.
        public List<ItemInCollectionModelV1V2> Items { get; set; }    
    }

    public class CollectionModelV3 : CollectionModel
    {
        // List of items within the collection.
        public List<ItemInCollectionModelV3> Items { get; set; }    

        #region V3 Attributes

        //Collection must be presented
        public bool Mandatory { get; set; }     

        //indicates whether Ziosk should propagate the selected modifier to subitems
        public bool Propagate { get; set; }

        public bool IsVisibleToGuest { get; set; }

        public bool ReplacesItem { get; set; }
        #endregion
    }

    public class CollectionModelV4 : CollectionModel
    {
        public CollectionModelV4()
        {
            Children = new List<ItemChildModelV4>();
        }

        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }
        
        public int Index { get; set; }
        // List of items within the collection.
        public List<ItemChildModelV4> Children { get; set; }
        
        //Collection must be presented
        public bool Mandatory { get; set; }

        //indicates whether Ziosk should propagate the selected modifier to subitems
        public bool Propagate { get; set; }

        public bool IsVisibleToGuest { get; set; }

        public bool ReplacesItem { get; set; }
    }
}