using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;
using Phoenix.RuleEngine;
using System.Text.RegularExpressions;
using SnowMaker;
using Microsoft.Practices.Unity;

namespace Phoenix.Web.Models
{
    public class ScheduleService : IScheduleService
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;

        public int ScheduleId { get; set; }
        public int NetworkObjectId { get; set; }
        public int Count { get; set; }

        //To determine overrides
        public List<int> parentNetworkNodeIds;
        public List<int> childNetworkNodeIds;
        private List<int> _itemIdsModified;
        private List<int> _catIdsModified;

        private IAuditLogService _auditLogger;
        public RuleService _ruleService { get; set; }
        private ICommonService _commonService;

        private List<MenuItemScheduleLink> _itmSchLinks;
        private List<MenuCategoryScheduleLink> _catSchLinks;
        private List<SchNetworkObjectLink> _schNetworkLinks;

        //To display the status
        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        private UniqueIdGenerator _irisIdGenerator;

        [Dependency]
        public UniqueIdGenerator IrisIdGenerator
        {
            get
            {
                return _irisIdGenerator;
            }
            set
            {
                _irisIdGenerator = value;
            }
        }
        /// <summary>
        /// .ctor
        /// </summary>
        public ScheduleService()
        {
            parentNetworkNodeIds = new List<int>();
            childNetworkNodeIds = new List<int>();
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _ruleService = new RuleService(RuleService.CallType.Web);
            _itmSchLinks = new List<MenuItemScheduleLink>();
            _catSchLinks = new List<MenuCategoryScheduleLink>();
            _commonService = new CommonService(_repository);
            _itemIdsModified = new List<int>();
            _catIdsModified = new List<int>();
        }

        /// <summary>
        /// Get all the schedules on particular network
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        public List<ScheduleModel> GetScheduleList(int networkId)
        {
            //Call the method in rule Engine to populate schList
            List<Schedule> schList = _ruleService.GetScheduleList(networkId, true);

            var retVal = (from m in schList
                          select new ScheduleModel
                          {
                              ScheduleId = m.ScheduleId,
                              IrisId = m.IrisId,
                              SchName = m.SchName,
                              StartDate = m.StartDate,
                              EndDate = m.EndDate,
                              Priority = m.Priority,
                              //TODO : Change how LastUpdatedDate is retrieved
                              LastUpdatedDate = m.LastUpdatedDate,
                              IsActive = m.OverrideStatus == OverrideStatus.HIDDEN ? false : true,
                              IsOverride = m.IsOverride,
                              IsCreatedAtThisNetwork = networkId == m.ScheduleOrginatedAt
                          }).ToList();

            return retVal.OrderByDescending(x => x.Priority).ToList();
        }

        /// <summary>
        /// Get NetworkObject info
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public NetworkObject GetNetworkObject(int netId, out int brandId, out string parentsBreadCrumm)
        {
            return _ruleService.GetNetworkObject(netId, out brandId, out parentsBreadCrumm);
        }

        /// <summary>
        /// Get list of inactive Schedules (i.e., deleted by this NW earlier) - Not used
        /// </summary>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public List<ScheduleModel> GetInActiveScheduleList(KendoGridRequest grdRequest)
        {
            List<ScheduleModel> inActSchList = new List<ScheduleModel>();
            try
            {
                KendoGrid<Schedule> schGrid = new KendoGrid<Schedule>();
                var filtering = schGrid.GetFiltering(grdRequest);
                var sorting = schGrid.GetSorting(grdRequest);

                //Get parentNetworkNodes
                parentNetworkNodeIds = _ruleService.ParentNetworkNodesList.ToList();

                //fetch all inactive records
                var InActiveschIds = (from i in _repository.GetQuery<SchNetworkObjectLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.HIDDEN && x.ParentScheduleId != null)
                                      select i.ParentScheduleId).ToList();

                //get Schedules of this NW
                var schList = _ruleService.GetScheduleList(NetworkObjectId);

                //fetch ids of  this NW scheddule
                var schIds = schList.Select(x => x.ScheduleId).ToList();

                //get inactives are exclude already exist records
                var qryList = (from i in _repository.GetQuery<Schedule>(x => InActiveschIds.Contains(x.ScheduleId) && !schIds.Contains(x.ScheduleId))
                               select i).Where(filtering).OrderBy(sorting);

                Count = qryList.Count();

                foreach (var i in qryList.Skip(grdRequest.Skip).Take(grdRequest.PageSize))
                {
                    inActSchList.Add(new ScheduleModel
                    {
                        ScheduleId = i.ScheduleId,
                        IrisId = i.IrisId,
                        SchName = i.SchName,
                        StartDate = i.StartDate,
                        EndDate = i.EndDate,
                        //TODO : Change how LastUpdatedDate is retrieved
                        LastUpdatedDate = i.LastUpdatedDate
                    });
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return inActSchList;
        }

        /// <summary>
        /// Returns Summary of the details of a schedule for each cycle
        /// </summary>
        /// <param name="scheduleId">Schedule Id</param>
        /// NetworkObjectId
        /// <returns></returns>
        public List<ScheduleSummary> GetScheduleDetailSummary(int scheduleId, int networkobjectId)
        {
            List<ScheduleSummary> schSumDetails = new List<ScheduleSummary>();

            //SchDetails at this network
            var schDetails = _ruleService.GetScheduleDetails(scheduleId, networkobjectId);

            // cycles available at this network
            var cycles = _ruleService.GetScheduleCycles(networkobjectId, false);
            foreach (var cycle in cycles)
            {
                ScheduleSummary summ = new ScheduleSummary();
                summ.SchCycleId = cycle.SchCycleId;
                summ.ScheduleId = scheduleId;
                summ.CycleName = cycle.CycleName;
                var schCycleDetails = schDetails.Where(x => x.SchCycleId == cycle.SchCycleId);
                foreach (var detail in schCycleDetails)
                {
                    switch (detail.DayofWeek)
                    {
                        case (byte)DayOfWeek.Sunday:
                            summ.SunST = detail.StartTime;
                            summ.SunET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Monday:
                            summ.MonST = detail.StartTime;
                            summ.MonET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Tuesday:
                            summ.TueST = detail.StartTime;
                            summ.TueET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Wednesday:
                            summ.WedST = detail.StartTime;
                            summ.WedET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Thursday:
                            summ.ThuST = detail.StartTime;
                            summ.ThuET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Friday:
                            summ.FriST = detail.StartTime;
                            summ.FriET = detail.EndTime;
                            break;
                        case (byte)DayOfWeek.Saturday:
                            summ.SatST = detail.StartTime;
                            summ.SatET = detail.EndTime;
                            break;
                    }
                }
                schSumDetails.Add(summ);
            }
            return schSumDetails;
        }

        /// <summary>
        /// Returns all cycles in the system
        /// </summary>
        /// <returns></returns>
        public List<SchCycleModel> GetScheduleCycles(int netId, bool includeInactives = false)
        {
            var schCycleModels = new List<SchCycleModel>();
            //If NetworkId is not passed then return empty list
            if (netId == 0)
            {
                return schCycleModels;
            }
            _ruleService.NetworkObjectId = NetworkObjectId == 0 ? netId : NetworkObjectId;
            var schCycles = _ruleService.GetScheduleCycles(netId, includeInactives);
            foreach (var c in schCycles)
            {
                schCycleModels.Add(new SchCycleModel
                             {
                                 SchCycleId = c.SchCycleId,
                                 CycleName = c.CycleName,
                                 SortOrder = c.SortOrder,
                                 NetworkObjectId = c.NetworkObjectId,
                                 IsActive = c.OverrideStatus == OverrideStatus.ACTIVE ? true : false,
                                 IsOverride = c.IsOverride,
                                 IsCreatedAtThisNetwork = netId == c.CycleOrginatedAt
                             });
            }
            return schCycleModels.OrderBy(x => x.SortOrder).ToList();
        }

        /// <summary>
        /// return Schedule info
        /// </summary>
        /// <param name="id">Schedule</param>
        /// <returns></returns>
        public ScheduleModel GetSchedule(int id, int netId)
        {
            ScheduleModel schModel = new ScheduleModel();
            var schNWLink = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == id).Include("Schedule1").FirstOrDefault();
            if (schNWLink != null && schNWLink.Schedule1 != null)
            {
                MapSchtoSchModel(schNWLink.Schedule1, schModel);
                //Name can be edited only if schedule is create at that network level
                if (schNWLink.NetworkObjectId == netId && schNWLink.ParentScheduleId == null)
                {
                    schModel.IsSchNameEditable = true;
                }
                else
                {
                    schModel.IsSchNameEditable = false;
                }
            }
            return schModel;
        }

        /// <summary>
        /// Added passed Schedules back to this NW - NOt in Use
        /// </summary>
        /// <param name="schIds">List if inactive Schedule Ids</param>
        /// NetworkObjectId
        /// <returns></returns>
        public List<int> AddInActiveSchedules(int[] schIds)
        {
            List<int> addedSchIds = new List<int>();
            string schname = string.Empty, addedSchNames = string.Empty, alreadyExistSchs = string.Empty, invalidSchs = string.Empty;
            try
            {
                if (schIds != null)
                {
                    //Excluded while fetch the list of Inactive Schedules - Hence no need to get available schedules
                    ////Get Avaliable Schedules for this NW (not to added them again)
                    //var schList = new List<Schedule>();
                    //_ruleService.GetScheduleList(NetworkObjectId, schList);
                    //Loop through each Id passed
                    foreach (var schId in schIds)
                    {
                        //Check if Schedule is present
                        var sch = _repository.GetQuery<Schedule>(x => x.ScheduleId == schId).FirstOrDefault();
                        schname = sch.SchName;
                        if (sch != null)
                        {
                            ////Added only if it is not present
                            //if (!schList.Where(x => x.ScheduleId == schId).Any())
                            //{
                            //Get child NetworkNodess
                            childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                            //remove rows if child NW added this Schedule before - As it will be available once parent adds it
                            _repository.Delete<SchNetworkObjectLink>(x => x.ScheduleId == schId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.ParentScheduleId == x.ScheduleId && x.OverrideStatus != OverrideStatus.HIDDEN);
                            //If this is deleted at this NW level make that record active else add new Override record
                            var schNWLink = _repository.GetQuery<SchNetworkObjectLink>(x => x.NetworkObjectId == NetworkObjectId && x.ParentScheduleId == x.ScheduleId && x.ScheduleId == schId && x.OverrideStatus == OverrideStatus.HIDDEN).FirstOrDefault();
                            if (schNWLink != null)
                            {
                                //removing delete override makes it active
                                _repository.Delete<SchNetworkObjectLink>(schNWLink);
                            }
                            else
                            {
                                //Adding override to add it Schedule back
                                SchNetworkObjectLink newSchNWLink = new SchNetworkObjectLink
                                {
                                    ScheduleId = schId,
                                    ParentScheduleId = schId,
                                    OverrideStatus = OverrideStatus.ACTIVE,
                                    NetworkObjectId = NetworkObjectId,
                                    LastUpdatedDate = DateTime.UtcNow
                                };
                                _repository.Add<SchNetworkObjectLink>(newSchNWLink);
                            }
                            addedSchNames = (string.IsNullOrWhiteSpace(addedSchNames) ? string.Empty : addedSchNames + ", ") + sch.SchName;
                            _auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: sch.SchName, operationDescription: string.Format(Constants.AuditMessage.SchNWLinkAddT, sch.SchName));
                            //}
                            //else
                            //{
                            //    alreadyExistSchs = (string.IsNullOrWhiteSpace(alreadyExistSchs) ? "" : alreadyExistSchs + ", ") + sch.SchName;
                            //}

                        }
                        else
                        {
                            invalidSchs = (string.IsNullOrWhiteSpace(invalidSchs) ? string.Empty : invalidSchs + ", ") + sch.SchName;
                        }
                    }
                    if (!string.IsNullOrEmpty(addedSchNames) && addedSchNames.Length > 2)
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.SchNWLinkAddT, addedSchNames);
                    }
                    if (!string.IsNullOrEmpty(alreadyExistSchs) && alreadyExistSchs.Length > 2)
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.AuditMessage.SchNWLinkExistT, alreadyExistSchs);
                    }
                    if (!string.IsNullOrEmpty(invalidSchs) && invalidSchs.Length > 2)
                    {
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : _lastActionResult + "<br/>") + string.Format(Constants.StatusMessage.ErrInvalidSchT, invalidSchs);
                    }
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchNWLinkAddT, schname);
                Logger.WriteError(ex);
            }
            return addedSchIds;
        }

        /// <summary>
        /// Chane the Priority of Schedule in a Network
        /// </summary>
        /// <param name="model"></param>
        /// <param name="netIdWhereOprInit"></param>
        /// <param name="newIndex"></param>
        /// <param name="oldIndex"></param>
        /// <returns></returns>
        public bool MoveSchedule(ScheduleModel model, int netIdWhereOprInit, int newIndex, int oldIndex)
        {
            var retVal = false;
            var schName = string.Empty;
            try
            {
                //Network where the operation is being performed.
                _ruleService.MultipleNetworkObjects = false;
                NetworkObjectId = _ruleService.NetworkObjectId = netIdWhereOprInit;
                schName = model.SchName;

                var childNetworkIds = _ruleService.GetNetworkChilds(netIdWhereOprInit);

                var networkIdsToPerformSort = new List<int>();
                networkIdsToPerformSort.Add(netIdWhereOprInit);

                //Get the child newtorks where there is a modification already present. Need to reorder separatelt at that etwork
                networkIdsToPerformSort.AddRange(_repository.GetQuery<SchNetworkObjectLink>(x => childNetworkIds.Contains(x.NetworkObjectId)).OrderBy(x => x.NetworkObject.NetworkObjectTypeId).Select(x => x.NetworkObjectId).Distinct());

                var scheduleListWhereOperationInitated = _ruleService.GetSchNetworkLinkList(netIdWhereOprInit, true).OrderByDescending(x => x.Priority).ToList();

                var newPositionSchText = string.Empty;

                if (newIndex > oldIndex)
                {// Move down - get text of schedule that is next to new position - Note: Name of schedule doesn't change on override
                    newPositionSchText = newIndex + 1 != scheduleListWhereOperationInitated.Count() ? scheduleListWhereOperationInitated[newIndex + 1].Schedule1.SchName : string.Empty;
                }
                else
                {//Move up - get the name of schedule where it is moved ( as this will be next schedule when moved up)
                    newPositionSchText = scheduleListWhereOperationInitated[newIndex].Schedule1.SchName;
                }

                foreach (var networkId in networkIdsToPerformSort)
                {
                    //create new ruleservice obj to get new repo in the rule service
                    _ruleService = new RuleService(RuleService.CallType.Web, networkId);
                    var scheduleList = netIdWhereOprInit == networkId ? scheduleListWhereOperationInitated : _ruleService.GetSchNetworkLinkList(networkId, true).OrderByDescending(x => x.Priority).ToList();

                    var movingSch = scheduleList.Where(x => x.Schedule1.SchName.CompareTo(model.SchName) == 0).FirstOrDefault();

                    var oldPriority = movingSch.Priority;
                    var thisLoopOldIndex = scheduleList.IndexOf(movingSch);

                    var newPriority = 0;
                    if (string.IsNullOrEmpty(newPositionSchText))
                    {// move to last priority
                        newPriority = -1;
                    }
                    else
                    {
                        var nextSch = scheduleList.Where(x => x.Schedule1.SchName.CompareTo(newPositionSchText) == 0).FirstOrDefault();
                        var thisLoopNewIndex = scheduleList.IndexOf(nextSch);
                        if (thisLoopNewIndex > thisLoopOldIndex)
                        {// Move Down - Get top of this schedule priority
                            newPriority = scheduleList[thisLoopNewIndex - 1].Priority;
                        }
                        else
                        {// MOve Up - get this sch priority
                            newPriority = nextSch.Priority;
                        }
                    }
                    //newPriority = string.IsNullOrEmpty(newPositionSchText) ? -1 : scheduleList.Where(x => x.Schedule1.SchName.CompareTo(newPositionSchText)==0).FirstOrDefault().Priority;
                    //Perform re arranging schedules
                    moveScheduleAtChildNetwork(model, scheduleList, networkId, newPriority, oldPriority);
                }

                _context.SaveChanges();
                retVal = true;
                _lastActionResult = string.Format(Constants.AuditMessage.ScheduleMoveT, schName);
                var moreDetails = string.Format(" {0} New position - {1}, Old position - {2}", _lastActionResult, newIndex + 1, oldIndex + 1);
                _auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: model.SchName, operationDescription: moreDetails, operationDetail: moreDetails);

            }
            catch (Exception ex)
            {
                retVal = false;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleMoveT, schName);
                Logger.WriteError(ex);
            }
            return retVal;

        }

        /// <summary>
        /// Saves the changes to Schedule and Details to DB or Creates new Schedule
        /// </summary>
        /// <param name="model">Schedule Model</param>
        /// <returns></returns>
        public ScheduleModel SaveSchedule(ScheduleModel model)
        {
            try
            {
                _ruleService.NetworkObjectId = NetworkObjectId;
                //Get the Schedule
                var qSch = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == model.ScheduleId).Include("Schedule1").FirstOrDefault();
                var schNetId = 0;
                var schPriority = 0;
                int? parSCHId = null;
                bool isSchNew = false;
                bool isOriginatedAtThisNetwork = false;
                if (model.ScheduleId == 0)
                {
                    isSchNew = true;
                }
                Schedule sch = new Schedule();
                //Check if Schedule is present
                if (qSch != null)
                {
                    //BSRule : any change in schedule should change evrywhere
                    schNetId = qSch.NetworkObjectId;
                    sch = qSch.Schedule1;
                    schPriority = qSch.Priority;

                    if (qSch.NetworkObjectId == NetworkObjectId && qSch.ParentScheduleId.HasValue == false)
                    {
                        isOriginatedAtThisNetwork = true;
                    }
                    parSCHId = qSch.ParentScheduleId.HasValue ? qSch.ParentScheduleId : qSch.ScheduleId;
                }

                // Validation for creating or editing schedule
                //isOriginatedAtThisNetwork  is true means it is either create or update the schedule at current NW. Only then Name should be validated
                if (isOriginatedAtThisNetwork || isSchNew)
                {
                    var isValid = true;
                    model.SchName = model.SchName.Trim();
                    //ServeSide Validations
                    var regxAlphaNumericNoSpace = new Regex(@"^[a-zA-Z0-9-_()]+$");
                    isValid = !string.IsNullOrEmpty(model.SchName) && regxAlphaNumericNoSpace.IsMatch(model.SchName);
                    if (!isValid)
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleSaveT, model.SchName) + " Invalid Name.";
                        return model;
                    }
                    if (IsScheduleNameNotUnique(model.SchName, model.ScheduleId, NetworkObjectId, false))
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleSaveT, model.SchName) + " Please Enter Unique Schedule Name.";
                        return model;
                    }
                }


                // IF the schedule is created at this NW then update the same SCH
                if (model.ScheduleId != 0 && schNetId == NetworkObjectId)
                {
                    MapSchModeltoSch(model, sch);
                    qSch.LastUpdatedDate = DateTime.UtcNow;

                    _repository.Update<Schedule>(sch);
                    _repository.Update<SchNetworkObjectLink>(qSch);
                }
                //If the SCH is not created at this NW  then add SCH and make it is a override.
                //If SCHid is zero then create new SCH at this NW
                else
                {
                    sch = new Schedule();
                    MapSchModeltoSch(model, sch);
                    sch.IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);

                    if (isSchNew)
                    {
                        //calculate priority of new schedule
                        var schNWList = _ruleService.GetSchNetworkLinkList(NetworkObjectId, true);
                        schPriority = schNWList.Max(x => x.Priority) + 1;
                    }
                    else
                    {
                        //delete overrides
                        var existingSchLinkOvr = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == model.ScheduleId && x.NetworkObjectId == NetworkObjectId && x.ParentScheduleId != null).FirstOrDefault();
                        if (existingSchLinkOvr != null)
                        {
                            schPriority = existingSchLinkOvr.Priority;
                            _repository.Delete<SchNetworkObjectLink>(existingSchLinkOvr);
                        }
                    }
                    //override link at this network
                    sch.SchNetworkObjectLinks1.Add(new SchNetworkObjectLink
                    {
                        NetworkObjectId = NetworkObjectId,
                        OverrideStatus = OverrideStatus.ACTIVE,
                        ParentScheduleId = parSCHId,
                        LastUpdatedDate = DateTime.UtcNow,
                        Priority = schPriority
                    });

                    if (isSchNew)
                    {
                        //Adjust the priority at child networks
                        var childNetworkIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                        var networkIdsToPerformSort = new List<int>();

                        //Get the newtorks where there are more schedules than parent
                        //ParentScheduleId is null at child network - There is new schedule created at child network
                        networkIdsToPerformSort.AddRange(_repository.GetQuery<SchNetworkObjectLink>(x => childNetworkIds.Contains(x.NetworkObjectId) && x.ParentScheduleId == null).OrderBy(x => x.NetworkObject.NetworkObjectTypeId).Select(x => x.NetworkObjectId).Distinct());

                        foreach (var childNetworkId in networkIdsToPerformSort)
                        {
                            _ruleService.NetworkObjectId = childNetworkId;
                            var childSchList = _ruleService.GetSchNetworkLinkList(childNetworkId, true);
                            sch.SchNetworkObjectLinks1.Add(new SchNetworkObjectLink
                            {
                                NetworkObjectId = childNetworkId,
                                OverrideStatus = OverrideStatus.MOVED,
                                ParentScheduleId = sch.ScheduleId,
                                LastUpdatedDate = DateTime.UtcNow,
                                Priority = childSchList.Max(x => x.Priority) + 1
                            });
                        }
                    }

                    _repository.Add<Schedule>(sch);
                }
                _context.SaveChanges();
                //Get the new id of Schedule
                model.ScheduleId = sch.ScheduleId;

                var retValue = true;
                ScheduleId = model.ScheduleId;
                //save the schedule details
                retValue = SaveSchDetails(model);

                if (retValue)
                {
                    if (isSchNew)
                    {
                        _auditLogger.Write(OperationPerformed.Created, EntityType.Schedule, entityNameList: model.SchName);
                        _lastActionResult = string.Format(Constants.AuditMessage.ScheduleCreateT, model.SchName);
                    }
                    else
                    {
                        _auditLogger.Write(OperationPerformed.Updated, EntityType.Schedule, entityNameList: model.SchName);
                        _lastActionResult = string.Format(Constants.AuditMessage.ScheduleUpdateT, model.SchName);
                    }

                }

            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleSaveT, model.SchName);
                Logger.WriteError(ex);
            }

            return model;
        }

        /// <summary>
        /// Deletes the schedule from DB for this NW
        /// </summary>
        /// <param name="schId">Schedule Id</param>
        /// NetworkObjectId
        public void DeleteSchedule(int schId)
        {
            var schname = "";
            try
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                //Get the Schedule and link
                var qSch = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == schId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Schedule1").Include("NetworkObject").OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                if (qSch != null)
                {
                    //get name to disaply in status
                    schname = qSch.Schedule1.SchName;

                    //If the Schedule link is originated at this NW and created by this NW
                    if (qSch.NetworkObjectId == NetworkObjectId && qSch.ParentScheduleId == null)
                    {
                        var overridenSchIds = _repository.GetQuery<SchNetworkObjectLink>(x => x.ParentScheduleId == schId).Select(x => x.ScheduleId).ToList();
                        //remove delete overrides of schedule and overrides  of schedule
                        _repository.Delete<SchDetail>(x => overridenSchIds.Contains(x.ScheduleId));
                        _repository.Delete<Schedule>(x => overridenSchIds.Contains(x.ScheduleId));
                        _repository.Delete<SchNetworkObjectLink>(x => x.ParentScheduleId == schId);

                        //delete link and Schedule and schedule details
                        _repository.Delete<SchDetail>(x => x.ScheduleId == schId);
                        _repository.Delete<SchNetworkObjectLink>(x => x.ScheduleId == schId);
                        _repository.Delete<Schedule>(x => x.ScheduleId == schId);

                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.ScheduleDeleteT, schname);
                        _auditLogger.Write(OperationPerformed.Deleted, EntityType.Schedule, entityNameList: schname);
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleNotEligibleDeleteT, schname);
                    }
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleDeleteT, schname);
                    Logger.WriteError(string.Format("{0}. Could not find the record with schedule id {1} to delete", _lastActionResult, schId));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleDeleteT, schname);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Disables the schedule from DB for this NW
        /// </summary>
        /// <param name="schId">Schedule Id</param>
        /// NetworkObjectId
        public void DisableSchedule(int schId)
        {
            var schname = "";
            try
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                //Get the Schedule and link
                var qSch = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == schId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Schedule1").Include("NetworkObject").OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                if (qSch != null)
                {
                    //get name to display in status
                    schname = qSch.Schedule1.SchName;

                    //flag to indicate whether to add delete override record
                    var addDisableLink = true;
                    //get the master schedule which should be overriden to delete
                    int pSchId = qSch.ParentScheduleId.HasValue ? qSch.ParentScheduleId.Value : schId;

                    //If the Schedule link is originated at this NW - Either created by this NW or overriden at this NW
                    if (qSch.NetworkObjectId == NetworkObjectId)
                    {
                        // If parent Schedule is null - this Schedule is overidden at this NW
                        if (qSch.ParentScheduleId != null)
                        {
                            addDisableLink = false;
                            //updat the link to disable the earlier overriden link
                            qSch.OverrideStatus = OverrideStatus.HIDDEN;
                        }
                        //Else - This Schedule is created by this NW
                        else
                        {//add new link to disable
                            addDisableLink = true;
                        }
                    }
                    if (addDisableLink)
                    {
                        SchNetworkObjectLink newSchNWLink = new SchNetworkObjectLink
                        {
                            ScheduleId = pSchId,
                            NetworkObjectId = NetworkObjectId,
                            ParentScheduleId = pSchId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            LastUpdatedDate = DateTime.UtcNow,
                            Priority = qSch.Priority
                        };
                        _repository.Add<SchNetworkObjectLink>(newSchNWLink);
                    }
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);

                    //delete all delete overrides
                    //_repository.Delete<SchNetworkObjectLink>(x => x.ParentScheduleId == pSchId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus == OverrideStatus.HIDDEN);

                    //disable all overrides of schedule by child NW
                    var allChildOverrides = _repository.GetQuery<SchNetworkObjectLink>(x => x.ParentScheduleId == pSchId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList();
                    allChildOverrides.ForEach(x => x.OverrideStatus = OverrideStatus.HIDDEN);

                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ScheduleDisableT, schname);
                    _auditLogger.Write(OperationPerformed.Disabled, EntityType.Schedule, entityNameList: schname);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleDisableT, schname);
                    Logger.WriteError(string.Format("{0}. Could not find the record with schedule id {1} to disable", _lastActionResult, schId));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleDisableT, schname);
                Logger.WriteError(ex);
            }
        }
        /// <summary>
        /// Enable a disabled schedule at that level
        /// </summary>
        /// <param name="id"></param>
        public void EnableSchedule(int id)
        {
            var scheduleName = string.Empty;
            try
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                var addEnableLink = false;
                var scheduleDisabled = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == id && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").Include("Schedule1")
                    .OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.SchNetworkObjectLinkId).FirstOrDefault();
                if (scheduleDisabled != null)
                {
                    scheduleName = scheduleDisabled.Schedule1.SchName;
                    var prntscheduleId = scheduleDisabled.ParentScheduleId == null ? scheduleDisabled.ScheduleId : scheduleDisabled.ParentScheduleId;
                    if (scheduleDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                    {
                        addEnableLink = true;
                        //delete diabled link
                        if (NetworkObjectId == scheduleDisabled.NetworkObjectId)
                        {
                            // if there is no override
                            if (scheduleDisabled.ParentScheduleId.HasValue && scheduleDisabled.ParentScheduleId == scheduleDisabled.ScheduleId)
                            {
                                addEnableLink = false;
                                _repository.Delete<SchNetworkObjectLink>(scheduleDisabled);
                                //Need recheck whether it is disabled by parent network
                                var scheduleStillDisabled = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == id && x.SchNetworkObjectLinkId != scheduleDisabled.SchNetworkObjectLinkId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject")
                                                            .OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.SchNetworkObjectLinkId).FirstOrDefault();
                                if (scheduleStillDisabled != null && scheduleStillDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                                {//Add active link again
                                    addEnableLink = true;
                                }
                            }
                            else
                            {//activate the earlier overriden schedule back
                                scheduleDisabled.OverrideStatus = OverrideStatus.ACTIVE;
                                addEnableLink = false;
                            }
                        }
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleAlreadyEnableT, scheduleName);
                        Logger.WriteError(_lastActionResult);
                    }
                    if (addEnableLink)
                    {
                        //if it is disabled at different level then enabled it at this network

                        //Add Active row to enable back SchNetworkObjectLink
                        SchNetworkObjectLink schLinkAsActive = new SchNetworkObjectLink
                        {
                            ScheduleId = scheduleDisabled.ScheduleId,
                            Priority = scheduleDisabled.Priority,
                            NetworkObjectId = NetworkObjectId,
                            OverrideStatus = OverrideStatus.ACTIVE,
                            LastUpdatedDate = DateTime.Now,
                            ParentScheduleId = prntscheduleId
                        };
                        _repository.Add<SchNetworkObjectLink>(schLinkAsActive);

                    }
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ScheduleEnableT, scheduleName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: scheduleName, operationDescription: _lastActionResult);

                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleEnableT, scheduleName);
                    Logger.WriteError(_lastActionResult);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleEnableT, scheduleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Copy any schedule on that level
        /// </summary>
        /// <param name="schModel"></param>
        public void CopySchedule(ScheduleModel schModel)
        {
            var scheduleName = string.Empty;
            try
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                var scheduleToCopy = _repository.GetQuery<Schedule>(x => x.ScheduleId == schModel.ScheduleId).Include("SchDetails")
                    .FirstOrDefault();
                if (scheduleToCopy != null)
                {
                    scheduleName = scheduleToCopy.SchName;
                    _schNetworkLinks = _repository.GetQuery<SchNetworkObjectLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).Include("Schedule1").ToList();

                    var newName = scheduleToCopy.SchName + "-Copy";
                    string numberPattern = "({0})";
                    //Make sure name is Unique
                    if (IsScheduleNameNotUnique(newName, 0, NetworkObjectId, true))
                    {
                        newName = getNextSchedulename(newName + numberPattern, NetworkObjectId);
                    }

                    var schNWLinks = _ruleService.GetSchNetworkLinkList(NetworkObjectId, true);

                    //copy schedule info
                    var newSchedule = new Schedule
                    {
                        SchName = newName,
                        LastUpdatedDate = DateTime.Now,
                        IrisId = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName),
                        StartDate = schModel.StartDate,
                        EndDate = schModel.EndDate
                    };
                    // Add new link info
                    newSchedule.SchNetworkObjectLinks1.Add(new SchNetworkObjectLink
                    {
                        NetworkObjectId = NetworkObjectId,
                        Priority = schNWLinks.Max(x => x.Priority) + 1,
                        OverrideStatus = OverrideStatus.ACTIVE,
                        ParentScheduleId = null,
                        LastUpdatedDate = DateTime.Now
                    });
                    //Adjust the priority at child networks
                    var childNetworkIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                    var networkIdsToPerformSort = new List<int>();

                    //Get the newtorks where there are more schedules than parent
                    //ParentScheduleId is null at child network - There is new schedule created at child network
                    networkIdsToPerformSort.AddRange(_schNetworkLinks.Where(x => childNetworkIds.Contains(x.NetworkObjectId) && x.ParentScheduleId == null).OrderBy(x => x.NetworkObject.NetworkObjectTypeId).Select(x => x.NetworkObjectId).Distinct());

                    foreach (var childNetworkId in networkIdsToPerformSort)
                    {
                        _ruleService.NetworkObjectId = childNetworkId;
                        var childSchList = _ruleService.GetSchNetworkLinkList(childNetworkId, true);
                        newSchedule.SchNetworkObjectLinks1.Add(new SchNetworkObjectLink
                        {
                            NetworkObjectId = childNetworkId,
                            OverrideStatus = OverrideStatus.MOVED,
                            ParentScheduleId = newSchedule.ScheduleId,
                            LastUpdatedDate = DateTime.UtcNow,
                            Priority = childSchList.Max(x => x.Priority) + 1
                        });
                    }

                    //copy schedule details
                    foreach (var schDetail in scheduleToCopy.SchDetails)
                    {
                        newSchedule.SchDetails.Add(new SchDetail
                        {
                            SchCycleId = schDetail.SchCycleId,
                            DayofWeek = schDetail.DayofWeek,
                            StartTime = schDetail.StartTime,
                            EndTime = schDetail.EndTime,
                            IsActive = schDetail.IsActive
                        });
                    }
                    _repository.Add<Schedule>(newSchedule);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ScheduleCopyT, scheduleName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: scheduleName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleCopyT, scheduleName);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleCopyT, scheduleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// revert schedule
        /// </summary>
        /// <param name="id"></param>
        public void RevertSchedule(int id)
        {
            var scheduleName = string.Empty;
            try
            {
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                var scheduleToRevert = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == id && x.NetworkObjectId == NetworkObjectId).Include("Schedule1")
                    .FirstOrDefault();
                if (scheduleToRevert != null)
                {
                    //Delete all the local customisations
                    scheduleName = scheduleToRevert.Schedule1.SchName;

                    _repository.Delete<SchDetail>(x => x.ScheduleId == scheduleToRevert.ScheduleId);
                    _repository.Delete<SchNetworkObjectLink>(x => x.ScheduleId == scheduleToRevert.ScheduleId);
                    _repository.Delete<Schedule>(x => x.ScheduleId == scheduleToRevert.ScheduleId);
                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.ScheduleRevertT, scheduleName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: scheduleName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleRevertT, scheduleName);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrScheduleRevertT, scheduleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Updated the Cycle
        /// </summary>
        /// <param name="model"></param>
        public SchCycleModel UpdateCycle(SchCycleModel model)
        {
            try
            {
                _ruleService.NetworkObjectId = NetworkObjectId = model.NetworkObjectId;

                if (IsCycleNameNotUnique(model.CycleName, model.SchCycleId, model.NetworkObjectId, false))
                {
                    _lastActionResult = "Cycle update failed. A Cycle with same name already exist at different level or same level.";
                }
                else
                {
                    //Get the Cycle
                    var cycle = _repository.GetQuery<SchCycle>(x => x.SchCycleId == model.SchCycleId).FirstOrDefault();

                    if (model != null && cycle != null)
                    {
                        //Update the Name
                        cycle.CycleName = model.CycleName.Trim();
                        cycle.SortOrder = model.SortOrder;

                        //Update all Menus under this network and its child networks Lastupdated Date
                        _commonService.SetMenuNetworksDateUpdated(netid: NetworkObjectId, isOperationDirectlyOnMenu: false);

                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.SchCycleUpdateT, model.CycleName);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.ScheduleCycle, entityNameList: model.CycleName, operationDescription: _lastActionResult);
                    }
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleUpdateT, model.CycleName);
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        /// Deletes the Cycle
        /// </summary>
        /// <param name="model"></param>
        public void DestroyCycle(SchCycleModel model, int netId)
        {
            var cycleName = string.Empty;
            try
            {
                //Network where the operation is being performed.
                NetworkObjectId = netId;
                cycleName = model.CycleName;
                //Get the Schedule and link
                var qSchCycle = _repository.GetQuery<SchCycle>(x => x.SchCycleId == model.SchCycleId).FirstOrDefault();
                if (qSchCycle != null)
                {

                    //If the Schedule link is originated at this NW and created by this NW
                    if (qSchCycle.NetworkObjectId == NetworkObjectId && qSchCycle.ParentSchCycleId == null)
                    {
                        var overridenSchCycleIds = _repository.GetQuery<SchCycle>(x => x.ParentSchCycleId == model.SchCycleId).Select(x => x.SchCycleId).ToList();

                        //remove delete overrides of cycles and cycles  of schedule
                        _repository.Delete<MenuCategoryCycleInSchedule>(x => overridenSchCycleIds.Contains(x.SchCycleId));
                        //TODO: Is there need to clean IsSelected false if there is nothing in CycleLink
                        //_repository.Delete<MenuCategoryScheduleLink>(x => overridenSchCycleIds.Contains(x.SchCycleId));
                        _repository.Delete<MenuItemCycleInSchedule>(x => overridenSchCycleIds.Contains(x.SchCycleId));
                        //_repository.Delete<MenuItemScheduleLink>(x => overridenSchCycleIds.Contains(x.SchCycleId));
                        _repository.Delete<SchDetail>(x => overridenSchCycleIds.Contains(x.SchCycleId));
                        _repository.Delete<SchCycle>(x => x.ParentSchCycleId == model.SchCycleId);


                        //Delete all the ItemSchedules of Cycle
                        _repository.Delete<MenuCategoryCycleInSchedule>(x => x.SchCycleId == model.SchCycleId);
                        //Delete all the CatSchedules of Cycle
                        _repository.Delete<MenuItemCycleInSchedule>(x => x.SchCycleId == model.SchCycleId);
                        ////Delete all the details of Cycle
                        _repository.Delete<SchDetail>(x => x.SchCycleId == model.SchCycleId);
                        //Delete Cycle
                        _repository.Delete<SchCycle>(x => x.SchCycleId == model.SchCycleId);


                        //Update all Menus under this network and its child networks Lastupdated Date
                        _commonService.SetMenuNetworksDateUpdated(netid: NetworkObjectId, isOperationDirectlyOnMenu: false);

                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.SchCycleDeleteT, cycleName);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.ScheduleCycle, entityNameList: model.CycleName, operationDescription: _lastActionResult);
                    }
                    else
                    {
                        _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleNotEligibleDeleteT, cycleName);
                    }
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleDeleteT, cycleName);
                    Logger.WriteError(string.Format("{0}. Could not find the record with cycle id {1} to delete", _lastActionResult, model.SchCycleId));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleDeleteT, cycleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Disable the Cycle
        /// </summary>
        /// <param name="model"></param>
        public void DisableCycle(SchCycleModel model, int netId)
        {
            var cycleName = string.Empty;
            try
            {
                //Network where the operation is being performed.
                NetworkObjectId = netId;
                cycleName = model.CycleName;
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                //Get the Schedule and link
                var qSchCycle = _repository.GetQuery<SchCycle>(x => x.SchCycleId == model.SchCycleId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.SchCycleId).FirstOrDefault();
                if (qSchCycle != null)
                {
                    //flag to indicate whether to add delete override record
                    var addDisableLink = true;
                    //get the master Cycle which should be overriden to delete
                    int pSchId = qSchCycle.ParentSchCycleId.HasValue ? qSchCycle.ParentSchCycleId.Value : model.SchCycleId;

                    //If the Cycle link is originated at this NW - Either created by this NW or overriden at this NW
                    if (qSchCycle.NetworkObjectId == NetworkObjectId)
                    {
                        // If parent Cycle is null - this Cycle is overidden at this NW
                        if (qSchCycle.ParentSchCycleId != null)
                        {
                            addDisableLink = false;
                            //updat the link to disable the earlier overriden link
                            qSchCycle.OverrideStatus = OverrideStatus.HIDDEN;
                        }
                        //Else - This Cycle is created by this NW
                        else
                        {//add new link to disable
                            addDisableLink = true;
                        }
                    }
                    if (addDisableLink)
                    {
                        SchCycle newSchCycleLink = new SchCycle
                        {
                            CycleName = model.CycleName,
                            NetworkObjectId = NetworkObjectId,
                            ParentSchCycleId = pSchId,
                            OverrideStatus = OverrideStatus.HIDDEN,
                            SortOrder = qSchCycle.SortOrder
                        };
                        _repository.Add<SchCycle>(newSchCycleLink);
                    }
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
                    //disable all overrides of schedule by child NW
                    var allChildOverrides = _repository.GetQuery<SchCycle>(x => x.ParentSchCycleId == pSchId && childNetworkNodeIds.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).ToList();
                    allChildOverrides.ForEach(x => x.OverrideStatus = OverrideStatus.HIDDEN);


                    //Update all Menus under this network and its child networks Lastupdated Date
                    _commonService.SetMenuNetworksDateUpdated(netid: NetworkObjectId, isOperationDirectlyOnMenu: false);

                    _context.SaveChanges();
                    _lastActionResult = string.Format(Constants.AuditMessage.SchCycleDisableT, cycleName);
                    _auditLogger.Write(OperationPerformed.Disabled, EntityType.ScheduleCycle, entityNameList: cycleName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleDisableT, cycleName);
                    Logger.WriteError(string.Format("{0}. Could not find the record with cycle id {1} to disable", _lastActionResult, cycleName));
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleDisableT, cycleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Enable the Cycle
        /// </summary>
        /// <param name="model"></param>
        public void EnableCycle(SchCycleModel model, int netId)
        {
            var cycleName = string.Empty;
            try
            {
                //Network where the operation is being performed.
                NetworkObjectId = _ruleService.NetworkObjectId = netId;
                parentNetworkNodeIds = _ruleService.GetNetworkParents(NetworkObjectId);
                cycleName = model.CycleName;
                var addEnableLink = true;
                if (model.IsActive == false)
                {
                    var schCycleDisabled = _repository.GetQuery<SchCycle>(x => x.SchCycleId == model.SchCycleId).FirstOrDefault();
                    if (schCycleDisabled != null && schCycleDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                    {
                        addEnableLink = true;
                        var prntcycleId = schCycleDisabled.ParentSchCycleId == null ? schCycleDisabled.SchCycleId : schCycleDisabled.ParentSchCycleId;

                        if (NetworkObjectId == model.NetworkObjectId)
                        {
                            addEnableLink = false;
                            //Need recheck whether it is disabled by parent network
                            var scheduleStillDisabled = _repository.GetQuery<SchCycle>(x => x.ParentSchCycleId == schCycleDisabled.ParentSchCycleId && x.SchCycleId != model.SchCycleId && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject")
                                                        .OrderByDescending(o => o.NetworkObject.NetworkObjectTypeId).ThenByDescending(x => x.SchCycleId).FirstOrDefault();
                            if (scheduleStillDisabled != null && scheduleStillDisabled.OverrideStatus == OverrideStatus.HIDDEN)
                            {//Add active link again
                                addEnableLink = true;
                            }
                            _repository.Delete<SchCycle>(schCycleDisabled);
                        }
                        if (addEnableLink)
                        {
                            //Add Active row to enable back schCycle
                            SchCycle schCycleAsActive = new SchCycle
                            {
                                CycleName = model.CycleName,
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = model.SortOrder,
                                ParentSchCycleId = prntcycleId
                            };
                            _repository.Add<SchCycle>(schCycleAsActive);
                        }

                        //Update all Menus under this network and its child networks Lastupdated Date
                        _commonService.SetMenuNetworksDateUpdated(netid: NetworkObjectId, isOperationDirectlyOnMenu: false);

                        _context.SaveChanges();
                        _lastActionResult = string.Format(Constants.AuditMessage.SchCycleEnableT, cycleName);
                        _auditLogger.Write(OperationPerformed.Other, EntityType.ScheduleCycle, entityNameList: model.CycleName, operationDescription: _lastActionResult);
                    }
                    else
                    {
                        _lastActionResult = string.Format("The Cycle '{0}' is already Enabled", cycleName);
                    }
                }
                else
                {
                    _lastActionResult = string.Format("The Cycle '{0}' is already Enabled", cycleName);
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleEnableT, cycleName);
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Move cycle - Not in use
        /// </summary>
        /// <param name="model"></param>
        /// <param name="netId"></param>
        /// <param name="newIndex"></param>
        /// <param name="oldIndex"></param>
        /// <returns></returns>
        public bool MoveCycle(SchCycleModel model, int netId, int newIndex, int oldIndex)
        {
            var retVal = false;
            var cycleName = string.Empty;
            try
            {
                //Network where the operation is being performed.
                NetworkObjectId = _ruleService.NetworkObjectId = netId;
                cycleName = model.CycleName;
                int alterOtherIndecesBy = 0;
                var schCycles = _ruleService.GetScheduleCycles(netId);
                var schCyclesInBetweenTheMove = new List<SchCycle>();

                if (newIndex > oldIndex)
                {
                    schCyclesInBetweenTheMove = schCycles.Where(x => x.SortOrder >= model.SortOrder).Take(newIndex - oldIndex + 1).ToList();
                    alterOtherIndecesBy = -1;
                }
                else
                {
                    schCyclesInBetweenTheMove = schCycles.Where(x => x.SortOrder <= model.SortOrder).Take(oldIndex - newIndex + 1).ToList();
                    alterOtherIndecesBy = 1;
                }
                foreach (var schCycleToMove in schCyclesInBetweenTheMove)
                {
                    if (schCycleToMove.SortOrder == oldIndex)
                    {
                        if (NetworkObjectId == schCycleToMove.NetworkObjectId)
                        {
                            schCycleToMove.SortOrder += newIndex;
                        }
                        else
                        {
                            //Add new SortOrder row to schCycle
                            SchCycle schCycleAsActive = new SchCycle
                            {
                                CycleName = schCycleToMove.CycleName,
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = newIndex,
                                ParentSchCycleId = schCycleToMove.SchCycleId
                            };
                            _repository.Add<SchCycle>(schCycleAsActive);
                        }
                    }
                    else
                    {
                        if (NetworkObjectId == schCycleToMove.NetworkObjectId)
                        {
                            schCycleToMove.SortOrder += alterOtherIndecesBy;
                        }
                        else
                        {
                            //Add new SortOrder row to schCycle
                            SchCycle schCycleAsActive = new SchCycle
                            {
                                CycleName = schCycleToMove.CycleName,
                                NetworkObjectId = NetworkObjectId,
                                OverrideStatus = OverrideStatus.ACTIVE,
                                SortOrder = schCycleToMove.SortOrder + alterOtherIndecesBy,
                                ParentSchCycleId = schCycleToMove.SchCycleId
                            };
                            _repository.Add<SchCycle>(schCycleAsActive);
                        }
                    }
                    //_context.SaveChanges();
                    retVal = true;
                    _lastActionResult = string.Format(Constants.AuditMessage.SchCycleMoveT, cycleName);
                    var moreDetails = string.Format(" {0} New position - {1}, Old position - {2}", _lastActionResult, newIndex, oldIndex);
                    //_auditLogger.Write(OperationPerformed.Other, EntityType.Schedule, entityNameList: model.CycleName, operationDescription: _lastActionResult, operationDetail: moreDetails);
                }
            }
            catch (Exception ex)
            {
                retVal = false;
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleMoveT, cycleName);
                Logger.WriteError(ex);
            }
            return retVal;

        }

        /// <summary>
        /// Create Cycle
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public SchCycleModel CreateCycle(SchCycleModel model)
        {
            try
            {
                _ruleService.NetworkObjectId = NetworkObjectId = model.NetworkObjectId;

                model.CycleName = model.CycleName.Trim();

                if (IsCycleNameNotUnique(model.CycleName, model.SchCycleId, model.NetworkObjectId, false))
                {
                    _lastActionResult = "Cycle creation failed. A Cycle with same name already exist at different level or same level.";
                }
                else
                {
                    //Populate the Cycle based on model and Add to Database
                    SchCycle cycle = new SchCycle();
                    cycle.SchCycleId = 0;
                    cycle.CycleName = model.CycleName;
                    cycle.NetworkObjectId = model.NetworkObjectId;
                    cycle.OverrideStatus = OverrideStatus.ACTIVE;
                    cycle.ParentSchCycleId = null;
                    cycle.SortOrder = model.SortOrder == 0 ? _repository.GetQuery<SchCycle>().Max(x => x.SortOrder) + 1 : model.SortOrder;
                    _repository.Add<SchCycle>(cycle);

                    //Update all Menus under this network and its child networks Lastupdated Date
                    _commonService.SetMenuNetworksDateUpdated(netid: NetworkObjectId, isOperationDirectlyOnMenu: false);

                    _context.SaveChanges();
                    model.SchCycleId = cycle.SchCycleId;
                    _lastActionResult = string.Format(Constants.AuditMessage.SchCycleCreateT, model.CycleName);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.ScheduleCycle, entityNameList: model.CycleName, operationDescription: _lastActionResult);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
                _lastActionResult = string.Format(Constants.StatusMessage.ErrSchCycleCreateT, model.CycleName);
            }
            return model;
        }

        /// <summary>
        /// Check if schedule name is not unique
        /// </summary>
        /// <param name="schName"></param>
        /// <param name="scheduleId"></param>
        /// <param name="networkObjectId"></param>
        /// <param name="isActionCopy"></param>
        /// <returns></returns>
        public bool IsScheduleNameNotUnique(string schName, int scheduleId, int networkObjectId, bool isActionCopy)
        {
            bool retVal = true;

            if (string.IsNullOrWhiteSpace(schName))
            {
                retVal = false;
            }
            else
            {
                if (!parentNetworkNodeIds.Any())
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(networkObjectId);
                }
                if (!childNetworkNodeIds.Any())
                {
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(networkObjectId);
                }
                if (_schNetworkLinks == null)
                {
                    _schNetworkLinks = _repository.GetQuery<SchNetworkObjectLink>(x => parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)).ToList();
                }
                //For New and Copy search all schedules
                if (scheduleId == 0 || isActionCopy)
                {
                    retVal = _schNetworkLinks.Where(x => x.ParentScheduleId == null && x.Schedule1.SchName.ToLower().CompareTo(schName.ToLower()) == 0).Any() ? true : false;
                }
                else
                {
                    retVal = _schNetworkLinks.Where(x => x.ParentScheduleId == null && x.ScheduleId != scheduleId && x.Schedule1.SchName.ToLower().CompareTo(schName.ToLower()) == 0).Any() ? true : false;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Check if cycle name is not unique
        /// </summary>
        /// <param name="cycleName"></param>
        /// <param name="schCyleId"></param>
        /// <param name="networkObjectId"></param>
        /// <param name="isActionCopy"></param>
        /// <returns></returns>
        public bool IsCycleNameNotUnique(string cycleName, int schCyleId, int networkObjectId, bool isActionCopy)
        {
            bool retVal = true;

            if (string.IsNullOrWhiteSpace(cycleName))
            {
                retVal = false;
            }
            else
            {
                if (!parentNetworkNodeIds.Any())
                {
                    parentNetworkNodeIds = _ruleService.GetNetworkParents(networkObjectId);
                }
                if (!childNetworkNodeIds.Any())
                {
                    childNetworkNodeIds = _ruleService.GetNetworkChilds(networkObjectId);
                }

                //For New and Copy search all menus
                if (schCyleId == 0 || isActionCopy)
                {
                    retVal = _repository.FindOne<SchCycle>(x => (parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)) && x.ParentSchCycleId == null && x.CycleName.ToLower().CompareTo(cycleName.ToLower()) == 0) == null ? false : true;
                }
                else
                {
                    retVal = _repository.FindOne<SchCycle>(x => (parentNetworkNodeIds.Contains(x.NetworkObjectId) || childNetworkNodeIds.Contains(x.NetworkObjectId)) && x.ParentSchCycleId == null && x.SchCycleId != schCyleId && x.CycleName.ToLower().CompareTo(cycleName.ToLower()) == 0) == null ? false : true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Private function to deteremine the Master Schedule Id - If Schedule doesn't have any parent it returns self Id
        /// </summary>
        /// <param name="ScheduleId">Current Schedule Id</param>
        /// <param name="pId">Parent Id</param>
        /// <returns></returns>
        private bool getMasterParentSchId(int ScheduleId, out int pId)
        {
            int? prntId = _repository.GetQuery<SchNetworkObjectLink>(x => x.ScheduleId == ScheduleId).FirstOrDefault().ParentScheduleId;

            if (!prntId.HasValue)
            {
                pId = ScheduleId;
                return true;
            }
            else
            {
                pId = prntId.Value;
                return false;
            }
        }

        /// <summary>
        /// Map function to map Schedule to a Model
        /// </summary>
        /// <param name="sch"></param>
        /// <param name="schModel"></param>
        private void MapSchtoSchModel(Schedule sch, ScheduleModel schModel)
        {
            schModel.ScheduleId = sch.ScheduleId;
            schModel.SchName = sch.SchName;
            schModel.StartDate = !sch.StartDate.HasValue ? null : (DateTime?)sch.StartDate.Value.ToLocalTime(); //get the local time
            schModel.EndDate = !sch.EndDate.HasValue ? null : (DateTime?)sch.EndDate.Value.ToLocalTime();// get the local time
            //TODO : Change how LastUpdatedDate is retrieved
            //schModel.LastUpdatedDate = sch.LastUpdatedDate;
        }

        /// <summary>
        /// Map function to Map model to Schedule
        /// </summary>
        /// <param name="model"></param>
        /// <param name="sch"></param>
        private void MapSchModeltoSch(ScheduleModel model, Schedule sch)
        {
            sch.SchName = model.SchName;
            sch.StartDate = !model.StartDate.HasValue ? null : (DateTime?)TimeZoneInfo.ConvertTimeToUtc(model.StartDate.Value); //store the UTC time
            sch.EndDate = !model.EndDate.HasValue ? null : (DateTime?)TimeZoneInfo.ConvertTimeToUtc(model.EndDate.Value);//store the UTC time
            //TODO : Change how LastUpdatedDate is retrieved
            //sch.LastUpdatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Move schedule at give network - used for recursion only
        /// </summary>
        /// <param name="model"></param>
        /// <param name="scheduleList"></param>
        /// <param name="childNetworkId"></param>
        /// <param name="newPriority"></param>
        /// <param name="oldPriority"></param>
        private void moveScheduleAtChildNetwork(ScheduleModel model, List<SchNetworkObjectLink> scheduleList, int childNetworkId, int newPriority, int oldPriority)
        {

            int alterOtherIndecesBy = 0;
            var scheduleInBetweenTheMove = new List<SchNetworkObjectLink>();

            // -1 is to move to last
            newPriority = (newPriority == -1) ? scheduleList.Min(x => x.Priority) : newPriority;

            if (newPriority > oldPriority)
            {//Move Up
                scheduleInBetweenTheMove = scheduleList.Where(x => x.Priority >= oldPriority && x.Priority <= newPriority).ToList();
                alterOtherIndecesBy = -1;
            }
            else
            {//Move Down
                scheduleInBetweenTheMove = scheduleList.Where(x => x.Priority <= oldPriority && x.Priority >= newPriority).ToList();
                alterOtherIndecesBy = 1;
            }
            foreach (var schToMove in scheduleInBetweenTheMove)
            {
                if (schToMove.Priority == oldPriority)
                {
                    if (childNetworkId == schToMove.NetworkObjectId)
                    {
                        schToMove.Priority = newPriority;
                        _repository.Update<SchNetworkObjectLink>(schToMove);
                    }
                    else
                    {
                        //Add new Priority row to scheduleNWLink
                        SchNetworkObjectLink schNetworkLinkwithNewPriority = new SchNetworkObjectLink
                        {
                            ScheduleId = schToMove.ScheduleId,
                            LastUpdatedDate = DateTime.Now,
                            NetworkObjectId = childNetworkId,
                            OverrideStatus = schToMove.OverrideStatus != OverrideStatus.HIDDEN ? OverrideStatus.MOVED : schToMove.OverrideStatus,
                            Priority = newPriority,
                            ParentScheduleId = schToMove.ParentScheduleId.HasValue ? schToMove.ParentScheduleId : schToMove.ScheduleId
                        };
                        _repository.Add<SchNetworkObjectLink>(schNetworkLinkwithNewPriority);
                    }
                }
                else
                {
                    if (childNetworkId == schToMove.NetworkObjectId)
                    {
                        schToMove.Priority += alterOtherIndecesBy;
                        _repository.Update<SchNetworkObjectLink>(schToMove);
                    }
                    else
                    {
                        //Add new Priority row to scheduleNetworkLink
                        SchNetworkObjectLink schNetworkLinkwithNewPriority = new SchNetworkObjectLink
                        {
                            ScheduleId = schToMove.ScheduleId,
                            LastUpdatedDate = DateTime.Now,
                            NetworkObjectId = childNetworkId,
                            OverrideStatus = schToMove.OverrideStatus != OverrideStatus.HIDDEN ? OverrideStatus.MOVED : schToMove.OverrideStatus,
                            Priority = schToMove.Priority + alterOtherIndecesBy,
                            ParentScheduleId = schToMove.ParentScheduleId.HasValue ? schToMove.ParentScheduleId : schToMove.ScheduleId
                        };
                        _repository.Add<SchNetworkObjectLink>(schNetworkLinkwithNewPriority);
                    }
                }
            }
        }

        /// <summary>
        /// Saves or create Schedule Details of Schedule and for a specific NW
        /// </summary>
        /// <param name="model">Schedule Model</param>
        private bool SaveSchDetails(ScheduleModel model)
        {
            var retVal = true;

            childNetworkNodeIds = _ruleService.GetNetworkChilds(NetworkObjectId);
            //No inner try catch blocks
            // get proper schedule details
            var schDetails = _ruleService.GetScheduleDetails(model.ScheduleId, NetworkObjectId);

            if (model.SchSummary != null)
            {
                //loop though each row of cycle and update the details
                foreach (var detailSumm in model.SchSummary)
                {
                    //get this cycle details
                    var schCycleDetails = schDetails.Where(x => x.SchCycleId == detailSumm.SchCycleId).ToList();

                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Sunday, detailSumm.SunST,
                        detailSumm.SunET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Monday, detailSumm.MonST,
                        detailSumm.MonET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Tuesday, detailSumm.TueST,
                        detailSumm.TueET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Wednesday, detailSumm.WedST,
                        detailSumm.WedET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Thursday, detailSumm.ThuST,
                        detailSumm.ThuET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Friday, detailSumm.FriST,
                        detailSumm.FriET);
                    UpdateSchDetail(schCycleDetails, detailSumm.SchCycleId, DayOfWeek.Saturday, detailSumm.SatST,
                        detailSumm.SatET);
                }
                _context.SaveChanges();
            }
            return retVal;
        }

        /// <summary>
        /// Add/Edit/Override/Delete a Schedule detail for specific Network
        /// </summary>
        /// <param name="schCycleDetails">List of all details in this cycle and schedule </param>
        /// <param name="schCycleid">Cycle Id</param>
        /// <param name="day">Day of Detail</param>
        /// <param name="sTime">Start Time</param>
        /// <param name="eTime">End Time</param>
        private void UpdateSchDetail(List<SchDetail> schCycleDetails, int schCycleid, DayOfWeek day, TimeSpan? sTime, TimeSpan? eTime)
        {
            // If Time has value is present then detail should be added/ updated
            if (sTime.HasValue && eTime.HasValue)
            {
                //If the a detail is present for this NW , cycle and Day - update it
                if (schCycleDetails.Where(x => x.DayofWeek == (int)day).Any())
                {
                    var currentSchDetailStartTimerow = schCycleDetails.Where(x => x.DayofWeek == (int)day).FirstOrDefault();
                    var currentSchDetailEndTimerow = schCycleDetails.Where(x => x.DayofWeek == (int)day).FirstOrDefault();

                    currentSchDetailStartTimerow.StartTime = sTime.Value;
                    currentSchDetailEndTimerow.EndTime = eTime.Value;
                    _repository.Update<SchDetail>(currentSchDetailStartTimerow);
                    _repository.Update<SchDetail>(currentSchDetailEndTimerow);
                }
                else
                {
                    SchDetail newSchDetail = new SchDetail
                    {
                        SchCycleId = schCycleid,
                        ScheduleId = ScheduleId,
                        DayofWeek = (byte)day,
                        StartTime = sTime.Value,
                        EndTime = eTime.Value,
                        IsActive = true
                    };
                    _repository.Add<SchDetail>(newSchDetail);

                }
            }
            else
            {
                //If a detail is present for this NW , cycle and Day - delete it
                if (schCycleDetails.Where(x => x.DayofWeek == (int)day).Any())
                {
                    var detailToDel = _repository.GetQuery<SchDetail>(x => x.SchCycleId == schCycleid && x.ScheduleId == ScheduleId && x.DayofWeek == (int)day).FirstOrDefault();

                    //remove the detail
                    _repository.Delete<SchDetail>(detailToDel);
                }
            }
        }

        /// <summary>
        /// Get next schedule name on copy
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="netId"></param>
        /// <returns></returns>
        private string getNextSchedulename(string pattern, int netId)
        {
            string tmp = string.Format(pattern, 1);
            if (tmp == pattern)
                throw new ArgumentException("The pattern must include an index place-holder", "pattern");

            if (!IsScheduleNameNotUnique(tmp, 0, NetworkObjectId, true))
                return tmp; // short-circuit if no matches

            int min = 1, max = 2; // min is inclusive, max is exclusive/untested

            while (IsScheduleNameNotUnique(string.Format(pattern, max), 0, NetworkObjectId, true))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (IsScheduleNameNotUnique(string.Format(pattern, pivot), 0, NetworkObjectId, true))
                    min = pivot;
                else
                    max = pivot;
            }

            return string.Format(pattern, max);
        }
    }

    public interface IScheduleService
    {
        RuleService _ruleService { get; set; }
        string LastActionResult { get; }
        int ScheduleId { get; set; }
        int NetworkObjectId { get; set; }
        int Count { get; set; }

        ScheduleModel GetSchedule(int id, int netId);
        List<ScheduleModel> GetScheduleList(int networkId);
        List<ScheduleSummary> GetScheduleDetailSummary(int schId, int networkObjectId);
        List<SchCycleModel> GetScheduleCycles(int netId, bool includeInactives = false);
        List<ScheduleModel> GetInActiveScheduleList(KendoGridRequest grdRequest);
        NetworkObject GetNetworkObject(int netId,out int brandId, out string parentsBreadCrum);

        ScheduleModel SaveSchedule(ScheduleModel model);
        void DeleteSchedule(int schId);
        void DisableSchedule(int schId);
        SchCycleModel UpdateCycle(SchCycleModel model);
        void DestroyCycle(SchCycleModel model, int netId);
        void DisableCycle(SchCycleModel model, int netId);
        void EnableCycle(SchCycleModel model, int netId);
        bool MoveCycle(SchCycleModel model, int netId, int newIndex, int oldIndex);
        SchCycleModel CreateCycle(SchCycleModel model);
        List<int> AddInActiveSchedules(int[] schIds);

        bool IsCycleNameNotUnique(string p1, int p2, int p3, bool p4);

        void EnableSchedule(int id);

        void CopySchedule(ScheduleModel schModel);

        void RevertSchedule(int id);
        bool MoveSchedule(ScheduleModel model, int netId, int newIndex, int oldIndex);
    }
}