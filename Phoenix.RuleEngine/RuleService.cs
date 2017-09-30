using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phoenix.Common;
using Phoenix.DataAccess;

namespace Phoenix.RuleEngine
{
    /// <summary>
    /// Generic Class to store overrides dictonary values
    /// </summary>
    /// <typeparam name="T">Any Class</typeparam>
    public class ovr<T>
    {
        public OverrideStatus OverrideStatus { get; set; }
        public int Id { get; set; }
        public NetworkObjectTypes NetworkTypeId { get; set; }
        public T Value { get; set; }
    }

    public class RuleService
    {
        private IRepository _repository;
        private DbContext _context;
        public NetworkObjectTypes currNetworkType = NetworkObjectTypes.Site;

        public enum CallType
        {
            Web = 1,
            API = 2
        }

        public CallType _callType;

        /// <summary>
        /// .ctor
        /// </summary>
        public RuleService(CallType callType)
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _callType = callType;
        }

        /// <summary>
        /// .ctor
        /// </summary>
        public RuleService(CallType callType, int networkObjectId)
        {
            _context = new ProductMasterContext();
            _repository = new GenericRepository(_context);
            _callType = callType;
            this.NetworkObjectId = networkObjectId;
        }

        /// <summary>
        /// .ctor
        /// </summary>
        public RuleService(CallType callType, DbContext context, IRepository repo)
        {
            _context = context;
            _repository = repo;
            _callType = callType;
        }

        private int _networkObjectId;
        public int NetworkObjectId
        {
            get
            {
                return _networkObjectId;
            }
            set
            {
                _networkObjectId = value;
                if (MultipleNetworkObjects)
                {
                    _parentNetworkNodesData = null;
                }
                else
                {
                    clearAllPreservedTablesData();
                }
            }
        }

        private int _menuId;
        public int MenuId
        {
            get
            {
                return _menuId;
            }
            set
            {
                _menuId = value;
                clearAllPreservedTablesData();
            }
        }

        private int _categoryId;
        private int _itemId;
        private int _collectionId;
        private int _scheduleId;

        public bool MultipleNetworkObjects { get; set; }

        #region Preserve Fetched Data

        //NOTE: Any new link table being added should be cleared in clearAllPreservedTablesData.
        private IList<vwNetworkObjectTree> _vwNetworkObjectTreeData = null;
        public IList<vwNetworkObjectTree> _vwNetworkObjectTree
        {
            get
            {
                if (_vwNetworkObjectTreeData == null)
                {
                    _vwNetworkObjectTreeData = _repository.GetAll<vwNetworkObjectTree>().ToList();
                }
                return _vwNetworkObjectTreeData;
            }
        }

        private Menu _menuData = null;
        public Menu Menu
        {
            get
            {
                if (_menuData == null)
                {
                    _menuData = _repository.GetQuery<Menu>(x => x.MenuId == _menuId).FirstOrDefault();
                }
                return _menuData;
            }
        }

        private List<int> _parentNetworkNodesData = null;
        public List<int> ParentNetworkNodesList
        {
            get
            {
                if (_parentNetworkNodesData == null)
                {
                    _parentNetworkNodesData = this.GetNetworkParents(NetworkObjectId);
                }
                return _parentNetworkNodesData;
            }
        }

        private List<int> _parentMenuNetworkNodesData = null;
        public List<int> ParentMenuNetworkNodesList
        {
            get
            {
                if (_parentMenuNetworkNodesData == null)
                {
                    _parentMenuNetworkNodesData = this.GetMenuNetworkLinkIds(ParentNetworkNodesList,MenuId);
                }
                return _parentMenuNetworkNodesData;
            }
        }

        private IList<NetworkObject> _networkObjectData = null;
        private IList<NetworkObject> _networkObjectTable
        {
            get
            {
                if (_networkObjectData == null)
                {
                    _networkObjectData = _repository.GetQuery<NetworkObject>().ToList();
                }
                return _networkObjectData;
            }
        }

        private IList<MenuNetworkObjectLink> _menuNetworkObjectLinkData = null;
        private IList<MenuNetworkObjectLink> _menuNetworkObjectLinkTable
        {
            get
            {
                if (_menuNetworkObjectLinkData == null)
                {
                    _menuNetworkObjectLinkData = _repository.GetQuery<MenuNetworkObjectLink>(m => MultipleNetworkObjects || ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("Menu").ToList();
                }
                return _menuNetworkObjectLinkData;
            }
        }

        private IList<MenuTagLink> _menuTagLinkData = null;
        private IList<MenuTagLink> _menuTagLinkTable
        {
            get
            {
                if (_menuTagLinkData == null)
                {
                    _menuTagLinkData = _repository.GetQuery<MenuTagLink>(m => MultipleNetworkObjects || ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("SelectedTag").ToList();
                }
                return _menuTagLinkData;
            }
        }

        private IList<CategoryMenuLink> _categoryMenuLinkData = null;
        private IList<CategoryMenuLink> _categoryMenuLinkTable
        {
            get
            {
                if (_categoryMenuLinkData == null)
                {
                   var _categoryMenuLinkDataQ = _repository.GetQuery<CategoryMenuLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("Category").ToList();
                    if(_menuId != 0)
                    {
                        _categoryMenuLinkDataQ = _categoryMenuLinkDataQ.Where(x => x.MenuId == _menuId).ToList();
                    }
                    _categoryMenuLinkData = _categoryMenuLinkDataQ.ToList();
                }
                return _categoryMenuLinkData;
            }
        }

        private IList<CategoryObject> _categoryObjectData = null;
        private IList<CategoryObject> _categoryObjectTable
        {
            get
            {
                if (_categoryObjectData == null)
                {
                    var _categoryObjectDataQ = _repository.GetQuery<CategoryObject>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("Item").Include("Item.ItemDescriptions").Include("Item.ModifierFlag").Include("Item.MasterItem.ItemDescriptions");
                    //if(_callType == CallType.API)
                    //{
                    //    _categoryObjectDataQ = _categoryObjectDataQ.Include("Item.ItemPOSDataLinks").Include("Item.ItemPOSDataLinks.POSData");
                    //}
                    if (_categoryId != 0)
                    {
                        _categoryObjectDataQ = _categoryObjectDataQ.Where(x => x.CategoryId == _categoryId);
                    }
                    else if (_menuId != 0)
                    {
                        _categoryObjectDataQ = _categoryObjectDataQ.Where(x => x.Category.MenuId == _menuId).Include("Category");
                    }
                    _categoryObjectData = _categoryObjectDataQ.ToList();
                }
                return _categoryObjectData;
            }
        }

        private IList<SubCategoryLink> _subCategoryLinkData = null;
        private IList<SubCategoryLink> _subCategoryLinkTable
        {
            get
            {
                if (_subCategoryLinkData == null)
                {
                    var _subCategoryLinkDataQ = _repository.GetQuery<SubCategoryLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("SubCategory");
                    if (_categoryId != 0)
                    {
                        _subCategoryLinkDataQ = _subCategoryLinkDataQ.Where(x => x.CategoryId == _categoryId);
                    }
                    else if (_menuId != 0)
                    {
                        _subCategoryLinkDataQ = _subCategoryLinkDataQ.Where(x => x.SubCategory.MenuId == _menuId);
                    }
                    _subCategoryLinkData = _subCategoryLinkDataQ.ToList();
                }
                return _subCategoryLinkData;
            }
        }

        private IList<ItemCollectionLink> _itemCollectionLinkData = null;
        private IList<ItemCollectionLink> _itemCollectionLinkTable
        {
            get
            {
                if (_itemCollectionLinkData == null)
                {
                    var _itemCollectionLinkDataQ = _repository.GetQuery<ItemCollectionLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("ItemCollection");
                    if (_itemId != 0)
                    {
                        _itemCollectionLinkDataQ = _itemCollectionLinkDataQ.Where(x => x.ItemId == _itemId);
                    }
                    else if (_menuId != 0)
                    {
                        _itemCollectionLinkDataQ = _itemCollectionLinkDataQ.Where(x => x.ItemCollection.MenuId == _menuId);
                    }
                    _itemCollectionLinkData = _itemCollectionLinkDataQ.ToList();
                }
                return _itemCollectionLinkData;
            }
        }

        private IList<ItemCollectionObject> _itemCollectionObjectData = null;
        private IList<ItemCollectionObject> _itemCollectionObjectTable
        {
            get
            {
                if (_itemCollectionObjectData == null)
                {
                    var _itemCollectionObjectDataQ = _repository.GetQuery<ItemCollectionObject>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("Item").Include("Item.ItemDescriptions").Include("Item.ModifierFlag").Include("Item.MasterItem.ItemDescriptions");
                    //if(_callType == CallType.API)
                    //{
                    //    _itemCollectionObjectDataQ = _itemCollectionObjectDataQ.Include("Item.ItemPOSDataLinks").Include("Item.ItemPOSDataLinks.POSData");
                    //}
                    if (_collectionId != 0)
                    {
                        _itemCollectionObjectDataQ = _itemCollectionObjectDataQ.Where(x => x.CollectionId == _collectionId);
                    }
                    else if (_menuId != 0)
                    {
                        _itemCollectionObjectDataQ = _itemCollectionObjectDataQ.Where(x => x.ItemCollection.MenuId == _menuId).Include("ItemCollection");
                    }
                    _itemCollectionObjectData = _itemCollectionObjectDataQ.ToList();
                }
                return _itemCollectionObjectData;
            }
        }

        private IList<PrependItemLink> _prependItemLinkData = null;
        private IList<PrependItemLink> _prependItemLinkTable
        {
            get
            {
                if (_prependItemLinkData == null)
                {
                    var _prependItemLinkDataQ = _repository.GetQuery<PrependItemLink>(m => ParentMenuNetworkNodesList.Contains(m.MenuNetworkObjectLinkId)).Include("MenuNetworkObjectLink").Include("MenuNetworkObjectLink.NetworkObject").Include("PrependItem").Include("PrependItem.ItemDescriptions").Include("PrependItem.ModifierFlag").Include("PrependItem.MasterItem.ItemDescriptions");
                    //if(_callType == CallType.API)
                    //{
                    //    _prependItemLinkDataQ = _prependItemLinkDataQ.Include("PrependItem.ItemPOSDataLinks").Include("PrependItem.ItemPOSDataLinks.POSData");
                    //}
                    if (_itemId != 0)
                    {
                        _prependItemLinkDataQ = _prependItemLinkDataQ.Where(x => x.ItemId == _itemId);
                    }
                    if (_menuId != 0)
                    {
                        _prependItemLinkDataQ = _prependItemLinkDataQ.Where(x => x.MenuNetworkObjectLink.MenuId == _menuId);
                    }
                    _prependItemLinkData = _prependItemLinkDataQ.ToList();
                }
                return _prependItemLinkData;
            }
        }

        private IList<MenuCategoryScheduleLink> _categoryScheduleLinkData = null;
        private IList<MenuCategoryScheduleLink> _categoryScheduleLinkTable
        {
            get
            {
                if (_categoryScheduleLinkData == null)
                {
                    var _categoryScheduleLinkDataQ = _repository.GetQuery<MenuCategoryScheduleLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("MenuCategoryCycleInSchedules");
                    if (_categoryId != 0)
                    {
                        _categoryScheduleLinkDataQ = _categoryScheduleLinkDataQ.Where(x => x.CategoryId == _categoryId);
                    }
                    _categoryScheduleLinkData = _categoryScheduleLinkDataQ.ToList();
                }
                return _categoryScheduleLinkData;
            }
        }

        private IList<MenuItemScheduleLink> _itemScheduleLinkData = null;
        private IList<MenuItemScheduleLink> _itemScheduleLinkTable
        {
            get
            {
                if (_itemScheduleLinkData == null)
                {
                    var _itemScheduleLinkDataQ = _repository.GetQuery<MenuItemScheduleLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("MenuItemCycleInSchedules");
                    if (_itemId != 0)
                    {
                        _itemScheduleLinkDataQ = _itemScheduleLinkDataQ.Where(x => x.ItemId == _itemId);
                    }
                    _itemScheduleLinkData = _itemScheduleLinkDataQ.ToList();
                }
                return _itemScheduleLinkData;
            }
        }

        private IList<SchNetworkObjectLink> _schNetworkObjectLinkData = null;
        private IList<SchNetworkObjectLink> _schNetworkObjectLinkTable
        {
            get
            {
                if (_schNetworkObjectLinkData == null)
                {
                   var _schNetworkObjectLinkDataQ = _repository.GetQuery<SchNetworkObjectLink>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject").Include("Schedule1");
                    if (_scheduleId != 0)
                    {
                        _schNetworkObjectLinkDataQ = _schNetworkObjectLinkDataQ.Where(x => x.ScheduleId == _scheduleId);
                    }
                    _schNetworkObjectLinkData = _schNetworkObjectLinkDataQ.ToList();
                }
                return _schNetworkObjectLinkData;
            }
        }

        private IList<SchDetail> _schDetailData = null;
        private IList<SchDetail> _schDetailTable
        {
            get
            {
                if (_schDetailData == null)
                {
                    var _schDetailDataQ = _repository.GetQuery<SchDetail>().Include("SchCycle");
                    if (_scheduleId != 0)
                    {
                        _schDetailDataQ = _schDetailDataQ.Where(x => x.ScheduleId == _scheduleId);
                    }
                    _schDetailData = _schDetailDataQ.ToList();
                }
                return _schDetailData;
            }
        }

        private IList<SchCycle> _schCycleData = null;
        private IList<SchCycle> _schCycleTable
        {
            get
            {
                if (_schCycleData == null)
                {
                    var _schCycleDataQ = _repository.GetQuery<SchCycle>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).Include("NetworkObject");
                    _schCycleData = _schCycleDataQ.ToList();
                }
                return _schCycleData;
            }
        }
        private IList<SpecialNoticeMenuLink> _specialNoticeMenuLinkData = null;
        private IList<SpecialNoticeMenuLink> _specialNoticeMenuLinkTable
        {
            get
            {
                if (_specialNoticeMenuLinkData == null)
                {
                    var _menuNetworkObjectLinkIds = _menuNetworkObjectLinkTable.Select(p => p.MenuNetworkObjectLinkId).ToList();
                    _specialNoticeMenuLinkData = _repository.GetQuery<SpecialNoticeMenuLink>(m => _menuNetworkObjectLinkIds.Contains(m.MenuNetworkObjectLinkId)).ToList();
                }
                return _specialNoticeMenuLinkData;
            }
        }

        #endregion

        private void clearAllPreservedTablesData()
        {
            _parentNetworkNodesData = null;
            _parentMenuNetworkNodesData = null;
            _networkObjectData = null;
            _menuNetworkObjectLinkData = null;
            _menuTagLinkData = null;
            _categoryMenuLinkData = null;
            _categoryObjectData = null;
            _categoryScheduleLinkData = null;
            _itemCollectionLinkData = null;
            _itemCollectionObjectData = null;
            _itemScheduleLinkData = null;
            _schNetworkObjectLinkData = null;
            _subCategoryLinkData = null;
            _specialNoticeMenuLinkData = null;
            _schCycleData = null;
            _prependItemLinkData = null;
        }

        /// <summary>
        /// Generic Method to run the business rule and determine the override
        /// </summary>
        /// <typeparam name="T">Class for which rule are being run</typeparam>
        /// <typeparam name="R">Generic Class for override dictonary value</typeparam>
        /// <param name="id">Id of the parent record</param>
        /// <param name="overrides">dictonary of overrides</param>
        /// <param name="value">if there is no override returns this original value of class</param>
        /// <returns></returns>
        public T GetOvrValue<T, R>(int id, Dictionary<string, R> overrides, T value, out bool IsOverride) where R : ovr<T>
        {
            string hldKey = string.Empty;

            ////walk up the tree row to see if current node on up through parents had overrides
            foreach (var cnt in Enum.GetValues(typeof(NetworkObjectTypes)).Cast<NetworkObjectTypes>().Reverse())
            {
                hldKey = id.ToString() + "_" + cnt.ToString();
                if (overrides.ContainsKey(hldKey) && overrides[hldKey].OverrideStatus == OverrideStatus.HIDDEN)
                {
                    //This is means it is a delete override
                    IsOverride = true;
                    return default(T);
                }
                else if (overrides.ContainsKey(hldKey) && overrides[hldKey].OverrideStatus != OverrideStatus.HIDDEN)
                {
                    IsOverride = true;
                    return overrides[hldKey].Value;
                }
            }

            IsOverride = false;
            return value;
        }

        public T GetSchOvrValue<T, R>(int id, Dictionary<string, R> overrides, T value, out bool IsOverride) where R : ovr<T>
        {
            string hldKey = string.Empty;
            //walk up the tree row to see if current node on up through parents had overrides
            foreach (var cnt in Enum.GetValues(typeof(NetworkObjectTypes)).Cast<NetworkObjectTypes>().Reverse())
            {
                hldKey = id.ToString() + "_" + cnt.ToString();
                if (overrides.ContainsKey(hldKey))
                {
                    IsOverride = true;
                    return overrides[hldKey].Value;
                }
            }

            IsOverride = false;
            return value;
        }

        /// <summary>
        /// Get NetworkObject Details for given network
        /// </summary>
        /// <param name="netId"></param>
        /// <returns></returns>
        public NetworkObject GetNetworkObject(int netId, out int brandId, out string parentsBreadCrum)
        {
            brandId = 0;
            parentsBreadCrum = string.Empty;
            var parents = new List<fnNetworkObjectParents_Result>();

            //Get the networkObject info and Its parents
            var network = GetNetworkObjectandParents(netId, out parents);

            if (parents != null && parents.Any())
            {
                //Create breadcrum with the names of all networks except brand
                parentsBreadCrum = string.Join(" > ", parents.Where(x => x.NetworkObjectId.HasValue && x.NetworkObjectTypeId.HasValue && x.NetworkObjectTypeId.Value != (int)NetworkObjectTypes.Root).OrderBy(x => x.NetworkObjectTypeId).Select(x => x.Name).ToList());

                //Get brandId
                var brand = parents.Where(x => x.NetworkObjectTypeId == (int)NetworkObjectTypes.Brand).FirstOrDefault();
                brandId = brand == null ? 0 : brand.NetworkObjectId.Value;
            }
            return network;
        }

        /// <summary>
        /// return networkObject and parents of the Network
        /// </summary>
        /// <param name="networkObjectId">NetworkId</param>
        /// <param name="parents" >Out parameter for list of parents</param>
        /// <returns>NetworkObject</returns>
        public NetworkObject GetNetworkObjectandParents(int networkObjectId, out List<fnNetworkObjectParents_Result> parents)
        {
            parents = new List<fnNetworkObjectParents_Result>();
            var network = _repository.FindOne<NetworkObject>(no => no.NetworkObjectId == networkObjectId);
            //Get The NetWork Parents
            var prntNetworkObjectResult = (_context as ProductMasterContext).fnNetworkObjectParents(networkObjectId).ToList();
            if (prntNetworkObjectResult != null && prntNetworkObjectResult.Any())
            {
                parents = prntNetworkObjectResult.ToList();
            }
            return network;
        }

        /// <summary>
        /// return NetworkObject Id list
        /// </summary>
        /// <param name="networkObjectId">networkObjectId</param>
        /// <returns>NetworkObjectIds List of int</returns>
        public List<int> GetNetworkParents(int networkObjectId)
        {
            List<int> parentNetworkNodes = null;
            if (MultipleNetworkObjects)
            {
                parentNetworkNodes = getNetworkParents(networkObjectId);
            }
            else
            {
                //Get The NetWork Parents
                var prntNetworkObjectResult = (_context as ProductMasterContext).fnNetworkObjectParents(networkObjectId).ToList();

                parentNetworkNodes = prntNetworkObjectResult.Where(x => x.NetworkObjectId.HasValue).Select(x => x.NetworkObjectId.Value).ToList();
                //Check how many levels it has to determine the currrent NetworkObject Type
                currNetworkType = !prntNetworkObjectResult.FirstOrDefault().NetworkObjectTypeId.HasValue ? NetworkObjectTypes.Site : (NetworkObjectTypes)prntNetworkObjectResult.FirstOrDefault().NetworkObjectTypeId.Value;
            }
            return parentNetworkNodes;
        }

        /// <summary>
        /// NOTE: Use this method ONLY if MultipleNetworkObject is true
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        private List<int> getNetworkParents(int networkObjectId)
        {
            List<int> parentNetworkNodes = null;

            //Only one of the following "Where" conditions will be true and hence only one of the following result Lists will be not nullable
            var vwNetworkObjectTreeRecordOfRoot = _vwNetworkObjectTree.Where(p => p.Root == networkObjectId).FirstOrDefault();
            var vwNetworkObjectTreeRecordOfBrand = _vwNetworkObjectTree.Where(p => p.Brand == networkObjectId).FirstOrDefault();
            var vwNetworkObjectTreeRecordOfFranchise = _vwNetworkObjectTree.Where(p => p.Franchise == networkObjectId).FirstOrDefault();
            var vwNetworkObjectTreeRecordOfMarket = _vwNetworkObjectTree.Where(p => p.Market == networkObjectId).FirstOrDefault();
            var vwNetworkObjectTreeRecordOfSite = _vwNetworkObjectTree.Where(p => p.Site == networkObjectId).FirstOrDefault();

            if (vwNetworkObjectTreeRecordOfRoot != null)
            {
                parentNetworkNodes = new List<int>();
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfRoot.Root);
                return parentNetworkNodes;
            }

            if (vwNetworkObjectTreeRecordOfBrand != null)
            {
                parentNetworkNodes = new List<int>();
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfBrand.Root);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfBrand.Brand.Value);
                return parentNetworkNodes;
            }

            if (vwNetworkObjectTreeRecordOfFranchise != null)
            {
                parentNetworkNodes = new List<int>();
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfFranchise.Root);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfFranchise.Brand.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfFranchise.Franchise.Value);
                return parentNetworkNodes;
            }

            if (vwNetworkObjectTreeRecordOfMarket != null)
            {
                parentNetworkNodes = new List<int>();
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfMarket.Root);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfMarket.Brand.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfMarket.Franchise.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfMarket.Market.Value);
                return parentNetworkNodes;
            }

            if (vwNetworkObjectTreeRecordOfSite != null)
            {
                parentNetworkNodes = new List<int>();
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfSite.Root);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfSite.Brand.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfSite.Franchise.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfSite.Market.Value);
                parentNetworkNodes.Add(vwNetworkObjectTreeRecordOfSite.Site.Value);
                return parentNetworkNodes;
            }

            return parentNetworkNodes;
        }

        /// <summary>
        /// Get all child networks of this Network
        /// </summary>
        public List<int> GetNetworkChilds(int networkObjectId)
        {
            var childNetworkNodes = new List<int>();

            //Get the NetworkObject for the selected Node to determine what Type it is
            var currNetworkObject = _repository.GetQuery<NetworkObject>(no => no.NetworkObjectId == networkObjectId).FirstOrDefault();
            List<vwNetworkObjectTree> networkTrees = new List<vwNetworkObjectTree>();
            //Now grab the row for the tree to look for childs at all other levels
            switch (currNetworkObject.NetworkObjectTypeId)
            {
                case NetworkObjectTypes.Root:
                    networkTrees = _repository.GetQuery<vwNetworkObjectTree>(vnot => vnot.Root == networkObjectId).ToList();
                    if (networkTrees.Any())
                    {// for all rows, find n/w less than this type
                        foreach (var nt in networkTrees)
                        {
                            if (nt.Brand.HasValue && !childNetworkNodes.Contains(nt.Brand.Value))
                            {
                                childNetworkNodes.Add(nt.Brand.Value);
                            }
                            if (nt.Franchise.HasValue && !childNetworkNodes.Contains(nt.Franchise.Value))
                            {
                                childNetworkNodes.Add(nt.Franchise.Value);
                            }
                            if (nt.Market.HasValue && !childNetworkNodes.Contains(nt.Market.Value))
                            {
                                childNetworkNodes.Add(nt.Market.Value);
                            }
                            if (nt.Site.HasValue && !childNetworkNodes.Contains(nt.Site.Value))
                            {
                                childNetworkNodes.Add(nt.Site.Value);
                            }
                        }
                    }
                    currNetworkType = NetworkObjectTypes.Root;
                    break;
                case NetworkObjectTypes.Brand:
                    networkTrees = _repository.GetQuery<vwNetworkObjectTree>(vnot => vnot.Brand == networkObjectId).ToList();
                    if (networkTrees.Any())
                    {
                        foreach (var nt in networkTrees)
                        {
                            if (nt.Franchise.HasValue && !childNetworkNodes.Contains(nt.Franchise.Value))
                            {
                                childNetworkNodes.Add(nt.Franchise.Value);
                            }
                            if (nt.Market.HasValue && !childNetworkNodes.Contains(nt.Market.Value))
                            {
                                childNetworkNodes.Add(nt.Market.Value);
                            }
                            if (nt.Site.HasValue && !childNetworkNodes.Contains(nt.Site.Value))
                            {
                                childNetworkNodes.Add(nt.Site.Value);
                            }
                        }
                    }
                    currNetworkType = NetworkObjectTypes.Brand;
                    break;
                case NetworkObjectTypes.Franchise:
                    networkTrees = _repository.GetQuery<vwNetworkObjectTree>(vnot => vnot.Franchise == networkObjectId).ToList();
                    if (networkTrees.Any())
                    {
                        foreach (var nt in networkTrees)
                        {
                            if (nt.Market.HasValue && !childNetworkNodes.Contains(nt.Market.Value))
                            {
                                childNetworkNodes.Add(nt.Market.Value);
                            }
                            if (nt.Site.HasValue && !childNetworkNodes.Contains(nt.Site.Value))
                            {
                                childNetworkNodes.Add(nt.Site.Value);
                            }
                        }
                    }
                    currNetworkType = NetworkObjectTypes.Franchise;
                    break;
                case NetworkObjectTypes.Market:
                    networkTrees = _repository.GetQuery<vwNetworkObjectTree>(vnot => vnot.Market == networkObjectId).ToList();
                    if (networkTrees.Any())
                    {
                        foreach (var nt in networkTrees)
                        {
                            if (nt.Site.HasValue && !childNetworkNodes.Contains(nt.Site.Value))
                            {
                                childNetworkNodes.Add(nt.Site.Value);
                            }
                        }
                    }
                    currNetworkType = NetworkObjectTypes.Market;
                    break;
                case NetworkObjectTypes.Site:
                    currNetworkType = NetworkObjectTypes.Site;
                    break;
            }

            return childNetworkNodes;
        }

        /// <summary>
        /// Get IrisIds for given NetworkIds
        /// </summary>
        /// <param name="networkIds"></param>
        /// <returns></returns>
        public List<long> GetNetworkIrisIds(List<int> networkIds)
        {
            return _repository.GetQuery<NetworkObject>(x => networkIds.Contains(x.NetworkObjectId)).Select(x => x.IrisId).ToList();
        }

        /// <summary>
        /// Get NetworkIds for given IrisIds
        /// </summary>
        /// <param name="irisIds"></param>
        /// <returns></returns>
        public List<int> GetNetworkIds(List<long> irisIds)
        {
            return _repository.GetQuery<NetworkObject>(x => irisIds.Contains(x.IrisId)).Select(x => x.NetworkObjectId).ToList();
        }

        public List<int> GetMenuNetworkLinkIds(List<int> networkIds, int menuId)
        {
            var query = _repository.GetQuery<MenuNetworkObjectLink>(x => networkIds.Contains(x.NetworkObjectId));

            if (menuId != 0)
            {
                query = query.Where(x => x.MenuId == menuId);
            }

            return query.Select(x => x.MenuNetworkObjectLinkId).ToList();
        }
        /// <summary>
        /// Retrieves all the Menus above and Below a NW, i.e., All Menus in a tree
        /// </summary>
        /// <param name="brandId"></param>
        /// <param name="menuList"></param>
        public void GetAllMenusInNetworkTree(int brandId, List<Menu> menuList)
        {
            var parentNetworkIds = new List<int>();
            var childNetworkIds = new List<int>();
            parentNetworkIds = GetNetworkParents(brandId);
            childNetworkIds = GetNetworkChilds(brandId);
            //Get all the Menus in Brand
            menuList.AddRange(_repository.GetQuery<Menu>(m => parentNetworkIds.Contains(m.NetworkObjectId) || childNetworkIds.Contains(m.NetworkObjectId)));
            if (menuList.Count > 0)
            {
                var allNetworkIds = parentNetworkIds;
                allNetworkIds.AddRange(childNetworkIds);
                getMenuLastUpdatedDateAndIsOverriden(allNetworkIds, menuList, brandId);
            }

        }

        /// <summary>
        /// Retrieves all the Menus created at given list of networks - Not in use
        /// </summary>
        /// <param name="networkIds"></param>
        /// <returns></returns>
        public List<Menu> GetMenusInNetworks(List<int> networkIds)
        {
            var retVal = new List<Menu>();
            List<Menu> lst = _repository.GetQuery<MenuNetworkObjectLink>(x => networkIds.Contains(x.NetworkObjectId)).OrderBy(x => x.Menu.SortOrder).Select(x => x.Menu).Distinct().ToList();
            retVal = lst != null ? lst : retVal;
            return retVal;
        }
        /// <summary>
        /// return Menu list to view model
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <param name="menuList"></param>
        public void GetMenus(int networkObjectId, List<Menu> menuList)
        {
            //Define RuleService propertis if they are not yet defined
            if (_networkObjectId == 0)
            {
                _networkObjectId = networkObjectId;
            }

            //Get all menu created by parent newtorks
            menuList.AddRange(_repository.GetQuery<Menu>(m => ParentNetworkNodesList.Contains(m.NetworkObjectId)).OrderBy(x => x.SortOrder));

            if (menuList.Count > 0)
            {
                //Get the menu's appropriate Lastupdated date and Is Overriden properties
                getMenuLastUpdatedDateAndIsOverriden(ParentNetworkNodesList.ToList(), menuList, networkObjectId);
            }
        }

        /// <summary>
        /// get Target's Channels
        /// </summary>
        /// <param name="siteNetworkObjectId"></param>
        /// <returns></returns>
        public List<Tag> GetTargetClientChannels(string clientID)
        {
            var tagsList = new List<Tag>();

            var target = _repository.GetQuery<MenuSyncTarget>(x => x.ApplicationIdentifier.Equals(clientID, StringComparison.InvariantCultureIgnoreCase))
                                .Include("TargetTagLinks").Include("TargetTagLinks.Tag").FirstOrDefault();
            if (target != null && target.TargetTagLinks.Any())
            {
                tagsList = target.TargetTagLinks.Where(x => x.Tag != null).Select(x => x.Tag).ToList();
            }
            return tagsList;
        }

        /// <summary>
        /// Tag List for menu in a NW to view model
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <param name="scheduleList"></param>
        public List<MenuTagLink> GetMenuTagLinkList(int menuid, int networkObjectId, bool includeInActives = false)
        {
            //NOTE: MenuTagLink is the entity/databasetable where a Menu is linked with a Tag and is overridden

            bool hadOverride = false;
            List<MenuTagLink> menuList = new List<MenuTagLink>();
            MenuTagLink menuTagLinkToAdd = null;
            MenuTagLink menuTagLink = null;

            if (NetworkObjectId != networkObjectId)
            {
                //Set the networkobjectId if it was not set before
                NetworkObjectId = networkObjectId;
            }
            //1. Get list of MenuTagLink for this and parent NetworkObjectIds           
            var menuTagLinkList = _menuTagLinkTable.Where(m => m.MenuId == menuid);
            if (menuTagLinkList != null && menuTagLinkList.Any())
            {
                //2. Get all root level MenuTagLinks from the list: mTLWithRootLevelTag_List
                var mTLWithRootLevelTag_List = menuTagLinkList.Where(m => m.ParentTagId == null);

                //3. Get all tagOverrides
                var mtLWithTagOverride_List = menuTagLinkList.Where(m => m.ParentTagId != null);

                //Get all overrides above this NW level
                var processedMTLWithTagOverrides_List = (from mTLWithTagOverride in mtLWithTagOverride_List
                                                              select new ovr<MenuTagLink> { OverrideStatus = mTLWithTagOverride.OverrideStatus, Id = mTLWithTagOverride.ParentTagId.Value, NetworkTypeId = mTLWithTagOverride.NetworkObject.NetworkObjectTypeId, Value = mTLWithTagOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                foreach (var mTLWithRootLevelTag in mTLWithRootLevelTag_List)
                {
                    hadOverride = false;
                    menuTagLinkToAdd = new MenuTagLink();
                    menuTagLink = mTLWithRootLevelTag;

                    if (processedMTLWithTagOverrides_List != null && processedMTLWithTagOverrides_List.Any())
                    {
                        //Run through all the rules to determine the value
                        if (includeInActives)
                        {
                            menuTagLink = GetSchOvrValue<MenuTagLink, ovr<MenuTagLink>>(mTLWithRootLevelTag.TagId, processedMTLWithTagOverrides_List, mTLWithRootLevelTag, out hadOverride);
                        }
                        else
                        {
                            menuTagLink = GetOvrValue<MenuTagLink, ovr<MenuTagLink>>(mTLWithRootLevelTag.TagId, processedMTLWithTagOverrides_List, mTLWithRootLevelTag, out hadOverride);
                        }
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (menuTagLink != null)
                    {
                        menuTagLinkToAdd = menuTagLink;
                        menuTagLinkToAdd.OverrideStatus = menuTagLink.OverrideStatus;
                        menuList.Add(menuTagLinkToAdd);
                    }
                }
            }
            return menuList.ToList();
        }

        private void getMenuLastUpdatedDateAndIsOverriden(List<int> networkIds, List<Menu> menuList, int networkId)
        {
            //1. Menu can be updated at this or any networkObject Level above it. 
            //2. Level of the networkObject is identified by NetworkObjectType
            //3. In order to get the latest LastUpdatedDate of Menu, check for the edits made the current networkObject Level and all parent levels and get the max of all LastUpdatedDates 

            //var networkObjectParents = this.GetNetworkParents(networkId);
            var mnuNWLinks = _repository.GetQuery<MenuNetworkObjectLink>(x => networkIds.Contains(x.NetworkObjectId)).ToList();
            foreach (var menu in menuList)
            {
                //3.2 Check 'MenuDataUpdate' for all the edits made to this menu by networkObjectParents and get the 'last' (/max) updated date
                var lastUpdatedDate = mnuNWLinks.Where(x => x.MenuId == menu.MenuId).Max(x => x.LastUpdatedDate);
                menu.LastUpdatedDate = lastUpdatedDate;

                // Set flag is Menu overriden at this network or not
                if (mnuNWLinks.Where(x => x.MenuId == menu.MenuId && x.NetworkObjectId == networkId).Any())
                {
                    menu.IsMenuOverriden = mnuNWLinks.Where(x => x.MenuId == menu.MenuId && x.NetworkObjectId == networkId).FirstOrDefault().IsMenuOverriden;
                }
            }
        }

        /// <summary>
        /// This method is used to get the menus which are mapped with POS Data.
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <param name="menuList"></param>       
        public void GetMappedMenu(int networkObjectId, List<Menu> menuList)
        {            
            var menuIdList = _menuNetworkObjectLinkTable.Where(x => x.NetworkObjectId == networkObjectId && x.IsPOSMapped == true).Select(p => p.MenuId).Distinct();

            Menu menu = null;
            foreach (var menuId in menuIdList)
            {                
                menu = _menuNetworkObjectLinkTable.Where(x => x.NetworkObjectId == networkObjectId && x.MenuId == menuId).First().Menu;
                if (MultipleNetworkObjects)
                {
                    menu.LastUpdatedDate = _menuNetworkObjectLinkTable.Where(x => ParentNetworkNodesList.Contains(x.NetworkObjectId) && x.MenuId == menuId).Max(x => x.LastUpdatedDate);
                }
                else
                {
                    menu.LastUpdatedDate = _menuNetworkObjectLinkTable.Where(x => x.MenuId == menuId).Max(x => x.LastUpdatedDate);
                }                
                menuList.Add(menu);
            }
        }

        /// <summary>
        /// Gets 'CATEGORIES in a MENU'
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<Category> GetCategoriesList(int menuId)
        {
            //NOTE: CategoryMenuLink is the entity/databasetable where a category is linked with a MENU and is overridden

            var returnCategoryList = new List<Category>();
            bool hadOverride = false;
            Category categoryToAdd = null;
            CategoryMenuLink categoryMenuLink = null;

            if (_callType == CallType.Web)
            {
                //Define Menuid property of RuleService
                _menuId = menuId;
            }

            //1. Get list of CategoryMenuLinks for this and parent NetworkObjectIds
            var categoryMenuLinkList = _categoryMenuLinkTable.Where(m => m.MenuId == menuId);
            if (categoryMenuLinkList != null && categoryMenuLinkList.Any())
            {
                //2. Get all root level CategoryMenuLinks from the list: cMLWithRootLevelCategory_List
                var cMLWithRootLevelCategory_List = categoryMenuLinkList.Where(m => m.ParentCategoryId == null).ToList();

                //3. Get all categoryOverrides
                var cMLWithCategoryOverride_List = categoryMenuLinkList.Where(m => m.ParentCategoryId != null);

                //Get all overrides above this NW level
                var processedCMLWithCategoryOverride_List = (from cMLWithCategoryOverride in cMLWithCategoryOverride_List
                                                             select new ovr<CategoryMenuLink> { OverrideStatus = cMLWithCategoryOverride.OverrideStatus, Id = cMLWithCategoryOverride.ParentCategoryId.Value, NetworkTypeId = cMLWithCategoryOverride.NetworkObject.NetworkObjectTypeId, Value = cMLWithCategoryOverride })
                                   .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                //WEB ONLY: Load only required children           
                List<int> allcatIds = null;
                List<CategoryObject> allcatObjs = null;
                List<SubCategoryLink> allsubCats = null;
                var networkIdWhereMenuCreated = 0;
                if (_callType == CallType.Web)
                {
                    //Network where Menu is created
                    networkIdWhereMenuCreated = Menu == null ? 0 : Menu.NetworkObjectId;

                    allcatIds = cMLWithRootLevelCategory_List.Select(i => i.CategoryId).ToList();
                    allcatObjs = _repository.GetQuery<CategoryObject>(m => allcatIds.Contains(m.CategoryId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.Category.MenuId == menuId).Include("Category").ToList();
                    allsubCats = _repository.GetQuery<SubCategoryLink>(m => allcatIds.Contains(m.CategoryId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.Category.MenuId == menuId).Include("Category").ToList();
                }

                foreach (var cMLWithRootLevelCategory in cMLWithRootLevelCategory_List)
                {
                    hadOverride = false;
                    categoryToAdd = new Category();
                    categoryMenuLink = cMLWithRootLevelCategory;

                    if (processedCMLWithCategoryOverride_List != null && processedCMLWithCategoryOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        categoryMenuLink = GetOvrValue<CategoryMenuLink, ovr<CategoryMenuLink>>(cMLWithRootLevelCategory.CategoryId, processedCMLWithCategoryOverride_List, cMLWithRootLevelCategory, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (categoryMenuLink != null)
                    {
                        categoryToAdd = categoryMenuLink.Category;
                        categoryToAdd.OrgCategoryId = cMLWithRootLevelCategory.CategoryId;
                        categoryToAdd.SortOrder = categoryMenuLink.SortOrder;
                        //WEB ONLY: 
                        if (_callType == CallType.Web)
                        {
                            // Rules to set isoverride flag:
                            // 1. If only position is changed  - Not an override
                            // 2. If overriden category and original category are created at same network - Not an override
                            // 3. Else, if there is a override - an override (retrieved from hadOverride property)
                            // 4. Else, if there is no overrides - not an override (retrieved from hadOverride property)
                            // 5. If this is created (newly added) at different network than Menu was created - an override 
                            // 6. If this is change is at different network than current Network we are looking - an Not an override 
                            categoryToAdd.IsOverride = (categoryMenuLink.ParentCategoryId.HasValue && categoryMenuLink.ParentCategoryId == categoryMenuLink.CategoryId) || (cMLWithRootLevelCategory.Category.NetworkObjectId == categoryMenuLink.Category.NetworkObjectId) || categoryMenuLink.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                            categoryToAdd.IsOverride = networkIdWhereMenuCreated == cMLWithRootLevelCategory.NetworkObjectId ? categoryToAdd.IsOverride : true;
                            categoryToAdd.IsOverride = NetworkObjectId == categoryMenuLink.NetworkObjectId ? categoryToAdd.IsOverride : false;
                            categoryToAdd.hasChildren = allcatObjs.Where(x => x.CategoryId == cMLWithRootLevelCategory.CategoryId).Count() > 0 || allsubCats.Where(x => x.CategoryId == cMLWithRootLevelCategory.CategoryId).Count() > 0;
                            categoryToAdd.CategoryObjects = allcatObjs.Where(x => x.CategoryId == cMLWithRootLevelCategory.CategoryId).ToList();
                            categoryToAdd.SubCategoryLinks = allsubCats.Where(x => x.CategoryId == cMLWithRootLevelCategory.CategoryId).ToList();
                        }
                        returnCategoryList.Add(categoryToAdd);
                    }
                }
                if (returnCategoryList.Any())
                {
                    returnCategoryList = returnCategoryList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnCategoryList;
        }

        /// <summary>
        /// Gets 'SUB CATEGORIES in a CATEGORY'
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<Category> GetSubCategoryList(int parentCategoryId, int menuId)
        {
            //NOTE: SubCategoryLink is the entity/databasetable where a (sub)category is linked with a CATEGORY and is overridden

            var returnSubCategoryList = new List<Category>();
            bool hadOverride = false;
            Category subCategoryToAdd = null;
            SubCategoryLink subCategoryLink = null;

            if (_callType == CallType.Web)
            {
                //Define properties of RuleService
                _menuId = menuId;
                _categoryId = parentCategoryId;
            }

            //1. Get all SubCategoryLinks for this and parent NetworkObjectIds            
            var subCategoryLinkList = _subCategoryLinkTable.Where(m => m.CategoryId == parentCategoryId);
            if (subCategoryLinkList != null && subCategoryLinkList.Any())
            {
                //2. Get all root level SubCategoryLinks from the list: sCLWithRootLevelSubCategory_List
                var sCLWithRootLevelSubCategory_List = subCategoryLinkList.Where(m => m.OverrideParentSubCategoryId == null).ToList();

                //3. Get all SubCategoryOverrides
                var sCLWithSubCategoryOverride_List = subCategoryLinkList.Where(m => m.OverrideParentSubCategoryId != null);

                //Get all overrides above this NW level
                var processedSCLWithSubCategoryOverride_List = (from sCLWithSubCategoryOverride in sCLWithSubCategoryOverride_List
                                                                select new ovr<SubCategoryLink> { OverrideStatus = sCLWithSubCategoryOverride.OverrideStatus, Id = sCLWithSubCategoryOverride.OverrideParentSubCategoryId.Value, NetworkTypeId = sCLWithSubCategoryOverride.NetworkObject.NetworkObjectTypeId, Value = sCLWithSubCategoryOverride })
                                   .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                //WEB ONLY: Load only required children           
                List<int> allsubCatIds = null;
                List<CategoryObject> allcatObjs = null;
                List<SubCategoryLink> allsubCats = null;
                var networkIdWhereMenuCreated = 0;
                if (_callType == CallType.Web)
                {
                    //Network where Menu is created
                    networkIdWhereMenuCreated = Menu == null ? 0 : Menu.NetworkObjectId;
                    allsubCatIds = sCLWithRootLevelSubCategory_List.Select(i => i.SubCategoryId).ToList();
                    allcatObjs = _repository.GetQuery<CategoryObject>(m => allsubCatIds.Contains(m.CategoryId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.Category.MenuId == menuId).Include("Category").ToList();
                    allsubCats = _repository.GetQuery<SubCategoryLink>(m => allsubCatIds.Contains(m.CategoryId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.Category.MenuId == menuId).Include("Category").ToList();
                }

                foreach (var sCLWithRootLevelSubCategory in sCLWithRootLevelSubCategory_List)
                {
                    hadOverride = false;
                    subCategoryToAdd = new Category();
                    subCategoryLink = sCLWithRootLevelSubCategory;

                    if (processedSCLWithSubCategoryOverride_List != null && processedSCLWithSubCategoryOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        subCategoryLink = GetOvrValue<SubCategoryLink, ovr<SubCategoryLink>>(sCLWithRootLevelSubCategory.SubCategoryId, processedSCLWithSubCategoryOverride_List, sCLWithRootLevelSubCategory, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (subCategoryLink != null)
                    {
                        subCategoryToAdd = subCategoryLink.SubCategory;
                        subCategoryToAdd.OrgCategoryId = sCLWithRootLevelSubCategory.SubCategoryId;
                        subCategoryToAdd.SortOrder = subCategoryLink.SortOrder;

                        //WEB ONLY: 
                        if (_callType == CallType.Web)
                        {
                            subCategoryToAdd.IsOverride = (subCategoryLink.OverrideParentSubCategoryId.HasValue && subCategoryLink.OverrideParentSubCategoryId == subCategoryLink.SubCategoryId) || (subCategoryLink.SubCategory.NetworkObjectId == sCLWithRootLevelSubCategory.SubCategory.NetworkObjectId) || subCategoryLink.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                            subCategoryToAdd.IsOverride = networkIdWhereMenuCreated == sCLWithRootLevelSubCategory.NetworkObjectId ? subCategoryToAdd.IsOverride : true;
                            subCategoryToAdd.IsOverride = NetworkObjectId == subCategoryLink.NetworkObjectId ? subCategoryToAdd.IsOverride : false;
                            subCategoryToAdd.hasChildren = allcatObjs.Where(x => x.CategoryId == sCLWithRootLevelSubCategory.SubCategoryId).Count() > 0 || allsubCats.Where(x => x.CategoryId == sCLWithRootLevelSubCategory.SubCategoryId).Count() > 0;
                            subCategoryToAdd.CategoryObjects = allcatObjs.Where(x => x.CategoryId == sCLWithRootLevelSubCategory.SubCategoryId).ToList();
                            subCategoryToAdd.SubCategoryLinks = allsubCats.Where(x => x.CategoryId == sCLWithRootLevelSubCategory.SubCategoryId).ToList();
                        }
                        returnSubCategoryList.Add(subCategoryToAdd);
                    }
                }
                if (returnSubCategoryList.Any())
                {
                    returnSubCategoryList = returnSubCategoryList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnSubCategoryList;
        }

        /// <summary>
        /// Gets 'ITEMs in a CATEGORY'
        /// </summary>
        /// <param name="parentCategoryId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<Item> GetItemList(int parentCategoryId, int menuId)
        {
            //NOTE: CategoryObject is the entity/databasetable where a item is linked with a Category and is overridden

            var returnItemList = new List<Item>();
            bool hadOverride = false;
            Item itemToAdd = null;
            CategoryObject categoryObject = null;

            if (_callType == CallType.Web)
            {
                //Define properties of RuleService
                _menuId = menuId;
                _categoryId = parentCategoryId;
            }

            //1. Get list of CategoryObjects for this and parent NetworkObjectIds              
            var categoryObjectList = _categoryObjectTable.Where(m => m.CategoryId == parentCategoryId);
            if (categoryObjectList != null && categoryObjectList.Any())
            {
                //2. Get all root level CategoryObjects from the list: catObjectWithRootLevelItem_List
                var catObjectWithRootLevelItem_List = categoryObjectList.Where(m => m.ParentItemId == null);

                //3. Get all itemOverrides
                var catObjectWithItemOverride_List = categoryObjectList.Where(m => m.ParentItemId != null);

                //Get all overrides above this NW level
                var processedCatObjectWithItemOverrides_List = (from catObjectWithItemOverride in catObjectWithItemOverride_List
                                                                select new ovr<CategoryObject> { OverrideStatus = catObjectWithItemOverride.OverrideStatus, Id = catObjectWithItemOverride.ParentItemId.Value, NetworkTypeId = catObjectWithItemOverride.NetworkObject.NetworkObjectTypeId, Value = catObjectWithItemOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

                //WEB ONLY: Load only required children           
                List<int> allitemIds = null;
                List<ItemCollectionLink> allitemCollectionLinks = null;
                var networkIdWhereMenuCreated = 0;
                if (_callType == CallType.Web)
                {
                    //Network where Menu is created
                    networkIdWhereMenuCreated = Menu == null ? 0 : Menu.NetworkObjectId;
                    allitemIds = catObjectWithRootLevelItem_List.Select(i => i.ItemId).ToList();
                    allitemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(m => allitemIds.Contains(m.ItemId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.ItemCollection.MenuId == menuId).Include("ItemCollection").ToList();
                }

                foreach (var catObjectWithRootLevelItem in catObjectWithRootLevelItem_List)
                {
                    hadOverride = false;
                    itemToAdd = new Item();
                    categoryObject = catObjectWithRootLevelItem;

                    if (processedCatObjectWithItemOverrides_List != null && processedCatObjectWithItemOverrides_List.Any())
                    {
                        //Run through all the rules to determine the value
                        categoryObject = GetOvrValue<CategoryObject, ovr<CategoryObject>>(catObjectWithRootLevelItem.ItemId, processedCatObjectWithItemOverrides_List, catObjectWithRootLevelItem, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (categoryObject != null)
                    {
                        itemToAdd = categoryObject.Item;
                        itemToAdd.SortOrder = categoryObject.SortOrder;
                        itemToAdd.OrgItemId = catObjectWithRootLevelItem.ItemId;
                        //item can have a different description for different menu n network
                        var masterItem = categoryObject.Item.MasterItem == null ? categoryObject.Item : categoryObject.Item.MasterItem;
                        var selectedDescription = masterItem.ItemDescriptions.Any() && categoryObject.Item.SelectedDescriptionId != null ? masterItem.ItemDescriptions.Where(p => p.ItemDescriptionId == categoryObject.Item.SelectedDescriptionId).FirstOrDefault() : null;
                        itemToAdd.DisplayDescription = selectedDescription == null ? categoryObject.Item.DisplayDescription : selectedDescription.Description;
                        //WEB ONLY: 
                        if (_callType == CallType.Web)
                        {
                            itemToAdd.IsOverride = (categoryObject.ParentItemId.HasValue && categoryObject.ParentItemId == categoryObject.ItemId) || categoryObject.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                            itemToAdd.IsOverride = networkIdWhereMenuCreated == catObjectWithRootLevelItem.NetworkObjectId ? itemToAdd.IsOverride : true;
                            itemToAdd.IsOverride = NetworkObjectId == categoryObject.NetworkObjectId ? itemToAdd.IsOverride : false;
                            itemToAdd.hasChildren = allitemCollectionLinks.Where(i => i.ItemId == catObjectWithRootLevelItem.ItemId).Count() > 0; // Orginal Item determines whether it has child or not
                            itemToAdd.ItemCollectionLinks = allitemCollectionLinks.Where(i => i.ItemId == catObjectWithRootLevelItem.ItemId).ToList(); // Orginal Item determines its children
                        }
                        returnItemList.Add(itemToAdd);
                    }
                }
                if (returnItemList.Any())
                {
                    returnItemList = returnItemList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnItemList;
        }

        /// <summary>
        /// Gets 'ITEMCOLLECTIONs in an ITEM' for current menu and Network
        /// </summary>
        /// <param name="originalItemId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<ItemCollection> GetCollectionList(int originalItemId, int menuId)
        {
            //NOTE: ItemCollectionLink is the entity/databasetable where a ItemCollection is linked with Item and is overridden

            var returnItemCollectionList = new List<ItemCollection>();
            bool hadOverride = false;
            ItemCollection itemCollectionToAdd = null;
            ItemCollectionLink itemCollectionLink = null;

            if (_callType == CallType.Web)
            {
                //Define properties of RuleService
                _menuId = menuId;
                _itemId = originalItemId;
            }

            //1. Get list of ItemCollectionLinks for this and parent NetworkObjectIds        
            var itemCollectionLinkList = _itemCollectionLinkTable.Where(m => m.ItemId == originalItemId && m.ItemCollection.MenuId == menuId);
            if (itemCollectionLinkList != null && itemCollectionLinkList.Any())
            {
                //2. Get all root level ItemCollectionLinks from the list: iCLWithRootLevelItemCollection_List
                var iCLWithRootLevelItemCollection_List = itemCollectionLinkList.Where(m => m.ParentCollectionId == null);

                //3. Get all ItemCollectionOverrides
                var iCLWithItemCollectionOverride_List = itemCollectionLinkList.Where(m => m.ParentCollectionId != null);

                //Get all overrides above this NW level
                var processedICLWithItemCollectionOverride_List = (from iCLWithItemCollectionOverride in iCLWithItemCollectionOverride_List
                                                                   select new ovr<ItemCollectionLink> { OverrideStatus = iCLWithItemCollectionOverride.OverrideStatus, Id = iCLWithItemCollectionOverride.ParentCollectionId.Value, NetworkTypeId = iCLWithItemCollectionOverride.NetworkObject.NetworkObjectTypeId, Value = iCLWithItemCollectionOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

                //WEB ONLY: Load only required children           
                //Load only required data
                List<int> allcolIds = null;
                List<ItemCollectionObject> allcotObjs = null;
                var networkIdWhereMenuCreated = 0;
                if (_callType == CallType.Web)
                {
                    //Network where Menu is created
                    networkIdWhereMenuCreated = Menu == null ? 0 : Menu.NetworkObjectId;
                    allcolIds = iCLWithRootLevelItemCollection_List.Select(i => i.CollectionId).ToList();
                    allcotObjs = _repository.GetQuery<ItemCollectionObject>(m => allcolIds.Contains(m.CollectionId) && ParentNetworkNodesList.Contains(m.NetworkObjectId)).ToList();
                }

                foreach (var iCLWithRootLevelItemCollection in iCLWithRootLevelItemCollection_List)
                {
                    hadOverride = false;
                    itemCollectionToAdd = new ItemCollection();
                    itemCollectionLink = iCLWithRootLevelItemCollection;

                    if (processedICLWithItemCollectionOverride_List != null && processedICLWithItemCollectionOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        itemCollectionLink = GetOvrValue<ItemCollectionLink, ovr<ItemCollectionLink>>(iCLWithRootLevelItemCollection.CollectionId, processedICLWithItemCollectionOverride_List, iCLWithRootLevelItemCollection, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (itemCollectionLink != null)
                    {
                        itemCollectionToAdd = itemCollectionLink.ItemCollection;
                        itemCollectionToAdd.OrgCollectionId = iCLWithRootLevelItemCollection.CollectionId;
                        itemCollectionToAdd.SortOrder = itemCollectionLink.SortOrder;
                        
                        //WEB ONLY: 
                        if (_callType == CallType.Web)
                        {
                            itemCollectionToAdd.IsOverride = (itemCollectionLink.ParentCollectionId.HasValue && itemCollectionLink.ParentCollectionId == itemCollectionLink.CollectionId) || (itemCollectionLink.ItemCollection.NetworkObjectId == iCLWithRootLevelItemCollection.ItemCollection.NetworkObjectId) || itemCollectionLink.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                            itemCollectionToAdd.IsOverride = networkIdWhereMenuCreated == iCLWithRootLevelItemCollection.NetworkObjectId ? itemCollectionToAdd.IsOverride : true;
                            itemCollectionToAdd.IsOverride = NetworkObjectId == itemCollectionLink.NetworkObjectId ? itemCollectionToAdd.IsOverride : false;
                            itemCollectionToAdd.hasChildren = allcotObjs.Where(x => x.CollectionId == iCLWithRootLevelItemCollection.CollectionId).Count() > 0; // Orginal Collection determines whether it has child or not
                            itemCollectionToAdd.ItemCollectionObjects = allcotObjs.Where(x => x.CollectionId == iCLWithRootLevelItemCollection.CollectionId).ToList(); // Orginal Collection determines its children
                        }
                        returnItemCollectionList.Add(itemCollectionToAdd);
                    }
                }
                if (returnItemCollectionList.Any())
                {
                    returnItemCollectionList = returnItemCollectionList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnItemCollectionList;
        }

        /// <summary>
        /// Gets ITEMS IN (ITEM)COLLECTION for current menu and Network
        /// </summary>
        /// <param name="originalCollectionId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<Item> GetCollectionItemList(int originalCollectionId, int menuId)
        {
            //NOTE: ItemCollectionObject is the entity/databasetable where a Item is linked with ItemCollection and is overridden        

            var returnItemInCollectionList = new List<Item>();
            bool hadOverride = false;
            Item itemToAdd = null;
            ItemCollectionObject itemCollectionObject = null;

            if (_callType == CallType.Web)
            {
                //Define properties of RuleService
                _menuId = menuId;
                _collectionId = originalCollectionId;
            }

            //1. Get list of ItemCollectionObjects for this and parent NetworkObjectIds                   
            var itemCollectionObjectList = _itemCollectionObjectTable.Where(m => m.CollectionId == originalCollectionId);
            if (itemCollectionObjectList != null && itemCollectionObjectList.Any())
            {
                //2. Get all root level ItemCollectionObjects from the list: iCOWithRootLevelItem_List
                var iCOWithRootLevelItem_List = itemCollectionObjectList.Where(m => m.ParentItemId == null);

                //3. Get all itemOverrides
                var iCOWithItemOverride_List = itemCollectionObjectList.Where(m => m.ParentItemId != null);

                //Get all overrides above this NW level
                var processedICOWithItemOverride_List = (from iCOWithItemOverride in iCOWithItemOverride_List
                                                         select new ovr<ItemCollectionObject> { OverrideStatus = iCOWithItemOverride.OverrideStatus, Id = iCOWithItemOverride.ParentItemId.Value, NetworkTypeId = iCOWithItemOverride.NetworkObject.NetworkObjectTypeId, Value = iCOWithItemOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

                //WEB ONLY: Load only required children           
                //Load only required data
                List<int> allitemIds = null;
                List<ItemCollectionLink> allitemCollectionLinks = null;
                var networkIdWhereMenuCreated = 0;
                if (_callType == CallType.Web)
                {
                    //Network where Menu is created
                    networkIdWhereMenuCreated = Menu == null ? 0 : Menu.NetworkObjectId;
                    allitemIds = iCOWithRootLevelItem_List.Select(i => i.ItemId).ToList();
                    allitemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(m => allitemIds.Contains(m.ItemId) && ParentNetworkNodesList.Contains(m.NetworkObjectId) && m.ItemCollection.MenuId == menuId).Include("ItemCollection").ToList();
                }

                foreach (var iCOWithRootLevelItem in iCOWithRootLevelItem_List)
                {
                    hadOverride = false;
                    itemToAdd = new Item();
                    itemCollectionObject = iCOWithRootLevelItem;

                    if (processedICOWithItemOverride_List != null && processedICOWithItemOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        itemCollectionObject = GetOvrValue<ItemCollectionObject, ovr<ItemCollectionObject>>(iCOWithRootLevelItem.ItemId, processedICOWithItemOverride_List, iCOWithRootLevelItem, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (itemCollectionObject != null)
                    {
                        itemToAdd = itemCollectionObject.Item;
                        itemToAdd.SortOrder = itemCollectionObject.SortOrder;
                        itemToAdd.OrgItemId = iCOWithRootLevelItem.ItemId;
                        itemToAdd.IsAutoSelect = itemCollectionObject.IsAutoSelect;
                        //item can have a different description for different menu n network
                        var masterItem = itemCollectionObject.Item.MasterItem == null ? itemCollectionObject.Item : itemCollectionObject.Item.MasterItem;
                        var selectedDescription = masterItem.ItemDescriptions.Any() && itemCollectionObject.Item.SelectedDescriptionId != null ? masterItem.ItemDescriptions.Where(p => p.ItemDescriptionId == itemCollectionObject.Item.SelectedDescriptionId).FirstOrDefault() : null;
                        itemToAdd.DisplayDescription = selectedDescription == null ? itemCollectionObject.Item.DisplayDescription : selectedDescription.Description;
                        
                        //WEB ONLY: 
                        if (_callType == CallType.Web)
                        {
                            itemToAdd.IsOverride = (itemCollectionObject.ParentItemId.HasValue && itemCollectionObject.ParentItemId == itemCollectionObject.ItemId) || itemCollectionObject.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                            itemToAdd.IsOverride = networkIdWhereMenuCreated == iCOWithRootLevelItem.NetworkObjectId ? itemToAdd.IsOverride : true;
                            itemToAdd.IsOverride = NetworkObjectId == itemCollectionObject.NetworkObjectId ? itemToAdd.IsOverride : false;
                            itemToAdd.hasChildren = allitemCollectionLinks.Where(i => i.ItemId == iCOWithRootLevelItem.ItemId).Count() > 0; //categoryObject.Item.ItemCollectionLinks.Count() > 0; // Orginal Item determines whether it has child or not
                            itemToAdd.ItemCollectionLinks = allitemCollectionLinks.Where(i => i.ItemId == iCOWithRootLevelItem.ItemId).ToList(); // Orginal Item determines its children
                        }
                        returnItemInCollectionList.Add(itemToAdd);
                    }
                }
                if (returnItemInCollectionList.Any())
                {
                    returnItemInCollectionList = returnItemInCollectionList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnItemInCollectionList;
        }

        /// <summary>
        /// Gets 'PREPENDITEMs in an ITEM' for current menu and Network
        /// </summary>
        /// <param name="originalItemId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<Item> GetPrependItemList(int originalItemId, int menuId)
        {
            //NOTE: PrependItemLink is the entity/databasetable where a PrependItem is linked with Item and is overridden to delete or reposition

            var returnPrependItemList = new List<Item>();
            bool hadOverride = false;
            Item prependItemToAdd = null;
            PrependItemLink prependItemLink = null;

            if (_callType == CallType.Web)
            {
                //Define properties of RuleService
                _menuId = menuId;
                _itemId = originalItemId;
            }
            //1. Get list of PrependItemLinks for this and parent NetworkObjectIds        
            var prependItemLinkList = _prependItemLinkTable.Where(m => m.ItemId == originalItemId && m.MenuNetworkObjectLink.MenuId == menuId);
            if (prependItemLinkList != null && prependItemLinkList.Any())
            {
                //2. Get all root level PrependItemLinks from the list: iPLWithRootLevelPrependItem_List
                var iPLWithRootLevelPrependItem_List = prependItemLinkList.Where(m => m.OverrideParentPrependItemId == null);

                //3. Get all PrependItemOverrides
                var iPLWithPrependItemOverride_List = prependItemLinkList.Where(m => m.OverrideParentPrependItemId != null);

                //Get all overrides above this NW level
                var processedICLWithItemCollectionOverride_List = (from iPLWithPrependItemOverride in iPLWithPrependItemOverride_List
                                                                   select new ovr<PrependItemLink> { OverrideStatus = iPLWithPrependItemOverride.OverrideStatus, Id = iPLWithPrependItemOverride.OverrideParentPrependItemId.Value, NetworkTypeId = iPLWithPrependItemOverride.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId, Value = iPLWithPrependItemOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);

                foreach (var iPLWithRootLevelPrependItem in iPLWithRootLevelPrependItem_List)
                {
                    hadOverride = false;
                    prependItemToAdd = new Item();
                    prependItemLink = iPLWithRootLevelPrependItem;

                    if (processedICLWithItemCollectionOverride_List != null && processedICLWithItemCollectionOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        prependItemLink = GetOvrValue<PrependItemLink, ovr<PrependItemLink>>(iPLWithRootLevelPrependItem.PrependItemId, processedICLWithItemCollectionOverride_List, iPLWithRootLevelPrependItem, out hadOverride);
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (prependItemLink != null)
                    {
                        prependItemToAdd = prependItemLink.PrependItem;
                        prependItemToAdd.OrgItemId = iPLWithRootLevelPrependItem.PrependItemId;
                        prependItemToAdd.SortOrder = prependItemLink.SortOrder;

                        //item can have a different description for different menu n network
                        var masterItem = prependItemLink.Item.MasterItem == null ? prependItemLink.Item : prependItemLink.Item.MasterItem;
                        var selectedDescription = masterItem.ItemDescriptions.Any() && prependItemLink.Item.SelectedDescriptionId != null ? masterItem.ItemDescriptions.Where(p => p.ItemDescriptionId == prependItemLink.Item.SelectedDescriptionId).FirstOrDefault() : null;
                        prependItemToAdd.DisplayDescription = selectedDescription == null ? prependItemLink.Item.DisplayDescription : selectedDescription.Description;
                        returnPrependItemList.Add(prependItemToAdd);
                    }
                }
                if (returnPrependItemList.Any())
                {
                    returnPrependItemList = returnPrependItemList.OrderBy(c => c.SortOrder).ToList();
                }
            }
            return returnPrependItemList;
        }

        /// <summary>
        /// Schedule List for a NW to view model
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <param name="scheduleList"></param>
        public List<Schedule> GetScheduleList(int networkObjectId,  bool includeInActives = false)
        {
            //NOTE: SchNetworkObjectLink is the entity/databasetable where a Schedule is linked with a NetworkObject and is overridden

            bool hadOverride = false;
            Schedule scheduleToAdd = null;
            SchNetworkObjectLink schNetworkObjectLink = null;
            var scheduleList = new List<Schedule>();

            //1. Get list of SchNetworkObjectLink for this and parent NetworkObjectIds           
            var _schNetworkObjectLinkList = _schNetworkObjectLinkTable;
            if (_schNetworkObjectLinkList != null && _schNetworkObjectLinkList.Any())
            {
                //2. Get all root level SchNetworkObjectLinks from the list: sNOWithRootLevelSchedule_List
                var sNLWithRootLevelSchedule_List = _schNetworkObjectLinkList.Where(m => m.ParentScheduleId == null);

                //3. Get all scheduleOverrides
                var sNLWithScheduleOverride_List = _schNetworkObjectLinkList.Where(m => m.ParentScheduleId != null);

                //Get all overrides above this NW level
                var processedSNOWithScheduleOverrides_List = (from sNOWithScheduleOverride in sNLWithScheduleOverride_List
                                                              select new ovr<SchNetworkObjectLink> { OverrideStatus = sNOWithScheduleOverride.OverrideStatus, Id = sNOWithScheduleOverride.ParentScheduleId.Value, NetworkTypeId = sNOWithScheduleOverride.NetworkObject.NetworkObjectTypeId, Value = sNOWithScheduleOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                foreach (var sNLWithRootLevelSchedule in sNLWithRootLevelSchedule_List)
                {
                    hadOverride = false;
                    scheduleToAdd = new Schedule();
                    schNetworkObjectLink = sNLWithRootLevelSchedule;

                    if (processedSNOWithScheduleOverrides_List != null && processedSNOWithScheduleOverrides_List.Any())
                    {
                        //Run through all the rules to determine the value
                        if (includeInActives)
                        {
                            schNetworkObjectLink = GetSchOvrValue<SchNetworkObjectLink, ovr<SchNetworkObjectLink>>(sNLWithRootLevelSchedule.ScheduleId, processedSNOWithScheduleOverrides_List, sNLWithRootLevelSchedule, out hadOverride);
                        }
                        else
                        {
                            schNetworkObjectLink = GetOvrValue<SchNetworkObjectLink, ovr<SchNetworkObjectLink>>(sNLWithRootLevelSchedule.ScheduleId, processedSNOWithScheduleOverrides_List, sNLWithRootLevelSchedule, out hadOverride);
                        }
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (schNetworkObjectLink != null)
                    {
                        scheduleToAdd = schNetworkObjectLink.Schedule1;
                        // Rules to set isoverride flag:
                        // 1. If only position is changed  - Not an override
                        // 2. If overriden category and original category are created at same network - Not an override
                        // 3. Else, if there is a override - an override (retrieved from hadOverride property)
                        // 4. Else, if there is no overrides - not an override (retrieved from hadOverride property) 
                        // 6. If this is change is at different network than current Network we are looking - an Not an override 
                        scheduleToAdd.IsOverride = (schNetworkObjectLink.ParentScheduleId.HasValue && schNetworkObjectLink.ParentScheduleId == schNetworkObjectLink.ScheduleId) || (sNLWithRootLevelSchedule.NetworkObjectId == schNetworkObjectLink.NetworkObjectId) || schNetworkObjectLink.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                        scheduleToAdd.IsOverride = NetworkObjectId == schNetworkObjectLink.NetworkObjectId ? scheduleToAdd.IsOverride : false;
                        scheduleToAdd.OverrideStatus = schNetworkObjectLink.OverrideStatus;
                        scheduleToAdd.ScheduleOrginatedAt = sNLWithRootLevelSchedule.NetworkObjectId;
                        scheduleToAdd.Priority = schNetworkObjectLink.Priority;
                        scheduleToAdd.ScheduleLinkedAt = schNetworkObjectLink.NetworkObjectId;
                        scheduleList.Add(scheduleToAdd);
                    }
                }
            }

            //Get the Last updated Date && Sort
            getScheduleLastUpdatedDate(scheduleList);

            return scheduleList.OrderByDescending(x => x.Priority).ToList();
        }

        /// <summary>
        /// Schedule List for a NW to view model
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <param name="scheduleList"></param>
        public List<SchNetworkObjectLink> GetSchNetworkLinkList(int networkObjectId,  bool includeInActives = false)
        {
            //NOTE: SchNetworkObjectLink is the entity/databasetable where a Schedule is linked with a NetworkObject and is overridden

            bool hadOverride = false;
            List<SchNetworkObjectLink> scheduleList = new List<SchNetworkObjectLink>();
            SchNetworkObjectLink schNetworkLinkToAdd = null;
            SchNetworkObjectLink schNetworkObjectLink = null;

            //1. Get list of SchNetworkObjectLink for this and parent NetworkObjectIds           
            var _schNetworkObjectLinkList = _schNetworkObjectLinkTable;
            if (_schNetworkObjectLinkList != null && _schNetworkObjectLinkList.Any())
            {
                //2. Get all root level SchNetworkObjectLinks from the list: sNOWithRootLevelSchedule_List
                var sNLWithRootLevelSchedule_List = _schNetworkObjectLinkList.Where(m => m.ParentScheduleId == null);

                //3. Get all scheduleOverrides
                var sNLWithScheduleOverride_List = _schNetworkObjectLinkList.Where(m => m.ParentScheduleId != null);

                //Get all overrides above this NW level
                var processedSNOWithScheduleOverrides_List = (from sNOWithScheduleOverride in sNLWithScheduleOverride_List
                                                              select new ovr<SchNetworkObjectLink> { OverrideStatus = sNOWithScheduleOverride.OverrideStatus, Id = sNOWithScheduleOverride.ParentScheduleId.Value, NetworkTypeId = sNOWithScheduleOverride.NetworkObject.NetworkObjectTypeId, Value = sNOWithScheduleOverride })
                               .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                foreach (var sNLWithRootLevelSchedule in sNLWithRootLevelSchedule_List)
                {
                    hadOverride = false;
                    schNetworkLinkToAdd = new SchNetworkObjectLink();
                    schNetworkObjectLink = sNLWithRootLevelSchedule;

                    if (processedSNOWithScheduleOverrides_List != null && processedSNOWithScheduleOverrides_List.Any())
                    {
                        //Run through all the rules to determine the value
                        if (includeInActives)
                        {
                            schNetworkObjectLink = GetSchOvrValue<SchNetworkObjectLink, ovr<SchNetworkObjectLink>>(sNLWithRootLevelSchedule.ScheduleId, processedSNOWithScheduleOverrides_List, sNLWithRootLevelSchedule, out hadOverride);
                        }
                        else
                        {
                            schNetworkObjectLink = GetOvrValue<SchNetworkObjectLink, ovr<SchNetworkObjectLink>>(sNLWithRootLevelSchedule.ScheduleId, processedSNOWithScheduleOverrides_List, sNLWithRootLevelSchedule, out hadOverride);
                        }
                    }

                    //delete override will be returned as Null hence, don't add if it is NULL
                    if (schNetworkObjectLink != null)
                    {
                        schNetworkLinkToAdd = schNetworkObjectLink;
                        schNetworkLinkToAdd.OverrideStatus = schNetworkObjectLink.OverrideStatus;
                        schNetworkLinkToAdd.Priority = schNetworkObjectLink.Priority;
                        scheduleList.Add(schNetworkLinkToAdd);
                    }
                }
            }
            return scheduleList.OrderByDescending(x => x.Priority).ToList();
        }

        private void getScheduleLastUpdatedDate(List<Schedule> scheduleList)
        {
            //1. Schedule can be updated at this or any networkObject Level above it. 
            //2. Level of the networkObject is identified by NetworkObjectType
            //3. In order to get the latest LastUpdatedDate of Schedule, check for the edits made the current networkObject Level and all parent levels and get the max of all LastUpdatedDates 

            foreach (var sch in scheduleList)
            {
                var currentSchNWLInk = _schNetworkObjectLinkTable.Where(x => x.ScheduleId == sch.ScheduleId).FirstOrDefault();
                if (currentSchNWLInk != null)
                {
                    //Get the original Schedule Id if it is overwritten
                    var orgSchId = currentSchNWLInk.ParentScheduleId.HasValue ? currentSchNWLInk.ParentScheduleId.Value : sch.ScheduleId;
                    if (_schNetworkObjectLinkTable.Any(x => (x.ScheduleId == orgSchId || (x.ParentScheduleId.HasValue && x.ParentScheduleId.Value == orgSchId)) && x.OverrideStatus != OverrideStatus.HIDDEN))
                    {
                        //3.2 Check 'SchLinkTable' for all the edits made to this schedule by networkObjectParents and get the 'last' (/max) updated date
                        var lastUpdatedDate = _schNetworkObjectLinkTable.Where(x => (x.ScheduleId == orgSchId || (x.ParentScheduleId.HasValue && x.ParentScheduleId.Value == orgSchId)) && x.OverrideStatus != OverrideStatus.HIDDEN).Max(x => x.LastUpdatedDate);
                        sch.LastUpdatedDate = lastUpdatedDate;
                    }
                }
            }

        }

        /// <summary>
        /// Gets 'SCHDETAILs in a SCHDETAIL' for a specific scheduleId
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>     
        public List<SchDetail> GetScheduleDetails(int scheduleId,int netId = 0)
        {
            //NOTE: SCHDETAIL is the entity/databasetable where a SCHDETAIL is added for a schedule and is overridden

            var returnSchDetailList = new List<SchDetail>();

            var schCycleAvailableForNetwork = GetScheduleCycles(netId != 0 ? netId : NetworkObjectId);
            //1. Get list of SchDetail for this and parent NetworkObjectIds for a specific scheduleId          
            var _schDetailList = _schDetailTable.Where(m => m.ScheduleId == scheduleId && schCycleAvailableForNetwork.Contains(m.SchCycle)).ToList();

            return _schDetailList == null ? returnSchDetailList : _schDetailList;
        }

        /// <summary>
        /// Gets 'SchCycles in a Network' 
        /// </summary>
        /// <param name="scheduleId"></param>
        /// <returns></returns>     
        public List<SchCycle> GetScheduleCycles(int networkId, bool includeInActives = false)
        {
            //NOTE: SchCycle is the entity/databasetable 

            var returnSchCycleList = new List<SchCycle>();

            bool hadOverride = false;
            SchCycle availbleSchCycle = null;
            SchCycle schCycle = null;

            //1. Get list of SchCycle for this and parent NetworkObjectIds for a specific networkId          
            var _schCycleList = _schCycleTable.Where(m => ParentNetworkNodesList.Contains(m.NetworkObjectId));
            if (_schCycleList != null && _schCycleList.Any())
            {
                //2. Get all root level SchCycles from the list: schCycleWithRootLevelScheduleCycle_List
                var schCycleWithRootLevelScheduleCycle_List = _schCycleList.Where(m => m.ParentSchCycleId == null);

                //3. Get all scheduleCycleOverrides
                var schCycleWithScheduleCycleOverride_List = _schCycleList.Where(m => m.ParentSchCycleId != null);

                //Get all overrides above this NW level
                var processedSchCycleWithScheduleCycleOverride_List = (from schCycleWithScheduleCycleOverride in schCycleWithScheduleCycleOverride_List
                                                                         select new ovr<SchCycle> { OverrideStatus = schCycleWithScheduleCycleOverride.OverrideStatus, Id = schCycleWithScheduleCycleOverride.ParentSchCycleId.Value, NetworkTypeId = schCycleWithScheduleCycleOverride.NetworkObject.NetworkObjectTypeId, Value = schCycleWithScheduleCycleOverride })
                                   .ToDictionary(r => r.Id.ToString() + "_" + r.NetworkTypeId.ToString(), r => r);


                foreach (var schCycleWithRootLevelScheduleCycle in schCycleWithRootLevelScheduleCycle_List)
                {
                    hadOverride = false;
                    availbleSchCycle = new SchCycle();
                    schCycle = schCycleWithRootLevelScheduleCycle;

                    if (processedSchCycleWithScheduleCycleOverride_List != null && processedSchCycleWithScheduleCycleOverride_List.Any())
                    {
                        //Run through all the rules to determine the value
                        if (includeInActives)
                        {
                            schCycle = GetSchOvrValue<SchCycle, ovr<SchCycle>>(schCycleWithRootLevelScheduleCycle.SchCycleId, processedSchCycleWithScheduleCycleOverride_List, schCycleWithRootLevelScheduleCycle, out hadOverride);
                        }
                        else
                        {
                            schCycle = GetOvrValue<SchCycle, ovr<SchCycle>>(schCycleWithRootLevelScheduleCycle.SchCycleId, processedSchCycleWithScheduleCycleOverride_List, schCycleWithRootLevelScheduleCycle, out hadOverride);
                        }
                    }

                    //if includeInActives is false, delete override will be returned as Null hence, don't add if it is NULL
                    if (schCycle != null)
                    {
                        availbleSchCycle = schCycle;
                        availbleSchCycle.IsOverride = (schCycle.ParentSchCycleId.HasValue && schCycle.ParentSchCycleId == schCycle.SchCycleId) || (schCycleWithRootLevelScheduleCycle.NetworkObjectId == schCycle.NetworkObjectId) || schCycle.OverrideStatus == OverrideStatus.MOVED ? false : hadOverride;
                        availbleSchCycle.IsOverride = NetworkObjectId == schCycle.NetworkObjectId ? availbleSchCycle.IsOverride : false;
                        availbleSchCycle.OverrideStatus = schCycle.OverrideStatus;
                        availbleSchCycle.CycleOrginatedAt = schCycleWithRootLevelScheduleCycle.NetworkObjectId;
                        returnSchCycleList.Add(availbleSchCycle);
                    }
                }
            }
            return returnSchCycleList.OrderBy(x => x.SortOrder).ToList();
        }

        /// <summary>
        /// returns Schedule details applicable for a particular item  in a Menu for current Network
        /// </summary>
        /// <param name="menuItemId"></param>
        /// <param name="scheduleId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<MenuItemScheduleLink> GetItemSchDetails(int menuItemId)
        {
            //NOTE: ItemScheduleLink is the entity/databasetable Item related SchDetail Information is added/overriden

            var returnItemScheduleLinkList = new List<MenuItemScheduleLink>();
            if (_callType == CallType.Web)
            {
                _itemId = menuItemId;
            }
            
            //1. Get list of ItemScheduleLinks for this and parent NetworkObjectIds
            var itemScheduleLinkList = _itemScheduleLinkTable.Where(m => m.ItemId == menuItemId).ToList();

            return itemScheduleLinkList == null ? returnItemScheduleLinkList : itemScheduleLinkList;
        }

        /// <summary>
        /// Get the details of Category in current schedule
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="scheduleId"></param>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<MenuCategoryScheduleLink> GetCatSchDetails(int categoryId)
        {
            //NOTE: CategoryScheduleLink is the entity/databasetable Category related SchDetail Information is added/overriden

            var returnCategoryScheduleLinkList = new List<MenuCategoryScheduleLink>();
            
            if (_callType == CallType.Web)
            {
                _categoryId = categoryId;
            }

            //1. Get list of CategoryScheduleLinks for this and parent NetworkObjectIds
            var categoryScheduleLinkList = _categoryScheduleLinkTable.Where(m => m.CategoryId == categoryId).ToList();

            return categoryScheduleLinkList == null ? returnCategoryScheduleLinkList : categoryScheduleLinkList;
        }


        /// <summary>
        /// This method is to get the Brand NetworkObjectId corresponding to the networkId provided
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public int GetBrandNetworkObjectId(int networkObjectId)
        {
            var brandNetworkObjectId = _repository.GetQuery<vwNetworkObjectTree>(p => p.Site == networkObjectId || p.Market == networkObjectId || p.Franchise == networkObjectId || p.Brand == networkObjectId || p.Root == networkObjectId).Select(p => p.Brand).FirstOrDefault();
            return brandNetworkObjectId.HasValue ? brandNetworkObjectId.Value : 0;
        }

        /// <summary>
        /// This method is to get the Brand NetworkObject corresponding to the networkId provided
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public NetworkObject GetBrandNetworkObject(int networkObjectId)
        {
            var brandNetworkObjectId = GetBrandNetworkObjectId(networkObjectId);

            return _repository.FindOne<NetworkObject>(x => x.NetworkObjectId == brandNetworkObjectId && x.NetworkObjectTypeId == NetworkObjectTypes.Brand);
        }

        /// <summary>
        /// Get list of specialNoticeTexts that are linked to this menu at this Network
        /// </summary>
        /// <param name="menuId"></param>
        /// <returns></returns>
        public List<string> GetMenuSpecialNotices(int menuId)
        {
            var specialNoticeMenuLinks = (from menuNetworkObjLink in _menuNetworkObjectLinkTable.Where(p => p.MenuId == menuId)
                                          join networkObject in _networkObjectTable on menuNetworkObjLink.NetworkObjectId equals networkObject.NetworkObjectId
                                          join specialNoticeMenuLink in _specialNoticeMenuLinkTable on menuNetworkObjLink.MenuNetworkObjectLinkId equals specialNoticeMenuLink.MenuNetworkObjectLinkId
                                          group new { menuNetworkObjLink, networkObject, specialNoticeMenuLink } by specialNoticeMenuLink.NoticeId into grouped
                                          select new { values = grouped.OrderByDescending(p => p.networkObject.NetworkObjectTypeId).FirstOrDefault() }).ToList();
            
            var specialNoticeTextlist = new List<string>();

            // Bug 4879 - Special Notices - allow apt in/out in menus
            // get the Brand NetworkObjectId corresponding to the networkId provided
            var brandNetworkObjId = GetBrandNetworkObjectId(_networkObjectId);

            //Get all Notices and mark IsLinkedToMenu = true if there is an existence of 'this' NoticeId in the list: specialNoticeIdlist
            var specialNotices = _repository.GetQuery<SpecialNotice>(p => p.NetworkObjectId == brandNetworkObjId);
            if (specialNotices != null && specialNotices.Any())
            {
                foreach (var specialNotice in specialNotices)
                {
                    var currentSpecialNoticeMenuLink = (specialNoticeMenuLinks != null && specialNoticeMenuLinks.Any()) ? specialNoticeMenuLinks.Where(p => p.values.specialNoticeMenuLink.NoticeId == specialNotice.NoticeId).Select(p => p.values.specialNoticeMenuLink).FirstOrDefault() : null;
                    if ((currentSpecialNoticeMenuLink != null) ? currentSpecialNoticeMenuLink.IsLinked : specialNotice.DefaultIncludeInMenu)
                    {
                        specialNoticeTextlist.Add(specialNotice.NoticeText);
                    }
                }
            }

            return specialNoticeTextlist.Any() ? specialNoticeTextlist : null;
        }
    }
}
