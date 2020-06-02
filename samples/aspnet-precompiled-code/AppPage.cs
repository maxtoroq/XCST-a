using System.Web;
using Xcst;
using Xcst.PackageModel;
using Xcst.Web.Mvc;

namespace AspNetPrecompiled {

   public abstract class AppPage : XcstViewPage {

      public override IHttpHandler CreateHttpHandler() => new PageInitHttpHandler(this);

      class PageInitHttpHandler : XcstViewPageHandler {

         public PageInitHttpHandler(XcstViewPage page)
            : base(page) { }

         protected override void RenderViewPage(XcstViewPage page, HttpContextBase context) {

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
