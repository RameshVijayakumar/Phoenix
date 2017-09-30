using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public static class GuidHelper
    {
        [Obsolete("Use ConvertTo methods instead")]
        public static string EncodeGuid(Guid g)
        {
            long i = 1;
            foreach (byte b in g.ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }

        /// <summary>
        /// Generates a new guid and converts it into string of 16 chars
        /// </summary>
        /// <returns></returns>
        public static string EncodeTo16()
        {
            return EncodeTo16(Guid.NewGuid());
        }

        /// <summary>
        /// Converts the input guid into string of 16 chars
        /// </summary>
        /// <returns></returns>
        public static string EncodeTo16(Guid g)
        {
            long i = 1;
            foreach (byte b in g.ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x16}", i - DateTime.Now.Ticks);
        }

        /// <summary>
        /// Generates a new guid and converts it into string of 15 chars
        /// </summary>
        /// <returns></returns>
        public static string EncodeTo15()
        {
            return EncodeTo15(Guid.NewGuid());
        }

        /// <summary>
        /// Converts the input guid into string of 15 chars
        /// </summary>
        /// <returns></returns>
        public static string EncodeTo15(Guid g)
        {
            long i = 1;
            foreach (byte b in g.ToByteArray())
            {
                i *= ((int)b + 1);
            }
            return string.Format("{0:x16}", i - DateTime.Now.Ticks).Substring(0,15);
        }

        public static string EncodeTo22()
        {
            return EncodeTo22(Guid.NewGuid());
        }

        public static string EncodeTo22(Guid g)
        {
            string encoded = Convert.ToBase64String(g.ToByteArray());
            encoded = encoded.Replace("/", "_").Replace("+", "-");
            return encoded.Substring(0, 22);
        }

        public static Guid DecodeFrom22(string guidValue)
        {
            guidValue = guidValue.Replace("_", "/").Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(guidValue + "==");
            return new Guid(buffer);
        }

    }
}