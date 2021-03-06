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
    
    public partial class Asset
    {
        public Asset()
        {
            this.TagAssetLinks = new HashSet<TagAssetLink>();
            this.AssetCategoryLinks = new HashSet<AssetCategoryLink>();
            this.AssetItemLinks = new HashSet<AssetItemLink>();
        }
    
        public int AssetId { get; set; }
        public string Filename { get; set; }
        public string Blobname { get; set; }
        public string FileHash { get; set; }
        public int DimX { get; set; }
        public int DimY { get; set; }
        public bool IsActive { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public bool IsCurrent { get; set; }
        public int Size { get; set; }
        public string ThumbnailBlob { get; set; }
        public int NetworkObjectId { get; set; }
        public AssetTypes AssetTypeId { get; set; }
        public long IrisId { get; set; }
    
        public virtual ICollection<TagAssetLink> TagAssetLinks { get; set; }
        public virtual ICollection<AssetCategoryLink> AssetCategoryLinks { get; set; }
        public virtual ICollection<AssetItemLink> AssetItemLinks { get; set; }
        public virtual NetworkObject NetworkObject { get; set; }
    }
}
