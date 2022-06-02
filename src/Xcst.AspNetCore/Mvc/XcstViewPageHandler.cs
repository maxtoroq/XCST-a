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
using System.Web.Mvc;
using Microsoft.AspNetCore.Http;

namespace Xcst.Web.Mvc;

public class XcstViewPageHandler : XcstPageHandler {

   readonly XcstViewPage
   _page;

   private ViewContext
   ViewContext => _page.ViewContext;

   public
   XcstViewPageHandler(XcstViewPage page)
      : base(page) {

      _page = page;
   }

   protected override void
   InitializePage(XcstPage page, HttpContext context) {

      base.InitializePage(page, context);

      _page.ViewContext = new ViewContext(context);
   }

   protected override void
   RenderPage(XcstPage page, HttpContext context) {

      // TempData is loaded lazily, see XcstViewPage

      try {
         RenderViewPage((XcstViewPage)page, context);
      } finally {
         if (_page.HasTempData) {
            _page.TempData.Save(this.ViewContext, this.ViewContext.TempDataProvider);
         }
      }
   }

   protected virtual void
   RenderViewPage(XcstViewPage page, HttpContext context) =>
      base.RenderPage(page, context);
}
