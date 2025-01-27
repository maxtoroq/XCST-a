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
   public TextareaDisposable
   Textarea(XcstWriter output, string name, object? value = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      if (value != null) {
         modelExplorer = new ModelExplorer(this.ViewData.MetadataProvider, modelExplorer.Container, modelExplorer.Metadata, value);
      }

      return GenerateTextarea(output, modelExplorer, name, default(object), @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TextareaDisposable
   TextareaForModel(XcstWriter output, object? value = null, string? @class = null) =>
      Textarea(output, String.Empty, value, @class);

   protected internal TextareaDisposable
   GenerateTextarea(XcstWriter output, ModelExplorer modelExplorer, string name, object? value, string? @class) {

      ArgumentNullException.ThrowIfNull(name);

      var fullName = FullNameNonEmpty(name);

      this.ViewData.ModelState.TryGetValue(fullName, out var modelState);

      var valueString = (modelState != null) ? modelState.AttemptedValue
         : FormatValue(value ?? modelExplorer.Model, null);

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
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);

      return new TextareaDisposable(output, text);
   }

   [EditorBrowsable(EditorBrowsableState.Never)]
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

      [GeneratedCodeReference]
      public void
      EndOfConstructor() {
         _eoc = true;
      }

      [GeneratedCodeReference]
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
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TextareaDisposable
   TextareaFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, string? @class = null) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateTextarea(output, modelExplorer, expressionString, default(object), @class);
   }
}
