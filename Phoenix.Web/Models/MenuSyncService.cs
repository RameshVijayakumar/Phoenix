using Newtonsoft.Json;
using Phoenix.Common;
using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using Phoenix.RuleEngine;
using Omu.ValueInjecter;
using SnowMaker;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure;
using Phoenix.Web.Models.Grid;
using System.Linq.Dynamic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Phoenix.Web.Models
{
    public class MenuSyncService : IMenuSyncService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        private UniqueIdGenerator _irisIdGenerator;
        public List<NetworkPermissions> Permissions { get; set; }
        private string auditType = Enum.GetName(typeof(AuditLogType), AuditLogType.Target);
        private IAuditLogService _auditLogger;
        private ICommonService _commonService;
        private ITagService _tagService;
        private RuleService _ruleService;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }
        public int Count { get; set; }

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
        public MenuSyncService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _commonService = new CommonService(_repository);
            _tagService = new TagService(_context, _repository);
            _ruleService = new RuleService(RuleService.CallType.Web);
        }

        /// <summary>
        /// gets all the targets
        /// </summary>      
        /// <returns></returns>
        public List<MenuSyncTargetModel> GetTargetList(int? networkObjectId, List<int> networkObjectsIds)
        {
            var targetlist = new List<MenuSyncTargetModel>();
            List<MenuSyncTarget> targets = null;

            if (networkObjectsIds == null)
            {
                networkObjectsIds = new List<int>();
            }

            if (networkObjectId.HasValue)
            {
                networkObjectsIds.Add(networkObjectId.Value);
            }

            //get Site NetworkObjects under provided NetworkObjectIds
            var networkBrandsOfSelectedNetworks = (_context as ProductMasterContext).fnNetworkObjectParentsOfSelectedNetworks(string.Join(",", networkObjectsIds)).Where(x => x.NetworkObjectTypeId == (int)NetworkObjectTypes.Brand).Select(x => x.NetworkObjectId).ToList();

            targets = _repository.GetQuery<MenuSyncTarget>(x => networkBrandsOfSelectedNetworks.Contains(x.NetworkObjectId)).Include("TargetTagLinks").Distinct().OrderBy(p => p.Name).ToList();
                    
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    var channelList = getTargetRelatedTags(target);
                    targetlist.Add(new MenuSyncTargetModel
                    {
                        MenuSyncTargetId = target.MenuSyncTargetId,
                        TargetName = target.Name,
                        URL = target.URL,
                        Token = target.Token,
                        LastSyncStatus = target.LastSyncStatus,
                        LastSyncDate = target.LastSyncDate,
                        NetworkObjectId = target.NetworkObjectId,
                        Channels = channelList,
                        ChannelNameList = string.Join(",", channelList.Select(x => x.TagName)),
                        ChannelIdList = string.Join(",", channelList.Select(x => x.TagId)),
                    });
                }
            }
            return targetlist;
        }

        /// <summary>
        /// Get Sync History
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="mulitpleNetworkObjIds"></param>
        /// <returns></returns>
        public List<MenuSyncTargetDetailModel> GetTargetDetailList(List<int> mulitpleNetworkObjIds, KendoGridRequest grdRequest)
        {
            var targetDetaillist = new List<MenuSyncTargetDetailModel>();
            try
            {

                if (mulitpleNetworkObjIds == null)
                {
                    mulitpleNetworkObjIds = new List<int>();
                }
                if (mulitpleNetworkObjIds.Any())
                {
                    KendoGrid<MenuSyncTargetDetail> targetGrid = new KendoGrid<MenuSyncTargetDetail>();
                    var filtering = targetGrid.GetFiltering(grdRequest);
                    var sorting = targetGrid.GetSorting(grdRequest);

                    var allSiteIdsSelected = _commonService.GetSiteIdsUnderNetworkObjects(mulitpleNetworkObjIds);

                    var targetDetails = _repository.GetQuery<MenuSyncTargetDetail>(x => allSiteIdsSelected.Contains(x.NetworkObjectId))
                        .Include("NetworkObject").Where(filtering).OrderBy(sorting);

                    Count = targetDetails.Count();

                    foreach (var targetDetail in targetDetails.Skip(grdRequest.Skip).Take(grdRequest.PageSize))
                    {
                        var targetDetailModel = new MenuSyncTargetDetailModel();
                        targetDetailModel.InjectFrom(targetDetail);
                        targetDetailModel.TargetName = targetDetail.TargetName;
                        targetDetailModel.NetworkObject.Name = targetDetail.NetworkObject.Name;

                        targetDetaillist.Add(targetDetailModel);
                    }

                    targetDetaillist = targetDetaillist.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
            }
            return targetDetaillist;
        }

        /// <summary>
        /// Check unique target name
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isTargetNotUnique"></param>
        /// <param name="isURLNotUnique"></param>
        public void CheckUniquenessOfReqdData(MenuSyncTargetModel model, out bool isTargetNotUnique, out bool isURLNotUnique)
        {
            isTargetNotUnique = false;
            isURLNotUnique = false;
            var tmpTarget = new MenuSyncTargetModel();

            var targetList = GetTargetList(model.NetworkObjectId, null);

            //Check for uniqueness of Target 
            tmpTarget = targetList.Find(item => item.MenuSyncTargetId != model.MenuSyncTargetId && item.TargetName == model.TargetName);
            if (tmpTarget != null)
            {
                isTargetNotUnique = true;
            }

            //Check for the uniqueness of URL
            tmpTarget = targetList.Find(item => item.MenuSyncTargetId != model.MenuSyncTargetId && item.URL == model.URL);
            if (tmpTarget != null)
            {
                isURLNotUnique = true;
            }
        }

        /// <summary>
        /// Saves Target Information
        /// </summary>
        /// <param name="model"></param>
        /// <param name="SupressAppendingActionResult">This parameter decides if _lastActionResult is to be update in after the operation</param>
        /// <returns></returns>
        public MenuSyncTargetModel SaveTarget(MenuSyncTargetModel model, bool SupressAppendingActionResultAndUpdatingChannels = false)
        {
            try
            {
                bool isDeletedOnly = false;
                List<int> intTagIds = null;
                var entityIds = new List<int>();
                var tagIdsAdded = string.Empty;
                var tagIdsRemoved = string.Empty;
                var tagKeyName = TagKeys.Channel.ToString();
                string addedTagNames = string.Empty, removedTagNames = string.Empty;

                var target = _repository.GetQuery<MenuSyncTarget>(x => x.MenuSyncTargetId == model.MenuSyncTargetId).Include("TargetTagLinks").FirstOrDefault();
                if (target != null)
                {
                    var targetChannelLinks = getTargetRelatedTags(target);
                    target.Name = string.IsNullOrWhiteSpace(model.TargetName) ? string.Empty : model.TargetName.Trim();
                    target.URL = string.IsNullOrWhiteSpace(model.URL) ? string.Empty : model.URL.Trim();
                    target.LastSyncStatus = model.LastSyncStatus;
                    target.LastSyncDate = model.LastSyncDate;
                    

                    if (!SupressAppendingActionResultAndUpdatingChannels)
                    {
                        //Load the Nodes of the current tree
                        _ruleService.NetworkObjectId = model.NetworkObjectId;
                        var parentNetworkNodeIds = _ruleService.GetNetworkParents(_ruleService.NetworkObjectId);

                        var existingChannelIds = string.Join(",", targetChannelLinks.Select(x => x.TagId));
                        var existingChannelNames = string.Join(",", targetChannelLinks.Select(x => x.TagName));
                        var allTags = _repository.GetQuery<Tag>(x => x.TagKey.Equals(tagKeyName, StringComparison.InvariantCultureIgnoreCase) && parentNetworkNodeIds.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();

                        if (existingChannelIds.Equals(model.ChannelIdList, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            entityIds.Add(model.MenuSyncTargetId);

                            _tagService.GetTagsAddedNRemoved(model.ChannelIdList, existingChannelIds, out tagIdsAdded, out tagIdsRemoved);
                            _tagService.GetTagsAddedNRemoved(model.ChannelNameList, existingChannelNames, out addedTagNames, out removedTagNames);
                            _tagService.AttachTagsToEntity(tagIdsAdded, tagIdsRemoved, entityIds.ToArray(), TagEntity.Target, TagKeys.Channel, ref isDeletedOnly, ref intTagIds);

                        }
                        _lastActionResult = string.Format(Constants.AuditMessage.EntityTagUpdated, string.Join(",", addedTagNames), string.Join(",", removedTagNames));
                    }
                    _repository.Update<MenuSyncTarget>(target);

                }
                else
                {
                    target = new MenuSyncTarget
                    {
                        Name = string.IsNullOrWhiteSpace(model.TargetName) ? string.Empty : model.TargetName.Trim(),
                        URL = string.IsNullOrWhiteSpace(model.URL) ? string.Empty : model.URL.Trim(),
                        Token = string.IsNullOrWhiteSpace(model.Token) ? string.Empty : model.Token.Trim(),
                        LastSyncStatus = model.LastSyncStatus,
                        LastSyncDate = model.LastSyncDate,
                        NetworkObjectId = model.NetworkObjectId
                    };
                    _repository.Add<MenuSyncTarget>(target);
                }

                _context.SaveChanges();
                if (model.MenuSyncTargetId == 0)
                {
                    if (!SupressAppendingActionResultAndUpdatingChannels)
                    {
                        _lastActionResult = string.Format(Constants.AuditMessage.TargetCreatedT, model.TargetName) + _lastActionResult;
                    }
                    _auditLogger.Write(OperationPerformed.Created, EntityType.Target, entityNameList: model.TargetName, operationDescription: _lastActionResult);                    
                }
                else
                {
                    if (!SupressAppendingActionResultAndUpdatingChannels)
                    {
                        _lastActionResult = string.Format(Constants.AuditMessage.TargetUpdatedT, model.TargetName) + _lastActionResult;
                    }
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.Target, entityNameList: model.TargetName, operationDescription: _lastActionResult);                 
                }

                model.MenuSyncTargetId = target.MenuSyncTargetId;
            }
            catch (Exception ex)
            {
                _lastActionResult = Constants.StatusMessage.ErrTargetSyncSave;
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        /// Delete targets 
        /// </summary>
        /// <param name="targetIds">array of target Ids</param>
        /// <returns></returns>
        public bool DeleteTargets(string targetList)
        {
            bool isActionFailed = false;
            try
            {
                if (!string.IsNullOrEmpty(targetList))
                {
                    //1. Deserialize the Data
                    var js = new JavaScriptSerializer();
                    var targets = js.Deserialize<List<MenuSyncTargetModel>>(targetList);
                    if (targets != null && targets.Any())
                    {
                        var targetIdList = targets.Select(p => p.MenuSyncTargetId);

                        // delete Target(s)
                        _repository.Delete<TargetTagLink>(p => targetIdList.Contains(p.TargetId));
                        _repository.Delete<MenuSyncTarget>(p => targetIdList.Contains(p.MenuSyncTargetId));
                        _context.SaveChanges();

                        var commaSeperatedTargetNames = string.Join(",", targets.Select(t => t.TargetName).ToList());
                        _lastActionResult = string.Format(Constants.AuditMessage.TargetDeletedT, commaSeperatedTargetNames);
                        _auditLogger.Write(OperationPerformed.Deleted, EntityType.Target, entityNameList: commaSeperatedTargetNames);
                    }
                }              
            }
            catch (Exception ex)
            {
                isActionFailed = true;
                // write an error.
                _lastActionResult = Constants.StatusMessage.ErrTargetSyncDelete;
                Logger.WriteError(ex);
            }
            return isActionFailed;
        }

        /// <summary>
        /// Sync the menu for a given set of networkObjectIds
        /// </summary>
        /// <param name="networkObjectIdList"></param>
        /// <param name="targetsList"></param>
        public bool SyncTargets(string networkObjectIdList, string targetsList)
        {
            bool isActionFailed = false;
            var tmpLastActionResult = string.Empty;
            var tmpAuditMessage = string.Empty;
            var tmpResponseMessage = string.Empty;
            try
            {
                HttpStatusCode? httpPostResponseCode = HttpStatusCode.BadRequest;

                //1. Deserialize the Data
                var js = new JavaScriptSerializer();

                //1.1 Deserialize JSON String:networkObjectIds to List<int>()
                var networkObjectIds = js.Deserialize<List<int>>(networkObjectIdList);

                //1.2 Deserialize JSON String:targetURLs to List<MenuSyncTargetModel>()
                var targets = js.Deserialize<List<MenuSyncTargetModel>>(targetsList);

                var menuSyncdata = new MenuSyncModel();

                var sites = _commonService.GetSitesUnderNetworkObjects(networkObjectIds);
                var siteIdList = sites.Select(p => p.IrisId).ToList();


                //2. Prepare "post body" : Comma-seperated siteIds
                //2.1 get SiteIrisIds of the provide NetworkObjectIds as List<string>
                menuSyncdata.Sites = siteIdList.ConvertAll(x => x.ToString());


                ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

                if (menuSyncdata.Sites.Any())
                {
                    //3. For each target in targets(Step 1.3) initiate HTTP POST requests
                    foreach (var target in targets)
                    {
                        //Send unique sync Id for each target.
                        menuSyncdata.Id = _irisIdGenerator.NextId(Constants.IrisConstants.IrisIdCommonScopeName);

                        //3.1 Build JSON
                        var postData = JsonConvert.SerializeObject(menuSyncdata);

                        //3.2 Initiate HTTP POST                             
                        HTTPRequestHelper.Call(target.URL, HTTPRequestHelper.HTTPMethod.POST, "application/json", postData, out httpPostResponseCode, out tmpResponseMessage);
                        if (httpPostResponseCode == HttpStatusCode.OK || httpPostResponseCode == HttpStatusCode.NoContent)
                        {
                            target.LastSyncStatus = "Sent";
                            tmpLastActionResult = Constants.AuditMessage.TargetSyncSuccessT;
                            tmpAuditMessage = Constants.AuditMessage.TargetSyncSuccessAuditT;
                        }
                        else
                        {
                            isActionFailed = true;
                            target.LastSyncStatus = "Failed";
                            tmpLastActionResult = Constants.StatusMessage.ErrTargetSyncFailedT;
                            tmpAuditMessage = Constants.StatusMessage.ErrTargetSyncFailedAuditT;
                        }

                        //3.3 Maintain Auditlog
                        _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : string.Format("{0}<br/>", _lastActionResult)) + string.Format(tmpLastActionResult, target.TargetName, httpPostResponseCode, tmpResponseMessage);

                        //3.2 Update the Sync calls Status in the database    
                        target.LastSyncDate = DateTime.UtcNow;
                        SaveTarget(target, true);

                        // Add details for logging
                        var requestSentTime = DateTime.UtcNow;
                        _context.Configuration.AutoDetectChangesEnabled = false;

                        foreach (var site in sites)
                        {
                            var targetDetail = new MenuSyncTargetDetail
                            {
                                SyncIrisId = menuSyncdata.Id,
                                TargetName = target.TargetName,
                                NetworkObjectId = site.NetworkObjectId,
                                RequestedTime = requestSentTime,
                                SyncStatus = target.LastSyncStatus
                            };
                            _repository.Add<MenuSyncTargetDetail>(targetDetail);
                        }

                        _context.SaveChanges();

                        _auditLogger.Write(OperationPerformed.Other, EntityType.Target, entityNameList: string.Join(",", targets.Select(p => p.TargetName).ToList()), operationDescription: string.Format(tmpAuditMessage, target.TargetName, menuSyncdata.Sites.Count(), httpPostResponseCode, tmpResponseMessage));
                    }
                }
                else
                {
                    isActionFailed = true;
                    _lastActionResult = Constants.StatusMessage.ErrTargetSyncFailedNoSitesT;
                }
            }
            catch (Exception ex)
            {
                isActionFailed = true;                
                Logger.WriteError(ex);
                _lastActionResult = (string.IsNullOrWhiteSpace(_lastActionResult) ? string.Empty : string.Format("{0}<br/>", _lastActionResult)) + Constants.StatusMessage.ErrTargetsSyncFailedUnexpectedT;
            }
            return isActionFailed;
        }

        /// <summary>
        /// Delete Sync History
        /// </summary>
        /// <param name="networkObjectDetails"></param>
        /// <returns></returns>
        public bool DeleteSyncHistory(string networkObjectDetails)
        {
            bool retStatus = false;
            var tmpLastActionResult = string.Empty;
            var tmpAuditMessage = string.Empty;
            var tmpResponseMessage = string.Empty;
            try
            {
                List<CheckedSiteModel> mulitpleNetworkObjs = null;
                if (string.IsNullOrWhiteSpace(networkObjectDetails) == false)
                {
                    mulitpleNetworkObjs = JsonConvert.DeserializeObject<List<CheckedSiteModel>>(networkObjectDetails);
                }

                if (mulitpleNetworkObjs == null)
                {
                    mulitpleNetworkObjs = new List<CheckedSiteModel>();
                }

                if (mulitpleNetworkObjs.Any())
                {
                    var mulitpleNetworkObjIds = mulitpleNetworkObjs.Select(x => x.Id).ToList();

                    (_context as ProductMasterContext).usp_DeleteMenuSyncDetails(string.Join(",",mulitpleNetworkObjIds));

                    var toplevelNetworkTypeId = mulitpleNetworkObjs.Select(x => x.Typ).Distinct().OrderBy(x => x).FirstOrDefault();
                    var toplevelNetworks = mulitpleNetworkObjs.Where(x => x.Typ == toplevelNetworkTypeId).ToList();
                    //Maintain Auditlog
                    _lastActionResult = string.Format(Constants.AuditMessage.SyncHistoryDeletedT);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Target, operationDescription: string.Format(Constants.AuditMessage.SyncHistoryDeletedDetailT, string.Join(",", toplevelNetworks.Select(p => p.Name).ToList())));

                    retStatus = true;
                }
                else
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.SyncHistoryDeleteInvalidSitesT);
                }
            }
            catch (Exception ex)
            {
                retStatus = false;
                Logger.WriteError(ex);
                _lastActionResult = Constants.StatusMessage.ErrTargetsSyncFailedUnexpectedT;
            }
            return retStatus;
        }

        /// <summary>
        /// Refresh Clients
        /// </summary>
        /// <returns></returns>
        public bool RefreshTargets()
        {
            var retVal = false;

            try
            {
                var config = new AppConfiguration();
                AuthenticationService authenticationService = null;
                var status = HttpStatusCode.OK;

                //Get Clients from SSO
                if (config.LoadSSOConfig(CloudConfigurationManager.GetSetting(AzureConstants.DiagnosticsConnectionString)))
                {
                    authenticationService = new AuthenticationService(config);
                }
                var clients = authenticationService.GetAllAccessibleClients(out status);

                if (status == HttpStatusCode.OK && clients.Any())
                {
                    var existingTargets = _repository.GetQuery<MenuSyncTarget>().ToList();
                    var clientIrisIds = clients.SelectMany(x => x.IrisIds).ToList();
                    var clientNetworks = _repository.GetQuery<NetworkObject>(x => clientIrisIds.Contains(x.IrisId)).ToList();
                    var clientAppsFound = new List<string>();

                    foreach (var client in clients)
                    {
                        //Add only if client has permissions
                        if (client.IrisIds.Any())
                        {
                            var network = clientNetworks.FirstOrDefault(x => x.IrisId == client.IrisIds[0]);
                            if(network != null)
                            {
                                clientAppsFound.Add(client.Id);
                                var matchingTarget = existingTargets.Where(x => x.ApplicationIdentifier.Equals(client.Id, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                                if (matchingTarget == null)
                                {
                                    var newTarget = new MenuSyncTarget
                                    {
                                        Name = client.Name,
                                        ApplicationIdentifier = client.Id,
                                        NetworkObject = network,
                                        URL = "http"
                                    };
                                    _repository.Add<MenuSyncTarget>(newTarget);
                                }
                                else
                                {
                                    if (matchingTarget.NetworkObjectId != network.NetworkObjectId)
                                    {
                                        _repository.Delete<TargetTagLink>(x => x.TargetId == matchingTarget.MenuSyncTargetId);
                                        matchingTarget.NetworkObjectId = network.NetworkObjectId;
                                        _repository.Update<MenuSyncTarget>(matchingTarget);
                                    }
                                }
                            }
                        }
                    }

                    var targetsNotExisting = existingTargets.Where(x => !clientAppsFound.Contains(x.ApplicationIdentifier)).Select(x => x.MenuSyncTargetId).ToList();

                    if (targetsNotExisting.Any())
                    {
                        // Delete Targets not present in SSO
                        _repository.Delete<TargetTagLink>(x => targetsNotExisting.Contains(x.TargetId));
                        _repository.Delete<MenuSyncTarget>(x => targetsNotExisting.Contains(x.MenuSyncTargetId));
                    }

                    _context.SaveChanges();
                    retVal = true;
                    _lastActionResult = string.Format(Constants.AuditMessage.TargetRefreshedT);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.StatusMessage.ErrTargetRefreshFailedUnableToRetrieveT);
                }


            }
            catch (Exception ex)
            {
                Logger.WriteError(ex);
                _lastActionResult = string.Format(Constants.StatusMessage.ErrTargetRefreshFailedT);
            }

            return retVal;
        }

        /// <summary>
        /// Get Target's Tags
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private List<TagModel> getTargetRelatedTags(MenuSyncTarget target)
        {
            var channels = new List<TagModel>();
            var channelTagKey = TagKeys.Channel.ToString().ToLower();
            foreach (var menuTagLink in target.TargetTagLinks.Where(x => x.Tag != null && x.Tag.TagKey == channelTagKey))
            {
                if (menuTagLink.Tag != null)
                {
                    channels.Add(new TagModel
                    {
                        TagId = menuTagLink.Tag.Id,
                        TagName = menuTagLink.Tag.TagName
                    });
                }
            }

            return channels;
        }

        /// <summary>
        /// certificate validation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

    }

    public interface IMenuSyncService
    {
        string LastActionResult { get; }
        int Count { get; set; }
        List<MenuSyncTargetModel> GetTargetList(int? networkObjectId, List<int> networkObjectsIds);
        List<MenuSyncTargetDetailModel> GetTargetDetailList(List<int> mulitpleNetworkObjIds, KendoGridRequest grdRequest);
        MenuSyncTargetModel SaveTarget(MenuSyncTargetModel model, bool SupressAppendingActionResult = false);
        void CheckUniquenessOfReqdData(MenuSyncTargetModel model, out bool isTargetNotUnique, out bool isURLNotUnique);
        bool DeleteTargets(string targetList);
        bool SyncTargets(string networkObjectIdList, string targetsList);
        bool DeleteSyncHistory(string networkObjectIds);
        bool RefreshTargets();
    }
}
