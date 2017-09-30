using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Phoenix.Common;
using Phoenix.DataAccess;
using System.Data.Entity;
using Phoenix.RuleEngine;
using System.Threading.Tasks;

namespace Phoenix.Web.Models
{
    public class CommonService : ICommonService
    {
        private IRepository _repository;
        private DbContext _context;
        private List<int> _parentNetworkNodes;
        private string _lastActionResult;
        private RuleService _ruleService;
        private List<MenuNetworkObjectLink> _mnuNetworkLinks;

        public List<int> ParentNetworkNodes
        {
            get { return _parentNetworkNodes; }
            set { _parentNetworkNodes = value; }
        }

        public string LastActionResult
        {
            get { return _lastActionResult; }
        }

        public CommonService(IRepository repository)
        {
            _context = new ProductMasterContext();
            _repository = repository;
            _parentNetworkNodes = new List<int>();
            _ruleService = new RuleService(RuleService.CallType.Web);
            _mnuNetworkLinks = new List<MenuNetworkObjectLink>();
        }

        /// <summary>
        /// Get parent Items for a Collection (as it recursive call, it is declared to pass list of collectionIds)
        /// </summary>
        /// <param name="collectionIdsToGetRestrictIds"></param>
        /// <param name="parentItemsToRestrict"></param>
        /// <returns></returns>
        public bool GetparentItemsFromCollection(List<int> collectionIdsToGetRestrictIds, List<int> parentItemsToRestrict)
        {
            var repeatLoop = false;
            var parentItemIds = new List<int>();
            var itmColLinks = _repository.GetQuery<ItemCollectionLink>(x => collectionIdsToGetRestrictIds.Contains(x.CollectionId) && _parentNetworkNodes.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            foreach (var itmCol in itmColLinks)
            {

                parentItemIds.Add(itmCol.ItemId);

            }

            parentItemsToRestrict.AddRange(parentItemIds);
            var newCollectionItems = _repository.GetQuery<ItemCollectionObject>(x => parentItemIds.Contains(x.ItemId) && _parentNetworkNodes.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId);

            //collectionItems.AddRange(newCollectionItems);
            var newcollectionIds = _repository.GetQuery<ItemCollectionObject>(x => newCollectionItems.Contains(x.ItemId) && !collectionIdsToGetRestrictIds.Contains(x.CollectionId) && _parentNetworkNodes.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.CollectionId).ToList();
            collectionIdsToGetRestrictIds.Clear();

            collectionIdsToGetRestrictIds.AddRange(newcollectionIds);

            if (collectionIdsToGetRestrictIds.Any())
            {
                repeatLoop = true;
            }
            return repeatLoop;
        }

        /// <summary>
        /// Get parent Items for a Item (as it recursive call, it is declared to pass list of itemIds)
        /// </summary>
        /// <param name="collectionIds"></param>
        /// <param name="parentItems"></param>
        /// <returns></returns>
        public bool GetparentItemsFromItem(List<int> itemIdsToGetRestrictIds, List<int> parentItemIdsToRestrict)
        {
            var repeatLoop = false;
            var parentCollectionIds = new List<int>();
            var itmColObjects = _repository.GetQuery<ItemCollectionObject>(x => itemIdsToGetRestrictIds.Contains(x.ItemId) && _parentNetworkNodes.Contains(x.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            foreach (var itmCol in itmColObjects)
            {

                parentCollectionIds.Add(itmCol.CollectionId);

            }
            var parentItemIds = _repository.GetQuery<ItemCollectionLink>(x => parentCollectionIds.Contains(x.CollectionId) && _parentNetworkNodes.Contains(x.NetworkObjectId) 
                && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId);
            
            if (parentItemIdsToRestrict.Any() == false)
            {
                parentItemIdsToRestrict.AddRange(itemIdsToGetRestrictIds);
            }
            parentItemIdsToRestrict.AddRange(parentItemIds);

            //clear the list and reset the Ids to new parentIds to search
            itemIdsToGetRestrictIds.Clear();
            itemIdsToGetRestrictIds.AddRange(parentItemIds);

            if (itemIdsToGetRestrictIds.Any())
            {
                repeatLoop = true;
            }
            return repeatLoop;
        }

        /// <summary>
        /// Get parent PrependItems for a Item (as it recursive call, it is declared to pass list of itemIds)
        /// </summary>
        /// <param name="collectionIds"></param>
        /// <param name="parentItems"></param>
        /// <returns></returns>
        public bool GetparentPrependItemsFromItem(List<int> itemIdsToGetRestrictIds, List<int> parentPrependItemIdsToRestrict)
        {
            var repeatLoop = false;
            var parentPrependItemIdsFound = new List<int>();
            var prependitemLinks = _repository.GetQuery<PrependItemLink>(x => itemIdsToGetRestrictIds.Contains(x.PrependItemId) && _parentNetworkNodes.Contains(x.MenuNetworkObjectLink.NetworkObjectId) && x.OverrideStatus != OverrideStatus.HIDDEN);
            foreach (var prependitemLink in prependitemLinks)
            {
                parentPrependItemIdsFound.Add(prependitemLink.ItemId);

                var ovrPrependItemId = prependitemLink.OverrideParentPrependItemId.HasValue ? prependitemLink.OverrideParentPrependItemId.Value : 0;
                if (ovrPrependItemId != 0 && !parentPrependItemIdsFound.Contains(ovrPrependItemId) && !parentPrependItemIdsToRestrict.Contains(ovrPrependItemId))
                {
                    parentPrependItemIdsFound.Add(ovrPrependItemId);
                }
            }
            var parentItemIds = _repository.GetQuery<ItemCollectionLink>(x => parentPrependItemIdsFound.Contains(x.CollectionId) && _parentNetworkNodes.Contains(x.NetworkObjectId)
                && x.OverrideStatus != OverrideStatus.HIDDEN).Select(x => x.ItemId);

            if (parentPrependItemIdsFound.Any())
            {
                // add the list of child Ids in every loop
                parentPrependItemIdsToRestrict.AddRange(parentPrependItemIdsFound);
                // clear the list all add new values for next search
                itemIdsToGetRestrictIds.Clear();
                itemIdsToGetRestrictIds.AddRange(parentPrependItemIdsFound);
                repeatLoop = true;
            }
            return repeatLoop;
        }

        /// <summary>
        /// Get Immediate children of the current Collection to exclude them from the list
        /// </summary>
        /// <param name="id"></param>
        /// <param name="childItemIds"></param>
        /// <param name="parentType"></param>
        public void GetImmediateItems(int id, List<int> childItemIds, MenuType parentType)
        {
            if (parentType == MenuType.ItemCollection)
            {
                //make sure this was not an override for another Collection
                var collObjovrride = _repository.GetQuery<ItemCollectionLink>(co => co.CollectionId == id && _parentNetworkNodes.Contains(co.NetworkObjectId) && co.OverrideStatus != OverrideStatus.HIDDEN).FirstOrDefault();
                if (collObjovrride != null && collObjovrride.ParentCollectionId != null)
                    id = collObjovrride.ParentCollectionId.Value;

                var links = _repository.GetQuery<ItemCollectionObject>(x => x.CollectionId == id && _parentNetworkNodes.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.ItemId == deletedbyParent.ItemId).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.ItemId);
                            if (deletedbyParent.ParentItemId.HasValue)
                                donotExcludeList.Add(deletedbyParent.ParentItemId.Value);
                        }
                    }
                }

                childItemIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.ItemId)).Select(x => x.ItemId));
                childItemIds.AddRange(links.Where(x => x.ParentItemId.HasValue && !donotExcludeList.Contains(x.ParentItemId.Value)).Select(x => x.ParentItemId.Value));
            }
            else if (parentType == MenuType.Category)
            {
                //make sure this was not an override for another category
                var ovrride = _repository.GetQuery<CategoryMenuLink>(co => co.CategoryId == id && _parentNetworkNodes.Contains(co.NetworkObjectId) && co.OverrideStatus != OverrideStatus.HIDDEN).FirstOrDefault();
                if (ovrride != null && ovrride.ParentCategoryId != null)
                {
                    id = ovrride.ParentCategoryId.Value;
                }
                else
                {
                    //make sure this was not an override for another category
                    var sovrride = _repository.GetQuery<SubCategoryLink>(co => co.SubCategoryId == id && co.OverrideStatus != OverrideStatus.HIDDEN).FirstOrDefault();
                    if (sovrride != null && sovrride.OverrideParentSubCategoryId != null)
                        id = sovrride.OverrideParentSubCategoryId.Value;
                }

                var links = _repository.GetQuery<CategoryObject>(x => x.CategoryId == id && _parentNetworkNodes.Contains(x.NetworkObjectId)).Include("NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.ItemId == deletedbyParent.ItemId).OrderByDescending(x => x.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.ItemId);
                            if (deletedbyParent.ParentItemId.HasValue)
                                donotExcludeList.Add(deletedbyParent.ParentItemId.Value);
                        }
                    }
                }

                childItemIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.ItemId)).Select(x => x.ItemId));
                childItemIds.AddRange(links.Where(x => x.ParentItemId.HasValue && !donotExcludeList.Contains(x.ParentItemId.Value)).Select(x => x.ParentItemId.Value));
            }

            else if (parentType == MenuType.Item)
            {
                //make sure this was not an override for another Item
                var ovrride = _repository.GetQuery<CategoryObject>(co => co.ItemId == id).FirstOrDefault();
                if (ovrride != null && ovrride.ParentItemId != null)
                {
                    id = ovrride.ParentItemId.Value;
                }
                else
                {
                    //make sure this was not an override for another Item
                    var collObjovrride = _repository.GetQuery<ItemCollectionObject>(co => co.ItemId == id).FirstOrDefault();
                    if (collObjovrride != null && collObjovrride.ParentItemId != null)
                        id = collObjovrride.ParentItemId.Value;
                }

                var links = _repository.GetQuery<PrependItemLink>(x => x.ItemId == id && _parentNetworkNodes.Contains(x.MenuNetworkObjectLink.NetworkObjectId)).Include("MenuNetworkObjectLink.NetworkObject").ToList();
                var donotExcludeList = new List<int>();
                if (links.Any(x => x.OverrideStatus == OverrideStatus.HIDDEN))
                {
                    foreach (var deletedbyParent in links)
                    {
                        var lastNWStatus = links.Where(x => x.PrependItemId == deletedbyParent.PrependItemId).OrderByDescending(x => x.MenuNetworkObjectLink.NetworkObject.NetworkObjectTypeId).FirstOrDefault();
                        if (lastNWStatus.OverrideStatus == OverrideStatus.HIDDEN)
                        {
                            donotExcludeList.Add(deletedbyParent.PrependItemId);
                            if (deletedbyParent.OverrideParentPrependItemId.HasValue)
                                donotExcludeList.Add(deletedbyParent.OverrideParentPrependItemId.Value);
                        }
                    }
                }

                childItemIds.AddRange(links.Where(x => !donotExcludeList.Contains(x.PrependItemId)).Select(x => x.PrependItemId));
                childItemIds.AddRange(links.Where(x => x.OverrideParentPrependItemId.HasValue && !donotExcludeList.Contains(x.OverrideParentPrependItemId.Value)).Select(x => x.OverrideParentPrependItemId.Value));
            }
        }

        /// <summary>
        /// Updates all the Menu and Networks LastUpdatedDate where these items are used
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public bool SetLastUpdatedDateofMenusUsingItems(List<int> itemIds)
        {
            bool retVal = false;
            try
            {
                if (itemIds.Any())
                {
                    //Get all object links where this Item is Added. If ParentItemId == null then it is added at that Network
                    var items = _repository.GetQuery<Item>(x => itemIds.Contains(x.ParentItemId.Value) && x.ParentItemId.HasValue && x.MenuId.HasValue).ToList();

                    var menuNetworkPairList = new List<Tuple<int, int>>();

                    foreach (var item in items)
                    {
                        //Get the menu n NW Pair where this item is used
                        var mnuNwPair = Tuple.Create(item.MenuId.Value, item.NetworkObjectId);
                        //If this is not in the List Add it
                        if (!menuNetworkPairList.Contains(mnuNwPair))
                        {
                            menuNetworkPairList.Add(mnuNwPair);
                        }
                    }

                    if (menuNetworkPairList.Any())
                    {
                        SetMenuNetworksDateUpdated(menuNWPairList: menuNetworkPairList, isGroupUpdate: true);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        /// <summary>
        /// Updates all the Menu and Networks LastUpdatedDate where these Cats are used
        /// </summary>
        /// <param name="itemIds"></param>
        /// <returns></returns>
        public bool SetLastUpdatedDateofMenusUsingCats(List<int> catIds)
        {
            bool retVal = false;
            try
            {
                if (catIds.Any())
                {
                    //Get all object links where this Item is Added. If ParentItemId == null then it is added at that Network
                    var catMnuLinks = _repository.GetQuery<CategoryMenuLink>(x => catIds.Contains(x.CategoryId) && x.ParentCategoryId == null).Include("Category").ToList();
                    var subCatLinks = _repository.GetQuery<SubCategoryLink>(x => catIds.Contains(x.SubCategoryId) && x.OverrideParentSubCategoryId == null).Include("SubCategory").ToList();

                    var menuNetworkPairList = new List<Tuple<int, int>>();

                    foreach (var catMnuLink in catMnuLinks)
                    {
                        //Get the menu n NW Pair where this item is used
                        var mnuNwPair = Tuple.Create(catMnuLink.Category.MenuId, catMnuLink.NetworkObjectId);
                        //If this is not in the List Add it
                        if (!menuNetworkPairList.Contains(mnuNwPair))
                        {
                            menuNetworkPairList.Add(mnuNwPair);
                        }
                    }
                    foreach (var subCatLink in subCatLinks)
                    {
                        //Get the menu n NW Pair where this item is used
                        var mnuNwPair = Tuple.Create(subCatLink.SubCategory.MenuId, subCatLink.NetworkObjectId);
                        //If this is not in the List Add it
                        if (!menuNetworkPairList.Contains(mnuNwPair))
                        {
                            menuNetworkPairList.Add(mnuNwPair);
                        }
                    }

                    if (menuNetworkPairList.Any())
                    {
                        SetMenuNetworksDateUpdated(menuNWPairList: menuNetworkPairList, isGroupUpdate: true);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        public bool SetLastUpdatedDateofMenusUsingItemsatCurrentNW(List<int> itemIds, int netId)
        {
            bool retVal = false;
            try
            {
                if (itemIds.Any())
                {
                    //Get all object links where this Item is Added. If ParentItemId == null then it is added at that Network
                    var menuIds = _repository.GetQuery<CategoryObject>(x => itemIds.Contains(x.ItemId) && x.ParentItemId == null).Include("Category").Select(x => x.Category.MenuId).Distinct().ToList();
                    menuIds.AddRange(_repository.GetQuery<ItemCollectionObject>(x => itemIds.Contains(x.ItemId) && x.ParentItemId == null).Include("ItemCollection").Select(x => x.ItemCollection.MenuId).Distinct().ToList());

                    var menuNetworkPairList = new List<Tuple<int, int>>();
                    var menus = _repository.GetQuery<Menu>(x => menuIds.Contains(x.MenuId)).ToList();
                    if (ParentNetworkNodes.Count() == 0)
                    {
                        ParentNetworkNodes = _ruleService.GetNetworkParents(netId);
                    }
                    foreach (var mnu in menus)
                    {
                        var networkId = netId;
                        if (!ParentNetworkNodes.Contains(mnu.NetworkObjectId))
                        {
                            networkId = mnu.NetworkObjectId;
                        }
                        //Get the menu n NW Pair where this item is used
                        var mnuNwPair = Tuple.Create(mnu.MenuId, networkId);
                        //If this is not in the List Add it
                        if (!menuNetworkPairList.Contains(mnuNwPair))
                        {
                            menuNetworkPairList.Add(mnuNwPair);
                        }
                    }

                    if (menuNetworkPairList.Any())
                    {
                        SetMenuNetworksDateUpdated(menuNWPairList: menuNetworkPairList, isGroupUpdate: true);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        public bool SetLastUpdatedDateofMenusUsingCatsatCurrentNW(List<int> catIds,int netId)
        {
            bool retVal = false;
            try
            {
                if (catIds.Any())
                {
                    //Get all object links where this Item is Added. If ParentItemId == null then it is added at that Network
                    var menuIds = _repository.GetQuery<CategoryMenuLink>(x => catIds.Contains(x.CategoryId) && x.ParentCategoryId == null).Select(x => x.MenuId).Distinct().ToList();
                    menuIds.AddRange(_repository.GetQuery<SubCategoryLink>(x => catIds.Contains(x.SubCategoryId) && x.OverrideParentSubCategoryId == null).Include("SubCategory").Select(x => x.SubCategory.MenuId).Distinct().ToList());

                    var menuNetworkPairList = new List<Tuple<int, int>>();

                    foreach (var mnuId in menuIds.Distinct())
                    {
                        //Get the menu n NW Pair where this item is used
                        var mnuNwPair = Tuple.Create(mnuId, netId);
                        //If this is not in the List Add it
                        if (!menuNetworkPairList.Contains(mnuNwPair))
                        {
                            menuNetworkPairList.Add(mnuNwPair);
                        }
                    }

                    if (menuNetworkPairList.Any())
                    {
                        SetMenuNetworksDateUpdated(menuNWPairList: menuNetworkPairList, isGroupUpdate: true);
                    }
                }
                retVal = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return retVal;
        }

        /// <summary>
        /// Updates all the Menu and Networks LastUpdatedDate where these assets are used
        /// </summary>
        /// <param name="assetIds"></param>
        /// <returns></returns>
        public bool SetLastUpdatedDateofMenusUsingAssets(List<int> assetIds)
        {
            bool retVal = true;
            try
            {
                if (assetIds.Any())
                {
                    //Get all object links where this Item is Added. If ParentItemId == null then it is added at that Network
                    var assetCatIds = _repository.GetQuery<AssetCategoryLink>(x => assetIds.Contains(x.AssetId) && x.CategoryId.HasValue).Select(x => x.CategoryId.Value).Distinct().ToList();
                    var assetItemIds = _repository.GetQuery<AssetItemLink>(x => assetIds.Contains(x.AssetId) && x.ItemId.HasValue).Select(x => x.ItemId.Value).Distinct().ToList();
                    if (assetCatIds.Any())
                    {
                        retVal = SetLastUpdatedDateofMenusUsingCats(assetCatIds);
                    }
                    if (assetItemIds.Any())
                    {
                        retVal = SetLastUpdatedDateofMenusUsingItems(assetItemIds);
                    }
                }
            }
            catch (Exception ex)
            {
                retVal = false;
                throw ex;
            }
            return retVal;
        }

        /// <summary>
        /// Updates all the Menu and Networks LastUpdatedDate where these tags are used
        /// </summary>
        /// <param name="tagIds"></param>
        /// <returns></returns>
        public bool SetLastUpdatedDateofMenusUsingTags(List<int> tagIds, TagKeys entityType)
        {
            bool retVal = true;
            try
            {
                if (tagIds.Any())
                {
                    if (entityType == TagKeys.Tag)
                    {
                        var assetIds = _repository.GetQuery<TagAssetLink>(x => tagIds.Contains(x.TagId)).Select(x => x.AssetId).Distinct().ToList();
                        if (assetIds.Any())
                        {
                            retVal = SetLastUpdatedDateofMenusUsingAssets(assetIds);
                        }
                    }
                    else if(entityType == TagKeys.Channel)
                    {
                        var menuNetworkPairList = new List<Tuple<int, int>>();
                        var menuTagLinks = _repository.GetQuery<MenuTagLink>(x => tagIds.Contains(x.TagId) && x.OverrideStatus != OverrideStatus.HIDDEN).Distinct().ToList();
                        foreach (var menutagLink in menuTagLinks)
                        {
                            
                            //Get the menu n NW Pair where this Tag is used
                            var mnuNwPair = Tuple.Create(menutagLink.MenuId, menutagLink.NetworkObjectId);
                            //If this is not in the List Add it
                            if (!menuNetworkPairList.Contains(mnuNwPair))
                            {
                                menuNetworkPairList.Add(mnuNwPair);
                            }
                        }

                        if (menuNetworkPairList.Any())
                        {
                            SetMenuNetworksDateUpdated(menuNWPairList: menuNetworkPairList, isGroupUpdate: true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                retVal = false;
                throw ex;
            }
            return retVal;
        }

        /// <summary>
        /// Sets updated date for a particular Network and Menu. Pass Menu and Network for particular menu and Network, pass only network id only to update all menus in that network Tree, pass menuNwPair for group update
        /// </summary>
        /// <param name="menuid">Menu Id</param>
        /// <param name="netid"></param>
        /// <param name="isOperationDirectlyOnMenu"></param>
        /// <param name="menuNWPairList"></param>
        /// <param name="isGroupUpdate"></param>
        public void SetMenuNetworksDateUpdated(int menuid = 0, int netid = 0, bool isOperationDirectlyOnMenu = true, bool isRevertMenu = false, List<Tuple<int, int>> menuNWPairList = null, bool isGroupUpdate = false)
        {
            //If Menu is not provided and Network is provided - Update all Menus in the network
            if (netid != 0 && menuid == 0)
            {
                var menusInUnderNetwork = new List<Menu>();
                _ruleService.NetworkObjectId = netid;
                _ruleService.GetAllMenusInNetworkTree(netid, menusInUnderNetwork);
                ParentNetworkNodes = _ruleService.ParentNetworkNodesList;
                foreach (var menu in menusInUnderNetwork)
                {
                    var isMenuOverriden = false;
                    //If is a menu operation and the menu is not created at this network level then set Menu is overriden
                    if (isRevertMenu == false && isOperationDirectlyOnMenu && menu != null)
                    {
                        isMenuOverriden = menu.NetworkObjectId == netid ? false : true;
                    }

                    //if the menu is created at parent then update the date only at current MNUNetwork as this menu is being modified because of change at this network.
                    var isParentMenu = (ParentNetworkNodes.Contains(menu.NetworkObjectId) || menu.NetworkObjectId == netid) ? true : false;

                    // check if this network + menu has link already
                    var menuNWLink = _repository.GetQuery<MenuNetworkObjectLink>(l => l.NetworkObjectId == (isParentMenu ? netid : menu.NetworkObjectId) && l.MenuId == menu.MenuId).FirstOrDefault();
                    if (menuNWLink == null)
                    {
                        // create a link
                        menuNWLink = new MenuNetworkObjectLink
                        {
                            NetworkObjectId = netid,
                            MenuId = menu.MenuId,
                            LastUpdatedDate = DateTime.UtcNow,
                            IsPOSMapped = false,
                            IsMenuOverriden = isMenuOverriden
                        };
                        _repository.Add<MenuNetworkObjectLink>(menuNWLink);
                    }
                    else
                    {
                        //update the flag only if it already false.
                        menuNWLink.IsMenuOverriden = isRevertMenu ? false : (menuNWLink.IsMenuOverriden ? menuNWLink.IsMenuOverriden : isMenuOverriden);
                        // update timestamp
                        menuNWLink.LastUpdatedDate = DateTime.UtcNow;
                        _repository.Update<MenuNetworkObjectLink>(menuNWLink);
                    }
                }
            }
            //If both Menu and Network is provided - Update specific Menu in the network
            else if (menuid != 0 && netid != 0)
            {
                var menu = _repository.GetQuery<Menu>(m => m.MenuId == menuid).FirstOrDefault();
                var isMenuOverriden = false;
                //If is not a revert and the menu is not created at this network level then set Menu is overriden
                if (isRevertMenu == false && isOperationDirectlyOnMenu && menu != null)
                {
                    isMenuOverriden = menu.NetworkObjectId == netid ? false : true;
                }

                // check if this site + menu has link already
                var menuNWLink = _repository.GetQuery<MenuNetworkObjectLink>(l => l.NetworkObjectId == netid && l.MenuId == menuid).FirstOrDefault();
                if (menuNWLink == null)
                {
                    // create a link
                    menuNWLink = new MenuNetworkObjectLink
                    {
                        NetworkObjectId = netid,
                        MenuId = menuid,
                        LastUpdatedDate = DateTime.UtcNow,
                        IsPOSMapped = false,
                        IsMenuOverriden = isMenuOverriden
                    };
                    _repository.Add<MenuNetworkObjectLink>(menuNWLink);
                }
                else
                {
                    //update the flag only if it already false.
                    menuNWLink.IsMenuOverriden =  isRevertMenu ? false : (menuNWLink.IsMenuOverriden ? menuNWLink.IsMenuOverriden : isMenuOverriden);
                    // update timestamp
                    menuNWLink.LastUpdatedDate = DateTime.UtcNow;
                    _repository.Update<MenuNetworkObjectLink>(menuNWLink);
                }
            }
            //If Group update of Menu and Network is provided - Update all the specific Menus in the specific networks
            else if (isGroupUpdate && menuNWPairList != null && menuNWPairList.Any())
            {
                if (_mnuNetworkLinks.Count() == 0)
                {
                    _mnuNetworkLinks = _repository.GetAll<MenuNetworkObjectLink>().ToList();
                }
                //Get all the links where the 2-tuple is present
                var mnuNWLinks = _mnuNetworkLinks.Where(x => x.MenuId.HasValue && menuNWPairList.Contains(Tuple.Create(x.MenuId.Value, x.NetworkObjectId))).ToList();

                //Update the last updated for all MenuNetworkObjectLinks so that the menus will be sync
                mnuNWLinks.ForEach(x => x.LastUpdatedDate = DateTime.UtcNow);
            }
        }

        /// <summary>
        /// To cleanup all orphan entities (overrides that are removed from menu)
        /// </summary>
        /// <param name="orphanRecords"></param>
        /// <returns></returns>
        public async Task DeleteOrphanEntitiesAsync(CleanUpDataModel orphanRecords)
        {
            try
            {
                //send back to caller
                await Task.Delay(1);
                
                // As this is background call, get its own new repository
                _repository = new GenericRepository(_context);
                _context.Configuration.AutoDetectChangesEnabled = false;
                _repository.UnitOfWork.BeginTransaction();

                var needChildrenCleanUp = false;
                var childrenToCleanup = new CleanUpDataModel();

                if (orphanRecords.CategoryIds.Any())
                {
                    var categoryMenuLinks = _repository.GetQuery<CategoryMenuLink>(x => orphanRecords.CategoryIds.Contains(x.CategoryId)).ToList();
                    var subCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => orphanRecords.CategoryIds.Contains(x.SubCategoryId)).ToList();
                    var orphanCategoryIds = new List<int>();
                    foreach (var categoryId in orphanRecords.CategoryIds.Distinct())
                    {
                        //If this Category is not used anywhere else. And not included as sub category anywhere
                        //Assuming that all the sort overrides and delete overrides of this category are cleaned up
                        if (categoryMenuLinks.Where(x => x.CategoryId == categoryId).Count() == 0 && subCategoryLinks.Where(x => x.SubCategoryId == categoryId).Count() == 0)
                        {
                            orphanCategoryIds.Add(categoryId);
                        }
                    }
                    if (orphanCategoryIds.Any())
                    {
                        var categoryObjects = _repository.GetQuery<CategoryObject>(x => orphanCategoryIds.Contains(x.CategoryId)).ToList();
                        var childSubCategoryLinks = _repository.GetQuery<SubCategoryLink>(x => orphanCategoryIds.Contains(x.CategoryId)).ToList();
                        if (categoryObjects.Any())
                        {
                            needChildrenCleanUp = true;
                            childrenToCleanup.ItemIds.AddRange(categoryObjects.Select(x => x.ItemId).ToList());
                            _repository.Delete<CategoryObject>(co => orphanCategoryIds.Contains(co.CategoryId));
                        }
                        if (childSubCategoryLinks.Any())
                        {
                            needChildrenCleanUp = true;
                            childrenToCleanup.CategoryIds.AddRange(childSubCategoryLinks.Select(x => x.SubCategoryId).ToList());
                            _repository.Delete<SubCategoryLink>(co => orphanCategoryIds.Contains(co.CategoryId));
                        }
                        _repository.Delete<MenuCategoryCycleInSchedule>(co => orphanCategoryIds.Contains(co.MenuCategoryScheduleLink.CategoryId));
                        _repository.Delete<MenuCategoryScheduleLink>(co => orphanCategoryIds.Contains(co.CategoryId));
                        _repository.Delete<AssetCategoryLink>(co => co.CategoryId.HasValue && orphanCategoryIds.Contains(co.CategoryId.Value));
                        _repository.Delete<Category>(co => orphanCategoryIds.Contains(co.CategoryId));
                    }
                }

                if (orphanRecords.ItemIds.Any())
                {
                    var categoryObjects = _repository.GetQuery<CategoryObject>(x => orphanRecords.ItemIds.Contains(x.ItemId)).ToList();
                    var itemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => orphanRecords.ItemIds.Contains(x.ItemId)).ToList();
                    var prependLinks = _repository.GetQuery<PrependItemLink>(x => orphanRecords.ItemIds.Contains(x.PrependItemId)).ToList();
                    var orphanItemIds = new List<int>();
                    var allItems = _repository.GetQuery<Item>(x => orphanRecords.ItemIds.Contains(x.ItemId)).ToList();
                    foreach (var itemId in orphanRecords.ItemIds.Distinct())
                    {
                        var currentItem = allItems.Where(x => x.ItemId == itemId).FirstOrDefault();
                        //Only overrides can be deleted
                        if (currentItem != null && currentItem.ParentItemId != null)
                        {
                            //If this Item is not used anywhere else. And not included in any other collection anywhere
                            if (categoryObjects.Where(x => x.ItemId == itemId).Count() == 0 && itemCollectionObjects.Where(x => x.ItemId == itemId).Count() == 0 && prependLinks.Where(x => x.PrependItemId == itemId).Count() == 0)
                            {
                                orphanItemIds.Add(itemId);
                            }
                        }
                    }
                    if (orphanItemIds.Any())
                    {
                        var childItemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => orphanItemIds.Contains(x.ItemId)).ToList();
                        var childPrependItemLinks = _repository.GetQuery<PrependItemLink>(x => orphanItemIds.Contains(x.ItemId)).ToList();
                        if (childItemCollectionLinks.Any())
                        {
                            needChildrenCleanUp = true;
                            childrenToCleanup.CollectionIds.AddRange(childItemCollectionLinks.Select(x => x.CollectionId).ToList());
                            _repository.Delete<ItemCollectionLink>(co => orphanItemIds.Contains(co.ItemId));
                        }
                        if (childPrependItemLinks.Any())
                        {
                            needChildrenCleanUp = true;
                            childrenToCleanup.ItemIds.AddRange(childPrependItemLinks.Select(x => x.PrependItemId).ToList());
                            _repository.Delete<PrependItemLink>(co => orphanItemIds.Contains(co.ItemId));
                        }
                        _repository.Delete<MenuItemCycleInSchedule>(co => orphanItemIds.Contains(co.MenuItemScheduleLink.ItemId));
                        _repository.Delete<MenuItemScheduleLink>(co => orphanItemIds.Contains(co.ItemId));
                        _repository.Delete<AssetItemLink>(co => co.ItemId.HasValue && orphanItemIds.Contains(co.ItemId.Value));
                        _repository.Delete<ItemDescription>(co => orphanItemIds.Contains(co.ItemId));
                        _repository.Delete<ItemPOSDataLink>(co => orphanItemIds.Contains(co.ItemId));
                        _repository.Delete<TempSchedule>(co => orphanItemIds.Contains(co.ItemId));
                        _repository.Delete<Item>(co => orphanItemIds.Contains(co.ItemId));
                    }
                }

                if (orphanRecords.CollectionIds.Any())
                {
                    var itemCollectionLinks = _repository.GetQuery<ItemCollectionLink>(x => orphanRecords.CollectionIds.Contains(x.CollectionId)).ToList();
                    var orphanCollectionIds = new List<int>();
                    foreach (var collectionId in orphanRecords.CollectionIds.Distinct())
                    {
                        //If this Collection is not used anywhere else.
                        //Assuming that all the sort overrides and delete overrides of this Collection are cleaned up
                        if (itemCollectionLinks.Where(x => x.CollectionId == collectionId).Count() == 0)
                        {
                            orphanCollectionIds.Add(collectionId);
                        }
                    }
                    if (orphanCollectionIds.Any())
                    {
                        var childItemCollectionObjects = _repository.GetQuery<ItemCollectionObject>(x => orphanCollectionIds.Contains(x.CollectionId)).ToList();
                        if (childItemCollectionObjects.Any())
                        {
                            needChildrenCleanUp = true;
                            childrenToCleanup.ItemIds.AddRange(childItemCollectionObjects.Select(x => x.ItemId).ToList());
                            _repository.Delete<ItemCollectionLink>(co => orphanCollectionIds.Contains(co.CollectionId));
                        }
                        _repository.Delete<ItemCollection>(co => orphanCollectionIds.Contains(co.CollectionId));
                    }
                }
                _context.SaveChanges();
                _repository.UnitOfWork.CommitTransaction();
                _lastActionResult = string.Format("{0} . ItemIds Deleted : {1}, CategoryIds deleted: {2}, CollectionIds deleted : {3}", Constants.AuditMessage.BackgroundCleanupComplete, string.Join(",", orphanRecords.ItemIds), string.Join(",", orphanRecords.CategoryIds), string.Join(",", orphanRecords.CollectionIds));
                Logger.WriteAudit(_lastActionResult);

                if (needChildrenCleanUp)
                {
                    var deleteorphanstask = new Task(() => DeleteOrphanEntitiesAsync(childrenToCleanup));
                    deleteorphanstask.Start();
                }
            }
            catch (Exception ex)
            {
                _lastActionResult = string.Format(Constants.StatusMessage.ErrBackgroundCleanup);
                Logger.WriteError(ex);
                _repository.UnitOfWork.RollBackTransaction();
            }
        }

        /// <summary>
        /// returns IrisIds of sites under the provide NetworkObjectIds
        /// </summary>      
        /// <returns></returns>
        public List<NetworkObject> GetSitesUnderNetworkObjects(List<int> networkObjectIds)
        {
            var siteList = GetSiteIdsUnderNetworkObjects(networkObjectIds);

            //After getting the distinct network objects'(sites') id, join the collection with NetworkObject table
            var siteIdList = _repository.GetQuery<NetworkObject>(p => siteList.Contains(p.NetworkObjectId)).ToList();

            return siteIdList;
        }

        /// <summary>
        /// returns sites under selected NetworkObjectIds
        /// </summary>      
        /// <returns></returns>
        public List<int> GetSiteIdsUnderNetworkObjects(List<int> networkObjectIds)
        {
            //This network object Id is the id of network object which could be at any network object level
            var siteList = _repository.GetQuery<vwNetworkObjectTree>(p => p.Site.HasValue && ((networkObjectIds.Contains(p.Root)) || (p.Brand.HasValue && networkObjectIds.Contains(p.Brand.Value)) || (p.Franchise.HasValue && networkObjectIds.Contains(p.Franchise.Value)) || (p.Market.HasValue && networkObjectIds.Contains(p.Market.Value)) || (p.Site.HasValue && networkObjectIds.Contains(p.Site.Value)))).Distinct().Select(p => p.Site.Value);

            return siteList.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemDetail"></param>
        /// <returns></returns>
        public List<Dropdown> MapPOSDataDropdown(Item itemDetail)
        {
            var posdataList = new List<Dropdown>();
            posdataList.Add(new Dropdown
            {
                Id = 0,
                Value = 0,
                Text = "Placeholder"
            });
            if (itemDetail.MasterItem != null && itemDetail.MasterItem.ItemPOSDataLinks.Any())
            {
                foreach (var posdatalink in itemDetail.MasterItem.ItemPOSDataLinks)
                {
                    if (posdatalink.POSData != null)
                    {
                        var altText = string.IsNullOrWhiteSpace(posdatalink.POSData.AlternatePLU) ? "" : " - " + posdatalink.POSData.AlternatePLU;
                        posdataList.Add(new Dropdown
                        {
                            Id = posdatalink.ItemPOSDataLinkId,
                            Value = posdatalink.POSDataId.Value,
                            Text = posdatalink.POSData.POSItemName + (posdatalink.POSData.PLU.HasValue ? " - " + posdatalink.POSData.PLU : "") + altText
                        });
                    }
                }
            }

            return posdataList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemDetail"></param>
        /// <returns></returns>
        public List<Dropdown> MapPOSDataDropdown(List<ItemPOSDataLink> itemPOSDataLinks)
        {
            var posdataList = new List<Dropdown>();
            posdataList.Add(new Dropdown
            {
                Id = 0,
                Value = 0,
                Text = "Placeholder"
            });
            if (itemPOSDataLinks.Any())
            {
                foreach (var posdatalink in itemPOSDataLinks)
                {
                    if (posdatalink.POSData != null)
                    {
                        var altText = string.IsNullOrWhiteSpace(posdatalink.POSData.AlternatePLU) ? "" : " - " + posdatalink.POSData.AlternatePLU;
                        posdataList.Add(new Dropdown
                        {
                            Id = posdatalink.ItemPOSDataLinkId,
                            Value = posdatalink.POSDataId.Value,
                            Text = posdatalink.POSData.POSItemName + (posdatalink.POSData.PLU.HasValue ? " - " + posdatalink.POSData.PLU : "") + altText
                        });
                    }
                }
            }

            return posdataList;
        }
        /// <summary>
        /// Check whether requested feature is enabled for the network
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        public bool CheckFeatureEnabled(int networkObjectId, NetworkFeaturesSet feature)
        {
            var retVal = false;

            var brand = _ruleService.GetBrandNetworkObject(networkObjectId);
            if (brand != null)
            {
                //Check whether brand has requested feature
                var brandFeature = brand.FeaturesSet & (int)feature;
                if (brandFeature == (int)feature)
                {
                    retVal = true;
                }
            }
            return retVal;
        }
    }
    public interface ICommonService
    {
        string LastActionResult { get; }
        List<int> ParentNetworkNodes { get; set; }
        bool GetparentItemsFromCollection(List<int> collectionIds, List<int> parentItems);
        void GetImmediateItems(int id, List<int> childItems, MenuType parentType);
        bool SetLastUpdatedDateofMenusUsingItems(List<int> itemIds);
        bool SetLastUpdatedDateofMenusUsingAssets(List<int> assetIds);
        bool SetLastUpdatedDateofMenusUsingCats(List<int> catIds);
        bool SetLastUpdatedDateofMenusUsingCatsatCurrentNW(List<int> catIds, int netId);
        bool SetLastUpdatedDateofMenusUsingItemsatCurrentNW(List<int> itemIds, int netId);
        void SetMenuNetworksDateUpdated(int menuid = 0, int netid = 0, bool isOperationDirectlyOnMenu = true,bool isRevertMenu = false, List<Tuple<int, int>> mnuNWPairList = null, bool isGroupUpdate = false);
        bool SetLastUpdatedDateofMenusUsingTags(List<int> tagIds, TagKeys entityType);
        Task DeleteOrphanEntitiesAsync(CleanUpDataModel orphanRecords);
        List<NetworkObject> GetSitesUnderNetworkObjects(List<int> networkObjectIds);
        List<int> GetSiteIdsUnderNetworkObjects(List<int> networkObjectIds);

        bool GetparentItemsFromItem(List<int> itemIdsToRestrict, List<int> parentItemIdsToRestrict);
        bool GetparentPrependItemsFromItem(List<int> itemIdsToGetRestrictIds, List<int> parentPrependItemIdsToRestrict);
        List<Dropdown> MapPOSDataDropdown(List<ItemPOSDataLink> itemPOSDataLinks);
        List<Dropdown> MapPOSDataDropdown(Item itemDetail);
        bool CheckFeatureEnabled(int networkObjectId, NetworkFeaturesSet feature);
    }
}