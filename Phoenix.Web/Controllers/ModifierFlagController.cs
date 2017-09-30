using System.Collections.Generic;
using System.Web.Mvc;
using Phoenix.Web.Models;
using System.Web.Script.Serialization;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace Phoenix.Web.Controllers
{
    [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
    public class ModifierFlagController : PMBaseController
    {
        private ModifierFlagService _modifierFlagService { get { return base.service as ModifierFlagService; } }

        public ModifierFlagController()
            : base(new ModifierFlagService())
        {

        }
        /// <summary>
        /// This method brings a list of all the modifier flags for the specified networkObjectid
        /// </summary>
        /// <param name="networkObjectId"></param>
        /// <returns></returns>
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,Viewer,SuperAdministrator")]
        public ActionResult GetModifierFlagList([DataSourceRequest] DataSourceRequest request, int? networkObjectId)
        {
            List<ModifierFlagModel> list = null;

            if (networkObjectId.HasValue)
            {
                list = _modifierFlagService.GetModifierFlagList(networkObjectId.Value);
            }

            if (list == null)
            {
                list = new List<ModifierFlagModel>();
            }
            return Json(list.ToDataSourceResult(request));
        }

        [HttpGet]
        public ActionResult GetModifierFlagList(int? networkObjectId)
        {
            List<ModifierFlagModel> list = null;

            if (networkObjectId.HasValue)
            {
                list = _modifierFlagService.GetModifierFlagList(networkObjectId.Value);
            }

            if (list == null)
            {
                list = new List<ModifierFlagModel>();
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        /// This method is used to save(create as well as update) Modifier Flag. 
        [HttpPost]
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult SaveModifierFlag([DataSourceRequest] DataSourceRequest request, ModifierFlagModel model)
        {
            if (ModelState.IsValid)
            {
                bool isNoticeNameNotUnique = false;

                _modifierFlagService.CheckUniquenessOfReqdData(model, out isNoticeNameNotUnique);

                if (isNoticeNameNotUnique)
                {
                    ModelState.AddModelError("Name", "Flag Name must be unique");
                    return Json(ModelState.ToDataSourceResult());
                }
                else
                {
                    var actionStatus = false;
                    model = _modifierFlagService.SaveModifierFlag(model, out actionStatus);
                    if (actionStatus)
                    {
                        ModelState.AddModelError("Name", _modifierFlagService.LastActionResult);
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

        /// <summary>
        /// This method is used to delete list of modifier flags
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>    
        [CustomAuthorize(Roles = "Administrator,Editor,SuperAdministrator")]
        public ActionResult DeleteModifierFlags(string flags)
        {
            var actionStatus = _modifierFlagService.DeleteModifierFlags(flags);
            return new JsonResult { Data = new { status = !actionStatus, lastActionResult = _modifierFlagService.LastActionResult }, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
        }
    }
}
