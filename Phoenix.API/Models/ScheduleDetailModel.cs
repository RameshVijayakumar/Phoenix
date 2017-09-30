using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace Phoenix.API.Models
{ 
    public class ScheduleDetailModelV1
    {
        [JsonPropertyAttribute("day")]
        public string DayOfWeek { get; set; }

        [JsonPropertyAttribute("parts")]
        public List<ScheduleSubDetailModelV1> ScheduleSubDetails { get; set; }
    }

    public class ScheduleSubDetailModelV1
    {
        [JsonPropertyAttribute("name")]
        public string CycleName { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }
    }

    public class ItemScheduleDetailModelV1
    {
        [JsonPropertyAttribute("day")]
        public string DayOfWeek { get; set; }

        [JsonPropertyAttribute("parts")]
        public List<ItemScheduleSubDetailModelV1> ScheduleSubDetails { get; set; }
    }

    public class ItemScheduleSubDetailModelV1
    {
        [JsonPropertyAttribute("name")]
        public string CycleName { get; set; }

        public int? PLU { get; set; }
    }

    public class CatScheduleDetailModelV1
    {
        [JsonPropertyAttribute("day")]
        public string DayOfWeek { get; set; }

        [JsonPropertyAttribute("parts")]
        public List<CatScheduleSubDetailModelV1> ScheduleSubDetails { get; set; }
    }

    public class CatScheduleSubDetailModelV1
    {
        [JsonPropertyAttribute("name")]
        public string CycleName { get; set; }      
    }

    public class ScheduleModelV2
    {
        public ScheduleModelV2()
        {
            Shows = new List<ScheduleDetailModelV2>();
            Hides = new List<ScheduleDetailModelV2>();
        }
        public List<ScheduleDetailModelV2> Shows { get; set; }
        public List<ScheduleDetailModelV2> Hides { get; set; }
    }


    public class ScheduleDetailModelV2
    {
        public ScheduleDetailModelV2()
        {
            ScheduleSubDetails = new List<string>();
        }
        [JsonPropertyAttribute("day")]
        public string DayOfWeek { get; set; }

        [JsonPropertyAttribute("parts")]
        public List<string> ScheduleSubDetails { get; set; }

    }
}