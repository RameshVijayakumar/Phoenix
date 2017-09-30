using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace PM.API.Models
{
    public class TagModel
    {
        [JsonPropertyAttribute("Id")]
        public int TagId { get; set; }

        [JsonPropertyAttribute("Name")]
        public string TagName { get; set; }   
    }
}