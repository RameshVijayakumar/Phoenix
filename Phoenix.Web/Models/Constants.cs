using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class Constants
    {
        public static class IrisConstants
        {
            public const string IrisIdContainerName = "iriscache";
            public const string IrisIdCommonScopeName = "iriscommonscope";
        }

        public static class ZioskClaimTypes
        {
            public const string Email = "Email";
            public const string Profile = "Profile";
            public const string IsBrandLevelAdmin = "IsBrandLevelAdmin";
            public const string HighestLevelAccess = "HighestLevelAccess";
            public const string Roles = "Roles";
        }

        public static class AuditMessage
        {
            public const string SiteCreateT = "Site '{0}' is created";
            public const string GroupCreateT = "Created group '{0}' under group '{1}'";
            public const string SiteUpdatedT = "Site '{0}' updated";
            public const string GroupUpdatedT = "Group '{0}' updated";
            public const string SiteDeletedT = "Site '{0}' is deleted";
            public const string GroupDeletedT = "Group '{0}' is deleted";

            // asset service
            public const string AssetCreateT = "Created asset '{0}' under Brand '{1}'";
            public const string AssetVersionUpdateT = "Updated asset '{0}' to current asset";
            public const string AssetItemLinkCreateT = "Added/Updated item '{0}' with asset '{1}'";
            public const string AssetItemLinkDeletedT = "Removed Asset '{0}' from Item(s) '{1}'";
            public const string AssetItemLinksDeletedT = "Items updated";
            public const string AssetDeletedT = "Asset '{0}' is deleted";
            public const string VersionDeletedT = "Asset(Version) '{0}' is deleted";
            public const string AssetCategoryLinkCreateT = "Added/Updated Category '{0}' with asset '{1}'";
            public const string AssetCategoryLinkDeletedT = "Removed Asset '{0}' from Category(s) '{1}'";
            public const string AssetCategoryLinksDeletedT = "Categories updated";

            // tag service                               
            public const string TagLinked = "Tag(s) linked";
            public const string TagRemoved = "Tag(s) removed";
            public const string TagLinkedT = "Tag(s) linked to Asset(s): {0}";
            public const string TagRemovedT = "Tag(s) removed from Asset(s): {0}";
            public const string TagSavedNLinkedT = "Tag '{0}' is saved and linked";
            public const string TagDeletedT = "{1}(s) deleted: '{0}'";
            public const string TagCreatedT = "{1} '{0}' is created";
            public const string TagUpdatedT = "{1} '{0}' is updated";
            public const string TagMovedT = "{1} '{0}' is moved";
            public const string GetTagAssociatedData = "Fetched {0} associated information";

            // Menu Sync Target                                    
            public const string TargetDeletedT = "Target '{0}' is deleted";
            public const string TargetCreatedT = "Target '{0}' is created";
            public const string TargetUpdatedT = "Target '{0}' is updated";
            public const string TargetSyncSuccessT = "Target '{0}' is synchronized";
            public const string TargetSyncSuccessAuditT = "Target '{0}' is synchronized. ({1}) sites involved in this synchronization";
            public const string SyncHistoryDeletedT = "Deleted sync history of selected sites";
            public const string SyncHistoryDeleteInvalidSitesT = "Unable to delete sync history as valid sites are not selected";
            public const string SyncHistoryDeletedDetailT = "Deleted sync history of sites under : {0}";
            public const string TargetRefreshedT = "Targets Refreshed";

            // Special Notice                                    
            public const string SpecialNoticeDeletedT = "Special Notice(s) deleted: '{0}'";
            public const string SpecialNoticeCreatedT = "Special Notice '{0}' is created";
            public const string SpecialNoticeUpdatedT = "Special Notice '{0}' is updated";
            public const string SpecialNoticeLinked = "Selected Special Notices are linked";
            public const string SpecialNoticeRemoved = "Unselected Special Notices are removed";
            public const string SpecialNoticeLinkedNRemoved = "Selected Special Notices are linked/removed";    
            public const string SpecialNoticeLinkedT = "Special Notices linked to menu {0} : {1}";
            public const string SpecialNoticeLinkRemovedT = "Special Notices removed from menu {0} : {1}";
            public const string SpecialNoticeLinkNRemoveT = "Special Notices linked and removed from menu {0}. Linked : {1}, Removed: {2}";  

            //Menu Service
            public const string MenuCreate = "Created Menu '{0}'";
            public const string MenuUpdate = "Updated Menu '{0}'.";
            public const string MenuDelete = "Deleted Menu '{0}'";
            public const string EntityTagUpdated = " Channels(s) Added : {0}, Channels(s) Removed : {1}";
            public const string CatObjectLinkAddT = "Added Item(s) '{0}' to Category '{1}'";
            public const string CatObjectLinkExistT = "Item(s) '{0}' already present in Category '{1}'";
            public const string CatObjectLinkDeleteT = "Deleted Item '{0}' from Category '{1}'";
            public const string CatCreateT = "Created Category '{0}'";
            public const string CatUpdateT = "Updated Category '{0}'";
            public const string CatDeleteT = "Removed Category '{0}' from menu '{1}'";
            public const string CatObjMoveT = "Changed the position of Item '{0}'";
            public const string CatObjMoveDetailT = "Changed the position of Item '{0}' in Category '{1}'";
            public const string CatMoveT = "Changed the position of Category '{0}'";
            public const string CatMoveDetailT = "Changed the position of Category '{0}' in Category '{1}'";
            public const string ItemCollnMoveT = "Changed the position of Collection '{0}'";
            public const string ColDeleteT = "Removed Collection '{0}' from Item '{1}'";
            public const string ItemCreateT = "Created Item '{0}'";
            public const string ItemUpdateT = "Updated Item '{0}'";
            public const string ItemColLinkNotAddedT = "Collection(s) '{0}' are not Eligible for adding to Item '{1}'";
            public const string ItemColLinkAddT = "Added Collection(s) '{0}' to Item '{1}'";
            public const string ItemColLinkExistT = "Collection(s) '{0}' already present in Item '{1}'";
            public const string ItemColLinkDeleteT = "Deleted Collection '{0}' from Item '{1}'";
            public const string ItemCollectionCreateT = "Created Collection '{0}'";
            public const string ItemCollectionUpdateT = "Updated Collection '{0}'";
            public const string ItemColObjectAddT = "Added Item(s) '{0}' to Collection '{1}'";
            public const string ItemColObjectExistT = "Item(s) '{0}' already present in Collection '{1}'";
            public const string ItemColObjectDeleteT = "Deleted Item '{0}' from Collection '{1}'";
            public const string ItemColObjectMoveT = "Changed the position of Item '{0}'";
            public const string SubCategoryCreateT = "Created Category '{0}'";
            public const string SubCategoryUpdateT = "Updated Category '{0}'";
            public const string SubCatLinkDeleteT = "Deleted Category '{0}' from Category '{1}'";
            public const string SubCatLinkAddT = "Added Category(s) '{0}' to Category '{1}'";
            public const string SubCatMoveT = "Changed the position of Category(s) '{0}'";
            public const string SubCatMoveDetailT = "Changed the position of Category '{0}' in Menu '{1}'";
            public const string SubCatLinkExistT = "Category(s) '{0}' already present in Category '{1}'";
            public const string CatMnuLinkAddT = "Added Category(s) '{0}' to Menu '{1}'";
            public const string CatMnuLinkExistT = "Category(s) '{0}' already present in Menu '{1}'";
            public const string PrependItemLinkAddT = "Prepended Item(s) '{0}' to Item '{1}'";
            public const string PrependItemLinkExistT = "Prepend Item(s) '{0}' already present in Item '{1}'";
            public const string PrependItemLinkDeleteT = "Deleted Prepend Item '{0}' from Item '{1}'";
            public const string PrependItemLinkMoveT = "Changed the position of Prepend Item '{0}'";
            public const string PrependItemLinkMoveDetailT = "Changed the position of Prepend Item '{0}' in Item '{1}'";

            public const string RevertCollectionObjT = "Reverted Item '{0}'";
            public const string RevertMenuT = "Reverted Menu '{0}'";
            public const string CopyMenuT = "Copied Menu '{0}'";
            public const string CopyMenuDetailsT = "Copied Menu '{0}' from Menu '{0}'"; 

            public const string MasterItemDescriptionAddT = "Added new Description to Item '{0}'";
            public const string MasterItemDescriptionUpdateT = "Updated Description of Item '{0}'";
            public const string MasterItemDescriptionDeleteT = "Deleted Description from Item '{0}'";
            public const string MasterItemPLUAddT = "Added new PLU '{0}' to Item '{1}'";
            public const string MasterItemPLUUpdateT = "Updated PLU '{0}' of Item '{1}'";
            public const string MasterItemPLUDeleteT = "Deleted PLU '{0}' from Item '{1}'";
            public const string MasterItemDeleteT = "Deleted Item '{0}'";
            public const string MasterItemDeactivateT = "Deactivated Item '{0}'";
            public const string MasterItemActivateT = "Activated Item '{0}'";

            public const string ScheduleCreateT = "Created Schedule '{0}'";
            public const string ScheduleUpdateT = "Updated Schedule '{0}'";
            public const string ScheduleDeleteT = "Deleted Schedule '{0}'";
            public const string ScheduleDisableT = "Disabled Schedule '{0}'";
            public const string ScheduleEnableT = "Enabled Schedule '{0}'";
            public const string ScheduleCopyT = "Copied Schedule '{0}'";
            public const string ScheduleRevertT = "Reverted Schedule '{0}'";
            public const string ScheduleMoveT = "Moved Schedule '{0}'.";
            public const string SchCycleCreateT = "Created Schedule Cycle '{0}'";
            public const string SchCycleUpdateT = "Updated Schedule Cycle '{0}'";
            public const string SchCycleDeleteT = "Deleted Schedule Cycle '{0}'";
            public const string SchCycleDisableT = "Disabled Schedule Cycle '{0}'";
            public const string SchCycleEnableT = "Enabled Schedule Cycle '{0}'";
            public const string SchCycleMoveT = "Moved Schedule Cycle '{0}'";
            public const string SchNWLinkAddT = "Added Schedule(s) '{0}'";
            public const string SchNWLinkExistT = "Schedule(s) '{0}' already present";
            public const string ItemSchDetailUpdate = "Updated Schedule details for Item {0}";
            public const string CatSchDetailUpdate = "Updated Schedule details for Category {0}";

            public const string BackgroundCleanupComplete = "Completed the background cleanup process";

            public const string ModifierFlagCreatedT = "Created Modifier Flag '{0}'";
            public const string ModifierFlagUpdatedT = "Updated Modifier Flag '{0}'";
            public const string ModifierFlagDeletedT = "Deleted Modifier Flag '{0}'";


            public const string POSItemCreatedT = "Created POS Item '{0}'";
            public const string POSItemUpdatedT = "Updated POS Item '{0}'";
            public const string POSItemDeletedT = "Deleted POS Item '{0}'";
            public const string POSItemAttachedT = "Attached POS '{0}-{1}' to Item '{2}'";
            public const string POSItemDetachedT = "Removed POS '{0}-{1}' from Item '{2}'";
            public const string POSItemChangedT = "Changed POS Item to '{0}' for Item '{1}'";
            public const string POSItemChangedDetailsT = "Changed POS Item to '{0} {1}' for Item '{2}' in Menu '{3}'";
        }

        public static class StatusMessage
        {
            // site  service

            public const string ErrSiteCreateT = "Site creation failed. Site Name: '{0}'";
            public const string ErrSiteUpdateT = "Site '{0}' Update failed.";
            public const string ErrGroupCreateT = "Group creation failed. Group '{0}' cannot be created under group '{1}'";
            public const string ErrGroupUpdateT = "group '{0}' Update failed.";
            public const string ErrGroupDeleteT = "Group '{0}' deletion failed.";
            public const string ErrSiteDeleteT = "Site '{0}' deletion failed.";
            public const string ErrSiteDeleteMenuT = "Site '{0}' deletion failed. Site is assoiciated with Menu '{1}' .";

            //asset service
            public const string ErrAssetCreateT = "Asset '{0}' creation failed.";
            public const string ErrAssetVersionUpdateT = "Making asset '{0}' as current failed.";
            public const string ErrAssetItemLinkAddT = "Adding Asset '{0}' to Item '{1}' failed.";
            public const string ErrAssetItemLinkDeleteT = "Removing Asset '{0}' from Item '{1}' failed.";
            public const string ErrAssetDeleteT = "Asset deletion failed.";
            public const string ErrVersionDeleteT = "Asset(Version) '{0}' deletion failed. .";
            public const string ErrAssetCategoryLinkAddT = "Adding Asset '{0}' to Category '{1}' failed.";
            public const string ErrAssetCategoryLinkDeleteT = "Removing Asset '{0}' to Category '{1}' failed.";

            public const string ErrInvalidAssetT = "Operation failed. Invalid Asset: '{0}'";
            public const string ErrInvalidItemT = "Operation failed. Invalid Item(s): '{0}'";
            public const string ErrInvalidCatT = "Operation failed. Invalid Category: '{0}'";
            public const string ErrInvalidItmColT = "Operation failed. Invalid Collection: '{0}'";
            public const string ErrInvalidSchT = "Operation failed. Invalid Schedule: '{0}'";
            public const string ErrInvalidMnuT = "Operation failed. Invalid Menu: '{0}'";

            // tag service       
            public const string ErrTagCreate = "{0} creation failed.";
            public const string ErrTagUpdate = "{0} update operation failed.";
            public const string ErrTagDelete = "{0} deletion failed.";
            public const string ErrTagLink = "Tag link operation failed.";
            public const string ErrTagAddNLink = "Tag add and link operation failed.";
            public const string ErrGetTagAssociatedData = "Error occurred while fetching {0} associated data";

            // Menu Sync Target           
            public const string ErrTargetSyncDelete = "Target deletion failed.";
            public const string ErrTargetSyncSave = "Target save operation failed.";
            public const string ErrTargetSyncFailedT = "Target '{0}' synchronization failed. Response Code - {1}, Message - {2}";
            public const string ErrTargetSyncFailedNoSitesT = "No Sites to synchronize";
            public const string ErrTargetRefreshFailedT = "Target(s) refresh failed";
            public const string ErrTargetRefreshFailedUnableToRetrieveT = "Unable to retrieve Target(s) to refresh.";
            public const string ErrTargetsSyncFailedUnexpectedT = "Target(s) synchronization failed or logging the History failed.";
            public const string ErrTargetSyncFailedAuditT = "Target '{0}' synchronization failed. Sites involved in this synchronization are : ({1}). Response Code - {2}, Message - {3}";
            public const string ErrSyncHistoryDeleteFailedT = "Deleting Sync  failed.";

            //Special Notice          
            public const string ErrSpecialNoticeDelete = "Special Notice deletion failed.";
            public const string ErrSpecialNoticeSave = "Special Notice save operation failed.";
            public const string ErrSpecialNoticeLinked = "Special Notice link operation failed.";

            //Menu Service
            public const string ErrMenuCreate = "Creating Menu '{0}' failed.";
            public const string ErrMenuUpdate = "Updating Menu '{0}' failed.";
            public const string ErrMenuDelete = "Deleting Menu '{0}' failed.";
            public const string ErrCatObjectLinkAddT = "Adding Item '{0}' to Category '{1}' failed.";
            public const string ErrCatObjectLinkDeleteT = "Deleting Item '{0}' from Category '{1}' failed.";
            public const string ErrCatLinkAddT = "Adding/Updating Category '{0}' failed.";
            public const string ErrCatDeleteT = "Removing Category '{0}' from menu '{1}' failed.";
            public const string ErrCatObjMoveT = "Changing the position of Item '{0}'  failed.";
            public const string ErrCatMoveT = "Changing the position of Category '{0}'  failed.";
            public const string ErrItemCollnMoveT = "Changing the position of Collection '{0}'  failed.";
            public const string ErrColDeleteT = "Removing Collection '{0}' from Item '{1}' failed.";
            public const string ErrItemAddT = "Creating/Updating Item '{0}' failed.";
            public const string ErrItemColLinkAddT = "Adding Collection '{0}' to Item '{1}' failed.";
            public const string ErrItemColLinkDeleteT = "Deleting Collection '{0}' from Item '{1}' failed.";
            public const string ErrItemCollectionSaveT = "Adding/Updating Collection '{0}' failed.";
            public const string ErrItemColObjectAddT = "Adding Item '{0}' to Collection '{1}' failed.";
            public const string ErrItemColObjectDeleteT = "Deleting Item '{0}' from Collection '{1}' failed.";
            public const string ErrItemColObjectMoveT = "Changing the position of Item '{0}'  failed.";
            public const string ErrItemColObjectNotAddedT = "Operation failed. Item(s) '{0}' are not Eligible for Collection '{1}'.";
            public const string ErrSubCategorySaveT = "Adding/Updating Category '{0}' failed.";
            public const string ErrSubCatLinkDeleteT = "Deleting Category '{0}' from Category '{1}' failed.";
            public const string ErrSubCatLinkAddT = "Adding Category '{0}' to Category '{1}' failed.";
            public const string ErrSubCatLinkNotAddedT = "Operation failed. Categories(s) '{0}' are not Eligible for Category '{1}'";
            public const string ErrSubCatMoveT = "Changing the position of Category '{0}' failed.";
            public const string ErrCatMnuLinkAddT = "Adding Category '{0}' to Menu '{1}' failed.";
            public const string ErrPrependItemLinkAddT = "Adding Prepend Item(s) '{0}' to Item '{1}' failed.";
            public const string ErrPrependItemLinkDeleteT = "Removing Prepend Item '{0}' from Item '{1}' failed.";
            public const string ErrPrependItemLinkMoveT = "Changing the position of Prepend Item '{0}'  failed.";
            public const string ErrNotEligiblePrependItemT = "Operation failed. Prepend Item(s) '{0}' are not Eligible for Item '{1}'";
            public const string ErrCategoryUniqueDeepLinkT = "Please enter unique Deep Link Id";

            public const string ErrRevertCollectionObjT = "Reverting Item '{0}' failed.";
            public const string ErrRevertMenuT = "Reverting Menu '{0}' failed.";
            public const string ErrCopyMenuT = "Copying Menu '{0}' failed.";

            //Master item
            public const string ErrMasterItemDescriptionAddT = "Adding Item Description to item '{0}' failed.";
            public const string ErrMasterItemDescriptionUpdateT = "Updating Item Description for Item '{0}' failed.";
            public const string ErrMasterItemDescriptionDeleteT = "Deleting Item Description for Item '{0}' failed.";
            public const string ErrMasterItemPLUAddT = "Adding Item PLU {0} to item '{1}' failed.";
            public const string ErrMasterItemPLUUpdateT = "Updating Item PLU '{0}' for '{1}' failed.";
            public const string ErrMasterItemPLUDeleteT = "Deleting Item PLU '{0}'  for '{1}' failed.";
            public const string ErrMasterItemDeleteT = "Deleting Item '{0}' failed";
            public const string ErrMasterItemDeactivateT = "Deactivating Item '{0}' failed";
            public const string ErrMasterItemActivateT = "Activating Item '{0}' failed";
            public const string ErrMasterItemALTIdAlreadyExistsT = "Please enter unique Alternate ID";
            public const string ErrMasterItemPLUAlreadyExistsT = "Please enter unique PLU";
            public const string ErrMasterItemPLUAltIdEmptyT = "Please enter either PLU or Alternate ID";
            public const string ErrMasterItemPLUandALTIdAlreadyExistsT = "Same Alternate ID already exists with the PLU. Please enter unique combination of PLU and Alternate ID";

            //schedule
            public const string ErrScheduleSaveT = "Adding/Updating Schedule '{0}' failed.";
            public const string ErrScheduleDeleteT = "Deleting Schedule '{0}' failed.";
            public const string ErrScheduleNotEligibleDeleteT = "Deleting Schedule '{0}' failed as it is not allowed to delete.";
            public const string ErrScheduleDisableT = "Disabling Schedule '{0}' failed.";
            public const string ErrScheduleEnableT = "Enabling Schedule '{0}' failed.";
            public const string ErrScheduleAlreadyEnableT = "Enabling Schedule '{0}' failed as it is already enabled.";
            public const string ErrScheduleCopyT = "Copying Schedule '{0}' failed.";
            public const string ErrScheduleRevertT = "Reverting Schedule '{0}' failed.";
            public const string ErrScheduleMoveT = "Moving Schedule '{0}' failed.";
            public const string ErrSchCycleDeleteT = "Deleting Cycle '{0}' failed.";
            public const string ErrSchCycleDisableT = "Disabling Cycle '{0}' failed.";
            public const string ErrSchCycleNotEligibleDeleteT = "Deleting Cycle '{0}' failed as it is not allowed to delete.";
            public const string ErrSchCycleCreateT = "Creating Cycle '{0}' failed.";
            public const string ErrSchCycleUpdateT = "Updating Cycle '{0}' failed.";
            public const string ErrSchCycleEnableT = "Enabling Cycle '{0}' failed.";
            public const string ErrSchCycleAlreadyEnableT = "Enabling Cycle '{0}' failed as it is already enabled.";
            public const string ErrSchCycleMoveT = "Moving Cycle '{0}' failed.";
            public const string ErrSchNWLinkAddT = "Adding Schedule '{0}' failed.";

            public const string ErrBackgroundCleanup = "Background cleanup failed.";

            //Flag
            public const string ErrModifierFlagDelete = "Modifier Flag deletion failed.";
            public const string ErrModifierFlagSave = "Modifier Flag save operation failed.";

            //POSItem
            public const string ErrPOSItemDeleteT = "POS Item '{0}' deletion failed.";
            public const string ErrPOSItemAttachT = "Attaching POS Item '{0}' to Item '{1}' failed."; 
            public const string ErrPOSItemNotFoundT = "Unable to find POS Item/Item.";
            public const string ErrPOSItemAlreadyAttachT = "Cannot attaching POS Item '{0}' as it is attached to different Item.";
            public const string ErrPOSItemAlreadyAttachToSameT = "Already attached POS Item '{0}' to Item.";
            public const string ErrPOSItemSaveT = "POS Item '{0}' save operation failed.";
            public const string ErrPOSItemLinkNotFoundT = "Cannot remove POS Item '{0}' as it is not attached.";
            public const string ErrPOSItemDetachT = "Removing POS Item '{0}' from Item '{1}' failed.";
            public const string ErrPOSItemChangedT = "Changing POS Item '{0}' for Item '{1}' failed.";

            public const string ErrUnExpectedErrorOccured = "Unexpected error occurred.";

        }

        public static class ZStyle
        {
            public static string cDN = System.Configuration.ConfigurationManager.AppSettings.Get("ZStyleCDN").TrimEnd('/');
            public static string CdnSrcUrl = cDN + "/src/";
            public static string CdnImageUrl = cDN + "/img/";
        }

        public static class NetworkObjectImages
        {
            public static string Root = "../../Content/img/Globe16.png";
            public static string Site = ZStyle.CdnImageUrl + "net_site.png";
            public static string Group = ZStyle.CdnImageUrl + "net_group.png";
            public static string Franchise = ZStyle.CdnImageUrl + "net_franchise.png";
            public static string Brand = ZStyle.CdnImageUrl + "net_brand.png";
            public static string Market = ZStyle.CdnImageUrl + "net_market.png";
        }

        public static class MenuTypeImages
        {
            public static string Menu = ZStyle.CdnImageUrl + "menu.png";
            public static string Category = ZStyle.CdnImageUrl + "menu_cat_default.png";
            public static string EndOfOrderCategory = ZStyle.CdnImageUrl + "menu_cat_end.png";
            public static string ReOrderCategory = ZStyle.CdnImageUrl + "menu_cat_reorder.png";
            public static string Item = ZStyle.CdnImageUrl + "menu_item.png";
            public static string ItemCollection = ZStyle.CdnImageUrl + "menu_collect_default.png";
            public static string ComboCollection = ZStyle.CdnImageUrl + "menu_collect_combo.png";
            public static string EndOfOrderCollection = ZStyle.CdnImageUrl + "menu_collect_endorder.png";
            public static string ModificationCollection = ZStyle.CdnImageUrl + "menu_collect_mod.png";
            public static string SubstitutionCollection = ZStyle.CdnImageUrl + "menu_collect_sub.png";
            public static string UpSellCollection = ZStyle.CdnImageUrl + "menu_collect_upsell.png";
            public static string CrossSellCollection = ZStyle.CdnImageUrl + "menu_collect_default.png";
            public static string ItemCollectionItem = ZStyle.CdnImageUrl + "menu_item.png";
        }

        public class SessionValue
        {
            public const string CURRENT_USER_ID = "CurrentUserId";
            public const string CURRENT_USER_NAME = "CurrentUserName";            
            public const string CURRENT_USER_PERM = "CurrentUser";

        }
        public class Misc
        {
            public const string Unknown = "Unknown";
        }
    }
}