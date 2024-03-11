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

#region TemplateHelpers is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Runtime;
using Xcst.Web.Builder;
using Xcst.Web.Mvc;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Runtime;

using TemplateAction = Action<HtmlHelper, IXcstPackage, ISequenceWriter<object>>;

static class TemplateHelpers {

   public static void
   Template(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, string expression, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, bool displayMode, object? additionalViewData) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, html.ViewData);

      TemplateHelper(
         html,
         package,
         output,
         modelExplorer,
         htmlFieldName: expression,
         templateName: templateName,
         membersNames,
         displayMode,
         additionalViewData
      );
   }

   public static void
   TemplateFor<TContainer, TValue>(HtmlHelper<TContainer> html, IXcstPackage package, ISequenceWriter<object> output, Expression<Func<TContainer, TValue>> expression,
         string? htmlFieldName, string? templateName, IList<string>? membersNames, bool displayMode, object? additionalViewData) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData);

      htmlFieldName ??= ExpressionHelper.GetExpressionText(expression);

      TemplateHelper(
         html,
         package,
         output,
         modelExplorer,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         displayMode,
         additionalViewData
      );
   }

   public static void
   TemplateHelper(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ModelExplorer modelExplorer, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, bool displayMode, object? additionalViewData) {

      var metadata = modelExplorer.Metadata;
      var model = modelExplorer.Model;

      if (metadata.ConvertEmptyStringToNull
         && String.Empty.Equals(model)) {

         model = null;
      }

      var formattedModelValue = model;

      if (model is null
         && displayMode) {

         formattedModelValue = metadata.NullDisplayText;
      }

      var formatString = (displayMode) ?
         metadata.DisplayFormatString
         : metadata.EditFormatString;

      if (model != null
         && !String.IsNullOrEmpty(formatString)) {

         formattedModelValue = (displayMode) ?
            package.Context.SimpleContent.Format(formatString!, model)
            : String.Format(CultureInfo.CurrentCulture, formatString, model);
      }

      // Normally this shouldn't happen, unless someone writes their own custom Object templates which
      // don't check to make sure that the object hasn't already been displayed

      var visitedObjectsKey = model ?? metadata.UnderlyingOrModelType;

      if (html.ViewData.TemplateInfo.VisitedObjects.Contains(visitedObjectsKey)) {
         // DDB #224750
         return;
      }

      var viewData = new ViewDataDictionary(html.ViewData) {
         Model = model,
         ModelExplorer = modelExplorer.GetExplorerForModel(model),
         TemplateInfo = new TemplateInfo {
            FormattedModelValue = formattedModelValue,
            HtmlFieldPrefix = html.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName),
            MembersNames = membersNames
         }
      };

      viewData.TemplateInfo.VisitedObjects = new HashSet<object>(html.ViewData.TemplateInfo.VisitedObjects); // DDB #224750

      if (additionalViewData != null) {

         var additionalParams = additionalViewData as IDictionary<string, object?>
            ?? TypeHelpers.ObjectToDictionary(additionalViewData);

         foreach (var kvp in additionalParams) {
            viewData[kvp.Key] = kvp.Value;
         }
      }

      viewData.TemplateInfo.VisitedObjects.Add(visitedObjectsKey); // DDB #224750

      new TemplateRenderer(html.ViewContext, viewData, templateName, membersNames, displayMode)
         .Render(package, output);
   }
}

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

   readonly
   ViewContext _viewContext;

   readonly
   ViewDataDictionary _viewData;

   readonly string?
   _templateName;

   readonly IList<string>?
   _membersNames;

   readonly bool
   _readOnly;

   public
   TemplateRenderer(ViewContext viewContext, ViewDataDictionary viewData, string? templateName, IList<string>? membersNames, bool readOnly) {
      _viewContext = viewContext;
      _viewData = viewData;
      _templateName = templateName;
      _membersNames = membersNames;
      _readOnly = readOnly;
   }

   public void
   Render(IXcstPackage package, ISequenceWriter<object> output) {

      var defaultActions = GetDefaultActions();

      var metadata = _viewData.ModelMetadata;
      var options = DefaultEditorTemplates.Options(_viewData);

      string?[] templateHints = {
         _templateName,
         metadata.TemplateHint,
         ((options != null) ?
            TypeHelpers.IsIEnumerableNotString(metadata.ModelType) ? "ListBox"
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
            defaultAction.Invoke(MakeHtmlHelper(_viewContext, _viewData), package, output);
            return;
         }
      }

      throw new InvalidOperationException($"Unable to locate an appropriate template for type {metadata.UnderlyingOrModelType.FullName}.");
   }

   private Dictionary<string, TemplateAction>
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

   static HtmlHelper
   MakeHtmlHelper(ViewContext viewContext, ViewDataDictionary viewData) =>
      new HtmlHelper(new ViewContext(viewContext), new ViewDataContainer(viewData));

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

   class ViewDataContainer : IViewDataContainer {

      public ViewDataDictionary
      ViewData { get; set; }

      public
      ViewDataContainer(ViewDataDictionary viewData) {
         this.ViewData = viewData;
      }
   }
}
