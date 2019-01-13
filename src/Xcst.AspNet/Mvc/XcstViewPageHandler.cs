// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Xcst.Web.Mvc {

   public class XcstViewPageHandler : XcstPageHandler {

      readonly XcstViewPage page;

      public XcstViewPageHandler(XcstViewPage page)
         : base(page) {

         this.page = page;
      }

      protected override void InitializePage(XcstPage page, HttpContextBase context) {

         base.InitializePage(page, context);

         RequestContext requestContext = context.Request.RequestContext
            ?? new RequestContext(context, new RouteData());

         var controllerContext = new ControllerContext(requestContext);
         var tempData = new TempDataDictionary(); ;

         this.page.ViewContext = new ViewContext(controllerContext, this.page.ViewData, tempData);
      }

      protected override void RenderPage(XcstPage page, HttpContextBase context) {

         ITempDataProvider tempDataProvider = this.page.ViewContext.TempDataProvider;

         PossiblyLoadTempData(tempDataProvider);

         try {
            base.RenderPage(page, context);
         } finally {
            PossiblySaveTempData(tempDataProvider);
         }
      }

      void PossiblyLoadTempData(ITempDataProvider tempDataProvider) {

         if (!this.page.ViewContext.IsChildAction) {
            this.page.TempData.Load(this.page.ViewContext, tempDataProvider);
         }
      }

      void PossiblySaveTempData(ITempDataProvider tempDataProvider) {

         if (!this.page.ViewContext.IsChildAction) {
            this.page.TempData.Save(this.page.ViewContext, tempDataProvider);
         }
      }
   }
}
