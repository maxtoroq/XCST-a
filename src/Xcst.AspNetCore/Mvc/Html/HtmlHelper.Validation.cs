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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   /// <exclude/>
   public ElementEndingDisposable
   ValidationMessage(XcstWriter output, string modelName, bool hasDefaultText = false,
         string? @class = null) {

      if (modelName is null) throw new ArgumentNullException(nameof(modelName));

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(modelName, this.ViewData);

      return GenerateValidationMessage(output, modelExplorer, modelName, hasDefaultText, @class);
   }

   protected internal ElementEndingDisposable
   GenerateValidationMessage(
         XcstWriter output, ModelExplorer modelExplorer, string expression, bool hasDefaultText, string? @class) {

      var viewData = this.ViewData;

      var modelName = viewData.TemplateInfo.GetFullHtmlFieldName(expression);
      var formContext = this.ViewContext.GetFormContextForClientValidation();

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

      if (!hasDefaultText && modelError != null) {
         output.WriteString(getModelErrorMessageOrDefault(modelError, modelState!, modelExplorer));
      }

      return new ElementEndingDisposable(output);

      static string? getModelErrorMessageOrDefault(ModelError error, ModelStateEntry modelState, ModelExplorer modelExplorer) {

         if (!String.IsNullOrEmpty(error.ErrorMessage)) {
            return error.ErrorMessage;
         }

         var arg = modelState.AttemptedValue ?? "null";

         return modelExplorer.Metadata.ModelBindingMessageProvider.ValueIsInvalidAccessor.Invoke(arg);
      }
   }

   /// <exclude/>
   public ValidationSummaryDisposable
   ValidationSummary(XcstWriter output, bool includePropertyErrors = false,
         string? @class = null) {

      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (this.ViewData.ModelState.IsValid) {

         if (!this.ViewContext.ClientValidationEnabled
            || !includePropertyErrors) {

            return new ValidationSummaryDisposable(output, null);
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

      return new ValidationSummaryDisposable(output, listBuilder);

      void listBuilder(XcstWriter output) {

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

   /// <exclude/>
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
}

partial class HtmlHelper<TModel> {

   /// <exclude/>
   public ElementEndingDisposable
   ValidationMessageFor<TResult>(
         XcstWriter output, Expression<Func<TModel, TResult>> expression, bool hasDefaultText = false,
         string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateValidationMessage(output, modelExplorer, expressionString, hasDefaultText, @class);
   }
}
