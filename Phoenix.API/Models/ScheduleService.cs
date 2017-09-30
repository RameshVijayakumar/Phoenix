using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data;
using Phoenix.DataAccess;
using Phoenix.Common;
using Omu.ValueInjecter;
using Phoenix.API;
using System.Text;
using System.Configuration;
using Phoenix.RuleEngine;
using System.Net;
using Microsoft.WindowsAzure;

namespace Phoenix.API.Models
{
    public class ScheduleService : IScheduleService
    {
        private IRepository _repository;
        private RuleService _ruleService;
        private DbContext _context;

        private AppConfiguration _config;
        private AuthenticationService _authenticationService;

        private string _lastActionResult;
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        private string _clientId;
        public string ClientID
        {
            get { return _clientId; }
            set { _clientId = value; }
        }

        public ScheduleService()
        {
            //TODO: inject these interfaces
            _ruleService = new RuleService(RuleService.CallType.API);
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);

            _config = new AppConfiguration();
            if (_config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
            {
                _authenticationService = new AuthenticationService(_config);
            }
        }

        /// <summary>
        /// This method is to get all the schedules for the provided siteId and version number.
        /// Optionally, comma-seperated schedule names can also be passed; depending on which the data is filtered.
        /// </summary>
        /// <param name="siteIrisId"></param>
        /// <param name="version"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        public dynamic GetSchedules(long siteIrisId, string version, out HttpStatusCode status, string channel = null)
        {
            status = HttpStatusCode.OK;
            dynamic returnModelList = null;
            dynamic returnModel = null;
            var scheduleLastUpdateDateList = new List<dynamic>();
            switch (version)
            {
                case "V1":
                    returnModelList = new ScheduleListModelV1();
                    break;
                default:
                    break;
            }
            try
            {
                var schedules = new List<Phoenix.DataAccess.Schedule>();
                var networkObjId = -1;

                SiteInfo siteInfo = null;
                var retSIState = getSiteInfo(siteIrisId, ref siteInfo);
                if (retSIState == SiteInfoState.NoIssue)
                {
                    networkObjId = siteInfo.NetworkObjectId;
                    _ruleService.NetworkObjectId = networkObjId;
                    // get all Schedules links for this networkObject(Site)
                    schedules = _ruleService.GetScheduleList(networkObjId);

                    //If a Channel exists, apply filter on schedule name ,'SchName'
                    if (channel != null)
                    {
                        var scheduleNames = channel.ToLower().Split(',');
                        schedules = (from m in schedules
                                     where scheduleNames.Contains(m.SchName.ToLower())
                                     select m).ToList();
                    }

                    foreach (var schedule in schedules)
                    {
                        if (schedule != null)
                        {
                            switch (version)
                            {
                                case "V1":
                                    returnModel = new ScheduleModelV1();
                                    break;
                                default:
                                    break;
                            }
                            if (getScheduleModel(schedule, returnModel, networkObjId, version))
                            {
                                returnModelList.Schedules.Add(returnModel);
                                scheduleLastUpdateDateList.Add(returnModel.LastUpdatedDate);
                            }
                            else
                            {
                                status = HttpStatusCode.NotFound;
                                _lastActionResult = string.Format("No schedule found for site {0} {1}, id {2}",
                                     siteInfo.NetworkObject.Name, siteInfo.StoreNumber, siteInfo.NetworkObject.IrisId);
                            }
                        }
                    }

                    //Get the max of last updated date
                    if (returnModelList != null && returnModelList.Schedules.Count > 0)
                    {
                        returnModelList.LastUpdatedDate = scheduleLastUpdateDateList.Max();
                    }
                }
                else
                {
                    switch (retSIState)
                    {
                        case SiteInfoState.InvalidId:
                            {
                                status = HttpStatusCode.BadRequest;
                                _lastActionResult = "Invalid site id";
                                break;
                            }
                        case SiteInfoState.NotFound:
                            {
                                status = HttpStatusCode.NotFound;
                                _lastActionResult = "No Site found";
                                break;
                            }
                        case SiteInfoState.NotAccessible:
                            {
                                status = HttpStatusCode.Forbidden;
                                _lastActionResult = "Access to this resource is forbidden";
                                break;
                            }
                        case SiteInfoState.Error:
                            {
                                status = HttpStatusCode.InternalServerError;
                                _lastActionResult = "Unexpected error occured.";
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                status = HttpStatusCode.InternalServerError;
                _lastActionResult = "Unexpected error occurred";
                //log exception
                Logger.WriteError(e);
            }
            return returnModelList;
        }

        /// <summary>
        /// This method is to prepare the ScheduleModel which represents a single object of 'Schedule'
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="returnModel"></param>
        /// <param name="networkObjId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private bool getScheduleModel(Phoenix.DataAccess.Schedule schedule, dynamic returnModel, int networkObjId, string version = "")
        {
            bool blnValidResult = false;

            returnModel.IrisId = schedule.IrisId;
            returnModel.SchName = schedule.SchName;
            returnModel.StartDate = schedule.StartDate;
            returnModel.EndDate = schedule.EndDate;
            //TODO : Change how LastUpdatedDate is retrieved
            returnModel.LastUpdatedDate = schedule.LastUpdatedDate;
            returnModel.Priority = schedule.Priority;
            returnModel.SchDetails = getSchDetailModel(networkObjId, schedule.ScheduleId, version);
            blnValidResult = true;

            return blnValidResult;
        }

        /// <summary>
        /// This method is to populate the Schedule Details of a specific schedule
        /// </summary>
        /// <param name="networkObjId"></param>
        /// <param name="scheduleId"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private dynamic getSchDetailModel(int networkObjId, int scheduleId, string version = "")
        {
            dynamic schDetailListModel = null;
            dynamic schDetailModel = null;
            dynamic scheduleSubDetailListModel = null;
            dynamic scheduleSubDetailModel = null;
            switch (version)
            {
                case "V1":
                    schDetailListModel = new List<ScheduleDetailModelV1>();
                    break;
                default:
                    break;
            }

            var schDetailList = _ruleService.GetScheduleDetails(scheduleId, networkObjId);
            if (schDetailList != null && schDetailList.Count > 0)
            {
                // The following statement gets a list of distinct 'DayofWeek' in the list of schedule details of this schedule
                var daysInDayParts = schDetailList.Select(p => p.DayofWeek).Distinct();

                //for each 'day' in the above list 'daysInDayParts' , parts(which includes Cycle Name, StartTime and EndTime) is added.
                foreach (var dayInDayParts in daysInDayParts)
                {
                    switch (version)
                    {
                        case "V1":
                            schDetailModel = new ScheduleDetailModelV1();
                            scheduleSubDetailListModel = new List<ScheduleSubDetailModelV1>();
                            break;
                        default:
                            break;
                    }
                    schDetailModel.DayOfWeek = ((System.DayOfWeek)dayInDayParts).ToString();
                    var partsOfDay = schDetailList.Where(p => p.DayofWeek == dayInDayParts);
                    foreach (var partOfDay in partsOfDay)
                    {
                        switch (version)
                        {
                            case "V1":
                                scheduleSubDetailModel = new ScheduleSubDetailModelV1();
                                break;
                            default:
                                break;
                        }
                        scheduleSubDetailModel.CycleName = partOfDay.SchCycle.CycleName;
                        scheduleSubDetailModel.StartTime = partOfDay.StartTime.ToString(@"hh\:mm");
                        scheduleSubDetailModel.EndTime = partOfDay.EndTime.ToString(@"hh\:mm");
                        scheduleSubDetailListModel.Add(scheduleSubDetailModel);
                    }
                    schDetailModel.ScheduleSubDetails = scheduleSubDetailListModel;
                    schDetailListModel.Add(schDetailModel);
                }
            }
            return schDetailListModel;
        }

        private SiteInfoState getSiteInfo(long siteIrisId, ref SiteInfo siteInfo)
        {
            if (siteIrisId != 0)
            {
                // CHECK if site id is valid
                var network = _repository.GetQuery<NetworkObject>(s => s.IrisId == siteIrisId && s.NetworkObjectTypeId == NetworkObjectTypes.Site).Include("SiteInfoes").FirstOrDefault();
                if (network != null)
                {
                    siteInfo = network.SiteInfoes.FirstOrDefault();
                    if (siteInfo == null)
                        return SiteInfoState.InvalidId;

                    var status = HttpStatusCode.OK;
                    if (_authenticationService.IsNetworkAccessible(ClientID, siteIrisId,out status))
                    {
                        return SiteInfoState.NoIssue;
                    }
                    else
                    {
                        return status == HttpStatusCode.Forbidden ? SiteInfoState.NotAccessible : SiteInfoState.Error;
                    }
                }
                else
                {
                    return SiteInfoState.NotFound;
                }
            }
            else
                return SiteInfoState.InvalidId;
        }

    }

    public interface IScheduleService
    {
        string LastActionResult { get; }
        string ClientID { get; set; }
        dynamic GetSchedules(long siteIrisId, string version, out HttpStatusCode status, string channel = null);
    }
}