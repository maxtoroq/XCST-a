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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class ValidationInstructions {

   [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", Justification = "'validationMessage' refers to the message that will be rendered by the ValidationMessage helper.")]
   public static ElementEndingDisposable
   ValidationMessage(HtmlHelper htmlHelper, XcstWriter output, string modelName, bool hasDefaultText = false,
         string? @class = null) {

      if (modelName is null) throw new ArgumentNullException(nameof(modelName));

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(modelName, htmlHelper.ViewData);

      return ValidationMessageHelper(htmlHelper, output, modelExplorer, modelName, hasDefaultText, @class);
   }

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static ElementEndingDisposable
   ValidationMessageFor<TModel, TProperty>(
         HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, bool hasDefaultText = false,
         string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValidationMessageHelper(htmlHelper, output, modelExplorer, expressionString, hasDefaultText, @class);
   }

   [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Normalization to lowercase is a common requirement for JavaScript and HTML values")]
   internal static ElementEndingDisposable
   ValidationMessageHelper(
         HtmlHelper htmlHelper, XcstWriter output, ModelExplorer modelExplorer, string expression, bool hasDefaultText,
         string? @class) {

      var viewData = htmlHelper.ViewData;

      var modelName = viewData.TemplateInfo.GetFullHtmlFieldName(expression);
      var formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

      if (!viewData.ModelState.ContainsKey(modelName)
         && formContext is null) {

         return new ElementEndingDisposable(output, elementStarted: false);
      }

      var modelState = viewData.ModelState[modelName];
      var modelErrors = modelState?.Errors;

      var modelError = (modelErrors is null || modelErrors.Count == 0) ? null
         : (modelErrors.FirstOrDefault(m => !String.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0]);

      if (modelError is null
         && formContext is null) {

         return new ElementEndingDisposable(output, elementStarted: false);
      }

      var tag = htmlHelper.ViewContext.ValidationMessageElement;

      var validationClass = (modelError != null) ?
         HtmlHelper.ValidationMessageCssClassName
         : HtmlHelper.ValidationMessageValidCssClassName;

      output.WriteStartElement(tag);
      HtmlAttributeHelper.WriteCssClass(@class, validationClass, output);

      if (formContext != null) {

         var replaceValidationMessageContents = !hasDefaultText;

         output.WriteAttributeString("data-valmsg-for", modelName);
         output.WriteAttributeString("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());
      }

      if (!hasDefaultText && modelError != null) {
         output.WriteString(GetModelErrorMessageOrDefault(modelError, modelState!, modelExplorer));
      }

      return new ElementEndingDisposable(output);
   }

   public static ValidationSummaryDisposable
   ValidationSummary(HtmlHelper htmlHelper, XcstWriter output, bool includePropertyErrors = false,
         string? @class = null) {

      if (htmlHelper is null) throw new ArgumentNullException(nameof(htmlHelper));

      var formContext = htmlHelper.ViewContext.GetFormContextForClientValidation();

      if (htmlHelper.ViewData.ModelState.IsValid) {

         if (!htmlHelper.ViewContext.ClientValidationEnabled
            || !includePropertyErrors) {

            return new ValidationSummaryDisposable(output, null);
         }
      }

      var validationClass = (htmlHelper.ViewData.ModelState.IsValid) ?
         HtmlHelper.ValidationSummaryValidCssClassName
         : HtmlHelper.ValidationSummaryCssClassName;

      output.WriteStartElement("div");
      HtmlAttributeHelper.WriteCssClass(@class, validationClass, output);

      if (formContext != null
         && includePropertyErrors) {

         // Only put errors in the validation summary if they're supposed to be included there
         output.WriteAttributeString("data-valmsg-summary", "true");
      }

      void listBuilder(XcstWriter output) {

         output.WriteStartElement("ul");

         var empty = true;
         var modelStates = GetModelStateList(htmlHelper, includePropertyErrors);

         foreach (var modelState in modelStates) {
            foreach (var modelError in modelState.Errors) {

               var errorText = GetModelErrorMessageOrDefault(modelError);

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
      };

      return new ValidationSummaryDisposable(output, listBuilder);
   }

   // Returns non-null list of model states, which caller will render in order provided.

   static IEnumerable<ModelStateEntry>
   GetModelStateList(HtmlHelper htmlHelper, bool includePropertyErrors) {

      var viewData = htmlHelper.ViewData;

      if (!includePropertyErrors) {

         if (viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out var ms)
            && ms != null) {

            return new ModelStateEntry[] { ms };
         }

         return Array.Empty<ModelStateEntry>();
      }

      // Sort modelStates to respect the ordering in the metadata.
      // ModelState doesn't refer to ModelMetadata, but we can correlate via the property name.

      var ordering = new Dictionary<string, int>();
      var metadata = viewData.ModelMetadata;

      if (metadata != null) {
         foreach (var m in metadata.Properties) {
            ordering[m.PropertyName!] = m.Order;
         }
      }

      return
         from kv in viewData.ModelState
         let name = kv.Key
         orderby ordering.GetOrDefault(name, ModelMetadata.DefaultOrder)
         select kv.Value;
   }

   static string
   GetModelErrorMessageOrDefault(ModelError error) {

      if (!String.IsNullOrEmpty(error.ErrorMessage)) {
         return error.ErrorMessage;
      }

      return String.Empty;
   }

   static string?
   GetModelErrorMessageOrDefault(ModelError error, ModelStateEntry modelState, ModelExplorer modelExplorer) {

      if (!String.IsNullOrEmpty(error.ErrorMessage)) {
         return error.ErrorMessage;
      }

      var arg = modelState.AttemptedValue ?? "null";

      return modelExplorer.Metadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(arg);
   }
}

public class ValidationSummaryDisposable : ElementEndingDisposable {

   readonly XcstWriter
   _output;

   readonly Action<XcstWriter>?
   _listBuilder;

   bool
   _eoc;

   bool
   _disposed;

   internal
   ValidationSummaryDisposable(XcstWriter output, Action<XcstWriter>? listBuilder)
      : base(output, listBuilder != null) {

      _output = output;
      _listBuilder = listBuilder;
   }

   public void
   EndOfConstructor() {
      _eoc = true;
   }

   public ValidationSummaryDisposable
   NoConstructor() {
      _eoc = this.ElementStarted;
      return this;
   }

   protected override void
   Dispose(bool disposing) {

      if (_disposed) {
         return;
      }

      // don't write list when end of constructor is not reached
      // e.g. an exception occurred, c:return, etc.

      if (disposing
         && _eoc) {

         _listBuilder?.Invoke(_output);
      }

      base.Dispose(disposing);

      _disposed = true;
   }
}
