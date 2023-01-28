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

#region ValidationInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

using HtmlAttribs = IDictionary<string, object>;

/// <exclude/>
public static class ValidationInstructions {

   [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", Justification = "'validationMessage' refers to the message that will be rendered by the ValidationMessage helper.")]
   public static void
   ValidationMessage(HtmlHelper htmlHelper, XcstWriter output, string modelName, string? validationMessage = null, HtmlAttribs? htmlAttributes = null,
         string? tag = null) {

      if (modelName is null) throw new ArgumentNullException(nameof(modelName));

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(modelName, htmlHelper.ViewData);

      ValidationMessageHelper(htmlHelper, output, modelExplorer, modelName, validationMessage, htmlAttributes, tag);
   }

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static void
   ValidationMessageFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, string? validationMessage = null,
         HtmlAttribs? htmlAttributes = null, string? tag = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      ValidationMessageHelper(htmlHelper, output, modelExplorer, expressionString, validationMessage, htmlAttributes, tag);
   }

   [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Normalization to lowercase is a common requirement for JavaScript and HTML values")]
   internal static void
   ValidationMessageHelper(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer modelExplorer, string expression, string? validationMessage,
         HtmlAttribs? htmlAttributes, string? tag) {

      var viewData = htmlHelper.ViewData;

      var modelName = viewData.TemplateInfo.GetFullHtmlFieldName(expression);
      var formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

      if (!viewData.ModelState.ContainsKey(modelName)
         && formContext is null) {

         return;
      }

      var modelState = viewData.ModelState[modelName];
      var modelErrors = modelState?.Errors;

      var modelError = (modelErrors is null || modelErrors.Count == 0) ? null
         : (modelErrors.FirstOrDefault(m => !String.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0]);

      if (modelError is null
         && formContext is null) {

         return;
      }

      if (String.IsNullOrEmpty(tag)) {
         tag = htmlHelper.ViewContext.ValidationMessageElement;
      }

      var validationClass = (modelError != null) ?
         HtmlHelper.ValidationMessageCssClassName
         : HtmlHelper.ValidationMessageValidCssClassName;

      output.WriteStartElement(tag);
      HtmlAttributeHelper.WriteClass(validationClass, htmlAttributes, output);

      if (formContext != null) {

         var replaceValidationMessageContents = String.IsNullOrEmpty(validationMessage);

         if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled) {

            output.WriteAttributeString("data-valmsg-for", modelName);
            output.WriteAttributeString("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());
         }
      }

      // class was already written

      HtmlAttributeHelper.WriteAttributes(htmlAttributes, output, excludeFn: n => n == "class");

      if (!String.IsNullOrEmpty(validationMessage)) {
         output.WriteString(validationMessage);

      } else if (modelError != null) {
         output.WriteString(GetUserErrorMessageOrDefault(modelError, modelState));
      }

      output.WriteEndElement();
   }

   public static void
   ValidationSummary(HtmlHelper htmlHelper, XcstWriter output, bool includePropertyErrors = false, string? message = null, HtmlAttribs? htmlAttributes = null,
         string? headingTag = null) {

      if (htmlHelper is null) throw new ArgumentNullException(nameof(htmlHelper));

      var formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

      if (htmlHelper.ViewData.ModelState.IsValid) {

         if (formContext is null) {

            // No client side validation

            return;
         }

         // TODO: This isn't really about unobtrusive; can we fix up non-unobtrusive to get rid of this, too?

         if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled
            && !includePropertyErrors) {

            // No client-side updates

            return;
         }
      }

      var validationClass = (htmlHelper.ViewData.ModelState.IsValid) ?
         HtmlHelper.ValidationSummaryValidCssClassName
         : HtmlHelper.ValidationSummaryCssClassName;

      output.WriteStartElement("div");
      HtmlAttributeHelper.WriteClass(validationClass, htmlAttributes, output);

      if (formContext != null) {

         if (htmlHelper.ViewContext.UnobtrusiveJavaScriptEnabled) {

            if (includePropertyErrors) {

               // Only put errors in the validation summary if they're supposed to be included there

               output.WriteAttributeString("data-valmsg-summary", "true");
            }
         }
      }

      // class was already written

      HtmlAttributeHelper.WriteAttributes(htmlAttributes, output, excludeFn: n => n == "class");

      if (!String.IsNullOrEmpty(message)) {

         if (String.IsNullOrEmpty(headingTag)) {
            headingTag = htmlHelper.ViewContext.ValidationSummaryMessageElement;
         }

         output.WriteStartElement(headingTag!);
         output.WriteString(message);
         output.WriteEndElement();
      }

      output.WriteStartElement("ul");

      var empty = true;
      var modelStates = GetModelStateList(htmlHelper, includePropertyErrors);

      foreach (var modelState in modelStates) {
         foreach (var modelError in modelState.Errors) {

            var errorText = GetUserErrorMessageOrDefault(modelError, modelState: null);

            if (!String.IsNullOrEmpty(errorText)) {

               empty = false;

               output.WriteStartElement("li");
               output.WriteString(errorText);
               output.WriteEndElement();
            }
         }
      }

      if (empty) {
         output.WriteStartElement("li");
         output.WriteAttributeString("style", "display:none");
         output.WriteEndElement();
      }

      output.WriteEndElement(); // </ul>
      output.WriteEndElement(); // </div>
   }

   // Returns non-null list of model states, which caller will render in order provided.

   static IEnumerable<ModelState>
   GetModelStateList(HtmlHelper htmlHelper, bool includePropertyErrors) {

      var viewData = htmlHelper.ViewData;

      if (!includePropertyErrors) {

         if (viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out var ms)
            && ms != null) {

            return new ModelState[] { ms };
         }

         return new ModelState[0];

      } else {

         // Sort modelStates to respect the ordering in the metadata.
         // ModelState doesn't refer to ModelMetadata, but we can correlate via the property name.

         var ordering = new Dictionary<string, int>();
         var metadata = viewData.ModelMetadata;

         if (metadata != null) {
            foreach (var m in metadata.Properties) {
               ordering[m.PropertyName] = m.Order;
            }
         }

         return
            from kv in viewData.ModelState
            let name = kv.Key
            orderby ordering.GetOrDefault(name, ModelMetadata.DefaultOrder)
            select kv.Value;
      }
   }

   static string?
   GetUserErrorMessageOrDefault(ModelError error, ModelState? modelState) {

      if (!String.IsNullOrEmpty(error.ErrorMessage)) {
         return error.ErrorMessage;
      }

      if (modelState is null) {
         return null;
      }

      var attemptedValue = modelState.Value?.AttemptedValue;

      var messageFormat = XcstWebConfiguration.Instance.EditorTemplates.DefaultValidationMessage?.Invoke()
         ?? "The value '{0}' is invalid.";

      return String.Format(CultureInfo.CurrentCulture, messageFormat, attemptedValue);
   }
}
