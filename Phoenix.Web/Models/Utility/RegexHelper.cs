using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public static class RegexPattern
    {
        public const string AlphaNumeric = "^[a-zA-Z0-9]+$";
        public const string AlphaNumeric01 = "^[A-Za-z][.A-Za-z0-9 _-]{1,99}$";
        public const string FileNamewithoutSpecialChar = @"(?:[^a-z0-9. _-]|(?<=['\""]))";
        public const string GUID = @"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$";
        public const string HostName = @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$";
        public const string IPAddress = @"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$";
        public const string PhoneNumber = @"^(?:(?:[\+]?(?<CountryCode>[\d]{1,3}(?:[ ]+|[\-.])))?[(]?(?<AreaCode>[\d]{3})[\-/)]?(?:[ ]+)?)?(?<Number>[a-zA-Z2-9][a-zA-Z0-9 \-.]{6,})(?:(?:[ ]+|[xX]|(i:ext[\.]?)){1,2}(?<Ext>[\d]{1,5}))?$";
        public const string Version = @"^(\d{1,4})\.(\d{1,4})\.(\d{1,4})\.(\d{1,4})$";
        public const string ZipCode = @"^\d{5}(-\d{4})?$";
    }
}