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
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Runtime;
using Xcst.Web.Builder;

namespace Xcst.Web.Mvc;

using TemplateAction = Action<HtmlHelper, ISequenceWriter<object>>;

sealed class TemplateRenderer {

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
      { "Date", DefaultEditorTemplates.DateTemplate },
      { "DateTime", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "DateTime-local", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
      { "Password", DefaultEditorTemplates.PasswordTemplate },
      { "Text", DefaultEditorTemplates.StringTemplate },
      { "Time", DefaultEditorTemplates.TimeTemplate },
      { "Upload", DefaultEditorTemplates.UploadTemplate },

      // primitive
      { "Boolean", DefaultEditorTemplates.BooleanTemplate },
      { "Byte", DefaultEditorTemplates.NumberTemplate },
      { "Decimal", DefaultEditorTemplates.DecimalTemplate },
      { "Enum", DefaultEditorTemplates.EnumTemplate },
      { "Int32", DefaultEditorTemplates.NumberTemplate },
      { "Int64", DefaultEditorTemplates.NumberTemplate },
      { "SByte", DefaultEditorTemplates.NumberTemplate },
      { "String", DefaultEditorTemplates.StringTemplate },
      { "UInt32", DefaultEditorTemplates.NumberTemplate },
      { "UInt64", DefaultEditorTemplates.NumberTemplate },

      // other
      { "Month", DefaultEditorTemplates.MonthTemplate },

      // this library's templates
      { "DropDownList", DefaultEditorTemplates.DropDownListTemplate },
      { "ListBox", DefaultEditorTemplates.ListBoxTemplate },
      { "IFormFile", DefaultEditorTemplates.IFormFileTemplate },

      // "special" templates
      { "Object", DefaultEditorTemplates.ObjectTemplate },
      { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
      { "Collection", DefaultEditorTemplates.CollectionTemplate },
   };

   readonly HtmlHelper
   _htmlHelper;

   readonly ViewDataDictionary
   _viewData;

   readonly string?
   _templateName;

   readonly bool
   _readOnly;

   public
   TemplateRenderer(HtmlHelper htmlHelper, ViewDataDictionary viewData, string? templateName, bool readOnly) {
      _htmlHelper = htmlHelper;
      _viewData = viewData;
      _templateName = templateName;
      _readOnly = readOnly;
   }

   public void
   Render(ISequenceWriter<object> output) {

      _viewData.TemplateInfo.TemplateName = null;

      var defaultActions = GetDefaultActions();

      var metadata = _viewData.ModelMetadata;
      var options = _viewData.TemplateInfo.OptionsForModel();

      var config = XcstWebOptions.Instance;

      foreach (var viewName in GetViewNames()) {

         var viewPage = ((_readOnly) ?
            config.DisplayTemplateFactory
            : config.EditorTemplateFactory)?.Invoke(viewName, _htmlHelper.ViewContext);

         if (viewPage != null) {
            _viewData.TemplateInfo.TemplateName = viewName;
            RenderViewPage(viewPage, output);
            return;
         }

         if (defaultActions.TryGetValue(viewName, out var defaultAction)) {
            _viewData.TemplateInfo.TemplateName = viewName;
            defaultAction.Invoke(MakeHtmlHelper(), output);
            return;
         }
      }

      throw new InvalidOperationException($"Unable to locate an appropriate template for type {metadata.UnderlyingOrModelType}.");
   }

   Dictionary<string, TemplateAction>
   GetDefaultActions() =>
      (_readOnly) ? _defaultDisplayActions
         : _defaultEditorActions;

   IEnumerable<string>
   GetViewNames() {

      var metadata = _viewData.ModelMetadata;
      var options = _viewData.TemplateInfo.OptionsForModel();

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

      // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)

      var fieldType = metadata.UnderlyingOrModelType;

      foreach (var typeName in GetTypeNames(metadata, fieldType)) {
         yield return typeName;
      }
   }

   internal static IEnumerable<string>
   GetTypeNames(ModelMetadata metadata, Type fieldType) {

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
      new HtmlHelper(_htmlHelper, new ViewContext(_htmlHelper.ViewContext), new ViewDataContainer(_viewData));

   void
   RenderViewPage(XcstViewPage viewPage, ISequenceWriter<object> output) {

      viewPage.ViewContext = new ViewContext(_htmlHelper.ViewContext);
      viewPage.ViewData = _viewData;

      XcstEvaluator.Using((object)viewPage)
         .WithParams(viewPage.TemplateInfo.TemplateParameters)
         .CallInitialTemplate()
         .OutputToRaw(output)
         .Run();
   }
}
