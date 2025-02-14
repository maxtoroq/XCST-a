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
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   Textarea(XcstWriter output, string name, object? value = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateTextarea(output, modelExplorer, name, value, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   TextareaForModel(XcstWriter output, object? value = null, string? @class = null) =>
      GenerateTextarea(output, this.ViewData.ModelExplorer, String.Empty, value, @class);

   protected internal DefaultContentDisposable
   GenerateTextarea(XcstWriter output, ModelExplorer modelExplorer, string name, object? value, string? @class) {

      ArgumentNullException.ThrowIfNull(name);

      var fullName = FullNameNonEmpty(name);

      this.ViewData.ModelState.TryGetValue(fullName, out var modelState);

      string? valueString;

      if (modelState != null) {
         valueString = modelState.AttemptedValue;
      } else {

         var format = (UsingFormattedModelValue(name)) ? null
            : modelExplorer.Metadata.EditFormatString;

         valueString = FormatValue(value ?? modelExplorer.Model, format);
      }

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
   TextareaFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, string? @class = null) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateTextarea(output, modelExplorer, expressionString, value: null, @class);
   }
}
