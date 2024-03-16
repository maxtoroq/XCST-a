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
using System.Globalization;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   public TextareaDisposable
   Textarea(XcstWriter output, string name, object? value = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      if (value != null) {
         modelExplorer = new ModelExplorer(this.ViewData.MetadataProvider, modelExplorer.Container, modelExplorer.Metadata, value);
      }

      return GenerateTextarea(output, modelExplorer, name, @class);
   }

   protected TextareaDisposable
   GenerateTextarea(XcstWriter output, ModelExplorer modelExplorer, string name,
         string? @class, string? innerHtmlPrefix = null) {

      var fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      output.WriteStartElement("textarea");
      WriteId(fullName, output);
      output.WriteAttributeString("name", fullName);

      this.ViewData.ModelState.TryGetValue(fullName, out var modelState);

      var cssClass = (modelState?.Errors.Count > 0) ?
         ValidationInputCssClassName
         : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);

      var value = (modelState != null) ? modelState.AttemptedValue
         : (modelExplorer.Model != null) ? Convert.ToString(modelExplorer.Model, CultureInfo.CurrentCulture)
         : String.Empty;

      // The first newline is always trimmed when a TextArea is rendered, so we add an extra one
      // in case the value being rendered is something like "\r\nHello".

      var text = (!String.IsNullOrEmpty(value)) ?
         (innerHtmlPrefix ?? Environment.NewLine) + value
         : value ?? String.Empty;

      return new TextareaDisposable(output, text);
   }

   [GeneratedCodeReference]
   public class TextareaDisposable : ElementEndingDisposable {

      readonly XcstWriter
      _output;

      readonly string
      _text;

      bool
      _eoc;

      bool
      _disposed;

      internal
      TextareaDisposable(XcstWriter output, string text)
         : base(output, true) {

         _output = output;
         _text = text;
      }

      public void
      EndOfConstructor() {
         _eoc = true;
      }

      public TextareaDisposable
      NoConstructor() {
         _eoc = true;
         return this;
      }

      protected override void
      Dispose(bool disposing) {

         if (_disposed) {
            return;
         }

         // don't write text when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (disposing
            && _eoc) {

            _output.WriteString(_text);
         }

         base.Dispose(disposing);

         _disposed = true;
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public TextareaDisposable
   TextareaFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, string? @class = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateTextarea(output, modelExplorer, expressionString, @class);
   }
}
