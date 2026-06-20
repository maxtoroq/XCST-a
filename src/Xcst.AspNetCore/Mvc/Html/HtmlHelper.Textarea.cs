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
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   public struct TextareaArgs {

      [GeneratedCodeReference]
      public object?
      value { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   Textarea(XcstWriter output, string name, TextareaArgs args = default) {

      var modelExplorer = GetModelExplorerFromString(name);

      return GenerateTextarea(output, modelExplorer, name, args);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   TextareaForModel(XcstWriter output, TextareaArgs args = default) =>
      GenerateTextarea(output, this.ModelExplorer, String.Empty, args);

   protected internal DefaultContentDisposable
   GenerateTextarea(XcstWriter output, ModelExplorer modelExplorer, string name, TextareaArgs args) {

      ArgumentNullException.ThrowIfNull(name);

      var value = args.value;
      var @class = args.@class;

      var fullName = FullNameNonEmpty(name);

      this.ModelState.TryGetValue(fullName, out var modelState);

      var valueOrModel = value ?? modelExplorer.Model;

      var valueString = (modelState != null) ?
         modelState.AttemptedValue
         : FormatValue(valueOrModel, modelExplorer.Metadata.EditFormatString);

      // The first newline is always trimmed when a TextArea is rendered, so we add an extra one
      // in case the value being rendered is something like "\r\nHello".

      var text = (!String.IsNullOrEmpty(valueString)) ?
         Environment.NewLine + valueString
         : String.Empty;

      output.WriteStartElement("textarea");

      WriteId(fullName, output);

      output.WriteAttributeString("name", fullName);

      var cssClass = (modelState?.Errors.Count > 0) ?
         ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteBoolean("readonly", modelExplorer.Metadata.IsReadOnly, output);

      if (!String.IsNullOrEmpty(modelExplorer.Metadata.Placeholder)) {
         output.WriteAttributeString("placeholder", modelExplorer.Metadata.Placeholder);
      }

      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);

      return new DefaultContentDisposable(output, elementStarted: true, contentFn);

      void contentFn(XcstWriter output) {
         output.WriteString(text);
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   TextareaFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, TextareaArgs args = default) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateTextarea(output, modelExplorer, expressionString, args);
   }
}
