using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.API.Models
{

    public class ItemInCategoryModelV1V2 : ItemModelV1V2
    {
        // Indicates if this is an alcoholic item.  
        public bool IsAlcohol { get; set; }
        public List<AssetModelV1V2> Assets { get; set; }
    }

    public class ItemInCategoryModelV3 : ItemModelV3
    {
        public List<AssetModelV3> Assets { get; set; }
    }
    
}