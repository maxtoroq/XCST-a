// Copyright 2026 Max Toro Q.
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
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public HtmlHelper
   NewFieldsetHelper(string expression) {

      var modelExplorer = GetModelExplorerFromString(expression);
      var newViewContext = NewFieldsetViewContext(expression);

      return new HtmlHelper(newViewContext, () => modelExplorer, this.MetadataProvider);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public HtmlHelper
   NewFieldsetHelperForModel() {

      var expression = String.Empty;
      var modelExplorer = this.ModelExplorer;
      var newViewContext = NewFieldsetViewContext(expression);

      return new HtmlHelper(newViewContext, () => modelExplorer, this.MetadataProvider);
   }

   private protected ViewContext
   NewFieldsetViewContext(string expression) {

      var newViewContext = new ViewContext(this.ViewContext) {
         HtmlFieldPrefix = GenerateName(expression),
      };

      return newViewContext;
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public HtmlHelper<TResult>
   NewFieldsetHelperFor<TResult>(Expression<Func<TModel, TResult?>> expression) {

      var expressionString = ExpressionHelper.GetExpressionText(expression);
      var modelExplorer = GetModelExplorerFromLambda(expression);
      var newViewContext = NewFieldsetViewContext(expressionString);

      return new HtmlHelper<TResult>(newViewContext, () => modelExplorer, this.MetadataProvider);
   }
}
