//using Phoenix.API.Models;
//using Phoenix.Common;
//using System.Net;
//using System.Net.Http;
//using System.Web.Http;

//namespace Phoenix.API.Controllers
//{
//    public class POSController : ApiController
//    {

//        [System.Web.Http.HttpPost]
//        [Authorize]
//        public string MapByPLU(string siteId)
//        {
//            POSMapService posMapService = new POSMapService();

//            HttpStatusCode status = HttpStatusCode.OK;
//            var statusMsg = posMapService.AutoMapPOSByPLU(siteId,out status);
            
//            if (status != HttpStatusCode.OK)
//            {
//                throw new HttpResponseException(Request.CreateErrorResponse(status, statusMsg));
//            }
//            return statusMsg;
//        }
//    }
//}
