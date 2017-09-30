using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public static class NetworkItemHelper
    {
        public static Phoenix.DataAccess.NetworkObjectTypes GetNetworkObjectType(string networkItemType)
        {
            NetworkObjectTypes networkObjectType = Phoenix.DataAccess.NetworkObjectTypes.Site;

            Enum.TryParse(networkItemType, out networkObjectType);
            
            return networkObjectType;
        }
    }
}