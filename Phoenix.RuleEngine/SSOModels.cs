using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.RuleEngine
{
    public class UserInfo
    {
        public UserInfo()
        {
            Roles = new List<string>();
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Application { get; set; }
        public bool IsAuthorized { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public List<string> Roles { get; set; }
        [JsonPropertyAttribute("nodes")]
        public List<long> IrisIds { get; set; }
    }
    public class NetworkPermissions
    {
        public long Id { get; set; }
        public int NetworkItemId { get; set; }
        public string NetworkType { get; set; }
        public bool HasAccess { get; set; }
        public int NetworkLevel { get; set; }
    }

    public class ResponseMessage
    {
        public string Message { get; set; }
    }
}
