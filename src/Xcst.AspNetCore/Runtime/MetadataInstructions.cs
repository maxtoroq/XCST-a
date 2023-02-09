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

#region MetadataInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Runtime;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class MetadataInstructions {

   // display name

   public static string
   DisplayName(HtmlHelper html, string name) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, html.ViewData);

      return DisplayNameHelper(modelExplorer, name);
   }

   public static string
   DisplayNameFor<TModel, TProperty>(HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression) {

      var modelExplorer = (typeof(IEnumerable<TModel>).IsAssignableFrom(typeof(TModel))) ?
          ExpressionMetadataProvider.FromLambdaExpression(expression, new ViewDataDictionary<TModel>(html.ViewData.MetadataProvider, html.ViewData.ModelState))
          : ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData);

      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return DisplayNameHelper(modelExplorer, expressionString);
   }

   public static string
   DisplayNameForModel(HtmlHelper html) =>
      DisplayNameHelper(html.ViewData.ModelExplorer, String.Empty);

   internal static string
   DisplayNameHelper(ModelExplorer modelExplorer, string htmlFieldName) {

      var metadata = modelExplorer.Metadata;

      // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
      // This is similar to how the LabelHelpers get the text of a label.

      var resolvedDisplayName = metadata.DisplayName
         ?? metadata.PropertyName
         ?? htmlFieldName.Split('.').Last();

      return resolvedDisplayName;
   }

   // display text

   public static void
   DisplayText(HtmlHelper html, ISequenceWriter<string> output, string name) =>
      DisplayTextHelper(html, output, ExpressionMetadataProvider.FromStringExpression(name, html.ViewData));

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static void
   DisplayTextFor<TModel, TResult>(HtmlHelper<TModel> html, ISequenceWriter<string> output, Expression<Func<TModel, TResult>> expression) =>
      DisplayTextHelper(html, output, ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData));

   internal static void
   DisplayTextHelper(HtmlHelper html, ISequenceWriter<string> output, ModelExplorer modelExplorer) {

      var text = modelExplorer.GetSimpleDisplayText();

      if (modelExplorer.Metadata.HtmlEncode) {
         output.WriteString(text);
      } else {
         output.WriteRaw(text);
      }
   }

   public static string
   DisplayString(HtmlHelper html, string name) =>
      DisplayStringHelper(html, ExpressionMetadataProvider.FromStringExpression(name, html.ViewData));

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static string
   DisplayStringFor<TModel, TResult>(HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression) =>
      DisplayStringHelper(html, ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData));

   internal static string
   DisplayStringHelper(HtmlHelper html, ModelExplorer modelExplorer) =>
      modelExplorer.GetSimpleDisplayText();
}
