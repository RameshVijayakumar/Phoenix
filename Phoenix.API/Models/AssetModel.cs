using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents an asset that is associated with an item
    /// </summary>
    public class AssetModelV1V2
    {

        // Id that uniquely identifies an asset
        [JsonIgnore]
        public int AssetId { get; set; }
        // Full URL of the asset in blob storage
        public string URL { get; set; }
        // Hash of the image 
        [JsonPropertyAttribute("Hash")]
        public string FileHash { get; set; }
        // Horizontal dimension of the asset
        public int DimX { get; set; }
        // Vertical dimension of the asset
        public int DimY { get; set; }

        // Name of the asset type
        public string Type { get; set; }
    }

    public class AssetModelV3 : AssetModelV1V2
    {
        public List<string> Tags { get; set; }
    }

    public class AssetModelV4 : AssetModelV1V2
    {
        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }

        public int Index { get; set; }

        public List<string> Tags { get; set; }
    }
}