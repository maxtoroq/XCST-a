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

#region TextAreaInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

using HtmlAttribs = IDictionary<string, object>;

/// <exclude/>
public static class TextAreaInstructions {

   public static IDisposable
   TextArea(HtmlHelper htmlHelper, XcstWriter output, string name, object? value = null,
         int? rows = null, int? cols = null, HtmlAttribs? htmlAttributes = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, htmlHelper.ViewData);

      if (value != null) {
         modelExplorer = new ModelExplorer(htmlHelper.ViewData.MetadataProvider, modelExplorer.Container, modelExplorer.Metadata, value);
      }

      return TextAreaHelper(htmlHelper, output, modelExplorer, name, rows, cols, htmlAttributes);
   }

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static IDisposable
   TextAreaFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression,
         int? rows = null, int? cols = null, HtmlAttribs? htmlAttributes = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return TextAreaHelper(htmlHelper, output, modelExplorer, expressionString, rows, cols, htmlAttributes);
   }

   [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "If this fails, it is because the string-based version had an empty 'name' parameter")]
   internal static IDisposable
   TextAreaHelper(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer modelExplorer, string name,
         int? rows, int? cols, HtmlAttribs? htmlAttributes, string? innerHtmlPrefix = null) {

      var fullName = htmlHelper.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      output.WriteStartElement("textarea");
      HtmlAttributeHelper.WriteId(fullName, output);
      output.WriteAttributeString("name", fullName);

      if (rows != null) {
         output.WriteAttributeString("rows", rows.Value.ToString(CultureInfo.InvariantCulture));
      }

      if (cols != null) {
         output.WriteAttributeString("cols", cols.Value.ToString(CultureInfo.InvariantCulture));
      }

      htmlHelper.ViewData.ModelState.TryGetValue(fullName, out var modelState);

      // If there are any errors for a named field, we add the css attribute.

      var cssClass = (modelState?.Errors.Count > 0) ?
         HtmlHelper.ValidationInputCssClassName
         : null;

      HtmlAttributeHelper.WriteClass(cssClass, htmlAttributes, output);
      HtmlAttributeHelper.WriteAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, modelExplorer), output);

      // name cannnot be overridden, and class was already written

      HtmlAttributeHelper.WriteAttributes(
         htmlAttributes,
         output,
         excludeFn: n => n is "name" or "class");

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
}

class TextareaDisposable : ElementEndingDisposable {

   readonly XcstWriter
   _output;

   readonly string
   _text;

   bool
   _disposed;

   internal
   TextareaDisposable(XcstWriter output, string text)
      : base(output, true) {

      _output = output;
      _text = text;
   }

   protected override void
   Dispose(bool disposing) {

      if (_disposed) {
         return;
      }

      if (disposing) {
         _output.WriteString(_text);
      }

      base.Dispose(disposing);

      _disposed = true;
   }
}
