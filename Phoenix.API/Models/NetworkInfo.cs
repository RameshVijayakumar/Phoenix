using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.API.Models
{
    public class NetworkPayloadInfo
    {
        public string Id { get; set; }

        public string Hash { get; set; }

        public string PreviousHash { get; set; }

        public DateTime? LastUpdated { get; set; }

        public RootInfo Payload { get; set; }
    }

    public class SyncStatusModel
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Msg { get; set; }
    }

    public class NetworkInfoBase
    {
        public long IrisId;
        public string Name;
        [JsonIgnore]
        public int Id;
        public bool IsActive;
        public string DataId;
    }
    public class RootInfo : NetworkInfoBase
    {
        public List<BrandInfo> Brands;
    }

    public class BrandInfo : NetworkInfoBase
    {
        public List<FranchiseInfo> Franchises;
    }

    public class FranchiseInfo : NetworkInfoBase
    {
        public List<MarketInfo> Markets;
    }

    public class MarketInfo : NetworkInfoBase
    {
        public List<NetworkSiteInfo> Sites;
    }

    public class NetworkSiteInfo : NetworkInfoBase
    {
        public int StoreNumber;
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public List<GroupInfo> Groups;
    }

    public class GroupInfo : NetworkInfoBase
    {

    }

    public class SyncInfo
    {
        public int Updated { get; set; }
        public int Created { get; set; }
        public int Deleted { get; set; }
    }
}