using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Phoenix.API.Models;
using Phoenix.Common;
using System.Diagnostics;

namespace Phoenix.API
{
    public class ApiUsageLogger : DelegatingHandler
    {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {

            Stopwatch sw = Stopwatch.StartNew();
            // Extract the request logging information
            var requestLoggingInfo = ExtractLoggingInfoFromRequest(request);

            // Execute the request, this does not block
            var response = base.SendAsync(request, cancellationToken);

            // Log the incoming data to the database
            WriteLoggingInfo(requestLoggingInfo, sw.Elapsed.TotalSeconds);

            //// Once the response is processed asynchronously, log the response data
            //// to the database
            //response.ContinueWith((responseMsg) =>
            //{
            //    // Extract the response logging info then persist the information
            //    //var responseLoggingInfo = ExtractResponseLoggingInfo(requestLoggingInfo, responseMsg.Result);
            //    //WriteLoggingInfo(responseLoggingInfo);
            //});
            return response;
        }

        public void WriteLoggingInfo(ApiLoggingInfo requestInfo , double elapsedTotalSeconds)
        {
            var s = string.Format("URL {0} | Caller {1} | Time {2:0.000}s ", requestInfo.UriAccessed, requestInfo.IPAddress, elapsedTotalSeconds);
            Logger.WriteAPIAudit(s);
        }
        private ApiLoggingInfo ExtractLoggingInfoFromRequest(HttpRequestMessage request)
        {
            var info = new ApiLoggingInfo();
            info.HttpMethod = request.Method.Method;
            info.UriAccessed = request.RequestUri.AbsoluteUri;
            info.IPAddress = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : "0.0.0.0";

            info.IPAddress2 = HttpContext.Current.Request.ServerVariables["remote_addr"];
            if (request.Content != null)
            {
                request.Content.ReadAsByteArrayAsync()
                    .ContinueWith((task) =>
                    {
                        info.BodyContent = System.Text.UTF8Encoding.UTF8.GetString(task.Result);
                        return info;

                    });
            }
            return info;
        }

        
    }
}