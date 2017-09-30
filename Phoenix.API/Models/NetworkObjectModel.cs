using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents an object that lists all available sites.
    /// </summary>
    public class SiteListModel
    {
        public List<SiteInfoModel> Sites { get; set; }
    }

    /// <summary>
    /// Represents a site.
    /// </summary>
    public class SiteInfoModel
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonPropertyAttribute("Id")]
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id is GUID as a string.
        [JsonIgnore]
        public string SiteId { get; set; }
        [JsonPropertyAttribute("Name")]
        // Store name.
        public string StoreName { get; set; }
        // ASI Restaurant Number
        [JsonPropertyAttribute("StoreNumber")]
        public string StoreNumber { get; set; }
        // Id of the menu that is available at the site.
        //public int? Menu { get; set; }

        public DateTime? LastMenuUpdate { get; set; }
        // List of categories in a menu.
        public List<SiteModelMenu> Menus { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SiteInfoModel()
        {
            Menus = new List<SiteModelMenu>();
        }

    }

    /// <summary>
    /// Represents a site.
    /// </summary>
    public class SiteModelMenu
    {
        public long MenuId { get; set; }
        public string Name { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    /// <summary>
    /// Represents a site.
    /// </summary>
    public class SiteModel
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SiteModel()
        {
            Menus = new List<SiteModelMenu>();
            Services = new List<SiteServiceModel>();
        }
        [JsonPropertyAttribute("Id")]
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id is GUID as a string.
        [JsonIgnore]
        public string SiteId { get; set; }

        [JsonPropertyAttribute("Name")]
        // Store name.
        public string StoreName { get; set; }
        // Store Number
        public string StoreNumber { get; set; }

        // Market to which this market belongs.
        //public string Market { get; set; }
        // Corporate or franchise group to which this site belongs.
        public string Group { get; set; }
        // Store’s physical street address.
        public string Street { get; set; }
        // City in which the store is located.
        public string City { get; set; }
        // State in which the store is located.
        public string State { get; set; }
        // 7-digit zip code in which the store is located.
        public string Zip { get; set; }
        // Id of the menu that is available at the site.
        //public int? Menu { get; set; }

        public string Phone { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [JsonPropertyAttribute("TimeZone")]
        public string SiteTimeZoneId { get; set; }

        //public TimeZoneInfo SiteTimeZone { get; set; }

        public List<SiteServiceModel> Services { get; set; }

        public List<string> Cuisines { get; set; }

        public DateTime? LastMenuUpdate { get; set; }

        public DateTime? LastSiteUpdated { get; set; }
        // List of categories in a menu.
        public List<SiteModelMenu> Menus { get; set; }
    }

    public class SiteServiceModel
    {
        [JsonIgnore]
        public int SerivceNetworkObjectLinkId { get; set; }

        [JsonIgnore]
        public int ServiceTypeId { get; set; }

        [JsonPropertyAttribute("ServiceType")]
        public string ServiceTypeName { get; set; }

        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public decimal? Fee { get; set; }

        public int? MinOrder { get; set; }

        public int? EstimatedTime { get; set; }

        [JsonPropertyAttribute("Tax")]
        public int? TaxTypeId { get; set; }

        [JsonPropertyAttribute("AreaCovered")]
        public int? AreaCovered { get; set; }
    }
    public class BrandListModel
    {
        public List<BrandModel> Brands { get; set; }
    }

    public class BrandModel
    {
        [JsonPropertyAttribute("Id")]
        public long  IrisId { get; set; }
        public string Name { get; set; }
    }

    public enum SiteInfoState
    {
        NoIssue = 0,
        InvalidId = 1,
        NotFound = 2,
        NotAccessible = 3,
        Error = 4
    };
}