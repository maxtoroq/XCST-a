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

#region LabelInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

using HtmlAttribs = IDictionary<string, object>;

/// <exclude/>
public static class LabelInstructions {

   public static void
   Label(HtmlHelper html, XcstWriter output, string expression, string? labelText = null, HtmlAttribs? htmlAttributes = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, html.ViewData);

      LabelHelper(html, output, modelExplorer, expression, labelText, htmlAttributes);
   }

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static void
   LabelFor<TModel, TValue>(HtmlHelper<TModel> html, XcstWriter output, Expression<Func<TModel, TValue>> expression, string? labelText = null,
         HtmlAttribs? htmlAttributes = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      LabelHelper(html, output, modelExplorer, expressionString, labelText, htmlAttributes);
   }

   public static void
   LabelForModel(HtmlHelper html, XcstWriter output, string? labelText = null, HtmlAttribs? htmlAttributes = null) =>
      LabelHelper(html, output, html.ViewData.ModelExplorer, String.Empty, labelText, htmlAttributes);

   internal static void
   LabelHelper(HtmlHelper html, XcstWriter output, ModelExplorer modelExplorer, string expression, string? labelText = null, HtmlAttribs? htmlAttributes = null) {

      var htmlFieldName = expression;
      var metadata = modelExplorer.Metadata;

      var resolvedLabelText = labelText
         ?? metadata.DisplayName
         ?? metadata.PropertyName
         ?? htmlFieldName.Split('.').Last();

      if (String.IsNullOrEmpty(resolvedLabelText)) {
         return;
      }

      var fullFieldName = html.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName);

      output.WriteStartElement("label");
      output.WriteAttributeString("for", TagBuilder.CreateSanitizedId(fullFieldName));
      HtmlAttributeHelper.WriteAttributes(htmlAttributes, output);
      output.WriteString(resolvedLabelText);
      output.WriteEndElement();
   }
}
