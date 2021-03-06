﻿// Copyright 2016 Max Toro Q.
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

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public static class HtmlHelperFactory {

      public static HtmlHelper<TModel> ForModel<TModel>(
            HtmlHelper currentHtml,
            TModel model,
            string? htmlFieldPrefix = null) {

         if (currentHtml is null) throw new ArgumentNullException(nameof(currentHtml));

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

         // new ViewContext resets FormContext
         ViewContext newViewContext = currentHtml.ViewContext.Clone(viewData: container.ViewData);

         return currentHtml.Clone<TModel>(newViewContext, container);
      }

      internal static HtmlHelper ForMemberTemplate(HtmlHelper currentHtml, ModelMetadata memberMetadata) {

         if (currentHtml is null) throw new ArgumentNullException(nameof(currentHtml));
         if (memberMetadata is null) throw new ArgumentNullException(nameof(memberMetadata));

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

         return currentHtml.Clone(currentHtml.ViewContext, container);
      }

      class ViewDataContainer : IViewDataContainer {

#pragma warning disable CS8618
         public ViewDataDictionary ViewData { get; set; }
#pragma warning restore CS8618
      }
   }
}
