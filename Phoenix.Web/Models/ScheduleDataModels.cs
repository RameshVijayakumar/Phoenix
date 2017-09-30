using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class ScheduleModel
    {
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        // Id that uniquely identifies a Schedule
        public int ScheduleId { get; set; }
        // bug - 4738
        [StringLength(128, ErrorMessage = "Please enter name between {2} and {1} characters long.", MinimumLength = 1)]
        [Required(ErrorMessage = "Please enter the Schedule Name")]
        [JsonPropertyAttribute("Name")]
        // name for the Schedule.
        public string SchName { get; set; }

        //Start Date of the Schedule
        public DateTime? StartDate { get; set; }

        //End Date of the Schedule
        public DateTime? EndDate { get; set; }

        //Last updated date of Schedule
        public DateTime LastUpdatedDate { get; set; }

        public int Priority { get; set; }

        //Flag to indicate if any details are changed in edit mode
        public bool HasSchDetailsChanged { get; set; }

        //Flag to indicate whether Schedule is ready to save in edit mode
        public bool SaveSchedule { get; set; }

        //list of item schedules
        public List<ItemScheduleModel> ItemSchedules { get; set; }

        //List of schedule details
        public List<SchDetailModel> SchDetails { get; set; }

        //list of schedule details in summary form to be displayed on view
        public List<ScheduleSummary> SchSummary { get; set; }

        public bool IsSchNameEditable { get; set; }

        public bool IsActive { get; set; }

        public bool IsOverride { get; set; }
        public bool IsCreatedAtThisNetwork { get; set; }
    }

    public class SchCycleModel
    {
        // Id that uniquely identifies a Schedule Cycle
        [JsonPropertyAttribute("Id")]
        public int SchCycleId { get; set; }

        // Name of the Cycle
        [Required]
        public string CycleName { get; set; }

        //position of Cycle
        public int SortOrder { get; set; }

        public bool IsOverride { get; set; }

        public bool IsCreatedAtThisNetwork { get; set; }

        public int NetworkObjectId { get; set; }

        public bool IsActive { get; set; }
    }

    public class SchDetailModel
    {
        // Id that uniquely identifies a Schedule Detail
        [JsonPropertyAttribute("Id")]
        public int SchDetailId { get; set; }

        //Determines to which schedule it belongs
        public int ScheduleId { get; set; }

        //Determines to which cycle it belongs
        public int SchCycleId { get; set; }

        public int DayofWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public int NetworkObjectId { get; set; }

        public bool IsActive { get; set; }

        //determines if it an override or not
        public int? ParentSchDetailId { get; set; }
    }

    public class ItemScheduleModel
    {
        // Id that uniquely identifies a Item Schedule
        [JsonPropertyAttribute("Id")]
        public int ItemScheduleLinkId { get; set; }

        public int ItemId { get; set; }

        public int ScheduleId { get; set; }

        public int? PLU { get; set; }

        public int NetworkObjectId { get; set; }

        public bool IsActive { get; set; }
    }
    public class CategoryScheduleModel
    {
        // Id that uniquely identifies a Item Schedule
        [JsonPropertyAttribute("Id")]
        public int CategoryScheduleLinkId { get; set; }

        public int CategoryId { get; set; }

        public int ScheduleId { get; set; }

        public int? PLU { get; set; }

        public int NetworkObjectId { get; set; }

        public bool IsActive { get; set; }
    }

    public class EntitySchSummary
    {
        public EntitySchSummary()
        {
            Cycles = new List<CycleInSchedule>();
        }
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public bool IsSelected { get; set; }
        public List<CycleInSchedule> Cycles { get; set; }
    }

    public class CycleInSchedule
    {
        public int Id { get; set; }
        public int LinkId { get; set; }
        public int SchCycleId { get; set; }
        public string CycleName { get; set; }
        public bool? IsShow { get; set; }

    }
    //Schedule Details Summary for Grid
    public class ScheduleSummary
    {
        public int ScheduleId { get; set; }
        public int SchCycleId { get; set; }
        public string CycleName { get; set; }
        public TimeSpan? SunST { get; set; }
        public TimeSpan? MonST { get; set; }
        public TimeSpan? TueST { get; set; }
        public TimeSpan? WedST { get; set; }
        public TimeSpan? ThuST { get; set; }
        public TimeSpan? FriST { get; set; }
        public TimeSpan? SatST { get; set; }
        public TimeSpan? SunET { get; set; }
        public TimeSpan? MonET { get; set; }
        public TimeSpan? TueET { get; set; }
        public TimeSpan? WedET { get; set; }
        public TimeSpan? ThuET { get; set; }
        public TimeSpan? FriET { get; set; }
        public TimeSpan? SatET { get; set; }
    }
}