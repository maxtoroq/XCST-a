using System;
using System.Web;
using System.Web.Mvc;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;
using AspNetPrecompiled.Infrastructure;

namespace AspNetPrecompiled {

   public class Application : HttpApplication {

      void Application_Start(object sender, EventArgs e) {

         ExtensionlessUrlModule.InitializePageMap();

         var config = XcstWebConfiguration.Instance;

         config.DisplayTemplates.TemplateFactory = LoadDisplayTemplate;
         config.EditorTemplates.TemplateFactory = LoadEditorTemplate;

         config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
            (info.InputType == InputType.Text
               || info.InputType == InputType.Password
               || info.TagName != "input") ? "form-control"
               : null;
      }

      static XcstViewPage? LoadDisplayTemplate(string templateName, ViewContext context) {

         switch (templateName) {
            case nameof(Object):
               return new DisplayTemplates.ObjectPackage();

            default:
               return null;
         }
      }

      static XcstViewPage? LoadEditorTemplate(string templateName, ViewContext context) {

         switch (templateName) {
            case nameof(Boolean):
               return new EditorTemplates.BooleanPackage();

            case nameof(Object):
               return new EditorTemplates.ObjectPackage();

            default:
               return null;
         }
      }
   }
}
