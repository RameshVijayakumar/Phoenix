using System.Collections.Generic;
using System.Web.Mvc;
using Phoenix.Web.Models;
using System.Web.Script.Serialization;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class TagController : PMBaseController
    {
        //private ITagService _tagService;

        //public TagController()
        //{
        //    //TODO: inject this interface
        //    _tagService = new TagService();          
        //}

        private TagService _tagService { get { return base.service as TagService; } }

        public TagController()
            : base(new TagService())
        {

            _tagService.Initialize(new ModelStateWrapper(this.ModelState));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// This method brings a list of all the tags for the specified networkObjectid
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetTagList(bool callFromGrid, [DataSourceRequest] DataSourceRequest request, int? networkObjectId, TagKeys tagKey)
        {
            var list = new List<TagModel>();

            if (networkObjectId.HasValue)
            {
                list = _tagService.GetTagList(networkObjectId,tagKey);
            }
            if (callFromGrid)
            {
                return Json(list.ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(list, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult CheckTagAtBrandLevel(string tagName, TagKeys tagKey)
        {
            var IsNotUnique = false;
            var networkObjectName = string.Empty;
            var networkObjectId = -1;
            var tagId = -1;
            IsNotUnique = _tagService.CheckTagAtBrandLevel(tagName, tagKey, out tagId, out networkObjectName);
            return new JsonResult { Data = new { IsNotUnique = IsNotUnique, ExistingTagId = tagId, AtNetworkObjectId = networkObjectId, AtNetworkObjectName = networkObjectName }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// This method brings a comma seperated string of all the tag Ids corresponding to the entity Ids
        /// </summary>
        /// <param name="entityIds"></param>
        /// <param name="entityType"></param>
        /// <returns></returns>
        [HttpGet]
        public string GetTagIdsForEntities(string entityIds, string entityType, int netId, TagKeys tagKey)
        {
            var js = new JavaScriptSerializer();
            var entityIdList = js.Deserialize<int[]>(entityIds);
            return _tagService.GetTagIdsForEntities(entityIdList, entityType, netId, tagKey);
        }

        /// <summary>
        /// This method brings the data in the form of comma seperated strings corresponding to tagId
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult GetTagAssociatedData(int tagId, int networkobjectId, TagKeys tagKey)
        {
            bool actionStatus = false;
            var tagAssociatedData = _tagService.GetTagAssociatedData(tagId, networkobjectId, tagKey, out actionStatus);
            return new JsonResult { Data = new { status = actionStatus, msg = _tagService.LastActionResult, associatedData = tagAssociatedData }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// This method is used to save(create as well as update) tags. It is invoked from Tag Manager page.
        /// </summary>
        /// <param name="tagId"></param>
        /// <param name="tagName"></param>
        /// <param name="networkObjectId"></param>     
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult SaveTag(int tagId, string tagName, int networkObjectId, bool isTagToMove, TagKeys tagKey)
        {
            var js = new JavaScriptSerializer();
            if (tagId == 0)
            {
                _tagService.CreateTag(tagName, tagKey, networkObjectId);
            }
            else
            {
                _tagService.UpdateTag(tagId, tagName, tagKey, networkObjectId, isTagToMove);
            }
            return new JsonResult { Data = new { status = _tagService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _tagService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult SaveChannel([DataSourceRequest] DataSourceRequest request, TagModel model, TagKeys tagKey)
        {
            if (ModelState.IsValid)
            {
                model = _tagService.SaveTag(model, tagKey);
                if (ModelState.IsValid)
                {
                    return Json(model);
                }
                else
                {
                    return Json(ModelState.ToDataSourceResult());
                }
            }
            else
            {
                return Json(ModelState.ToDataSourceResult());
            }
        }


        /// <summary>
        /// This method is used to add a new tag and link the tag to a set of entities. 
        /// </summary>
        /// <param name="tagIdsAdded"></param>
        /// <param name="tagIdsRemoved"></param>
        /// <param name="entityIds"></param>
        /// <param name="enityType"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult AddNLinkTagToEntities(string tagIdsAdded, string entityIds, TagEntity enityType, int networkObjectId, TagKeys tagKey)
        {
            var js = new JavaScriptSerializer();
            var entityIdList = js.Deserialize<int[]>(entityIds);
            _tagService.AddNLinkTagToEntities(string.IsNullOrEmpty(tagIdsAdded) ? tagIdsAdded : tagIdsAdded.Remove(0, 1), entityIdList, enityType, networkObjectId, tagKey);
            return new JsonResult { Data = new { status = _tagService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _tagService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// This method is to manage links(add new links or remove existing links) between entities(of type 'enityType') and tags.
        /// </summary>
        /// <param name="tagIdsAdded"></param>
        /// <param name="tagIdsRemoved"></param>
        /// <param name="entityIds"></param>
        /// <param name="enityType"></param>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult LinkTagsToEntities(string tagIdsAdded, string tagIdsRemoved, string entityIds, TagEntity enityType, int networkObjectId, TagKeys tagKey)
        {
            var js = new JavaScriptSerializer();
            var entityIdList = js.Deserialize<int[]>(entityIds);
            _tagService.LinkTagsToEntities(string.IsNullOrEmpty(tagIdsAdded) ? tagIdsAdded : tagIdsAdded.Remove(0, 1), string.IsNullOrEmpty(tagIdsRemoved) ? tagIdsRemoved : tagIdsRemoved.Remove(0, 1), entityIdList, enityType, networkObjectId, tagKey);
            return new JsonResult { Data = new { status = _tagService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _tagService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// This method is used to delete tag and its associated data
        /// </summary>
        /// <param name="selectedIds"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Administrator,SuperAdministrator")]
        public ActionResult DeleteTags(string selectedIds, TagKeys tagKey)
        {
            _tagService.DeleteTags(selectedIds, tagKey);         
            return new JsonResult { Data = new { status = _tagService.LastActionResult.Contains("failed") ? false : true, lastActionResult = _tagService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
