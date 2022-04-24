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
using System.Web.Mvc;
using Xcst.Runtime;

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public static class MetadataInstructions {

      // display name

      public static string
      DisplayName(HtmlHelper html, string name) {

         var metadata = ModelMetadata.FromStringExpression(name, html.ViewData);

         return DisplayNameHelper(metadata, name);
      }

      public static string
      DisplayNameFor<TModel, TProperty>(HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression) {

         ModelMetadata metadata;

         if (typeof(IEnumerable<TModel>).IsAssignableFrom(typeof(TModel))) {
            metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>());
         } else {
            metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
         }

         var expressionString = ExpressionHelper.GetExpressionText(expression);

         return DisplayNameHelper(metadata, expressionString);
      }

      public static string
      DisplayNameForModel(HtmlHelper html) =>
         DisplayNameHelper(html.ViewData.ModelMetadata, String.Empty);

      internal static string
      DisplayNameHelper(ModelMetadata metadata, string htmlFieldName) {

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
         DisplayTextHelper(html, output, ModelMetadata.FromStringExpression(name, html.ViewData));

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void
      DisplayTextFor<TModel, TResult>(HtmlHelper<TModel> html, ISequenceWriter<string> output, Expression<Func<TModel, TResult>> expression) =>
         DisplayTextHelper(html, output, ModelMetadata.FromLambdaExpression(expression, html.ViewData));

      internal static void
      DisplayTextHelper(HtmlHelper html, ISequenceWriter<string> output, ModelMetadata metadata) {

         var text = metadata.SimpleDisplayText;

         if (metadata.HtmlEncode) {
            output.WriteString(text);
         } else {
            output.WriteRaw(text);
         }
      }

      public static string
      DisplayString(HtmlHelper html, string name) =>
         DisplayStringHelper(html, ModelMetadata.FromStringExpression(name, html.ViewData));

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static string
      DisplayStringFor<TModel, TResult>(HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression) =>
         DisplayStringHelper(html, ModelMetadata.FromLambdaExpression(expression, html.ViewData));

      internal static string
      DisplayStringHelper(HtmlHelper html, ModelMetadata metadata) =>
         metadata.SimpleDisplayText;
   }
}
