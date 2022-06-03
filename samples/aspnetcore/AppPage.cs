using Microsoft.AspNetCore.Http;
using Xcst;
using Xcst.PackageModel;
using Xcst.Web.Mvc;

namespace aspnetcore;

public abstract class AppPage : XcstViewPage {

   protected override void
   RenderViewPage() {

      Response.ContentType = "text/html";

      if (this is IPageInit pInit) {

         XcstEvaluator.Using(pInit)
            .CallFunction(p => p.Init())
            .Run();

      } else {
         base.RenderViewPage();
      }
   }
}

public interface IPageInit : IXcstPackage {

   void
   Init();
}
