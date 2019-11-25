// Copyright 2016 Max Toro Q.
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
using System.Collections.Generic;
using System.Web.Mvc;

namespace Xcst.Web.Runtime {

   public static class HtmlHelperFactory {

      public static HtmlHelper<TModel> ForModel<TModel>(
            HtmlHelper currentHtml,
            TModel model,
            string htmlFieldPrefix = null,
            object additionalViewData = null) {

         if (currentHtml == null) throw new ArgumentNullException(nameof(currentHtml));

         ViewDataDictionary currentViewData = currentHtml.ViewData;

         // Cannot call new ViewDataDictionary<TModel>(currentViewData)
         // because currentViewData.Model might be incompatible with TModel

         var tempDictionary = new ViewDataDictionary(currentViewData) {
            Model = model
         };

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary<TModel>(tempDictionary) {

               // setting new TemplateInfo clears VisitedObjects cache
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.HtmlFieldPrefix
               }
            }
         };

         if (!String.IsNullOrEmpty(htmlFieldPrefix)) {

            TemplateInfo templateInfo = container.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);
         }

         if (additionalViewData != null) {

            IDictionary<string, object> additionalParams = additionalViewData as IDictionary<string, object>
               ?? TypeHelpers.ObjectToDictionary(additionalViewData);

            foreach (var kvp in additionalParams) {
               container.ViewData[kvp.Key] = kvp.Value;
            }
         }

         // new ViewContext resets FormContext

         var newViewContext = currentHtml.ViewContext.Clone(viewData: container.ViewData);

         return new HtmlHelper<TModel>(newViewContext, container, currentHtml.RouteCollection) {
            Html5DateRenderingMode = currentHtml.Html5DateRenderingMode
         };
      }

      internal static HtmlHelper ForMemberTemplate(HtmlHelper currentHtml, ModelMetadata memberMetadata) {

         if (currentHtml == null) throw new ArgumentNullException(nameof(currentHtml));
         if (memberMetadata == null) throw new ArgumentNullException(nameof(memberMetadata));

         ViewDataDictionary currentViewData = currentHtml.ViewData;

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary(currentViewData) {
               Model = memberMetadata.Model,
               ModelMetadata = memberMetadata,
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.GetFullHtmlFieldName(memberMetadata.PropertyName)
               }
            }
         };

         return new HtmlHelper(currentHtml.ViewContext, container, currentHtml.RouteCollection) {
            Html5DateRenderingMode = currentHtml.Html5DateRenderingMode
         };
      }

      class ViewDataContainer : IViewDataContainer {

         public ViewDataDictionary ViewData { get; set; }
      }
   }
}
