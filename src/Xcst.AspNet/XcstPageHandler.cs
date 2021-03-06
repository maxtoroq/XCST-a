﻿// Copyright 2015 Max Toro Q.
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
using System.Web.SessionState;

namespace Xcst.Web {

   public class XcstPageHandler : IHttpHandler, IRequiresSessionState {

      readonly XcstPage _page;

      public bool IsReusable => false;

      public XcstPageHandler(XcstPage page) {

         if (page is null) throw new ArgumentNullException(nameof(page));

         _page = page;
      }

      public virtual void ProcessRequest(HttpContext context) {

         InitializePage(_page, new HttpContextWrapper(context));
         AddFileDependencies(_page, _page.Response);
         RenderPage(_page, _page.Context);
      }

      protected virtual void InitializePage(XcstPage page, HttpContextBase context) =>
         page.Context = context;

      protected virtual void RenderPage(XcstPage page, HttpContextBase context) {

         XcstEvaluator.Using((object)page)
            .CallInitialTemplate()
            .OutputTo(context.Response.Output)
            .Run();
      }

      static void AddFileDependencies(object instance, HttpResponseBase response) {

         if (instance is IFileDependent fileDependent) {

            string[] files = fileDependent.FileDependencies;

            if (files != null) {
               response.AddFileDependencies(files);
            }
         }
      }
   }
}
