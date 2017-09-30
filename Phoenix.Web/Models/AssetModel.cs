using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Phoenix.Web.Models
{
    public class AssetModel
    {
        //Iris Id - Unique Id
        public long IrisId { get; set; }

        [Display(Name = "AssetId")]
        public int AssetId { get; set; }
        
        public string URL { get; set; }

        [Display(Name = "FileName")]
        public string FileName { get; set; }

        public string BlobName { get; set; }

        public string FileHash { get; set; }

        public bool IsActive { get; set; }

        [Display(Name = "Linked")]
        public bool Linked { get; set; }

        [Display(Name = "Width")]
        public int DimX { get; set; }

        [Display(Name = "Height")]
        public int DimY { get; set; }
        
        [Display(Name = "Size")]
        public int Size { get; set; }

        [Display(Name = "IsCurrent")]
        public bool IsCurrent { get; set; }

        [Display(Name = "Brand")]
        public int BrandId { get; set; }

        [Display(Name = "UploadedDate")]
        public DateTime CreatedDate { get; set; }

        public bool HasVersions { get; set; }

        public List<AssetItemModel> AssetItemLinks { get; set; }

        public int AssetItemCount { get; set; }
        
        public string ItemName { get; set; }

        public string CatName { get; set; }
        
        public string ThumbnailBlob { get; set; }

        public string AssetType { get; set; }

        public int AssetTypeId { get; set; }

        public bool IsInherited { get; set; }

        public List<AssetTagModel> AssetTagLinks { get; set; }

        public string TagName { get; set; }
    }

    public class AssetItemModel
    {
        [Display(Name = "AssetItemLinkId")]
        public int AssetItemLinkId { get; set; }

        [Display(Name = "AssetId")]
        public int AssetId { get; set; }

        public int AssetTypeId { get; set; }

        [Display(Name = "ItemId")]
        public int? ItemId { get; set; }

        public string ItemName { get; set; }

        public string FileName { get; set; }

        public string ThumbNailBlobName { get; set; }

        public string BlobName { get; set; }

        public string URL { get; set; }

        public bool ToDelete { get; set; }
    }

    public class AssetTagModel
    {
        public int TagAssetLinkId { get; set; }

        public int AssetId { get; set; }

        public int? TagId { get; set; }

        public string TagName { get; set; }
    }
}