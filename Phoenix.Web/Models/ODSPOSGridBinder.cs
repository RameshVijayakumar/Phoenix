using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Web.Models.Grid;


namespace Phoenix.Web.Models
{
    public class ODSPOSGridBinder : KendoGridBinder, IModelBinder
    {
        public new object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            ODSPOSGridRequest gridObject = new ODSPOSGridRequest(base.BindModel(controllerContext, bindingContext) as KendoGridRequest);

            var hldValue = base.GetQueryStringValue("networkObjectId");
            gridObject.networkObjectId = hldValue == "" || hldValue == null ? 0 : Convert.ToInt32(hldValue);


            return gridObject;
        }

    }
}
