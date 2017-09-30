using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Phoenix.DataAccess;

namespace Phoenix.Web.Models
{
    public class SiteModel
    {
        public SiteModel()
        {
            SiteTimeZone = TimeZoneInfo.Utc;
            CuisinesOffered = new List<CuisineModel>();
            ServicesOffered = new List<SiteServiceModel>();
        }
        //Iris Id - Unique Id
        [Display(Name = "SiteId")]
        public long IrisId { get; set; }

        [Display(Name = "SiteId")]
        public Guid SiteId { get; set; }

        [Required(ErrorMessage = "Store Number is required")]
        [StringLength(16, ErrorMessage = "The {0} must be less than 16 characters long.")]
        [Display(Name = "Store Number")]
        public string StoreNumber { get; set; }

        [Display(Name = "Address1")]
        [StringLength(256, ErrorMessage = "The {0} must be less than 256 characters long.")]
        public string Address1 { get; set; }

        [Display(Name = "Address2")]
        [StringLength(256, ErrorMessage = "The {0} must be less than 256 characters long.")]
        public string Address2 { get; set; }

        [Display(Name = "City")]
        [StringLength(64, ErrorMessage = "The {0} must be less than 64 characters long.")]
        public string City { get; set; }

        [Display(Name = "State")]
        [StringLength(64, ErrorMessage = "The {0} must be less than 64 characters long.")]
        public string State { get; set; }

        [Display(Name = "Zip")]
        [StringLength(10, ErrorMessage = "The {0} must be less than 10 characters long.")]
        public string Zip { get; set; }

        [Display(Name = "Phone")]
        //[RegularExpression(RegexPattern.PhoneNumber, ErrorMessage = "Please enter valid phone number.")]
        //[StringLength(10, ErrorMessage = "The {0} must be 10 characters long.")]
        public string Phone { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.000000}", ApplyFormatInEditMode = true)]
        [Display(Name = "Latitude")]
        //[RegularExpression(@"(-?[0-8]?[0-9](\.\d*)?)|-?90(\.[0]*)?", ErrorMessage = "Please enter valid latitude.")]
        [Range(-90, 90, ErrorMessage = "Please enter valid latitude.")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        //[RegularExpression(@"(-?([1]?[0-7][1-9]|[1-9]?[0-9])?(\.\d*)?)|-?180(\.[0]*)?", ErrorMessage = "Please enter valid longitude.")]
        [Range(-180, 180, ErrorMessage = "Please enter valid longitude.")]
        public decimal? Longitude { get; set; }

        [Display(Name = "Time Zone")]
        public TimeZoneInfo SiteTimeZone { get; set; }

        public string SiteTimeZoneId { get; set; }

        public int NetworkObjectId { get; set; }

        public string Name { get; set; }

        public int? ParentNetworkObjectId { get; set; }

        public List<SiteServiceModel> ServicesOffered { get; set; }

        [Display(Name = "Cuisines Offered")]
        public List<CuisineModel> CuisinesOffered { get; set; }

        public string CuisinesAdded { get; set; }
        public string CuisinesRemoved { get; set; }
        private NetworkObjectModel _networkObject;
        public NetworkObjectModel NetworkObject
        {
            get
            {
                if (_networkObject == null)
                {
                    _networkObject = new NetworkObjectModel();
                }
                return _networkObject;
            }
            set { _networkObject = value; }
        }

        public DateTime LastUpdatedDate { get; set; }
    }

    public class SiteServiceModel
    {
        public int SerivceNetworkObjectLinkId { get; set; }
        public int ServiceTypeId { get; set; }
        [Display(Name = "Service Type")]
        public string ServiceTypeName { get; set; }

        [DisplayFormat(DataFormatString = "{0:n2}", ApplyFormatInEditMode = true)]
        public decimal? Fee { get; set; }
        [Display(Name = "Minimum Order")]
        public int? MinOrder { get; set; }
        [Display(Name = "Est Time(in min)")]
        public int? EstimatedTime { get; set; }
        [Display(Name = "Tax")]
        public int? TaxTypeId { get; set; }
        [Display(Name = "Area Covered(in miles)")]
        public int? AreaCovered { get; set; }
        public bool ToDelete { get; set; }
    }

    public class CuisineModel
    {
        public int CuisineId { get; set; }
        public string CuisineName { get; set; }
    }

    public class NetworkObjectModel
    {
        [Display(Name = "SiteId")]
        public long IrisId { get; set; }

        [Required(ErrorMessage = "Restaurant Name is required")]
        //bug 4738 - Increase Internal Name character limit
        [StringLength(128, ErrorMessage = "The {0} must be less than {1} characters long.")]
        [Display(Name = "Restaurant Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Market Name is required")]
        [Display(Name = "Market")]
        public int? ParentNetworkObjectId { get; set; }
    }

    public class GroupModel
    {
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        [Display(Name = "NetowrkObjectId")]
        public int NetowrkObjectId { get; set; }

        [Required(ErrorMessage = "Group Name is required")]
        //bug 4738 - Increase Internal Name character limit
        [StringLength(128, ErrorMessage = "The {0} must be less than {1} characters long.")]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; }

        [Display(Name = "Group Type")]
        public string GroupType { get; set; }

        public string GroupTypeId { get; set; }

        [Display(Name = "Parent Name")]
        public string ParentName { get; set; }

        public int ParentNetworkObjectId { get; set; }
    }


    public class NetworkModel
    {
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        [Display(Name = "Id")]
        public int Id { get; set; }

        [Display(Name = "Name")]
        //bug 4738 - Increase Internal Name character limit
        [StringLength(128, ErrorMessage = "The {0} must be less than {1} characters long.")]
        public string Name { get; set; }

        [Display(Name = "ItemType")]
        public NetworkObjectTypes ItemType { get; set; }

        [Display(Name = "HasChildren")]
        public bool HasChildren { get; set; }

        [Display(Name = "Image")]
        public string Image { get; set; }
    }

    public class TreeNetworkModel : NetworkModel
    {
        [Display(Name = "HasAccess")]
        public bool HasAccess { get; set; }

        [Display(Name = "Items")]
        public List<TreeNetworkModel> items { get; set; }

        [Display(Name = "features")]
        public List<int> Features { get; set; }
        public bool expanded { get; set; }

        public int? parentId { get; set; }
    }

}