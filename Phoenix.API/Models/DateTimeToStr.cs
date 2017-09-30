using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Omu.ValueInjecter;

namespace Phoenix.API.Models
{
    /// <summary>
    /// Convention class for OMU ValueInjector to convert DateTime properties to string
    /// </summary>
    public class DateTimeToStr : ConventionInjection
    {
        /// <summary>
        /// If the source type is a DateTime and the Target type is string and the source value exists then convert that value.
        /// </summary>
        /// <param name="c"></param>
        /// <returns>bool</returns>
        protected override bool Match(ConventionInfo c)
        {
            return c.SourceProp.Name == c.TargetProp.Name &&
                   (c.SourceProp.Type == typeof(DateTime) || c.SourceProp.Type == typeof(DateTime?)) && c.TargetProp.Type == typeof(string) &&
                   c.SourceProp.Value != null;
        }

        protected override object SetValue(ConventionInfo ci)
        {
            return ci.SourceProp.Value.ToString();
        }
    }
}