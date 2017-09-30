using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents a menu. This class provides minimalistic details like id and version.
    /// </summary>
    public class MenuModel
    {
        // Id that uniquely identifies the menu.
        [JsonPropertyAttribute("Id")]
        public int MenuId { get; set; }
    }
}