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
   public struct NewModelHelperArgs {

      [GeneratedCodeReference]
      public string?
      htmlFieldPrefix { get; set; }

      [GeneratedCodeReference]
      public string?
      formMethod { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public HtmlHelper<TModel>
   NewModelHelper<TModel>(IXcstPackage currentPackage, TModel? model, NewModelHelperArgs args = default) {

      var htmlFieldPrefix = args.htmlFieldPrefix;
      var formMethod = args.formMethod;

      ArgumentNullException.ThrowIfNull(currentPackage);

      var container = new ViewDataContainer(
         this.MetadataProvider.GetModelExplorerForType(typeof(TModel), model));

      var newViewContext = new ViewContext(this.ViewContext, null, currentPackage) {
         HtmlFieldPrefix = GetFullHtmlFieldName(htmlFieldPrefix),
         FormContext = null,
         MembersOptions = null,
         ViewParameters = null,
         VisitedObjects = null,
      };

      if (formMethod != null) {
         newViewContext.SetFormMethodString(formMethod);
      }

      return new HtmlHelper<TModel>(newViewContext, container, this.MetadataProvider);
   }
}
