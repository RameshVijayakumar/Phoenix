using Phoenix.Common;
using Phoenix.DataAccess;
using Phoenix.RuleEngine;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;

namespace Phoenix.Web.Models
{
    public class SpecialNoticeService : ISpecialNoticeService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        public List<NetworkPermissions> Permissions { get; set; }
        private string auditType = Enum.GetName(typeof(AuditLogType), AuditLogType.SpecialNotice);
        private IAuditLogService _auditLogger;
        private ICommonService _commonService;
        private RuleService _ruleService;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        /// <summary>
        /// .ctor
        /// </summary>
        public SpecialNoticeService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _auditLogger = new AuditLogService();
            _ruleService = new RuleService(RuleService.CallType.Web);
            _commonService = new CommonService(_repository);
        }

        /// <summary>
        /// gets all the special notes
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public List<SpecialNoticeModel> GetSpecialNoticeList(int networkObjectId)
        {
            var specialNoticelist = new List<SpecialNoticeModel>();
            var specialNotices = _repository.GetQuery<SpecialNotice>(p => p.NetworkObjectId == networkObjectId).Include("SpecialNoticeMenuLinks").OrderBy(p => p.NoticeName);
            if (specialNotices != null && specialNotices.Any())
            {
                foreach (var specialNotice in specialNotices)
                {
                    specialNoticelist.Add(new SpecialNoticeModel
                    {
                        NoticeId = specialNotice.NoticeId,
                        NoticeName = specialNotice.NoticeName,
                        NoticeText = specialNotice.NoticeText,
                        NetworkObjectId = specialNotice.NetworkObjectId,
                        LastUpdated = specialNotice.LastUpdatedDate.HasValue ? specialNotice.LastUpdatedDate.Value.ToString() : string.Empty,
                        DefaultIncludeInMenu = specialNotice.DefaultIncludeInMenu,
                        IsLinkedToMenu = (specialNotice.SpecialNoticeMenuLinks != null && specialNotice.SpecialNoticeMenuLinks.Any()) || specialNotice.DefaultIncludeInMenu
                    });
                }
            }
            return specialNoticelist;
        }

        /// <summary>
        /// gets menu related list of special notices
        /// </summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        public List<SpecialNoticeModel> GetMenuRelatedSpecialNoticeList(int menuId, int networkObjectId)
        {
            var specialNoticelist = new List<SpecialNoticeModel>();

            var networkParents = _ruleService.GetNetworkParents(networkObjectId);
            var specialNoticeMenuLinks = (from menuNetworkObjLink in _repository.GetQuery<MenuNetworkObjectLink>(p => p.MenuId == menuId && networkParents.Contains(p.NetworkObjectId))
                                          join networkObject in _repository.GetAll<NetworkObject>() on menuNetworkObjLink.NetworkObjectId equals networkObject.NetworkObjectId
                                          join specialNoticeMenuLink in _repository.GetAll<SpecialNoticeMenuLink>() on menuNetworkObjLink.MenuNetworkObjectLinkId equals specialNoticeMenuLink.MenuNetworkObjectLinkId
                                          group new { menuNetworkObjLink, networkObject, specialNoticeMenuLink } by specialNoticeMenuLink.NoticeId into grouped
                                          select new { values = grouped.OrderByDescending(p => p.networkObject.NetworkObjectTypeId).FirstOrDefault() }).ToList();

            // get the Brand NetworkObjectId corresponding to the networkId provided
            var networkObjId = _ruleService.GetBrandNetworkObjectId(networkObjectId);

            //Get all Notices and mark IsLinkedToMenu = true if there is an existence of 'this' NoticeId in the list: specialNoticeIdlist
            var specialNotices = _repository.GetQuery<SpecialNotice>(p => p.NetworkObjectId == networkObjId);
            if (specialNotices != null && specialNotices.Any())
            {
                foreach (var specialNotice in specialNotices)
                {
                    var currentSpecialNoticeMenuLink = (specialNoticeMenuLinks != null && specialNoticeMenuLinks.Any()) ? specialNoticeMenuLinks.Where(p => p.values.specialNoticeMenuLink.NoticeId == specialNotice.NoticeId).Select(p => p.values.specialNoticeMenuLink).FirstOrDefault() : null;
                    specialNoticelist.Add(new SpecialNoticeModel
                    {
                        NoticeId = specialNotice.NoticeId,
                        NoticeName = specialNotice.NoticeName,
                        NoticeText = specialNotice.NoticeText,
                        LastUpdated = specialNotice.LastUpdatedDate.HasValue ? specialNotice.LastUpdatedDate.Value.ToString() : string.Empty,
                        IsLinkedToMenu = (currentSpecialNoticeMenuLink != null ) ? currentSpecialNoticeMenuLink.IsLinked : specialNotice.DefaultIncludeInMenu
                    });
                }
            }

            return specialNoticelist;
        }

        /// <summary>
        /// Saves Special Notice information
        /// </summary>
        /// <param name="model"></param>      
        /// <returns></returns>
        public SpecialNoticeModel SaveSpecialNotice(SpecialNoticeModel model, out bool actionStatus)
        {
            //this flag turns true if the action fails
            actionStatus = false;
            try
            {
                var specialNotice = new SpecialNotice
                {
                    NoticeName = string.IsNullOrWhiteSpace(model.NoticeName)? string.Empty : model.NoticeName.Trim(),
                    NoticeText = string.IsNullOrWhiteSpace(model.NoticeText) ? string.Empty : model.NoticeText.Trim(),
                    NetworkObjectId = model.NetworkObjectId,
                    DefaultIncludeInMenu = model.DefaultIncludeInMenu,
                    LastUpdatedDate = DateTime.UtcNow
                };

                if (model.NoticeId != 0)
                {
                    specialNotice.NoticeId = model.NoticeId;
                    _repository.Update<SpecialNotice>(specialNotice);
                }
                else
                {
                    _repository.Add<SpecialNotice>(specialNotice);
                }

                //After the successful Above operation, update the Menu's Last Update Date Information
                if (model.NoticeId != 0)
                {
                    //Check if this notice is linked with any menu
                    var menuNetworkObjectLinkIdList = _repository.GetQuery<SpecialNoticeMenuLink>(p => p.NoticeId == model.NoticeId).Select(p => p.MenuNetworkObjectLinkId).Distinct();
                    if (menuNetworkObjectLinkIdList != null && menuNetworkObjectLinkIdList.Any())
                    {
                        //For Each MenuNetworkObjectLinkId found to be linked with this NoticeId, update MenuNetworkObjectLink
                        foreach (var menuNetworkObjectLinkId in menuNetworkObjectLinkIdList)
                        {
                            saveMenuLastUpdatedDate(menuNetworkObjectLinkId: menuNetworkObjectLinkId);
                        }
                    }
                }

                _context.SaveChanges();

                if (model.NoticeId == 0)
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.SpecialNoticeCreatedT, model.NoticeName);
                    _auditLogger.Write(OperationPerformed.Created, EntityType.SpecialNotice, entityNameList: model.NoticeName);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.SpecialNoticeUpdatedT, model.NoticeName);
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.SpecialNotice, entityNameList: model.NoticeName);
                }

                model.NoticeId = specialNotice.NoticeId;
            }
            catch (Exception ex)
            {
                actionStatus = true;
                _lastActionResult = Constants.StatusMessage.ErrSpecialNoticeSave;
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        /// save the link information of menu and special notices
        /// </summary>
        /// <param name="noticeList"></param>
        /// <param name="menuId"></param>
        /// <param name="menuName"></param>
        /// <param name="networkObjectId"></param>
        public bool SaveSpecialNoticeMenuLink(string noticesToAddLink, string noticesToRemoveLink, int menuId, string menuName, int networkObjectId)
        {
            bool hasActionFailed = false;
            bool isMenuNetObjLinkAlreadyPresent = false;
            var noticeNamesToLink = string.Empty;
            var namesToRemoveLink = string.Empty;
            try
            {
                //Get/Add menuNetworkObjectLink corresponding to this menuId and this networkObjectId
                var menuNetworkObjectLink = _repository.FindOne<MenuNetworkObjectLink>(p => p.MenuId == menuId && p.NetworkObjectId == networkObjectId);
                if (menuNetworkObjectLink == null)
                {
                    if (string.IsNullOrEmpty(noticesToAddLink) && string.IsNullOrEmpty(noticesToRemoveLink))
                    {
                        //The menu was never edited before and even now, no special Notices have actually linked. 

                        //This is the case when user clicks the save button without selecting any special Notice to link 
                        //and this is the first action ever to edit this menu. And hence the action is being ignored.                       
                    }
                    else
                    {
                        //create a MenuNetworkObjectLink
                        menuNetworkObjectLink = new MenuNetworkObjectLink
                        {
                            NetworkObjectId = networkObjectId,
                            MenuId = menuId,
                            LastUpdatedDate = DateTime.UtcNow,
                            IsPOSMapped = false
                        };
                        _repository.Add<MenuNetworkObjectLink>(menuNetworkObjectLink);
                    }
                }
                else
                {
                    isMenuNetObjLinkAlreadyPresent = true;
                }

                if (!string.IsNullOrEmpty(noticesToAddLink) || !string.IsNullOrEmpty(noticesToRemoveLink))
                {
                    // Deserialize the Data
                    var js = new JavaScriptSerializer();
                    var noticeListToAddLink = js.Deserialize<List<SpecialNoticeModel>>(noticesToAddLink);
                    var noticeListToRemoveLink = js.Deserialize<List<SpecialNoticeModel>>(noticesToRemoveLink);

                    //notices To Add Links 
                    manageNoticeLink(menuNetworkObjectLink, noticeListToAddLink, true, out  noticeNamesToLink);

                    //notices To Remove Links                                  
                    manageNoticeLink(menuNetworkObjectLink, noticeListToRemoveLink, false, out  namesToRemoveLink);
                }

                //After the successful above operation, update the Menu's Last Update Date Information
                if (isMenuNetObjLinkAlreadyPresent)
                {
                    //update MenuNetworkObjectLink                  
                    saveMenuLastUpdatedDate(menuNetworkObjectLink: menuNetworkObjectLink);
                }
                _context.SaveChanges();

                _lastActionResult = Constants.AuditMessage.SpecialNoticeLinked;
                if (!string.IsNullOrEmpty(noticeNamesToLink) && !string.IsNullOrEmpty(namesToRemoveLink))
                {
                    _lastActionResult = Constants.AuditMessage.SpecialNoticeLinkedNRemoved;
                    _auditLogger.Write(OperationPerformed.Other, EntityType.SpecialNotice, entityNameList: noticeNamesToLink, operationDescription: string.Format(Constants.AuditMessage.SpecialNoticeLinkNRemoveT, menuName, noticeNamesToLink, namesToRemoveLink));
                }
                else if (!string.IsNullOrEmpty(noticeNamesToLink))
                {
                    _lastActionResult = Constants.AuditMessage.SpecialNoticeLinked;
                    _auditLogger.Write(OperationPerformed.Other, EntityType.SpecialNotice, entityNameList: noticeNamesToLink, operationDescription: string.Format(Constants.AuditMessage.SpecialNoticeLinkedT, menuName, noticeNamesToLink));
                }
                else if (!string.IsNullOrEmpty(namesToRemoveLink))
                {
                    _lastActionResult = Constants.AuditMessage.SpecialNoticeRemoved;
                    _auditLogger.Write(OperationPerformed.Other, EntityType.SpecialNotice, entityNameList: noticeNamesToLink, operationDescription: string.Format(Constants.AuditMessage.SpecialNoticeLinkRemovedT, menuName, namesToRemoveLink));
                }
            }
            catch (Exception ex)
            {
                // write an error.
                hasActionFailed = true;
                _lastActionResult = Constants.StatusMessage.ErrSpecialNoticeLinked;
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        private void manageNoticeLink(MenuNetworkObjectLink menuNetworkObjectLink, List<SpecialNoticeModel> noticeListToLink, bool ToAdd, out string noticeNamesToLink)
        {
            noticeNamesToLink = string.Empty;

            //notices To Add Links                                    
            if (noticeListToLink != null && noticeListToLink.Any())
            {
                List<int> existingSpecialNoticeIdList = null;
                var noticeIdList = noticeListToLink.Select(p => p.NoticeId);

                var existingSpecialNoticeMenuLinks = _repository.GetQuery<SpecialNoticeMenuLink>(p => noticeIdList.Contains(p.NoticeId) && p.MenuNetworkObjectLinkId == menuNetworkObjectLink.MenuNetworkObjectLinkId);
                if (existingSpecialNoticeMenuLinks != null && existingSpecialNoticeMenuLinks.Any())
                {
                    existingSpecialNoticeIdList = new List<int>();
                    foreach (var existingSpecialNoticeMenuLink in existingSpecialNoticeMenuLinks)
                    {
                        existingSpecialNoticeMenuLink.IsLinked = ToAdd;
                        existingSpecialNoticeIdList.Add(existingSpecialNoticeMenuLink.NoticeId);
                    }
                }

                noticeIdList = existingSpecialNoticeIdList == null ? noticeIdList : noticeIdList.Except(existingSpecialNoticeIdList);
                foreach (var noticeToAddLink in noticeIdList)
                {
                    _repository.Add<SpecialNoticeMenuLink>(new SpecialNoticeMenuLink
                    {
                        NoticeId = Convert.ToInt32(noticeToAddLink),
                        MenuNetworkObjectLinkId = menuNetworkObjectLink.MenuNetworkObjectLinkId,
                        IsLinked = ToAdd
                    });
                }

                //Build a comma-seperated Notice Names for the audit logging
                noticeNamesToLink = string.Join(",", noticeListToLink.Select(t => t.NoticeName).ToList());
            }
        }

        /// <summary>
        /// Deletes notices and its associated data
        /// </summary>
        /// <param name="noticeList">list of notices(model)</param>
        /// <returns></returns>
        public bool DeleteSpecialNotices(string noticeList)
        {
            bool hasActionFailed = false;
            try
            {
                //1. Deserialize the Data
                var js = new JavaScriptSerializer();
                var notices = js.Deserialize<List<SpecialNoticeModel>>(noticeList);
                if (notices != null && notices.Any())
                {
                    var noticeIdList = notices.Select(p => p.NoticeId);

                    //get the list of existing menuNetworkObjectLinkId in  SpecialNoticeMenuLink        
                    var menuNetworkObjectLinkIdList = _repository.GetQuery<SpecialNoticeMenuLink>(p => noticeIdList.Contains(p.NoticeId)).Select(p => p.MenuNetworkObjectLinkId).Distinct();

                    // delete related SpecialNoticeMenuLink        
                    _repository.Delete<SpecialNoticeMenuLink>(p => noticeIdList.Contains(p.NoticeId));

                    // delete Notice
                    _repository.Delete<SpecialNotice>(p => noticeIdList.Contains(p.NoticeId));

                    //After the successful Above operation, update the Menu's Last Update Date Information
                    if (menuNetworkObjectLinkIdList != null && menuNetworkObjectLinkIdList.Any())
                    {
                        foreach (var menuNetworkObjectLinkId in menuNetworkObjectLinkIdList)
                        {
                            //update MenuNetworkObjectLink                  
                            saveMenuLastUpdatedDate(menuNetworkObjectLinkId: menuNetworkObjectLinkId);
                        }
                    }
                    _context.SaveChanges();

                    var networkObjectId = notices.ElementAt(0).NetworkObjectId;
                    var commaSeperatedNoticeNames = string.Join(",", notices.Select(t => t.NoticeName).ToList());

                    _lastActionResult = string.Format(Constants.AuditMessage.SpecialNoticeDeletedT, commaSeperatedNoticeNames);
                    _auditLogger.Write(OperationPerformed.Deleted, EntityType.SpecialNotice, entityNameList: commaSeperatedNoticeNames);
                }
            }
            catch (Exception ex)
            {
                // write an error.
                hasActionFailed = true;
                _lastActionResult = Constants.StatusMessage.ErrSpecialNoticeDelete;
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        /// <summary>
        /// checks if the NoticeName is unique
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isNoticeNameNotUnique"></param>
        public void CheckUniquenessOfReqdData(SpecialNoticeModel model, out bool isNoticeNameNotUnique)
        {
            isNoticeNameNotUnique = false;
            var tmpSpecialNotice = new SpecialNoticeModel();

            var specialNoticeList = GetSpecialNoticeList(model.NetworkObjectId);

            //Check for uniqueness of Notice Name 
            tmpSpecialNotice = specialNoticeList.Find(item => item.NoticeId != model.NoticeId && item.NoticeName == model.NoticeName);
            if (tmpSpecialNotice != null)
            {
                isNoticeNameNotUnique = true;
            }
        }

        /// <summary>
        ///Saves the Last Updated Date of Menu
        /// </summary>
        /// <param name="menuNetworkObjectLinkId"></param>
        /// <param name="menuNetworkObjectLink"></param>
        private void saveMenuLastUpdatedDate(int? menuNetworkObjectLinkId = null, MenuNetworkObjectLink menuNetworkObjectLink = null)
        {
            var menuNetworkObjectLinkObj = menuNetworkObjectLink ?? _repository.FindOne<MenuNetworkObjectLink>(p => p.MenuNetworkObjectLinkId == menuNetworkObjectLinkId.Value);
            menuNetworkObjectLinkObj.LastUpdatedDate = DateTime.UtcNow;
            _repository.Update<MenuNetworkObjectLink>(menuNetworkObjectLinkObj);
        }
    }

    public interface ISpecialNoticeService
    {
        string LastActionResult { get; }
        List<SpecialNoticeModel> GetSpecialNoticeList(int networkObjectId);
        List<SpecialNoticeModel> GetMenuRelatedSpecialNoticeList(int menuId, int networkObjectId);
        SpecialNoticeModel SaveSpecialNotice(SpecialNoticeModel model, out bool actionStatus);
        bool SaveSpecialNoticeMenuLink(string noticesToAddLink, string noticesToRemoveLink, int menuId, string menuName, int networkObjectId);
        bool DeleteSpecialNotices(string noticeList);
        void CheckUniquenessOfReqdData(SpecialNoticeModel model, out bool isNoticeNameNotUnique);
    }
}
