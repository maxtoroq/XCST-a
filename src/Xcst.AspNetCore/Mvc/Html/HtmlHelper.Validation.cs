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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   ValidationMessage(XcstWriter output, string name, bool hasDefaultText = false, string? @class = null) {

      ArgumentNullException.ThrowIfNull(name);

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateValidationMessage(output, modelExplorer, name, hasDefaultText, @class);
   }

   protected internal DefaultContentDisposable
   GenerateValidationMessage(
         XcstWriter output, ModelExplorer modelExplorer, string name, bool hasDefaultText, string? @class) {

      var viewData = this.ViewData;

      var modelName = viewData.TemplateInfo.GetFullHtmlFieldName(name);
      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (!viewData.ModelState.ContainsKey(modelName)
         && formContext is null) {

         return new DefaultContentDisposable(output, elementStarted: false, null);
      }

      var modelState = viewData.ModelState[modelName];
      var modelErrors = modelState?.Errors;

      var modelError = (modelErrors is null || modelErrors.Count == 0) ? null
         : (modelErrors.FirstOrDefault(m => !String.IsNullOrEmpty(m.ErrorMessage)) ?? modelErrors[0]);

      if (modelError is null
         && formContext is null) {

         return new DefaultContentDisposable(output, elementStarted: false, null);
      }

      var tag = this.ViewContext.ValidationMessageElement;

      var validationClass = (modelError != null) ?
         ValidationMessageCssClassName
         : ValidationMessageValidCssClassName;

      output.WriteStartElement(tag);
      WriteCssClass(@class, validationClass, output);

      if (formContext != null) {

         var replaceValidationMessageContents = !hasDefaultText;

         output.WriteAttributeString("data-valmsg-for", modelName);
         output.WriteAttributeString("data-valmsg-replace", replaceValidationMessageContents.ToString().ToLowerInvariant());
      }

      var text = (!hasDefaultText && modelError != null) ?
         getModelErrorMessageOrDefault(modelError, modelState!, modelExplorer)
         : null;

      return new DefaultContentDisposable(output, elementStarted: true, contentFn);

      void contentFn(XcstWriter output) {

         if (text != null) {
            output.WriteString(text);
         }
      }

      static string? getModelErrorMessageOrDefault(ModelError error, ModelStateEntry modelState, ModelExplorer modelExplorer) {

         if (!String.IsNullOrEmpty(error.ErrorMessage)) {
            return error.ErrorMessage;
         }

         var arg = modelState.AttemptedValue ?? "null";

         return modelExplorer.Metadata.ModelBindingMessageProvider.ValueIsInvalidAccessor.Invoke(arg);
      }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   ValidationSummary(XcstWriter output, bool includePropertyErrors = false, string? @class = null) {

      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (this.ViewData.ModelState.IsValid) {

         if (!this.ViewContext.ClientValidationEnabled
            || !includePropertyErrors) {

            return new DefaultContentDisposable(output, elementStarted: false, null);
         }
      }

      var validationClass = (this.ViewData.ModelState.IsValid) ?
         ValidationSummaryValidCssClassName
         : ValidationSummaryCssClassName;

      output.WriteStartElement("div");
      WriteCssClass(@class, validationClass, output);

      if (formContext != null
         && includePropertyErrors) {

         // Only put errors in the validation summary if they're supposed to be included there
         output.WriteAttributeString("data-valmsg-summary", "true");
      }

      return new DefaultContentDisposable(output, elementStarted: true, contentFn);

      void contentFn(XcstWriter output) {

         output.WriteStartElement("ul");

         var empty = true;
         var modelStates = getModelStateList(includePropertyErrors);

         foreach (var modelState in modelStates) {
            foreach (var modelError in modelState.Errors) {

               var errorText = getModelErrorMessageOrDefault(modelError);

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

      // Returns non-null list of model states, which caller will render in order provided.
      IEnumerable<ModelStateEntry> getModelStateList(bool includePropertyErrors) {

         var viewData = this.ViewData;

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

      static string getModelErrorMessageOrDefault(ModelError error) {

         if (!String.IsNullOrEmpty(error.ErrorMessage)) {
            return error.ErrorMessage;
         }

         return String.Empty;
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public DefaultContentDisposable
   ValidationMessageFor<TResult>(
         XcstWriter output, Expression<Func<TModel, TResult>> expression, bool hasDefaultText = false,
         string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateValidationMessage(output, modelExplorer, expressionString, hasDefaultText, @class);
   }
}
