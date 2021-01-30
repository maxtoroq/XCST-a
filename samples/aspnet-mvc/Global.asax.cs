using System.Web.Mvc;
using System.Web.Routing;
using Xcst.Web.Configuration;

namespace AspNetMvc {

   public class MvcApplication : System.Web.HttpApplication {

      protected void Application_Start() {
         AreaRegistration.RegisterAllAreas();
         FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
         RouteConfig.RegisterRoutes(RouteTable.Routes);
         ConfigureXcst(XcstWebConfiguration.Instance);
      }

      void ConfigureXcst(XcstWebConfiguration config) {

         config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
            (info.InputType == InputType.Text
               || info.InputType == InputType.Password
               || info.TagName != "input") ? "form-control"
               : null;
      }
   }
}
