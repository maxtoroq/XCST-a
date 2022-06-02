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

using System.Web.Mvc;

namespace Xcst.Web;

static class FrameworkExtensions {

   internal static ViewContext
   Clone(this ViewContext context) =>
      new ViewContext(context) {
         ClientValidationEnabled = context.ClientValidationEnabled,
         UnobtrusiveJavaScriptEnabled = context.UnobtrusiveJavaScriptEnabled,
         ValidationMessageElement = context.ValidationMessageElement,
         ValidationSummaryMessageElement = context.ValidationSummaryMessageElement
      };

   internal static HtmlHelper
   Clone(this HtmlHelper currentHtml, ViewContext viewContext, IViewDataContainer container) =>
      new HtmlHelper(viewContext, container) {
         Html5DateRenderingMode = currentHtml.Html5DateRenderingMode
      };

   internal static HtmlHelper<TModel>
   Clone<TModel>(this HtmlHelper currentHtml, ViewContext viewContext, IViewDataContainer container) =>
      new HtmlHelper<TModel>(viewContext, container) {
         Html5DateRenderingMode = currentHtml.Html5DateRenderingMode
      };
}
