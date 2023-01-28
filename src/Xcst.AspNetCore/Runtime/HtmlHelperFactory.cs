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
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class HtmlHelperFactory {

   public static HtmlHelper<TModel>
   ForModel<TModel>(HtmlHelper currentHtml, TModel model, string? htmlFieldPrefix = null) {

      if (currentHtml is null) throw new ArgumentNullException(nameof(currentHtml));

      var currentViewData = currentHtml.ViewData;

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

         var templateInfo = container.ViewData.TemplateInfo;
         templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);
      }

      // new ViewContext resets FormContext
      var newViewContext = new ViewContext(currentHtml.ViewContext);

      return new HtmlHelper<TModel>(newViewContext, container);
   }

   internal static HtmlHelper
   ForMemberTemplate(HtmlHelper currentHtml, ModelExplorer memberExplorer) {

      if (currentHtml is null) throw new ArgumentNullException(nameof(currentHtml));
      if (memberExplorer is null) throw new ArgumentNullException(nameof(memberExplorer));

      var currentViewData = currentHtml.ViewData;

      var container = new ViewDataContainer {
         ViewData = new ViewDataDictionary(currentViewData) {
            Model = memberExplorer.Model,
            ModelExplorer = memberExplorer,
            TemplateInfo = new TemplateInfo {
               HtmlFieldPrefix = currentViewData.TemplateInfo.GetFullHtmlFieldName(memberExplorer.Metadata.PropertyName)
            }
         }
      };

      return new HtmlHelper(currentHtml.ViewContext, container);
   }

   class ViewDataContainer : IViewDataContainer {

#pragma warning disable CS8618
      public ViewDataDictionary
      ViewData { get; set; }
#pragma warning restore CS8618
   }
}
