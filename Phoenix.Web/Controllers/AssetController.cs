using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Web.Models;
using System.Web.Script.Serialization;
using System.IO;
using System.Configuration;
using System.Drawing;
using System.Web.Helpers;
using SnowMaker;

namespace Phoenix.Web.Controllers
{
    public class AssetController  : PMBaseController
    {

        public AssetService _assetService { get { return base.service as AssetService; } }

        public AssetController(AssetService assetService)
            : base(assetService)
        {
        }

        /// <summary>
        /// View of Asset Manager
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult Index()
        {
            ViewBag.AssetBlobStorage = ConfigurationManager.AppSettings["CDNEndpoint"];
            ViewBag.AssetBlobContainer = ConfigurationManager.AppSettings["AssetBlobContainer"];
            return View();
        }

        /// <summary>
        /// Get all Assets in a Network Object
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult GetAssetList(int? networkObjectId)
        {
            var list = new List<AssetModel>();
            list = _assetService.GetAssetlist(networkObjectId);
            return Json(list.Where(x => x.IsCurrent), JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Delete all the selectes asset in index page
        /// </summary>
        /// <param name="selectedIds"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult DeleteAssets(string selectedIds)
        {
            var js = new JavaScriptSerializer();
            var assetIds = js.Deserialize<int[]>(selectedIds);
            _assetService.DeleteAssets(assetIds);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Delete selected versions in versions Popup
        /// </summary>
        /// <param name="selectedIds"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult DeleteAssetVersions(string selectedIds, bool isAllVersionsToDelete)
        {
            var js = new JavaScriptSerializer();
            var assetIds = js.Deserialize<int[]>(selectedIds);
            int currentAssetAfterChanges = _assetService.DeleteAssetVersions(assetIds, isAllVersionsToDelete);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult, currentAssetId = currentAssetAfterChanges }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Get all AssetItemLinks for grid in itemspopup
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult GetAssetItemList(int? assetId, int networkObjectId)
        {
            var list = new List<ItemModel>();
            if (assetId.HasValue)
            {
                list = _assetService.GetAssetItemlist(assetId.Value, networkObjectId);
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get all AssetCategoryLinks for grid in categories popup
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult GetAssetCategoryList(int? assetId, int networkObjectId)
        {
            var list = new List<CategoryModel>();
            if (assetId.HasValue)
            {
                list = _assetService.GetAssetCategorylist(assetId.Value, networkObjectId);
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <summary>
        /// get all versions of a asset in versions popup
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult GetAssetVersionList(int? assetId)
        {
            var list = _assetService.GetAssetVersionList(assetId);
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        
        /// <summary>
        /// Use this version of asset everywhere
        /// </summary>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult MakeAssetCurrent(int assetId)
        {
            _assetService.MakeAssetCurrent(assetId);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Remove this Image from selected Items
        /// </summary>
        /// <param name="selectedItems"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult RemoveItemsfromAsset(string selectedItems,int assetId)
        {
            var js = new JavaScriptSerializer();
            var itemIds = js.Deserialize<int[]>(selectedItems);
            _assetService.RemoveItemsfromAsset(itemIds, assetId);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add this Image to selected Items
        /// </summary>
        /// <param name="itemIds"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult AddItemstoAsset(string itemIds, int assetId)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(itemIds);
            _assetService.AddItemtoAsset(ids, assetId);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Remove this Image from selected categories
        /// </summary>
        /// <param name="selectedCats"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult RemoveCatsfromAsset(string selectedCats, int assetId)
        {
            var js = new JavaScriptSerializer();
            var itemIds = js.Deserialize<int[]>(selectedCats);
            _assetService.RemoveCatsfromAsset(itemIds, assetId);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Add this image to selected categories
        /// </summary>
        /// <param name="catIds"></param>
        /// <param name="assetId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult AddCatstoAsset(string catIds, int assetId)
        {
            var js = new JavaScriptSerializer();
            var ids = js.Deserialize<int[]>(catIds);
            _assetService.AddCatstoAsset(ids, assetId);
            return new JsonResult { Data = new { status = _assetService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _assetService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Populates Image Type DDL
        /// </summary>
        /// <returns></returns>
        public JsonResult GetImageTypes()
        {
            var retVal = _assetService.GetImageTypes();
            return Json(retVal, JsonRequestBehavior.AllowGet);
        }

        public ActionResult IsImageAVersion(string name, int size, int? netId, int? imgType)
        {
            var result = _assetService.IsImageAVersion(name, size, netId, imgType);
            return new JsonResult { Data = result,JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// Save uploaded file as a Image/Icon for given NW(brand)
        /// </summary>
        /// <param name="photos"></param>
        /// <param name="netId"></param>
        /// <param name="imgType"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult Save(IEnumerable<HttpPostedFileBase> photos, int? netId, int? imgType, string tagIdsToLink)
        {         
            string container = ConfigurationManager.AppSettings["AssetBlobContainer"].ToString();
            // The Name of the Upload component is "photos"
            if (photos != null)
            {
                foreach (var file in photos)
                {
                    //Save to DB
                    _assetService.SaveImagetoStorageandCreateAsset(file, container, netId, imgType, tagIdsToLink);
                }
            }

            // Return an empty string to signify success
            //return Content("");
            return Json(new { status = true }, "text/plain");
        }

        /// <summary>
        /// Remove uploaded files
        /// </summary>
        /// <param name="photos"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult RemoveFiles(string[] fileNames, int? netId)
        {
            if (fileNames != null)
            {
                foreach (var fullName in fileNames)
                {
                    var fileName = Path.GetFileName(fullName);
                    // Get asset by name
                    AssetModel asset = _assetService.GetAssetbyFileName(fileName,netId);

                    if (asset != null)
                    {
                        // Delete Asset
                        _assetService.DeleteLatestAsset(asset);
                    }
                }
            }

            // Return an empty string to signify success
            //return Content("");
            return Json(new { status = true }, "text/plain");
        }

        /// <summary>
        /// Cancel uploading file - Currently not used
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult CancelFile(string filename, int? netId)
        {
            // Get asset by name
            AssetModel asset = _assetService.GetAssetbyFileName(filename,netId);

            if (asset != null)
            {
                // Delete Asset
                _assetService.DeleteLatestAsset(asset);
            }

            // Return an empty string to signify success
            return Content("");
        }

    }
}
