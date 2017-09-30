﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Phoenix.DataAccess
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Objects;
    using System.Data.Objects.DataClasses;
    using System.Linq;
    
    public partial class ProductMasterContext : DbContext
    {
        public ProductMasterContext()
            : base("name=ProductMasterContext")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<Nutrition> Nutritions { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<webpages_ExtraUserInformation> webpages_ExtraUserInformation { get; set; }
        public DbSet<webpages_Membership> webpages_Membership { get; set; }
        public DbSet<webpages_OAuthMembership> webpages_OAuthMembership { get; set; }
        public DbSet<webpages_Roles> webpages_Roles { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<UnitOfMeasure> UnitOfMeasures { get; set; }
        public DbSet<TempSchedule> TempSchedules { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<ItemDescription> ItemDescriptions { get; set; }
        public DbSet<SchCycle> SchCycles { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagAssetLink> TagAssetLinks { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<MenuNetworkObjectLink> MenuNetworkObjectLinks { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<CategoryMenuLink> CategoryMenuLinks { get; set; }
        public DbSet<CategoryObject> CategoryObjects { get; set; }
        public DbSet<ItemCollectionLink> ItemCollectionLinks { get; set; }
        public DbSet<ItemCollectionObject> ItemCollectionObjects { get; set; }
        public DbSet<SubCategoryLink> SubCategoryLinks { get; set; }
        public DbSet<MenuSyncTarget> MenuSyncTargets { get; set; }
        public DbSet<SpecialNotice> SpecialNotices { get; set; }
        public DbSet<SpecialNoticeMenuLink> SpecialNoticeMenuLinks { get; set; }
        public DbSet<vwNetworkObjectTree> vwNetworkObjectTrees { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<PrependItemLink> PrependItemLinks { get; set; }
        public DbSet<ModifierFlag> ModifierFlags { get; set; }
        public DbSet<AssetCategoryLink> AssetCategoryLinks { get; set; }
        public DbSet<AssetItemLink> AssetItemLinks { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ItemCollection> ItemCollections { get; set; }
        public DbSet<MenuCategoryCycleInSchedule> MenuCategoryCycleInSchedules { get; set; }
        public DbSet<MenuItemCycleInSchedule> MenuItemCycleInSchedules { get; set; }
        public DbSet<MenuItemScheduleLink> MenuItemScheduleLinks { get; set; }
        public DbSet<SchNetworkObjectLink> SchNetworkObjectLinks { get; set; }
        public DbSet<SchDetail> SchDetails { get; set; }
        public DbSet<DWGroup> DWGroups { get; set; }
        public DbSet<DWGroupItemLink> DWGroupItemLinks { get; set; }
        public DbSet<DWItemCategorization> DWItemCategorizations { get; set; }
        public DbSet<DWItemSubType> DWItemSubTypes { get; set; }
        public DbSet<DWItemType> DWItemTypes { get; set; }
        public DbSet<DWItemCookTime> DWItemCookTimes { get; set; }
        public DbSet<SiteInfo> SiteInfoes { get; set; }
        public DbSet<CuisineNetworkObjectLink> CuisineNetworkObjectLinks { get; set; }
        public DbSet<SerivceNetworkObjectLink> SerivceNetworkObjectLinks { get; set; }
        public DbSet<ServiceType> ServiceTypes { get; set; }
        public DbSet<Cuisine> Cuisines { get; set; }
        public DbSet<MenuCategoryScheduleLink> MenuCategoryScheduleLinks { get; set; }
        public DbSet<ImportMapping> ImportMappings { get; set; }
        public DbSet<NetworkObject> NetworkObjects { get; set; }
        public DbSet<ItemPOSDataLink> ItemPOSDataLinks { get; set; }
        public DbSet<DWItemLookup> DWItemLookups { get; set; }
        public DbSet<MenuTagLink> MenuTagLinks { get; set; }
        public DbSet<TargetTagLink> TargetTagLinks { get; set; }
        public DbSet<MenuSyncTargetDetail> MenuSyncTargetDetails { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<POSData> POSDatas { get; set; }
        public DbSet<vwItemwithPOS> vwItemwithPOS { get; set; }
        public DbSet<vwPOSwithItem> vwPOSwithItems { get; set; }
    
        public virtual int usp_InsertAuditLog(string userName, Nullable<int> networkObjectId, string netObjectName, string entityType, string entityId, string entityNameList, string description, string details)
        {
            var userNameParameter = userName != null ?
                new ObjectParameter("UserName", userName) :
                new ObjectParameter("UserName", typeof(string));
    
            var networkObjectIdParameter = networkObjectId.HasValue ?
                new ObjectParameter("NetworkObjectId", networkObjectId) :
                new ObjectParameter("NetworkObjectId", typeof(int));
    
            var netObjectNameParameter = netObjectName != null ?
                new ObjectParameter("NetObjectName", netObjectName) :
                new ObjectParameter("NetObjectName", typeof(string));
    
            var entityTypeParameter = entityType != null ?
                new ObjectParameter("EntityType", entityType) :
                new ObjectParameter("EntityType", typeof(string));
    
            var entityIdParameter = entityId != null ?
                new ObjectParameter("EntityId", entityId) :
                new ObjectParameter("EntityId", typeof(string));
    
            var entityNameListParameter = entityNameList != null ?
                new ObjectParameter("EntityNameList", entityNameList) :
                new ObjectParameter("EntityNameList", typeof(string));
    
            var descriptionParameter = description != null ?
                new ObjectParameter("Description", description) :
                new ObjectParameter("Description", typeof(string));
    
            var detailsParameter = details != null ?
                new ObjectParameter("Details", details) :
                new ObjectParameter("Details", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("usp_InsertAuditLog", userNameParameter, networkObjectIdParameter, netObjectNameParameter, entityTypeParameter, entityIdParameter, entityNameListParameter, descriptionParameter, detailsParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkCategories")]
        public virtual IQueryable<fnNetworkCategories_Result> fnNetworkCategories(Nullable<int> networkObject, Nullable<int> menuId, Nullable<bool> includeDeleted)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkCategories_Result>("[ProductMasterContext].[fnNetworkCategories](@networkObject, @menuId, @IncludeDeleted)", networkObjectParameter, menuIdParameter, includeDeletedParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkCategoryItems")]
        public virtual IQueryable<fnNetworkCategoryItems_Result> fnNetworkCategoryItems(Nullable<int> networkObject, Nullable<int> catId)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var catIdParameter = catId.HasValue ?
                new ObjectParameter("catId", catId) :
                new ObjectParameter("catId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkCategoryItems_Result>("[ProductMasterContext].[fnNetworkCategoryItems](@networkObject, @catId)", networkObjectParameter, catIdParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkObjectParents")]
        public virtual IQueryable<fnNetworkObjectParents_Result> fnNetworkObjectParents(Nullable<int> networkObject)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkObjectParents_Result>("[ProductMasterContext].[fnNetworkObjectParents](@networkObject)", networkObjectParameter);
        }
    
        public virtual ObjectResult<POSMappedItemsResult> usp_POSMappedItems_Category(Nullable<int> networkObject, Nullable<int> menuId)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<POSMappedItemsResult>("usp_POSMappedItems_Category", networkObjectParameter, menuIdParameter);
        }
    
        public virtual ObjectResult<POSMappedItemsResult> usp_POSMappedItems_Collection(Nullable<int> networkObject, Nullable<int> menuId, Nullable<int> collectionType)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            var collectionTypeParameter = collectionType.HasValue ?
                new ObjectParameter("collectionType", collectionType) :
                new ObjectParameter("collectionType", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<POSMappedItemsResult>("usp_POSMappedItems_Collection", networkObjectParameter, menuIdParameter, collectionTypeParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkCollectionItems")]
        public virtual IQueryable<fnNetworkCollectionItems_Result> fnNetworkCollectionItems(Nullable<int> networkObject, Nullable<int> colId)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var colIdParameter = colId.HasValue ?
                new ObjectParameter("colId", colId) :
                new ObjectParameter("colId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkCollectionItems_Result>("[ProductMasterContext].[fnNetworkCollectionItems](@networkObject, @colId)", networkObjectParameter, colIdParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkCollections")]
        public virtual IQueryable<fnNetworkCollections_Result> fnNetworkCollections(Nullable<int> networkObject, Nullable<int> menuId, Nullable<bool> includeDeleted)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkCollections_Result>("[ProductMasterContext].[fnNetworkCollections](@networkObject, @menuId, @IncludeDeleted)", networkObjectParameter, menuIdParameter, includeDeletedParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkItemCollections")]
        public virtual IQueryable<fnNetworkItemCollections_Result> fnNetworkItemCollections(Nullable<int> networkObject, Nullable<int> itemId)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var itemIdParameter = itemId.HasValue ?
                new ObjectParameter("itemId", itemId) :
                new ObjectParameter("itemId", typeof(int));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkItemCollections_Result>("[ProductMasterContext].[fnNetworkItemCollections](@networkObject, @itemId)", networkObjectParameter, itemIdParameter);
        }
    
        [EdmFunction("ProductMasterContext", "udfGetSiteMenusInfo")]
        public virtual IQueryable<udfGetSiteMenusInfo_Result> udfGetSiteMenusInfo(Nullable<int> networkObjectId, Nullable<long> siteIrisId, Nullable<bool> includeExtraDetails)
        {
            var networkObjectIdParameter = networkObjectId.HasValue ?
                new ObjectParameter("networkObjectId", networkObjectId) :
                new ObjectParameter("networkObjectId", typeof(int));
    
            var siteIrisIdParameter = siteIrisId.HasValue ?
                new ObjectParameter("siteIrisId", siteIrisId) :
                new ObjectParameter("siteIrisId", typeof(long));
    
            var includeExtraDetailsParameter = includeExtraDetails.HasValue ?
                new ObjectParameter("includeExtraDetails", includeExtraDetails) :
                new ObjectParameter("includeExtraDetails", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<udfGetSiteMenusInfo_Result>("[ProductMasterContext].[udfGetSiteMenusInfo](@networkObjectId, @siteIrisId, @includeExtraDetails)", networkObjectIdParameter, siteIrisIdParameter, includeExtraDetailsParameter);
        }
    
        public virtual ObjectResult<usp_GetUserPermissionsFromRoot_Result> usp_GetUserPermissionsFromRoot(Nullable<int> userId, string providerUserId)
        {
            var userIdParameter = userId.HasValue ?
                new ObjectParameter("UserId", userId) :
                new ObjectParameter("UserId", typeof(int));
    
            var providerUserIdParameter = providerUserId != null ?
                new ObjectParameter("ProviderUserId", providerUserId) :
                new ObjectParameter("ProviderUserId", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<usp_GetUserPermissionsFromRoot_Result>("usp_GetUserPermissionsFromRoot", userIdParameter, providerUserIdParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkObjectParentsOfSelectedNetworks")]
        public virtual IQueryable<fnNetworkObjectParentsOfSelectedNetworks_Result> fnNetworkObjectParentsOfSelectedNetworks(string idList)
        {
            var idListParameter = idList != null ?
                new ObjectParameter("idList", idList) :
                new ObjectParameter("idList", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkObjectParentsOfSelectedNetworks_Result>("[ProductMasterContext].[fnNetworkObjectParentsOfSelectedNetworks](@idList)", idListParameter);
        }
    
        public virtual ObjectResult<uspGetAllSiteMenusInfoInBrand_Result> uspGetAllSiteMenusInfoInBrand(Nullable<long> brandIrisId)
        {
            var brandIrisIdParameter = brandIrisId.HasValue ?
                new ObjectParameter("brandIrisId", brandIrisId) :
                new ObjectParameter("brandIrisId", typeof(long));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<uspGetAllSiteMenusInfoInBrand_Result>("uspGetAllSiteMenusInfoInBrand", brandIrisIdParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnNetworkItems")]
        public virtual IQueryable<fnNetworkItems_Result> fnNetworkItems(Nullable<int> networkObject, Nullable<int> menuId, Nullable<bool> includeDeleted)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnNetworkItems_Result>("[ProductMasterContext].[fnNetworkItems](@networkObject, @menuId, @IncludeDeleted)", networkObjectParameter, menuIdParameter, includeDeletedParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnMenuTagsAtSpecificNetwork")]
        public virtual IQueryable<fnMenuTagsAtSpecificNetwork_Result> fnMenuTagsAtSpecificNetwork(Nullable<int> networkObject, Nullable<int> menuId, Nullable<bool> includeDeleted)
        {
            var networkObjectParameter = networkObject.HasValue ?
                new ObjectParameter("networkObject", networkObject) :
                new ObjectParameter("networkObject", typeof(int));
    
            var menuIdParameter = menuId.HasValue ?
                new ObjectParameter("menuId", menuId) :
                new ObjectParameter("menuId", typeof(int));
    
            var includeDeletedParameter = includeDeleted.HasValue ?
                new ObjectParameter("IncludeDeleted", includeDeleted) :
                new ObjectParameter("IncludeDeleted", typeof(bool));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<fnMenuTagsAtSpecificNetwork_Result>("[ProductMasterContext].[fnMenuTagsAtSpecificNetwork](@networkObject, @menuId, @IncludeDeleted)", networkObjectParameter, menuIdParameter, includeDeletedParameter);
        }
    
        [EdmFunction("ProductMasterContext", "fnSitesOfSelectedNetworks")]
        public virtual IQueryable<Nullable<int>> fnSitesOfSelectedNetworks(string idList)
        {
            var idListParameter = idList != null ?
                new ObjectParameter("idList", idList) :
                new ObjectParameter("idList", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.CreateQuery<Nullable<int>>("[ProductMasterContext].[fnSitesOfSelectedNetworks](@idList)", idListParameter);
        }
    
        public virtual int usp_DeleteMenuSyncDetails(string idList)
        {
            var idListParameter = idList != null ?
                new ObjectParameter("idList", idList) :
                new ObjectParameter("idList", typeof(string));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("usp_DeleteMenuSyncDetails", idListParameter);
        }
    }
}
