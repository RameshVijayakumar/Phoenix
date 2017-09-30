using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Phoenix.Web.Models.Grid;

namespace Phoenix.Web.Models
{

    [ModelBinder(typeof(ODSPOSGridBinder))]
    public class ODSPOSGridRequest : KendoGridRequest
    {

        public ODSPOSGridRequest(KendoGridRequest rqst)
        {
            this.Take = rqst.Take;
            this.Skip = rqst.Skip;
            this.Page = rqst.Page;
            this.PageSize = rqst.PageSize;
            this.Logic = rqst.Logic;

            this.FilterObjectWrapper = rqst.FilterObjectWrapper;
            this.SortObjects = rqst.SortObjects;
        }

        public int? networkObjectId { get; set; }


    }
}