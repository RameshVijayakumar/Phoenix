using System.Collections.Generic;
using System.Web.Mvc;
using Phoenix.Web.Models;
using System.Web.Script.Serialization;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class SpecialNoticeController : PMBaseController
    {

        private SpecialNoticeService _specialNoticeService { get { return base.service as SpecialNoticeService; } }

        public SpecialNoticeController()
            : base(new SpecialNoticeService())
        {

        }
        
        /// <summary>
        /// This method brings a list of all the special notices for the specified networkObjectid
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [HttpGet]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetSpecialNoticeList(int? networkObjectId)
        {
            List<SpecialNoticeModel> list = null;

            if (networkObjectId.HasValue)
            {
                list = _specialNoticeService.GetSpecialNoticeList(networkObjectId.Value);
            }

            if (list == null)
            {
                list = new List<SpecialNoticeModel>();
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

     
        /// This method gets menu related list of special notices        
        [HttpGet]
        public ActionResult GetMenuRelatedSpecialNoticeList(int menuId, int networkObjectId)
        {
            var list = _specialNoticeService.GetMenuRelatedSpecialNoticeList(menuId, networkObjectId);          
            if (list == null)
            {
                list = new List<SpecialNoticeModel>();
            }
            return Json(list, JsonRequestBehavior.AllowGet);   
        }

        /// This method is used to save(create as well as update) Special Notice. 
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult SaveSpecialNotice([DataSourceRequest] DataSourceRequest request, SpecialNoticeModel model)
        {
            if (ModelState.IsValid)
            {
                bool isNoticeNameNotUnique = false;

                _specialNoticeService.CheckUniquenessOfReqdData(model, out isNoticeNameNotUnique);

                if (isNoticeNameNotUnique)
                {
                    ModelState.AddModelError("NoticeName", "Notice Name must be unique");
                    return Json(ModelState.ToDataSourceResult());
                }
                else
                {
                    var actionStatus = false;
                    model = _specialNoticeService.SaveSpecialNotice(model, out actionStatus);
                    if (actionStatus)
                    {
                        ModelState.AddModelError("NoticeName", _specialNoticeService.LastActionResult);
                        return Json(ModelState.ToDataSourceResult());
                    }
                    else
                    {
                        return Json(model);
                    }
                }         
            }
            else
            {
                return Json(ModelState.ToDataSourceResult());
            }     
        }

        /// This method is used to save the link information of menu and special notices
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult SaveSpecialNoticeMenuLink(string noticesToAddLink, string noticesToRemoveLink, int menuId, string menuName, int networkObjectId)
        {
            var actionStatus = _specialNoticeService.SaveSpecialNoticeMenuLink(noticesToAddLink, noticesToRemoveLink, menuId, menuName, networkObjectId);
            return new JsonResult { Data = new { status = !actionStatus , lastActionResult = _specialNoticeService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

        /// <summary>
        /// This method is used to delete list of special notices
        /// </summary>
        /// <param name="noticeIds"></param>
        /// <returns></returns>  
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]  
        public ActionResult DeleteSpecialNotices(string noticeIds)
        {
          var actionStatus =  _specialNoticeService.DeleteSpecialNotices(noticeIds);
          return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _specialNoticeService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }

    }
}
