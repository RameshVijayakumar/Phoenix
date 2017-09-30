using System.Web.Http;
using Phoenix.API.Models;
using System.Net;
using System.Net.Http;
using System.Security.Claims;

namespace Phoenix.API.Controllers
{
    public class SchedulesController : ApiController
    {
        private IScheduleService _scheduleService;

        public SchedulesController()
        {
            //TODO: inject this interface
            _scheduleService = new ScheduleService();
            _scheduleService.ClientID = string.Empty;
            foreach (var existingClaim in ((ClaimsIdentity)User.Identity).Claims)
            {
                if (existingClaim.Type == "appid")
                {

                    _scheduleService.ClientID = existingClaim.Value;
                }

            }

        }     

        /// <summary>
        /// GET: get specfic version of all the schedules(and related information) of Site by passing ID and/or channel(which is comman seperated schedule names) 
        /// </summary>
        /// <param name="id">SiteId</param>
        /// <param name="version"></param>
        /// <param name="channel">comman seperated menu names</param>
        /// <returns></returns>
        [Authorize]
        public dynamic Get(long id, string version, string channel = null)
        {
            HttpStatusCode status = HttpStatusCode.OK;
            var model = _scheduleService.GetSchedules(id, version.ToUpper(),out status, channel);

            if (status != HttpStatusCode.OK)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(status, _scheduleService.LastActionResult));
            }
            return model;
        }        
    }
}
