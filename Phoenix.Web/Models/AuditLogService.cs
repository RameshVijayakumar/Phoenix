using Phoenix.Common;
using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Security.Principal;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;
using Phoenix.RuleEngine;
using System.Security.Claims;
using Phoenix.Web.Models.Utility;

namespace Phoenix.Web.Models
{

    public class AuditLogService : KendoGrid<AuditLog>, IAuditLogService
    {
        private string _loggedInUserName = ClaimPrincipalHelper.GetLoggedInUserName(System.Security.Claims.ClaimsPrincipal.Current);
        private IRepository _repository;
        private DbContext _context;
        private RuleEngine.RuleService _ruleService;
        public AuditLogService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _ruleService = new RuleEngine.RuleService(RuleEngine.RuleService.CallType.Web);

        }

        /// <summary>
        /// Get Audit Logs
        /// </summary>
        /// <param name="noOfDays"></param>
        /// <param name="resultCount"></param>
        /// <param name="grdRequest"></param>
        /// <returns></returns>
        public List<AuditLogModel> GetAll(string noOfDays, out int resultCount, KendoGridRequest grdRequest)
        {
           var aLogs = new List<AuditLogModel>();
           resultCount = 0;
            try
            {
                DateTime startDate = new DateTime();
                DateTime endDate = new DateTime();
                bool isDateRangeAvailable = true;

                int lastNoOfDays = 0;
                //If last number of days is selected
                if (Int32.TryParse(noOfDays, out lastNoOfDays))
                {
                    startDate = DateTime.UtcNow.AddDays(lastNoOfDays * -1).Date;
                    endDate = DateTime.UtcNow.AddDays(lastNoOfDays == 1 ? 0 : 1).Date;
                }
                else
                {//get all records
                    isDateRangeAvailable = false;
                }

                List<NetworkPermissions> permissions = HttpContext.Current.Session[Constants.SessionValue.CURRENT_USER_PERM] as List<NetworkPermissions>;
               
                var filtering = GetFiltering(grdRequest);
                var sorting = GetSorting(grdRequest);
                var permittedNetworkIrisIdList = permissions.Where(p => p.HasAccess == true).Select(p => p.Id).ToList();
                var permittedNetworkIdList = _ruleService.GetNetworkIds(permittedNetworkIrisIdList);
                var indeterminateLogs = _repository.GetQuery<AuditLog>(p => permittedNetworkIdList.Contains(p.NetworkObjectId)).Where(filtering);

                if (isDateRangeAvailable)
                {
                    indeterminateLogs = indeterminateLogs.Where(x => x.CreatedDate >= startDate && x.CreatedDate <= endDate);
                } 
                resultCount = indeterminateLogs.Count();
             
                var allLogs = indeterminateLogs.OrderBy(sorting).Skip(grdRequest.Skip).Take(grdRequest.PageSize);


                foreach (var log in allLogs)
                {
                    aLogs.Add(new AuditLogModel
                    {
                        AuditLogId = log.AuditLogId,                      
                        UserName = log.UserName,
                        NetworkObjectId = log.NetworkObjectId,
                        NetworkObjectName = log.NetworkObjectName,
                        Type = log.Type,
                        Description = log.Description,
                        Details = log.Details,
                        CreatedDate = log.CreatedDate,
                        Name = log.Name
                    });               
                }
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return aLogs;
        }

        //public void Write(int? netId, AuditLogType type, string desc, string details, int? entityId = 0, string name = "")
        //{
        //    try
        //    {
        //        if (entityId.HasValue && entityId != 0)
        //        {
        //          //  (_context as ProductMasterContext).usp_InsertAuditLog(_userId, netId, type.ToString(), desc, details, entityId);
        //        }
        //        else
        //        {
        //            //AuditLog auditLog = new AuditLog();
        //            //auditLog.UserId = _userId;
        //            //auditLog.NetworkObjectId = netId;
        //            //auditLog.Type = type.ToString();
        //            //auditLog.Description = desc;
        //            //auditLog.Details = details;
        //            //auditLog.CreatedDate = DateTime.UtcNow;
        //            //auditLog.Name = name;
        //            //_repository.Add<AuditLog>(auditLog);
        //            //_context.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.WriteError(ex);
        //    }
        //}

        /// <summary>
        /// logs the information of the operation performed for the audit maintenance
        /// </summary>
        /// <param name="operationPerformed">Name of the operation performed</param>
        /// <param name="entityType">name of the entity</param>
        /// <param name="netObjectId">Optional. The method does expect the value of either this parameter or the parameter, 'netObjectName'</param>
        /// <param name="entityId">Optional. This is a comma-seperated list of entity names. The method does expect the value of either this parameter or the parameter, 'entityNameList'</param>
        /// <param name="operationDescription">Optional. The method does expect the value if OperationPerformed is <b>Other</b></param>
        /// <param name="netObjectName">Optional. The method does expect the value of either this parameter or netObjectId</param>
        /// <param name="entityNameList">Optional. The method does expect the value of either this parameter or the parameter, 'entityId'</param>
        /// <param name="operationDetail">Optional</param>
        public void Write(OperationPerformed operationPerformed, EntityType entityType, int? netObjectId = null, string netObjectName = "", IEnumerable<int> entityId = null, string entityNameList = "", string operationDescription = "", string operationDetail = "")
        {
            try
            {
                var userName = _loggedInUserName;
                var entityTypeString = entityType.ToString();
                var commaSeparatedEntityIds = entityId != null ? string.Join(",", entityId) : string.Empty;
                operationDescription = string.IsNullOrEmpty(operationDescription) ? operationPerformed.ToString() : operationDescription;

                if (string.IsNullOrEmpty(netObjectName))
                {
                    netObjectName = (string)HttpContext.Current.Session["SelectedNetworkObjectName"];
                }

                if (!netObjectId.HasValue)
                {
                    var netObjIdFromSession = HttpContext.Current.Session["SelectedNetworkObjectId"];
                    if (netObjIdFromSession != null && string.IsNullOrEmpty(netObjIdFromSession.ToString()) == false)
                    {
                        netObjectId = Convert.ToInt32(HttpContext.Current.Session["SelectedNetworkObjectId"]);
                    }                   
                }

                //If the above statements still leave these two variables blank then 
                //these variable will be set with the hightest level network object this user has access on.
                if (!netObjectId.HasValue && string.IsNullOrEmpty(netObjectName))
                {
                    var permissions = HttpContext.Current.Session[Constants.SessionValue.CURRENT_USER_PERM] as List<NetworkPermissions>;
                    var netId = permissions != null && permissions.FirstOrDefault() != null ? permissions.FirstOrDefault().Id : 0;
                    var network = _repository.GetQuery<NetworkObject>(x => x.IrisId == netId).FirstOrDefault();
                    if(network != null)
                    {
                        netObjectId = network.NetworkObjectId;
                    }
                }

                if ((!string.IsNullOrEmpty(commaSeparatedEntityIds) && string.IsNullOrEmpty(entityNameList)) || (netObjectId.HasValue && string.IsNullOrEmpty(netObjectName)))
                {
                    //Use the Stored Procedure to save information
                    (_context as ProductMasterContext).usp_InsertAuditLog(userName, netObjectId.Value, netObjectName, entityTypeString, commaSeparatedEntityIds, entityNameList, operationDescription, operationDetail);
                }
                else
                {
                    _repository.Add<AuditLog>(
                         new AuditLog
                         {
                             NetworkObjectId = netObjectId.Value,
                             UserName = userName,
                             Type = entityTypeString,
                             NetworkObjectName = netObjectName,
                             Name = entityNameList,
                             CreatedDate = DateTime.UtcNow,
                             Description = operationDescription,
                             Details = operationDetail
                         });
                    _context.SaveChanges();
                }
                //No need to logging in multiple places
                //Logger.WriteAudit(string.Format("Audit: action - {0} performed on '{1}' at network '{2}', Details - {3}",operationDescription,entityTypeString,netObjectName,operationDetail));
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
        }
    }

    public interface IAuditLogService
    {
        List<AuditLogModel> GetAll(string noofDays, out int resultCount, KendoGridRequest grdRequest);
        //void Write(int? netId, AuditLogType type, string desc, string details, int? entityId = 0, string name = "");
        void Write(OperationPerformed operationPerformed, EntityType entityType, int? netObjectId = null, string netObjectName = "", IEnumerable<int> entityId = null, string entityNameList = "", string operationDescription = "", string operationDetail = "");
    }
}