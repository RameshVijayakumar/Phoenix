
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace Phoenix.Common
{
    public static class HTTPRequestHelper
    {
        public enum HTTPMethod { GET, POST, PUT, DELETE };

        /// <summary>
        /// Helper method for making REST calls.
        /// </summary>
        /// <param name="url">Final url for REST service. If parameters are to be passed, the caller must ensure they are included</param>
        /// <param name="method">HTTP method like GET/PUT/POST/etc</param>
        /// <param name="contentType">Optional. Request content type. When null/empty, it is set to empty string</param>
        /// <param name="data">Optional. Data that is to posted in the REST call body. Content length is updated from this string.</param>
        /// <param name="statusCode">HttpStatusCode value returned from the service. It will be null if response object is not received</param>
        /// <param name="responseMessage">Response from server after the call is comopleted</param>
        /// <returns>true if status OK is returned from the server, else false.</returns>
        public static bool Call(string url, HTTPMethod method, string contentType, string data, out HttpStatusCode? statusCode, out string responseMessage)
        {
            bool retStatus = true;
            statusCode = null;
            responseMessage = string.Empty;

            Uri address = new Uri(url);

            // Create the web request  
            HttpWebRequest request = WebRequest.Create(address) as HttpWebRequest;

            // set method type
            request.Method = method.ToString();
            request.ContentType = string.IsNullOrEmpty(contentType) ? string.Empty : contentType;
            request.ContentLength = 0;

            try
            {
                if (!string.IsNullOrEmpty(data))
                {

                    // Create a byte array of the data we want to send  
                    byte[] byteData = UTF8Encoding.UTF8.GetBytes(data);

                    // Set the content length in the request headers  
                    request.ContentLength = byteData.Length;

                    // Write data  
                    using (Stream postStream = request.GetRequestStream())
                    {
                        postStream.Write(byteData, 0, byteData.Length);
                    }

                }

                // Get response  
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    // Get the response stream  
                    StreamReader reader = new StreamReader(response.GetResponseStream());

                    statusCode = response.StatusCode;
                    responseMessage = reader.ReadToEnd();

                }
            }
            catch (WebException wex)
            {
                retStatus = false;
                System.Net.HttpWebResponse resp = (System.Net.HttpWebResponse)wex.Response;

                if (resp == null)
                {
                    // the call did not reach the service
                    responseMessage = string.Format("{0}: {1} {2}",
                        wex.Status,
                        wex.Message,
                        wex.InnerException != null ? wex.InnerException.Message : string.Empty);
                }
                else
                {
                    // the call made it to the service but application error occurred
                    statusCode = resp.StatusCode;
                    responseMessage = (new StreamReader(resp.GetResponseStream())).ReadToEnd();
                }
            }
            return retStatus;
        }

        /// <summary>
        /// Overload #1
        /// </summary>
        public static bool CallPOST(string url, out HttpStatusCode? statusCode, out string responseMessage)
        {
            HTTPMethod method = HTTPMethod.POST;
            string contentType = null;
            string data = null;

            return Call(url, method, contentType, data, out statusCode, out responseMessage);
        }

        /// <summary>
        /// Overload #2
        /// </summary>
        public static bool CallGET(string url, out HttpStatusCode? statusCode, out string responseMessage)
        {
            HTTPMethod method = HTTPMethod.GET;
            string contentType = null;
            string data = null;

            return Call(url, method, contentType, data, out statusCode, out responseMessage);
        }
    }

}
