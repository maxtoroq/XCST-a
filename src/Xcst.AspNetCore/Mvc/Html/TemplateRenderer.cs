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

#region TemplateRenderer is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Runtime;
using Xcst.Web.Builder;

namespace Xcst.Web.Mvc;

using TemplateAction = Action<HtmlHelper, ISequenceWriter<object>>;

sealed class TemplateRenderer {

   static readonly FrozenDictionary<string, TemplateAction>
   _defaultDisplayActions = new KeyValuePair<string, TemplateAction>[] {

      // System.ComponentModel.DataAnnotations.DataType
      new("EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate),
      new("Html", DefaultDisplayTemplates.HtmlTemplate),
      new("ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate),
      new("Text", DefaultDisplayTemplates.StringTemplate),
      new("Url", DefaultDisplayTemplates.UrlTemplate),

      // primitive
      new("Boolean", DefaultDisplayTemplates.BooleanTemplate),
      new("Decimal", DefaultDisplayTemplates.DecimalTemplate),
      new("Enum", DefaultDisplayTemplates.EnumTemplate),
      new("String", DefaultDisplayTemplates.StringTemplate),

      // other
      new("Month", DefaultDisplayTemplates.MonthTemplate),

      // "special" templates
      new("Object", DefaultDisplayTemplates.ObjectTemplate),
      new("HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate),
      new("Collection", DefaultDisplayTemplates.CollectionTemplate),
   }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

   static readonly FrozenDictionary<string, TemplateAction>
   _defaultEditorActions = new KeyValuePair<string, TemplateAction>[] {

      // System.ComponentModel.DataAnnotations.DataType
      new("Date", DefaultEditorTemplates.StringTemplate),
      new("DateTime", DefaultEditorTemplates.StringTemplate),
      new("DateTime-local", DefaultEditorTemplates.StringTemplate),
      new("EmailAddress", DefaultEditorTemplates.StringTemplate),
      new("MultilineText", DefaultEditorTemplates.MultilineTextTemplate),
      new("Password", DefaultEditorTemplates.PasswordTemplate),
      new("PhoneNumber", DefaultEditorTemplates.StringTemplate),
      new("Text", DefaultEditorTemplates.StringTemplate),
      new("Time", DefaultEditorTemplates.StringTemplate),
      new("Upload", DefaultEditorTemplates.UploadTemplate),
      new("Url", DefaultEditorTemplates.StringTemplate),

      // primitive
      new("Boolean", DefaultEditorTemplates.BooleanTemplate),
      new("Byte", DefaultEditorTemplates.StringTemplate),
      new("Decimal", DefaultEditorTemplates.StringTemplate),
      new("Enum", DefaultEditorTemplates.EnumTemplate),
      new("Int32", DefaultEditorTemplates.StringTemplate),
      new("Int64", DefaultEditorTemplates.StringTemplate),
      new("Int128", DefaultEditorTemplates.StringTemplate),
      new("SByte", DefaultEditorTemplates.StringTemplate),
      new("String", DefaultEditorTemplates.StringTemplate),
      new("UInt32", DefaultEditorTemplates.StringTemplate),
      new("UInt64", DefaultEditorTemplates.StringTemplate),
      new("UInt128", DefaultEditorTemplates.StringTemplate),

      // other
      new("DateOnly", DefaultEditorTemplates.StringTemplate),
      new("Month", DefaultEditorTemplates.StringTemplate),
      new("TimeOnly", DefaultEditorTemplates.StringTemplate),
      new("Week", DefaultEditorTemplates.StringTemplate),

      // this library's templates
      new("DropDownList", DefaultEditorTemplates.DropDownListTemplate),
      new("ListBox", DefaultEditorTemplates.ListBoxTemplate),
      new("IFormFile", DefaultEditorTemplates.UploadTemplate),

      // "special" templates
      new("Object", DefaultEditorTemplates.ObjectTemplate),
      new("HiddenInput", DefaultEditorTemplates.HiddenInputTemplate),
      new("Collection", DefaultEditorTemplates.CollectionTemplate),
   }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

   readonly ViewContext
   _viewContext;

   readonly IViewDataContainer
   _viewDataContainer;

   readonly IModelMetadataProvider
   _metadataProvider;

   readonly string?
   _templateName;

   readonly bool
   _readOnly;

   public
   TemplateRenderer(ViewContext viewContext, IViewDataContainer viewDataContainer, IModelMetadataProvider metadataProvider, string? templateName, bool readOnly) {
      _viewContext = viewContext;
      _viewDataContainer = viewDataContainer;
      _metadataProvider = metadataProvider;
      _templateName = templateName;
      _readOnly = readOnly;
   }

   public void
   Render(ISequenceWriter<object> output) {

      _viewContext.ViewName = null;

      var defaultActions = (_readOnly) ? _defaultDisplayActions : _defaultEditorActions;

      var metadata = _viewDataContainer.ModelExplorer.Metadata;
      var config = XcstWebOptions.Instance;

      foreach (var viewName in GetViewNames()) {

         var viewPage = ((_readOnly) ?
            config.DisplayTemplateFactory
            : config.EditorTemplateFactory)?.Invoke(viewName, _viewContext);

         if (viewPage != null) {
            _viewContext.ViewName = viewName;
            RenderViewPage(viewPage, output);
            return;
         }

         if (defaultActions.TryGetValue(viewName, out var defaultAction)) {
            _viewContext.ViewName = viewName;
            defaultAction.Invoke(MakeHtmlHelper(), output);
            return;
         }
      }

      throw new InvalidOperationException(
         $"Unable to locate an appropriate template for type '{metadata.UnderlyingOrModelType}'.");
   }

   IEnumerable<string>
   GetViewNames() {

      var metadata = _viewDataContainer.ModelExplorer.Metadata;
      var options = _viewContext.OptionsForModel();

      var templateHints = new[] {
         _templateName,
         metadata.TemplateHint,
         ((options != null) ?
            metadata.IsEnumerableType ? "ListBox"
            : "DropDownList"
            : null),
         metadata.DataTypeName
      };

      foreach (var templateHint in templateHints.Where(s => !String.IsNullOrEmpty(s))) {
         yield return templateHint!;
      }

      foreach (var typeName in GetTypeNames(metadata)) {
         yield return typeName;
      }
   }

   internal static IEnumerable<string>
   GetTypeNames(ModelMetadata metadata) {

      // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)
      var fieldType = metadata.UnderlyingOrModelType;

      yield return fieldType.Name;

      if (fieldType == typeof(string)) {

         // Nothing more to provide
         yield break;
      }

      if (!metadata.IsComplexType) {

         // IsEnum is false for the Enum class itself
         if (fieldType.IsEnum) {

            // Same as fieldType.BaseType.Name in this case
            yield return nameof(Enum);

         } else if (fieldType == typeof(DateTimeOffset)) {
            yield return nameof(DateTime);
         }

         yield return nameof(String);
         yield break;
      }

      if (!fieldType.IsInterface) {

         var baseType = fieldType;

         while (true) {

            baseType = fieldType.BaseType;

            if (baseType is null
               || baseType == typeof(object)) {

               break;
            }

            yield return baseType.Name;
         }
      }

      if (fieldType != typeof(IFormFile)
         && typeof(IFormFile).IsAssignableFrom(fieldType)) {

         yield return nameof(IFormFile);
      }

      if (typeof(IEnumerable).IsAssignableFrom(fieldType)) {
         yield return "Collection";
      }

      yield return nameof(Object);
   }

   HtmlHelper
   MakeHtmlHelper() =>
      new HtmlHelper(_viewContext, _viewDataContainer, _metadataProvider);

   void
   RenderViewPage(XcstViewPage viewPage, ISequenceWriter<object> output) {

      var modelExplorer = _viewDataContainer.ModelExplorer;

      if (!viewPage.DeclaredModelType.IsAssignableFrom(modelExplorer.Metadata.ModelType)) {
         throw new InvalidOperationException(
            $"{(_readOnly ? "Display" : "Editor")} template '{_viewContext.ViewName}' is not type-compatible"
            + $" with model of type '{modelExplorer.Metadata.ModelType}'.");
      }

      viewPage.Contextualize(_viewContext);
      viewPage.ModelExplorer = modelExplorer;

      XcstEvaluator.Using((object)viewPage)
         .WithParams(_viewContext.ViewParameters)
         .CallInitialTemplate()
         .OutputToRaw(output)
         .Run();
   }
}
