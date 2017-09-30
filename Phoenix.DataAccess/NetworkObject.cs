//------------------------------------------------------------------------------
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
    using System.Collections.Generic;
    
    public partial class NetworkObject
    {
        public NetworkObject()
        {
            this.Assets = new HashSet<Asset>();
            this.Categories = new HashSet<Category>();
            this.CategoryMenuLinks = new HashSet<CategoryMenuLink>();
            this.CategoryObjects = new HashSet<CategoryObject>();
            this.CuisineNetworkObjectLinks = new HashSet<CuisineNetworkObjectLink>();
            this.ItemCollections = new HashSet<ItemCollection>();
            this.ItemCollectionLinks = new HashSet<ItemCollectionLink>();
            this.ItemCollectionObjects = new HashSet<ItemCollectionObject>();
            this.Menus = new HashSet<Menu>();
            this.MenuCategoryScheduleLinks = new HashSet<MenuCategoryScheduleLink>();
            this.MenuItemScheduleLinks = new HashSet<MenuItemScheduleLink>();
            this.MenuNetworkObjectLinks = new HashSet<MenuNetworkObjectLink>();
            this.MenuSyncTargets = new HashSet<MenuSyncTarget>();
            this.ModifierFlags = new HashSet<ModifierFlag>();
            this.NetworkObject1 = new HashSet<NetworkObject>();
            this.SchCycles = new HashSet<SchCycle>();
            this.SchNetworkObjectLinks = new HashSet<SchNetworkObjectLink>();
            this.SerivceNetworkObjectLinks = new HashSet<SerivceNetworkObjectLink>();
            this.SiteInfoes = new HashSet<SiteInfo>();
            this.SpecialNotices = new HashSet<SpecialNotice>();
            this.SubCategoryLinks = new HashSet<SubCategoryLink>();
            this.Tags = new HashSet<Tag>();
            this.UserPermissions = new HashSet<UserPermission>();
            this.ItemPOSDataLinks = new HashSet<ItemPOSDataLink>();
            this.MenuTagLinks = new HashSet<MenuTagLink>();
            this.MenuSyncTargetDetails = new HashSet<MenuSyncTargetDetail>();
            this.Items = new HashSet<Item>();
            this.POSDatas = new HashSet<POSData>();
        }
    
        public int NetworkObjectId { get; set; }
        public string Name { get; set; }
        public NetworkObjectTypes NetworkObjectTypeId { get; set; }
        public Nullable<int> ParentId { get; set; }
        public string DataId { get; set; }
        public bool IsActive { get; set; }
        public long IrisId { get; set; }
        public int FeaturesSet { get; set; }
    
        public virtual ICollection<Asset> Assets { get; set; }
        public virtual ICollection<Category> Categories { get; set; }
        public virtual ICollection<CategoryMenuLink> CategoryMenuLinks { get; set; }
        public virtual ICollection<CategoryObject> CategoryObjects { get; set; }
        public virtual ICollection<CuisineNetworkObjectLink> CuisineNetworkObjectLinks { get; set; }
        public virtual ICollection<ItemCollection> ItemCollections { get; set; }
        public virtual ICollection<ItemCollectionLink> ItemCollectionLinks { get; set; }
        public virtual ICollection<ItemCollectionObject> ItemCollectionObjects { get; set; }
        public virtual ICollection<Menu> Menus { get; set; }
        public virtual ICollection<MenuCategoryScheduleLink> MenuCategoryScheduleLinks { get; set; }
        public virtual ICollection<MenuItemScheduleLink> MenuItemScheduleLinks { get; set; }
        public virtual ICollection<MenuNetworkObjectLink> MenuNetworkObjectLinks { get; set; }
        public virtual ICollection<MenuSyncTarget> MenuSyncTargets { get; set; }
        public virtual ICollection<ModifierFlag> ModifierFlags { get; set; }
        public virtual ICollection<NetworkObject> NetworkObject1 { get; set; }
        public virtual NetworkObject NetworkObject2 { get; set; }
        public virtual ICollection<SchCycle> SchCycles { get; set; }
        public virtual ICollection<SchNetworkObjectLink> SchNetworkObjectLinks { get; set; }
        public virtual ICollection<SerivceNetworkObjectLink> SerivceNetworkObjectLinks { get; set; }
        public virtual ICollection<SiteInfo> SiteInfoes { get; set; }
        public virtual ICollection<SpecialNotice> SpecialNotices { get; set; }
        public virtual ICollection<SubCategoryLink> SubCategoryLinks { get; set; }
        public virtual ICollection<Tag> Tags { get; set; }
        public virtual ICollection<UserPermission> UserPermissions { get; set; }
        public virtual ICollection<ItemPOSDataLink> ItemPOSDataLinks { get; set; }
        public virtual ICollection<MenuTagLink> MenuTagLinks { get; set; }
        public virtual ICollection<MenuSyncTargetDetail> MenuSyncTargetDetails { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<POSData> POSDatas { get; set; }
    }
}
