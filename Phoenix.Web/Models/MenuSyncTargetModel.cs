using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class MenuSyncTargetModel
    {
        public int MenuSyncTargetId { get; set; }

        [Required(ErrorMessage = "Target Name is required")]
        public string TargetName { get; set; }

        [Required(ErrorMessage = "Target URL is required")]
        [Url(ErrorMessage = "Target URL is not a valid URL")]  
        public string URL { get; set; }
     
        public string Token { get; set; }

        public string LastSyncStatus { get; set; }

        public DateTime? LastSyncDate { get; set; }

        public int NetworkObjectId { get; set; }

        public string ChannelNameList { get; set; }

        public string ChannelIdList { get; set; }

        public string OperationStatus { get; set; }

        public List<TagModel> Channels { get; set; }
    }

    public class MenuSyncTargetDetailModel
    {
        public MenuSyncTargetDetailModel()
        {
            NetworkObject = new NetworkObjectModel();
        }
        public int MenuSyncTargetDetailId { get; set; }
        public long SyncIrisId { get; set; }
        public int TargetId { get; set; }
        public string TargetName { get; set; }
        public int NetworkObjectId { get; set; }
        public NetworkObjectModel NetworkObject { get; set; }
        public string MenuName { get; set; }
        public string SyncStatus { get; set; }
        public string SyncMessage { get; set; }
        public DateTime RequestedTime { get; set; }
        public DateTime? ResponseTime { get; set; }
    }

    public class MenuSyncModel
    {
        public long Id { get; set; }
        public List<string> Sites { get; set; }
    }

    public class CheckedSiteModel
    {
        public int Id { get; set; }
        public string Typ { get; set; }
        public string Name { get; set; }
    }
}


