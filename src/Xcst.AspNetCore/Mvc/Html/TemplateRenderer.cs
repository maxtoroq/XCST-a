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
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

using TemplateAction = Action<HtmlHelper, ISequenceWriter<object>>;

sealed class TemplateRenderer {

   public const string
   IEnumerableOfIFormFileName = "IEnumerable`" + nameof(IFormFile);

   static readonly Dictionary<string, TemplateAction>
   _defaultDisplayActions = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType
      { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
      { "Html", DefaultDisplayTemplates.HtmlTemplate },
      { "ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate },
      { "Text", DefaultDisplayTemplates.StringTemplate },
      { "Url", DefaultDisplayTemplates.UrlTemplate },

      // primitive
      { "Boolean", DefaultDisplayTemplates.BooleanTemplate },
      { "Decimal", DefaultDisplayTemplates.DecimalTemplate },
      { "Enum", DefaultDisplayTemplates.EnumTemplate },
      { "String", DefaultDisplayTemplates.StringTemplate },

      // other
      { "Month", DefaultDisplayTemplates.MonthTemplate },

      // "special" templates
      { "Object", DefaultDisplayTemplates.ObjectTemplate },
      { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
      { "Collection", DefaultDisplayTemplates.CollectionTemplate },
   };

   static readonly Dictionary<string, TemplateAction>
   _defaultEditorActions = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType
      { "Date", DefaultEditorTemplates.StringTemplate },
      { "DateTime", DefaultEditorTemplates.StringTemplate },
      { "DateTime-local", DefaultEditorTemplates.StringTemplate },
      { "EmailAddress", DefaultEditorTemplates.StringTemplate },
      { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
      { "Password", DefaultEditorTemplates.PasswordTemplate },
      { "PhoneNumber", DefaultEditorTemplates.StringTemplate },
      { "Text", DefaultEditorTemplates.StringTemplate },
      { "Time", DefaultEditorTemplates.StringTemplate },
      { "Upload", DefaultEditorTemplates.UploadTemplate },
      { "Url", DefaultEditorTemplates.StringTemplate },

      // primitive
      { "Boolean", DefaultEditorTemplates.BooleanTemplate },
      { "Byte", DefaultEditorTemplates.StringTemplate },
      { "Decimal", DefaultEditorTemplates.StringTemplate },
      { "Enum", DefaultEditorTemplates.EnumTemplate },
      { "Int32", DefaultEditorTemplates.StringTemplate },
      { "Int64", DefaultEditorTemplates.StringTemplate },
      { "Int128", DefaultEditorTemplates.StringTemplate },
      { "SByte", DefaultEditorTemplates.StringTemplate },
      { "String", DefaultEditorTemplates.StringTemplate },
      { "UInt32", DefaultEditorTemplates.StringTemplate },
      { "UInt64", DefaultEditorTemplates.StringTemplate },
      { "UInt128", DefaultEditorTemplates.StringTemplate },

      // other
      { "DateOnly", DefaultEditorTemplates.StringTemplate },
      { "Month", DefaultEditorTemplates.StringTemplate },
      { "TimeOnly", DefaultEditorTemplates.StringTemplate },
      { "Week", DefaultEditorTemplates.StringTemplate },

      // this library's templates
      { "DropDownList", DefaultEditorTemplates.DropDownListTemplate },
      { "ListBox", DefaultEditorTemplates.ListBoxTemplate },
      { "IFormFile", DefaultEditorTemplates.UploadTemplate },
      { IEnumerableOfIFormFileName, DefaultEditorTemplates.UploadTemplate },

      // "special" templates
      { "Object", DefaultEditorTemplates.ObjectTemplate },
      { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
      { "Collection", DefaultEditorTemplates.CollectionTemplate },
   };

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

      var options = _viewContext.Options;
      var tmplFactory = (_readOnly) ? options.DisplayTemplateFactory : options.EditorTemplateFactory;
      var defaultActions = (_readOnly) ? _defaultDisplayActions : _defaultEditorActions;

      foreach (var viewName in GetViewNames()) {

         if (tmplFactory?.Invoke(viewName, _viewContext) is { } viewPage) {
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

      var modelType = _viewDataContainer.ModelExplorer.ModelType;

      throw new InvalidOperationException(
         $"Unable to locate an appropriate template for type '{modelType}'.");
   }

   IEnumerable<string>
   GetViewNames() {

      var metadata = _viewDataContainer.ModelExplorer.Metadata;

      if (!String.IsNullOrEmpty(_templateName)) {
         yield return _templateName;
      }

      if (!String.IsNullOrEmpty(metadata.TemplateHint)) {
         yield return metadata.TemplateHint;
      }

      if (_viewContext.OptionsForModel() != null) {
         yield return (metadata.IsEnumerableType ?
            "ListBox"
            : "DropDownList");
      }

      if (!String.IsNullOrEmpty(metadata.DataTypeName)) {
         yield return metadata.DataTypeName;
      }

      foreach (var typeName in GetTypeNames(metadata)) {
         yield return typeName;
      }
   }

   internal static IEnumerable<string>
   GetTypeNames(ModelMetadata metadata) {

      // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)
      var fieldType = metadata.UnderlyingOrModelType;

      // Not returning type name here for IEnumerable<IFormFile> since we will be returning
      // a more specific name, IEnumerableOfIFormFileName.
      if (typeof(IEnumerable<IFormFile>) != fieldType) {
         yield return fieldType.Name;
      }

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

            baseType = baseType.BaseType;

            if (baseType is null
               || baseType == typeof(object)) {

               break;
            }

            yield return baseType.Name;
         }
      }

      if (metadata.IsEnumerableType) {

         if (typeof(IFormFile).IsAssignableFrom(metadata.ElementType)) {
            yield return IEnumerableOfIFormFileName;
         }

         yield return "Collection";

      } else if (fieldType != typeof(IFormFile)
         && typeof(IFormFile).IsAssignableFrom(fieldType)) {

         yield return nameof(IFormFile);
      }

      yield return nameof(Object);
   }

   HtmlHelper
   MakeHtmlHelper() =>
      new HtmlHelper(_viewContext, _viewDataContainer, _metadataProvider);

   void
   RenderViewPage(XcstViewPage viewPage, ISequenceWriter<object> output) {

      var modelExplorer = _viewDataContainer.ModelExplorer;
      var modelType = modelExplorer.ModelType;

      if (!viewPage.DeclaredModelType.IsAssignableFrom(modelType)) {
         throw new InvalidOperationException(
            $"{(_readOnly ? "Display" : "Editor")} template '{_viewContext.ViewName}' is not type-compatible"
            + $" with model of type '{modelType}'.");
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
