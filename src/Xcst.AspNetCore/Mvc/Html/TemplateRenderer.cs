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
using Xcst.Runtime;
using Xcst.Web.Builder;

namespace Xcst.Web.Mvc;

using TemplateAction = Action<HtmlHelper, ISequenceWriter<object>>;

sealed class TemplateRenderer {

   static readonly string
   _cacheItemId = Guid.NewGuid().ToString();

   static readonly Dictionary<string, TemplateAction>
   _defaultDisplayActions = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType templates
      { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
      { "Html", DefaultDisplayTemplates.HtmlTemplate },
      { "ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate },
      { "Text", DefaultDisplayTemplates.StringTemplate },
      { "Url", DefaultDisplayTemplates.UrlTemplate },

      // primitive templates
      { "Boolean", DefaultDisplayTemplates.BooleanTemplate },
      { "Decimal", DefaultDisplayTemplates.DecimalTemplate },
      { "Enum", DefaultDisplayTemplates.EnumTemplate },
      { "String", DefaultDisplayTemplates.StringTemplate },

      // "special" templates
      { "Object", DefaultDisplayTemplates.ObjectTemplate },
      { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
      { "Collection", DefaultDisplayTemplates.CollectionTemplate },
   };

   static readonly Dictionary<string, TemplateAction>
   _defaultEditorActions = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType templates
      { "Date", DefaultEditorTemplates.DateTemplate },
      { "DateTime", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "DateTime-local", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "EmailAddress", DefaultEditorTemplates.EmailAddressTemplate },
      { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
      { "Password", DefaultEditorTemplates.PasswordTemplate },
      { "PhoneNumber", DefaultEditorTemplates.PhoneNumberTemplate },
      { "Text", DefaultEditorTemplates.StringTemplate },
      { "Time", DefaultEditorTemplates.TimeTemplate },
      { "Upload", DefaultEditorTemplates.UploadTemplate },
      { "Url", DefaultEditorTemplates.UrlTemplate },

      // primitive templates
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

      // this library's templates
      { "DropDownList", DefaultEditorTemplates.DropDownListTemplate },
      { "ListBox", DefaultEditorTemplates.ListBoxTemplate },
      { "IFormFile", DefaultEditorTemplates.IFormFileTemplate },

      // "special" templates
      { "Object", DefaultEditorTemplates.ObjectTemplate },
      { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
      { "Collection", DefaultEditorTemplates.CollectionTemplate },
   };

   readonly IXcstPackage
   _package;

   readonly
   ViewContext _viewContext;

   readonly
   ViewDataDictionary _viewData;

   readonly string?
   _templateName;

   readonly bool
   _readOnly;

   public static TemplateRenderer
   NullRenderer() =>
      new TemplateRenderer(default!, default!, default!, default, default);

   public
   TemplateRenderer(IXcstPackage package, ViewContext viewContext, ViewDataDictionary viewData, string? templateName, bool readOnly) {
      _package = package;
      _viewContext = viewContext;
      _viewData = viewData;
      _templateName = templateName;
      _readOnly = readOnly;
   }

   public void
   Render(ISequenceWriter<object> output) {

      if (_viewData is null) {
         return;
      }

      var defaultActions = GetDefaultActions();

      var metadata = _viewData.ModelMetadata;
      var options = DefaultEditorTemplates.Options(_viewData);

      string?[] templateHints = {
         _templateName,
         metadata.TemplateHint,
         ((options != null) ?
            metadata.IsEnumerableType ? "ListBox"
            : "DropDownList"
            : null),
         metadata.DataTypeName
      };

      var config = XcstWebOptions.Instance;

      foreach (var viewName in GetViewNames(templateHints)) {

         var viewPage = ((_readOnly) ?
            config.DisplayTemplateFactory
            : config.EditorTemplateFactory)?.Invoke(viewName, _viewContext);

         if (viewPage != null) {
            RenderViewPage(viewPage, output);
            return;
         }

         if (defaultActions.TryGetValue(viewName, out var defaultAction)) {
            defaultAction.Invoke(MakeHtmlHelper(_viewContext, _viewData), output);
            return;
         }
      }

      throw new InvalidOperationException($"Unable to locate an appropriate template for type {metadata.UnderlyingOrModelType.FullName}.");
   }

   Dictionary<string, TemplateAction>
   GetDefaultActions() =>
      (_readOnly) ? _defaultDisplayActions
         : _defaultEditorActions;

   IEnumerable<string>
   GetViewNames(params string?[] templateHints) {

      var metadata = _viewData.ModelMetadata;

      foreach (var templateHint in templateHints.Where(s => !String.IsNullOrEmpty(s))) {
         yield return templateHint!;
      }

      // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)

      var fieldType = metadata.UnderlyingOrModelType;

      // TODO: Make better string names for generic types

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

      } else if (fieldType.IsInterface) {

         if (typeof(IEnumerable).IsAssignableFrom(fieldType)) {
            yield return "Collection";
         }

         yield return nameof(Object);

      } else {

         var isEnumerable = typeof(IEnumerable).IsAssignableFrom(fieldType);

         while (true) {

            fieldType = fieldType.BaseType;

            if (fieldType is null) {
               break;
            }

            if (isEnumerable && fieldType == typeof(Object)) {
               yield return "Collection";
            }

            yield return fieldType.Name;
         }
      }
   }

   HtmlHelper
   MakeHtmlHelper(ViewContext viewContext, ViewDataDictionary viewData) =>
      new HtmlHelper(new ViewContext(viewContext), new ViewDataContainer(viewData), _package);

   void
   RenderViewPage(XcstViewPage viewPage, ISequenceWriter<object> output) {

      viewPage.ViewContext = new ViewContext(_viewContext);
      viewPage.ViewData = _viewData;

      var evaluator = XcstEvaluator.Using((object)viewPage);

      foreach (var item in _viewData) {
         evaluator.WithParam(item.Key, item.Value);
      }

      evaluator.CallInitialTemplate()
         .OutputToRaw(output)
         .Run();
   }
}
