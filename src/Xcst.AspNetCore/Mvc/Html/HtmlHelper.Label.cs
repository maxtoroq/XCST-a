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

#region HtmlHelper is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public struct LabelArgs {

      [GeneratedCodeReference]
      public bool
      hasDefaultText { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   Label(XcstWriter output, string expression, LabelArgs args = default) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return GenerateLabel(output, modelExplorer, expression, args);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   LabelForModel(XcstWriter output, LabelArgs args = default) =>
      GenerateLabel(output, this.ModelExplorer, String.Empty, args);

   protected internal DefaultContentDisposable
   GenerateLabel(XcstWriter output, ModelExplorer modelExplorer, string expression, LabelArgs args) {

      var hasDefaultText = args.hasDefaultText;
      var @class = args.@class;

      var metadata = modelExplorer.Metadata;

      var fullFieldName = GenerateName(expression);
      var id = GenerateIdFromName(fullFieldName);
      var reqClass = this.ViewContext.Options.LabelCssClass?.Invoke(metadata.IsRequired);

      output.WriteStartElement("label");
      output.WriteAttributeString("for", id);

      WriteCssClass(@class, reqClass, output);

      var text = (!hasDefaultText) ?
         metadata.DisplayName
            ?? metadata.PropertyName
            ?? expression.Split('.').Last()
         : null;

      return new DefaultContentDisposable(output, elementStarted: true, contentFn);

      void contentFn(XcstWriter output) {

         if (text != null) {
            output.WriteString(text);
         }
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   LabelFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, LabelArgs args = default) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateLabel(output, modelExplorer, expressionString, args);
   }
}
