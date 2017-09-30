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
    public class ModifierFlagService : IModifierFlagService, IUserPermissions
    {
        private IRepository _repository;
        private DbContext _context;
        private string _lastActionResult;
        public List<NetworkPermissions> Permissions { get; set; }
        private string auditType = Enum.GetName(typeof(AuditLogType), AuditLogType.ModifierFlag);
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
        public ModifierFlagService()
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
        public List<ModifierFlagModel> GetModifierFlagList(int networkObjectId)
        {
            var modifierFlaglist = new List<ModifierFlagModel>();
            var modifierFlags = _repository.GetQuery<ModifierFlag>(p => p.NetworkObjectId == networkObjectId);
            if (modifierFlags != null && modifierFlags.Any())
            {
                foreach (var modifierFlag in modifierFlags)
                {
                    modifierFlaglist.Add(new ModifierFlagModel
                    {
                        ModifierFlagId = modifierFlag.ModifierFlagId,
                        Name = modifierFlag.Name,
                        Code = modifierFlag.Code,
                        NetworkObjectId = modifierFlag.NetworkObjectId
                    });
                }
            }
            return modifierFlaglist;
        }

        /// <summary>
        /// Saves Special flag information
        /// </summary>
        /// <param name="model"></param>      
        /// <returns></returns>
        public ModifierFlagModel SaveModifierFlag(ModifierFlagModel model, out bool actionStatus)
        {
            //this flag turns true if the action fails
            actionStatus = false;
            try
            {
                var modifierFlag = new ModifierFlag
                {
                    Name = string.IsNullOrWhiteSpace(model.Name) ? string.Empty : model.Name.Trim(),
                    Code = model.Code,
                    NetworkObjectId = model.NetworkObjectId
                };

                if (model.ModifierFlagId != 0)
                {
                    modifierFlag.ModifierFlagId = model.ModifierFlagId;
                    _repository.Update<ModifierFlag>(modifierFlag);
                }
                else
                {
                    _repository.Add<ModifierFlag>(modifierFlag);
                }

                //After the successful Above operation, update the Menu's Last Update Date Information
                if (model.ModifierFlagId != 0)
                {
                    //Check if this flag is linked with any menu
                    var menuItemList = _repository.GetQuery<Item>(p => p.ModifierFlagId == model.ModifierFlagId).Select(p => p.ItemId).Distinct().ToList();
                    if (menuItemList != null && menuItemList.Any())
                    {
                        _commonService.SetLastUpdatedDateofMenusUsingItems(menuItemList);
                    }
                }
                _context.SaveChanges();

                if (model.ModifierFlagId == 0)
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.ModifierFlagCreatedT, model.Name);
                    _auditLogger.Write(OperationPerformed.Created, EntityType.ModifierFlag, entityNameList: model.Name);
                }
                else
                {
                    _lastActionResult = string.Format(Constants.AuditMessage.ModifierFlagUpdatedT, model.Name);
                    _auditLogger.Write(OperationPerformed.Updated, EntityType.ModifierFlag, entityNameList: model.Name);
                }

                model.ModifierFlagId = modifierFlag.ModifierFlagId;
            }
            catch (Exception ex)
            {
                actionStatus = true;
                _lastActionResult = Constants.StatusMessage.ErrModifierFlagSave;
                Logger.WriteError(ex);
            }
            return model;
        }

        /// <summary>
        /// Deletes flags and its associated data
        /// </summary>
        /// <param name="flagList">list of flags(model)</param>
        /// <returns></returns>
        public bool DeleteModifierFlags(string flagList)
        {
            bool hasActionFailed = false;
            try
            {
                //1. Deserialize the Data
                var js = new JavaScriptSerializer();
                var flags = js.Deserialize<List<ModifierFlagModel>>(flagList);
                if (flags != null && flags.Any())
                {
                    var ModifierFlagIdList = flags.Select(p => p.ModifierFlagId);

                    //get the list of existing Item using ModifierFlag     
                    var itemList = _repository.GetQuery<Item>(p => p.ModifierFlagId.HasValue && ModifierFlagIdList.Contains(p.ModifierFlagId.Value)).ToList();
                    var itemIdList = itemList.Select(x => x.ItemId).ToList();

                    itemList.ForEach(p => p.ModifierFlagId = null);
                    // delete flag
                    _repository.Delete<ModifierFlag>(p => ModifierFlagIdList.Contains(p.ModifierFlagId));

                    //After the successful Above operation, update the Menu's Last Update Date Information
                    if (itemIdList != null && itemIdList.Any())
                    {
                        _commonService.SetLastUpdatedDateofMenusUsingItems(itemIdList);
                    }
                    _context.SaveChanges();

                    var networkObjectId = flags.ElementAt(0).NetworkObjectId;
                    var commaSeperatedNames = string.Join(",", flags.Select(t => t.Name).ToList());

                    _lastActionResult = string.Format(Constants.AuditMessage.ModifierFlagDeletedT, commaSeperatedNames);
                    _auditLogger.Write(OperationPerformed.Deleted, EntityType.ModifierFlag, entityNameList: commaSeperatedNames);
                }
            }
            catch (Exception ex)
            {
                // write an error.
                hasActionFailed = true;
                _lastActionResult = Constants.StatusMessage.ErrModifierFlagDelete;
                Logger.WriteError(ex);
            }
            return hasActionFailed;
        }

        /// <summary>
        /// checks if the Name is unique
        /// </summary>
        /// <param name="model"></param>
        /// <param name="isNameNotUnique"></param>
        public void CheckUniquenessOfReqdData(ModifierFlagModel model, out bool isNameNotUnique)
        {
            isNameNotUnique = false;
            var tmpModifierFlag = new ModifierFlagModel();

            var modifierFlagList = GetModifierFlagList(model.NetworkObjectId);

            //Check for uniqueness of flag Name 
            tmpModifierFlag = modifierFlagList.Find(item => item.ModifierFlagId != model.ModifierFlagId && item.Name == model.Name);
            if (tmpModifierFlag != null)
            {
                isNameNotUnique = true;
            }
        }

        /// <summary>
        /// This method is to get the Brand NetworkObjectId corresponding to the networkId provided
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        private int getBrandNetworkObjectId(int networkObjectId)
        {
            var brandNetworkObjectId = _repository.GetQuery<vwNetworkObjectTree>(p => p.Site == networkObjectId || p.Market == networkObjectId || p.Franchise == networkObjectId || p.Brand == networkObjectId || p.Root == networkObjectId).Select(p => p.Brand).FirstOrDefault();
            return brandNetworkObjectId.HasValue ? brandNetworkObjectId.Value : 0;
        }
    }

    public interface IModifierFlagService
    {
        string LastActionResult { get; }
        List<ModifierFlagModel> GetModifierFlagList(int networkObjectId);
        ModifierFlagModel SaveModifierFlag(ModifierFlagModel model, out bool actionStatus);
        bool DeleteModifierFlags(string flagList);
        void CheckUniquenessOfReqdData(ModifierFlagModel model, out bool isNameNotUnique);
    }
}