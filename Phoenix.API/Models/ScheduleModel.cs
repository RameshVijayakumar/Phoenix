using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Represents an object that lists all available sites.
    /// </summary>
    public class ScheduleListModelV1
    {
        public DateTime LastUpdatedDate { get; set; } 

        public List<ScheduleModelV1> Schedules { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ScheduleListModelV1()
        {
            Schedules = new List<ScheduleModelV1>();
        }
    }

    public class ScheduleModelV1
    {
        //Iris Id - Unique Id
        [JsonPropertyAttribute("Id")]
        public long IrisId { get; set; }
        
        [JsonPropertyAttribute("name")]
        public string SchName { get; set; }

        public int Priority { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime LastUpdatedDate { get; set; }

        [JsonPropertyAttribute("dayparts")]
        public List<ScheduleDetailModelV1> SchDetails { get; set; }
    }

    public class ItemScheduleModelV1
    {
        [JsonPropertyAttribute("schedule")]
        public string SchName { get; set; }

        [JsonPropertyAttribute("dayparts")]
        public List<ItemScheduleDetailModelV1> SchDetails { get; set; }
    }

    public class CategoryScheduleModelV1
    {
        [JsonPropertyAttribute("schedule")]
        public string SchName { get; set; }

        [JsonPropertyAttribute("dayparts")]
        public List<CatScheduleDetailModelV1> SchDetails { get; set; }
    }
}