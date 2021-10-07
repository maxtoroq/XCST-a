using System;
using System.Web;
using System.Web.Mvc;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;

[assembly: Xcst.Web.Precompilation.PrecompiledModule]

namespace AspNetPrecompiled {

   public class Application : HttpApplication {

      void Application_Start(object sender, EventArgs e) {

         var config = XcstWebConfiguration.Instance;

         config.DisplayTemplates.TemplateFactory = LoadDisplayTemplate;
         config.EditorTemplates.TemplateFactory = LoadEditorTemplate;

         config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
            (info.InputType == InputType.Text
               || info.InputType == InputType.Password
               || info.TagName != "input") ? "form-control"
               : null;
      }

      static XcstViewPage? LoadDisplayTemplate(string templateName, ViewContext context) =>
         templateName switch {
            nameof(Object) => new DisplayTemplates.ObjectPackage(),
            _ => null,
         };

      static XcstViewPage? LoadEditorTemplate(string templateName, ViewContext context) =>
         templateName switch {
            nameof(Boolean) => new EditorTemplates.BooleanPackage(),
            nameof(Object) => new EditorTemplates.ObjectPackage(),
            _ => null,
         };
   }
}
