using System.Web;
using System.Web.Optimization;

namespace Phoenix.Web
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
#if !DEBUG
            BundleTable.EnableOptimizations = true;
#endif
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui", @"http://ajax.googleapis.com/ajax/libs/jqueryui/1.9.1/jquery-ui.min.js")
                .Include("~/Scripts/jquery-ui-{version}.js"));
            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/kendoui")
                .Include("~/scripts/kendo.web.*", "~/scripts/kendo.aspnetmvc.*", "~/scripts/jszip.*"));

            bundles.Add(new ScriptBundle("~/bundles/jquerblockUI")
                .Include("~/scripts/jquery.blockui.*"));

            bundles.Add(new ScriptBundle("~/bundles/commonjs")
                .Include("~/scripts/common.js", "~/scripts/mapdatacommon.js", "~/scripts/sessionredirect.js"));

            bundles.Add(new ScriptBundle("~/bundles/treeviewcontrol")
              .Include("~/scripts/kendotreeview.helper.js"));

            bundles.Add(new ScriptBundle("~/bundles/menuedit")
              .Include("~/scripts/menu.js"));

            bundles.Add(new ScriptBundle("~/bundles/scheduleedit")
              .Include("~/scripts/schedule.js"));

            bundles.Add(new ScriptBundle("~/bundles/cycle")
              .Include("~/scripts/cycle.js"));

            bundles.Add(new ScriptBundle("~/bundles/masteritemedit")
              .Include("~/scripts/masteritem.js", "~/scripts/itemformcommon.js"));

            bundles.Add(new ScriptBundle("~/bundles/menuselect")
              .Include("~/scripts/menuselect.js"));

            bundles.Add(new ScriptBundle("~/bundles/scheduleselect")
              .Include("~/scripts/scheduleselect.js"));

            bundles.Add(new ScriptBundle("~/bundles/masteritemselect")
              .Include("~/scripts/itemselect.js"));


            bundles.Add(new ScriptBundle("~/bundles/positemselect")
              .Include("~/scripts/positemselect.js"));

            bundles.Add(new ScriptBundle("~/bundles/asset")
              .Include("~/scripts/asset.js"));

            bundles.Add(new ScriptBundle("~/bundles/formatcurrency")
              .Include("~/scripts/jquery.formatCurrency-1.4.0.js"));

            bundles.Add(new ScriptBundle("~/bundles/maskedinput")
              .Include("~/scripts/jquery.maskedinput.1.3.1.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/poscommon")
              .Include("~/scripts/posdatacommon.js"));

            bundles.Add(new ScriptBundle("~/bundles/posadmin")
              .Include("~/scripts/posadmin.js"));

            bundles.Add(new ScriptBundle("~/bundles/itemformcommon")
              .Include("~/scripts/itemformcommon.js"));
            bundles.Add(new ScriptBundle("~/bundles/posmap")
              .Include("~/scripts/posmap.js"));

            bundles.Add(new ScriptBundle("~/bundles/siteinfoedit")
              .Include("~/scripts/siteinfoedit.js"));
            bundles.Add(new ScriptBundle("~/bundles/siteactions")
              .Include("~/scripts/siteactions.js"));
            bundles.Add(new ScriptBundle("~/bundles/tagcommon")
              .Include("~/scripts/tagcommon.js"));

            bundles.Add(new ScriptBundle("~/bundles/channel")
              .Include("~/scripts/channel.js"));

            bundles.Add(new ScriptBundle("~/bundles/tag")
              .Include("~/scripts/tag.js"));


            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));
            bundles.Add(new StyleBundle("~/Content/bootstrap/css", @"http://netdna.bootstrapcdn.com/twitter-bootstrap/2.2.1/css/bootstrap-combined.min.css")
                .Include("~/Content/bootstrap/bootstrap.css"));

            bundles.Add(new StyleBundle("~/Content/kendocss/css")
                .Include("~/Content/kendocss/kendo.common.*",
                    "~/Content/kendocss/kendo.bootstrap.*"));
        }
    }
}
