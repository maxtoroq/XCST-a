using Microsoft.AspNetCore.Http;
using Xcst;
using Xcst.PackageModel;
using Xcst.Web.Mvc;

namespace aspnetcore {

   public abstract class AppPage : XcstViewPage {

      public override XcstViewPageHandler CreateHttpHandler() => new PageInitHttpHandler(this);

      class PageInitHttpHandler : XcstViewPageHandler {

         public PageInitHttpHandler(XcstViewPage page)
            : base(page) { }

         protected override void RenderViewPage(XcstViewPage page, HttpContext context) {

            context.Response.ContentType = "text/html";

            if (page is IPageInit pInit) {

               XcstEvaluator.Using(pInit)
                  .CallFunction(p => p.Init())
                  .Run();

            } else {
               base.RenderViewPage(page, context);
            }
         }
      }
   }

   public interface IPageInit : IXcstPackage {

      void Init();
   }
}
