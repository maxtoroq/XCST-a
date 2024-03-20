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
using System.ComponentModel;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public HtmlHelper<TModel>
   NewModelHelper<TModel>(IXcstPackage currentPackage, TModel? model, string? htmlFieldPrefix = null) {

      if (currentPackage is null) throw new ArgumentNullException(nameof(currentPackage));

      var currentViewData = this.ViewData;

      // Cannot call new ViewDataDictionary<TModel>(currentViewData)
      // because currentViewData.Model might be incompatible with TModel

      var tempDictionary = new ViewDataDictionary(currentViewData) {
         Model = model
      };

      var container = new ViewDataContainer(
         new ViewDataDictionary<TModel>(tempDictionary) {
            // setting new TemplateInfo clears VisitedObjects cache
            TemplateInfo = new TemplateInfo {
               HtmlFieldPrefix = currentViewData.TemplateInfo.HtmlFieldPrefix
            }
         }
      );

      if (!String.IsNullOrEmpty(htmlFieldPrefix)) {

         var templateInfo = container.ViewData.TemplateInfo;
         templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);
      }

      // new ViewContext resets FormContext
      var newViewContext = new ViewContext(this.ViewContext);

      return new HtmlHelper<TModel>(newViewContext, container, currentPackage);
   }
}
