using System.Threading.Tasks;
using Xcst;
using Xcst.Web.Mvc;

namespace aspnetcore;

public abstract class AppPage : XcstViewPage {

   protected override async Task
   RenderViewPageAsync() {

      Response.ContentType = "text/html";

      if (this is IPageInit pInit) {

         await XcstEvaluator.Using(pInit)
            .CallFunction(async p => await p.InitAsync())
            .Evaluate();

         return;
      }

      await base.RenderViewPageAsync();
   }
}

public interface IPageInit : IXcstPackage {

   Task
   InitAsync();
}
