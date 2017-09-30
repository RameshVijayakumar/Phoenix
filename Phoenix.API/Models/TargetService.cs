using Phoenix.Common;
using Phoenix.DataAccess;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;

namespace Phoenix.API.Models
{
    public class TargetService : ITargetService
    {
        private IRepository _repository;
        private DbContext _context;

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
        /// <summary>
        /// Constructor where DBContext object and Repository object gets initialised.
        /// </summary>
        public TargetService()
        {
            //TODO: inject these interfaces
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
        }

        public HttpStatusCode UpdateStatus(TargetStatusModel status)
        {
            var retCode = HttpStatusCode.OK;
            _lastActionResult = string.Empty;
            try
            {
                if (status != null)
                {
                    Logger.WriteAudit(string.Format("Processing request to update the target status from Menu Sync - {0}.",status.Id));
                    //Get all Sync Details
                    var syncDetails = _repository.GetQuery<MenuSyncTargetDetail>(x => x.SyncIrisId == status.Id).ToList();

                    if (syncDetails.Any())
                    {
                        var distinctSiteIrisId = status.Sites.Select(x => x.Id).Distinct().ToList();

                        //Get NetworkObjects
                        var networkObjects = _repository.GetQuery<NetworkObject>(x => distinctSiteIrisId.Contains(x.IrisId)).ToList();

                        if (networkObjects.Any())
                        {
                            var existingTargetDetail = syncDetails.FirstOrDefault();
                            var sitesIdentified = new HashSet<int>();
                            var sitesNotIdentified = new HashSet<long>();
                            _context.Configuration.AutoDetectChangesEnabled = false;

                            foreach (var siteStatus in status.Sites)
                            {
                                var site = networkObjects.Where(x => x.IrisId == siteStatus.Id).FirstOrDefault();
                                if (site != null)
                                {
                                    sitesIdentified.Add(site.NetworkObjectId);

                                    //update the history of same site and Menu
                                    var siteHistoryDetail = syncDetails.Where(x => x.NetworkObjectId == site.NetworkObjectId && string.IsNullOrWhiteSpace(x.MenuName) == false && x.MenuName.Equals(siteStatus.Menu, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                                    if (siteHistoryDetail != null)
                                    {
                                        siteHistoryDetail.SyncStatus = siteStatus.Status;
                                        siteHistoryDetail.SyncMessage = siteStatus.Message;
                                        siteHistoryDetail.ResponseTime = DateTime.UtcNow;
                                        _repository.Update<MenuSyncTargetDetail>(siteHistoryDetail);
                                    }
                                    else
                                    {
                                        //Add history detail for menu and site
                                        siteHistoryDetail = new MenuSyncTargetDetail
                                        {
                                            SyncIrisId = status.Id,
                                            TargetName = existingTargetDetail.TargetName,
                                            NetworkObjectId = site.NetworkObjectId,
                                            MenuName = siteStatus.Menu,
                                            SyncStatus = siteStatus.Status,
                                            SyncMessage = siteStatus.Message,
                                            RequestedTime = existingTargetDetail.RequestedTime,
                                            ResponseTime = DateTime.UtcNow
                                        };
                                        _repository.Add<MenuSyncTargetDetail>(siteHistoryDetail);
                                    }
                                }
                                else
                                {
                                    sitesNotIdentified.Add(siteStatus.Id);
                                }
                            }

                            if (sitesIdentified.Any())
                            {
                                //Delete Sent record for this Site
                                _repository.Delete<MenuSyncTargetDetail>(x => x.SyncIrisId == status.Id && string.IsNullOrEmpty(x.MenuName) && sitesIdentified.Contains(x.NetworkObjectId));
                                _lastActionResult = "Updated the status.";
                            }

                            if(sitesNotIdentified.Any())
                            {
                                retCode = HttpStatusCode.Accepted;
                                _lastActionResult = string.Format("Unable to identify following site(s) for sync '{1}' - {0} ", string.Join(",", sitesNotIdentified), status.Id);
                                Logger.WriteAudit(_lastActionResult);
                                _lastActionResult = "Unable to identify to few sites, updated the status of remaining sites.";
                            }
                            _context.SaveChanges();
                        }
                        else
                        {
                            retCode = HttpStatusCode.BadRequest;
                            _lastActionResult = "Unable to identify sites.";
                        }
                    }
                    else
                    {
                        retCode = HttpStatusCode.BadRequest;
                        _lastActionResult = "Unable to identify sync.";
                    }
                    Logger.WriteAudit(_lastActionResult);
                }
                else
                {
                    retCode = HttpStatusCode.BadRequest;
                    _lastActionResult = "Not valid status to update.";
                }
            }
            catch (Exception e)
            {
                retCode = HttpStatusCode.InternalServerError;
                _lastActionResult = "Failed to update the Target's status.";

                //log exception
                Logger.WriteError(e);
            }
            return retCode;
        }
    }

    /// <summary>
    /// Interface class that TargetService class implements
    /// </summary>
    public interface ITargetService
    {
        string ClientID { get; set; }
        string LastActionResult { get; }
        HttpStatusCode UpdateStatus(TargetStatusModel status);
    }
}