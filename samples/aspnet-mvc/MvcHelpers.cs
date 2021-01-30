using System;
using System.IO;
using System.Web.Mvc;

namespace AspNetMvc {

   public static class MvcHelpers {

      public static string RenderViewAsString(this ControllerContext context, string viewName, object model) =>
         RenderViewAsString(context, viewName, new ViewDataDictionary(model));

      public static string RenderViewAsString(this ControllerContext context, string viewName, ViewDataDictionary viewData) {

         using (var writer = new StringWriter()) {
            RenderView(context, viewName, viewData, writer);
            return writer.ToString();
         }
      }

      public static void RenderView(this ControllerContext context, string viewName, object model, TextWriter output) =>
         RenderView(context, viewName, new ViewDataDictionary(model), output);

      public static void RenderView(this ControllerContext context, string viewName, ViewDataDictionary viewData, TextWriter output) {

         ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(context, viewName);

         if (viewResult.View is null) {
            throw new InvalidOperationException();
         }

         var viewContext = new ViewContext(
            context,
            viewResult.View,
            viewData,
            new TempDataDictionary(),
            output
         );

         viewResult.View.Render(viewContext, output);
      }
   }
}
