using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Phoenix.Common;
using Phoenix.DataAccess;
using System.Text;
using Phoenix.RuleEngine;
using System.Web.Script.Serialization;

namespace Phoenix.Web.Models
{
    public class TagService : ITagService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        public List<NetworkPermissions> Permissions { get; set; }
        private string auditType = Enum.GetName(typeof(AuditLogType), AuditLogType.Tag);
        private IAuditLogService _auditLogger;
        private RuleService _ruleService = null;
        private CommonService _commonService = null;
        private Char valueDelimiter = ',';
        private IValidationDictionary _validatonDictionary;

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        /// <summary>
        /// Initialize the validation dictionary
        /// </summary>
        /// <param name="validatonDictionary"></param>
        public void Initialize(IValidationDictionary validatonDictionary)
        {
            _validatonDictionary = validatonDictionary;
        }

        /// <summary>
        /// .ctor
        /// </summary>
        public TagService()
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _ruleService = new RuleService(RuleService.CallType.Web);
            _auditLogger = new AuditLogService();
            _commonService = new CommonService(_repository);
        }

        public TagService(DbContext context, IRepository repo)
        {
            _context = context;
            _repository = repo;
            _ruleService = new RuleService(RuleService.CallType.Web);
            _auditLogger = new AuditLogService();
            _commonService = new CommonService(_repository);
        }

        /// <summary>
        /// returns all the tags for a specific networkObjectId
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public List<TagModel> GetTagList(int? networkObjectId, TagKeys tagKey)
        {
            var taglist = new List<TagModel>();
            if (networkObjectId.HasValue)
            {
                var tagKeyName = Enum.GetName(typeof(TagKeys), tagKey);
                var networkObjectList = new List<int>();
                networkObjectList = _ruleService.GetNetworkParents(networkObjectId.Value);
                var tags = from a in _repository.GetQuery<Tag>(a => networkObjectList.Contains(a.NetworkObjectId))
                           //join up in Permissions on a.NetworkObject.IrisId equals up.IrisId
                           //where up.HasAccess == true && networkObjectList.Contains(a.NetworkObjectId)
                           where a.TagKey.Equals(tagKeyName, StringComparison.InvariantCultureIgnoreCase)
                           orderby a.TagName
                           select a;

                foreach (var tag in tags)
                {
                    taglist.Add(new TagModel
                    {
                        TagId = tag.Id,
                        TagName = tag.TagName,
                        NetworkObjectId = networkObjectId.Value,
                        //This condition needs to be extended to know the presence of other associated data like tag-item data etc. or other tag keys
                        HasAssociatedData = tagKey == TagKeys.Tag ? tag.TagAssetLinks.Count() > 0 : (tag.TargetTagLinks.Count() > 0 || tag.MenuTagLinks.Count() > 0),
                        IsInheritedTag = tag.NetworkObjectId != networkObjectId.Value
                    });
                }
            }
            return taglist;
        }

        public bool CheckTagAtBrandLevel(string tagName, TagKeys tagKey, out int tagId, out string networkObjectName)
        {
            tagId = -1;
            networkObjectName = string.Empty;
            var tagkeyName = tagKey.ToString().ToLower();
            var tag = _repository.GetQuery<Tag>(p => p.TagName == tagName.ToLower() && p.TagKey.Equals(tagkeyName, StringComparison.InvariantCultureIgnoreCase) && p.NetworkObject.NetworkObjectTypeId == NetworkObjectTypes.Brand).Include("NetworkObject").FirstOrDefault();
            if (tag != null)
            {
                tagId = tag.Id;
                networkObjectName = tag.NetworkObject.Name;
                return true;
            }
            return false;
        }

        public string GetTagIdsForEntities(int[] entityIds, string entityType, int netId, TagKeys tagKey)
        {
            string commaSeparatedString = null;
            try
            {
                var tagEntity = TagEntity.Asset;                
                Enum.TryParse(entityType, out tagEntity);

                var tagKeyName = Enum.GetName(typeof(TagKeys), tagKey);

                var tags = new List<Tag>();

                if (entityIds.Any())
                {
                    var tagQuery = _repository.GetQuery<Tag>(t => t.TagKey.Equals(tagKeyName, StringComparison.InvariantCultureIgnoreCase));
                    switch (tagEntity)
                    {
                        case TagEntity.Asset:
                            tags = (from a in tagQuery
                                    join ail in _repository.GetQuery<TagAssetLink>() on a.Id equals ail.TagId
                                    where entityIds.Contains(ail.AssetId)
                                    select a).Distinct().OrderBy(p => p.TagName).ToList();
                            break;
                        case TagEntity.Menu:
                            if (netId != 0)
                            {   //tag of only one Menu entity will be requested at a time
                                tags = _ruleService.GetMenuTagLinkList(entityIds.First(), netId).Select(x => x.SelectedTag).ToList();
                            }
                            break;
                        case TagEntity.Target:
                            tags = (from a in tagQuery
                                    join ttl in _repository.GetQuery<TargetTagLink>() on a.Id equals ttl.TagId
                                    where entityIds.Contains(ttl.TargetId)
                                    select a).Distinct().OrderBy(p => p.TagName).ToList();
                            break;

                    }
                }

                commaSeparatedString = string.Join(",", tags.Select(t => t.Id).ToList());
            }
            catch (Exception ex)
            {
                // write an error.
                Logger.WriteError(ex);
            }
            return commaSeparatedString;
        }

        /// <summary>
        /// This method brings the data in the form of comma seperated strings corresponding to tagId
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetTagAssociatedData(int tagId,int networkobjectId, TagKeys tagKey, out bool actionStatus)
        {
            var tagAssociatedData = new Dictionary<string, string>();
            actionStatus = false;
            try
            {
                #region Get associated Assets
                var parentNetworkIds = _ruleService.GetNetworkParents(networkobjectId);
                switch (tagKey)
                {
                    case TagKeys.Tag:
                        var strAssets = string.Empty;

                        var tagAssetLinks = _repository.GetQuery<TagAssetLink>(m => m.TagId == tagId && parentNetworkIds.Contains(m.Asset.NetworkObjectId)).Include("Asset");

                        if (tagAssetLinks != null)
                        {
                            strAssets = string.Join(", ", tagAssetLinks.Select(t => t.Asset.Filename).ToList());
                        }

                        tagAssociatedData.Add("assets", strAssets);

                        tagAssociatedData.Add("items", string.Empty);
                        break;
                    case TagKeys.Channel:
                        var strTargets = string.Empty;

                        var targetTagLinks = _repository.GetQuery<TargetTagLink>(m => m.TagId == tagId && parentNetworkIds.Contains(m.MenuSyncTarget.NetworkObjectId)).Include("MenuSyncTarget");
                        if (targetTagLinks != null)
                        {
                            strTargets = string.Join(", ", targetTagLinks.Select(t => t.MenuSyncTarget.Name).ToList());
                        }

                        var strMenus = string.Empty;

                        var menuTagLinks = _repository.GetQuery<MenuTagLink>(m => m.TagId == tagId && m.ParentTagId == null && m.OverrideStatus == OverrideStatus.ACTIVE && parentNetworkIds.Contains(m.Menu.NetworkObjectId)).Include("Menu");
                        if (menuTagLinks.Any())
                        {
                            strMenus = string.Join(", ", menuTagLinks.Select(t => t.Menu.InternalName).ToList());
                        }

                        tagAssociatedData.Add("targets", strTargets);

                        tagAssociatedData.Add("menus", strMenus);
                        break;
                }
                #endregion
                _lastActionResult = string.Format(Constants.AuditMessage.GetTagAssociatedData, tagKey.ToString());
                actionStatus = true;
            }
            catch(Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrGetTagAssociatedData, tagKey.ToString());
                Logger.WriteError(ex);
            }
            return tagAssociatedData;
        }


        /// <summary>
        /// This method is used to create tag
        /// Business Logic: tag names should always be in lower cases.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="networkObjectId"></param>
        public bool CreateTag(string tagName, TagKeys tagKey, int networkObjectId)
        {
            var actionStatus = false;
            try
            {
                var operationOnEntity = tagKey == TagKeys.Tag ? EntityType.Tag : EntityType.Channel;
                var tagObj = new Tag();
                //Business Logic: tag names should always be in lower cases.
                tagObj.TagName = tagName.ToLower().Trim();
                tagObj.TagKey = tagKey.ToString().ToLower();
                tagObj.NetworkObjectId = networkObjectId;
                _repository.Add<Tag>(tagObj);

                _context.SaveChanges();
                _lastActionResult = string.Format(Constants.AuditMessage.TagCreatedT, tagName, tagKey.ToString());
                _auditLogger.Write(OperationPerformed.Created, operationOnEntity, entityNameList: tagName);
                actionStatus = true;
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrTagCreate, tagKey.ToString());
                Logger.WriteError(ex);
            }
            return actionStatus;
        }

        /// <summary>
        /// This method is used to update tag
        /// Business Logic: tag names should always be in lower cases.
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="tagName"></param>
        /// <param name="networkObjectId"></param>
        public bool UpdateTag(int tagId, string tagName, TagKeys tagKey, int networkObjectId, bool isTagToMove)
        {
            var actionStatus = false;
            try
            {
                var operationOnEntity = tagKey == TagKeys.Tag ? EntityType.Tag : EntityType.Channel;
                var tagObj = _repository.GetQuery<Tag>(x => x.Id == tagId).FirstOrDefault();

                if (tagObj == null)
                {
                    tagObj = new Tag();
                    tagObj.TagKey = tagKey.ToString().ToLower();
                }

                //Business Logic: tag names should always be in lower cases.
                tagObj.TagName = tagName.ToLower().Trim();
                tagObj.NetworkObjectId = networkObjectId;

                tagObj.Id = tagId;
                _repository.Update<Tag>(tagObj);

                //TODO:After the successful Above operation, update the Menu's Last Update Date Information
                _commonService.SetLastUpdatedDateofMenusUsingTags(new List<int> { tagId }, tagKey);

                _context.SaveChanges();
                if (isTagToMove)
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.TagMovedT, tagName, tagKey.ToString());
                    _auditLogger.Write(OperationPerformed.Other, operationOnEntity, entityNameList: tagName, operationDescription: _lastActionResult);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.TagUpdatedT, tagName, tagKey.ToString());
                    _auditLogger.Write(OperationPerformed.Updated, operationOnEntity, entityNameList: tagName);
                }
                actionStatus = true;
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrTagUpdate, tagKey.ToString());
                Logger.WriteError(ex);
            }
            return actionStatus;
        }

        /// <summary>
        /// Add Tag-Entity Links. Also, if any tagId is of type string then insert it as a new tag and then link with Entity.
        /// 1. Business Logic: tag names should always start with a character and hence will never be just integer
        /// 'tag' which is not int and which is not empty string will be treated as a tagname which is to be inserted 
        /// in the 'Tag' database table.
        /// 2. Business Logic: tag names should always be in lower cases.
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="entityIds"></param>
        /// <param name="entityType"></param>
        /// <param name="networkObjectId"></param>
        public void AddNLinkTagToEntities(string tagName, int[] entityIds, TagEntity entityType, int networkObjectId, TagKeys tagKey)
        {
            try
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    var tagObj = new Tag
                    {
                        TagName = tagName.ToLower().Trim(),
                        TagKey = tagKey.ToString().ToLower(),
                        NetworkObjectId = networkObjectId
                    };

                    if (entityType == TagEntity.Asset)
                    {
                        foreach (var entity in entityIds)
                        {
                            tagObj.TagAssetLinks.Add(new TagAssetLink
                            {
                                AssetId = entity
                            });
                        }
                    }
                    _repository.Add<Tag>(tagObj);

                    //TODO:After the successful Above operation, update the Menu's Last Update Date Information
                    _commonService.SetLastUpdatedDateofMenusUsingAssets(entityIds.ToList());

                    _context.SaveChanges();
                }

                _lastActionResult = string.Format(Constants.AuditMessage.TagSavedNLinkedT, tagName);
                _auditLogger.Write(OperationPerformed.Other, EntityType.Tag, entityNameList: tagName, operationDescription: _lastActionResult);
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = Constants.StatusMessage.ErrTagAddNLink;
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// This method is to delete and Tag-Entity Links        
        /// </summary>
        /// <param name="tagIds"></param>
        /// <param name="entityIds"></param>
        /// <param name="entityType"></param>
        /// <param name="networkObjectId"></param>
        public void LinkTagsToEntities(string tagIdsAdded, string tagIdsRemoved, int[] entityIds, TagEntity entityType, int networkObjectId, TagKeys tagKey)
        {
            bool isDeletedOnly = false;
            List<int> intTagIds = null;
            try
            {
                if (tagKey == 0)
                {
                    tagKey = TagKeys.Tag;
                }

                AttachTagsToEntity(tagIdsAdded, tagIdsRemoved, entityIds, entityType, tagKey, ref isDeletedOnly, ref intTagIds);
                _context.SaveChanges();

                _lastActionResult = isDeletedOnly ? Constants.AuditMessage.TagRemoved : Constants.AuditMessage.TagLinked;
                if (intTagIds != null && intTagIds.Count() > 0)
                {
                    var assetNameList = _repository.GetQuery<Asset>(p => entityIds.Contains(p.AssetId)).Select(p => p.Filename).ToList();
                    var commaSeperatedAssetNames = string.Join(",", assetNameList);
                    _auditLogger.Write(OperationPerformed.Other, EntityType.Tag, entityId: intTagIds, operationDescription: isDeletedOnly ? string.Format(Constants.AuditMessage.TagRemovedT, commaSeperatedAssetNames) : string.Format(Constants.AuditMessage.TagLinkedT, commaSeperatedAssetNames));
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = Constants.StatusMessage.ErrTagLink;
                Logger.WriteError(ex);
            }
        }

        public void AttachTagsToEntity(string tagIdsAdded, string tagIdsRemoved, int[] entityIds, TagEntity entityType, TagKeys tagKey, ref bool isDeletedOnly, ref List<int> tagIds)
        {
            var intTagIds = new List<int>();
            if (!string.IsNullOrEmpty(tagIdsAdded) || !string.IsNullOrEmpty(tagIdsRemoved))
            {
                //Delete Tag-Entity Links
                if (!string.IsNullOrEmpty(tagIdsRemoved))
                {
                    var tagIdsToBeRemoved = tagIdsRemoved.Split(',');
                    var tagIdsToBeRemovedCnt = tagIdsToBeRemoved.Count();
                    if (tagIdsToBeRemovedCnt > 0)
                    {
                        intTagIds = tagIdsToBeRemoved.Select(int.Parse).ToList();
                        switch (entityType)
                        {
                            case TagEntity.Asset:
                                _repository.Delete<TagAssetLink>(w => intTagIds.Contains(w.TagId) && entityIds.Contains(w.AssetId));
                                break;
                            case TagEntity.Target:
                                _repository.Delete<TargetTagLink>(w => intTagIds.Contains(w.TagId) && entityIds.Contains(w.TargetId));
                                break;
                        }

                        isDeletedOnly = true;
                    }
                }

                //Add Tag-Entity Links
                if (!string.IsNullOrEmpty(tagIdsAdded))
                {
                    var tagIdsToBeAdded = tagIdsAdded.Split(',');
                    if (tagIdsToBeAdded.Count() > 0)
                    {
                        foreach (var tagId in tagIdsToBeAdded)
                        {
                            foreach (var entity in entityIds)
                            {


                                switch (entityType)
                                {
                                    case TagEntity.Asset:
                                        _repository.Add<TagAssetLink>(new TagAssetLink
                                        {
                                            AssetId = entity,
                                            TagId = Convert.ToInt32(tagId)
                                        });
                                        break;
                                    case TagEntity.Target:
                                        _repository.Add<TargetTagLink>(new TargetTagLink
                                        {
                                            TargetId = entity,
                                            TagId = Convert.ToInt32(tagId)
                                        });
                                        break;
                                }
                            }
                        }

                        intTagIds = tagIdsToBeAdded.Select(int.Parse).ToList();
                        isDeletedOnly = false;
                    }
                }

                if (entityType == TagEntity.Asset && entityIds.Any())
                {
                    //TODO:After the successful Above operation, update the Menu's Last Update Date Information
                    _commonService.SetLastUpdatedDateofMenusUsingAssets(entityIds.ToList());
                }
            }
            tagIds = intTagIds;
        }

        /// <summary>
        /// Delete tags along its associations
        /// </summary>
        /// <param name="tagIds">list of tag(model)</param>
        /// <returns></returns>
        public void DeleteTags(string tagList, TagKeys tagKey)
        {
            try
            {
                //1. Deserialize the Data
                var js = new JavaScriptSerializer();
                var tagModels = js.Deserialize<List<TagModel>>(tagList);
                if (tagModels != null && tagModels.Any())
                {
                    var operationOnEntity = tagKey == TagKeys.Tag ? EntityType.Tag : EntityType.Channel;
                    var tagIdList = tagModels.Select(p => p.TagId);

                    // delete related TagAssetLink        
                    _repository.Delete<TagAssetLink>(p => tagIdList.Contains(p.TagId));

                    // delete related TagAssetLink        
                    _repository.Delete<TargetTagLink>(p => tagIdList.Contains(p.TagId));

                    // delete related TagAssetLink        
                    _repository.Delete<MenuTagLink>(p => tagIdList.Contains(p.TagId));

                    // delete Tags
                    _repository.Delete<Tag>(p => tagIdList.Contains(p.Id));

                    //TODO:After the successful Above operation, update the Menu's Last Update Date Information
                    _commonService.SetLastUpdatedDateofMenusUsingTags(tagIdList.ToList(), tagKey);

                    _context.SaveChanges();

                    var commaSeperatedTagNames = string.Join(",", tagModels.Select(t => t.TagName).ToList());

                    _lastActionResult = string.Format(Constants.AuditMessage.TagDeletedT, commaSeperatedTagNames,tagKey.ToString());
                    _auditLogger.Write(OperationPerformed.Deleted, operationOnEntity, entityNameList: commaSeperatedTagNames);
                }
            }
            catch (Exception ex)
            {
                // write an error.
                _lastActionResult = string.Format(Constants.StatusMessage.ErrTagDelete, tagKey.ToString());
                Logger.WriteError(ex);
            }
        }

        /// <summary>
        /// Get tags Added and Removed
        /// </summary>
        /// <param name="changedTagsVal"></param>
        /// <param name="initialTagsVal"></param>
        public void GetTagsAddedNRemoved(string changedTagsVal, string initialTagsVal, out string tagsAdded, out string tagsRemoved)
        {
            tagsAdded = string.Empty;
            tagsRemoved = string.Empty;

            //Compare with the initial tag list and determine n hence preserve what changes have been done
            if (changedTagsVal == "" && initialTagsVal == "")
            {
                tagsAdded = "";
                tagsRemoved = "";
                return;
            }
            else if (initialTagsVal == "")
            {
                tagsAdded = valueDelimiter + changedTagsVal + valueDelimiter;
                tagsRemoved = "";
            }
            else if (changedTagsVal == "")
            {
                tagsAdded = "";
                tagsRemoved = valueDelimiter + initialTagsVal + valueDelimiter;
            }
            else
            {
                //prepend with valueDelimiter so that on string compare can done with valueDelimiter+tag+valueDelimiter name so that completetagname is compared
                changedTagsVal = valueDelimiter + changedTagsVal + valueDelimiter;
                var initialTagList = initialTagsVal.Split(valueDelimiter);
                var initialTagListLength = initialTagList.Count();
                initialTagsVal = valueDelimiter + initialTagsVal + valueDelimiter;

                if (initialTagListLength > 0)
                {
                    for (var loopCntr = 0; loopCntr < initialTagListLength; loopCntr++)
                    {
                        if (changedTagsVal.IndexOf(valueDelimiter + initialTagList[loopCntr] + valueDelimiter) != -1)
                        {
                            initialTagsVal = initialTagsVal.Replace(valueDelimiter + initialTagList[loopCntr], "");
                            changedTagsVal = changedTagsVal.Replace(valueDelimiter + initialTagList[loopCntr], "");
                        }
                    }
                    tagsAdded = changedTagsVal;
                    tagsRemoved = initialTagsVal;
                }
            }

            tagsAdded = string.IsNullOrEmpty(tagsAdded) ? tagsAdded : tagsAdded.Remove(0, 1);
            if(tagsAdded.EndsWith(valueDelimiter.ToString()))
            {
                tagsAdded = tagsAdded.Remove(tagsAdded.LastIndexOf(','),1);
            }
            tagsRemoved = string.IsNullOrEmpty(tagsRemoved) ? tagsRemoved : tagsRemoved.Remove(0, 1);
            if (tagsRemoved.EndsWith(valueDelimiter.ToString()))
            {
                tagsRemoved = tagsRemoved.Remove(tagsRemoved.LastIndexOf(','), 1);
            }
        }


        public TagModel SaveTag(TagModel model, TagKeys tagKey)
        {
            var actionStatus = false;
            if (CheckIsNameNotUnique(model, tagKey))
            {
                _validatonDictionary.AddError("TagName", "Name must be unique");
            }
            else
            {
                if (model.TagId == 0)
                {
                    actionStatus = CreateTag(model.TagName, tagKey, model.NetworkObjectId);
                }
                else
                {
                    actionStatus = UpdateTag(model.TagId, model.TagName, tagKey, model.NetworkObjectId, false);
                }

                if (actionStatus == false)
                {
                    _validatonDictionary.AddError("TagName", _lastActionResult);
                }
            }
            return model;
        }

        public bool CheckIsNameNotUnique(TagModel model, TagKeys tagKey)
        {
            var isNameNotUnique = false;
            var tmpModifierFlag = new TagModel();
            model.TagName = model.TagName.Trim();
            var tagList = GetTagList(model.NetworkObjectId, tagKey);

            //Check for uniqueness of flag Name 
            tmpModifierFlag = tagList.Find(item => item.TagId != model.TagId && item.TagName == model.TagName);
            if (tmpModifierFlag != null)
            {
                isNameNotUnique = true;
            }
            return isNameNotUnique;
        }
    }

    public interface ITagService
    {
        string LastActionResult { get; }
        List<TagModel> GetTagList(int? networkObjectId, TagKeys tagKey);
        string GetTagIdsForEntities(int[] entityIds, string entityType, int netId, TagKeys tagKey);
        IDictionary<string, string> GetTagAssociatedData(int tagId, int networkobjectId, TagKeys tagKey, out bool actionStatus);
        bool CreateTag(string tagName, TagKeys tagKey, int networkObjectId);
        bool UpdateTag(int tagId, string tagName, TagKeys tagKey, int networkObjectId, bool isTagToMove);
        void AddNLinkTagToEntities(string tagName, int[] entityIds, TagEntity entityType, int networkObjectId, TagKeys tagKey);
        void LinkTagsToEntities(string tagIdsAdded, string tagIdsRemoved, int[] entityIds, TagEntity entityType, int networkObjectId, TagKeys tagKey);
        void GetTagsAddedNRemoved(string changedTagsVal, string initialTagsVal, out string tagsAdded, out string tagsRemoved);
        void DeleteTags(string tagList, TagKeys tagKey); 
        void AttachTagsToEntity(string tagIdsAdded, string tagIdsRemoved, int[] entityIds, TagEntity entityType, TagKeys tagKey, ref bool isDeletedOnly, ref List<int> tagIds);
    }
}