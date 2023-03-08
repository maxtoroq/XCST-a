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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Runtime;
using Xcst.Web.Builder;
using Xcst.Web.Mvc;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Runtime;

using TemplateAction = Action<HtmlHelper, IXcstPackage, ISequenceWriter<object>>;

static class TemplateHelpers {

   static readonly Dictionary<DataBoundControlMode, string>
   _modeViewPaths = new() {
      { DataBoundControlMode.ReadOnly, "DisplayTemplates" },
      { DataBoundControlMode.Edit, "EditorTemplates" }
   };

   static readonly Dictionary<string, TemplateAction>
   _defaultDisplayActions = new(StringComparer.OrdinalIgnoreCase) {
      { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
      { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
      { "Html", DefaultDisplayTemplates.HtmlTemplate },
      { "Text", DefaultDisplayTemplates.StringTemplate },
      { "Url", DefaultDisplayTemplates.UrlTemplate },
      { "ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate },
      { "Collection", DefaultDisplayTemplates.CollectionTemplate },
      { nameof(Enum), DefaultDisplayTemplates.EnumTemplate },
      { nameof(Boolean), DefaultDisplayTemplates.BooleanTemplate },
      { nameof(Decimal), DefaultDisplayTemplates.DecimalTemplate },
      { nameof(String), DefaultDisplayTemplates.StringTemplate },
      { nameof(Object), DefaultDisplayTemplates.ObjectTemplate },
   };

   static readonly Dictionary<string, TemplateAction>
   _defaultEditorActions = new(StringComparer.OrdinalIgnoreCase) {
      { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
      { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
      { "Password", DefaultEditorTemplates.PasswordTemplate },
      { "Text", DefaultEditorTemplates.StringTemplate },
      { "Collection", DefaultEditorTemplates.CollectionTemplate },
      { "PhoneNumber", DefaultEditorTemplates.PhoneNumberTemplate },
      { "Url", DefaultEditorTemplates.UrlTemplate },
      { "EmailAddress", DefaultEditorTemplates.EmailAddressTemplate },
      { "DateTime", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "DateTime-local", DefaultEditorTemplates.DateTimeLocalTemplate },
      { "Date", DefaultEditorTemplates.DateTemplate },
      { "Time", DefaultEditorTemplates.TimeTemplate },
      { "Upload", DefaultEditorTemplates.UploadTemplate },
      { "DropDownList", DefaultEditorTemplates.DropDownListTemplate },
      { "ListBox", DefaultEditorTemplates.ListBoxTemplate },
      { nameof(Enum), DefaultEditorTemplates.EnumTemplate },
      { nameof(Byte), DefaultEditorTemplates.NumberTemplate },
      { nameof(SByte), DefaultEditorTemplates.NumberTemplate },
      { nameof(Int32), DefaultEditorTemplates.NumberTemplate },
      { nameof(UInt32), DefaultEditorTemplates.NumberTemplate },
      { nameof(Int64), DefaultEditorTemplates.NumberTemplate },
      { nameof(UInt64), DefaultEditorTemplates.NumberTemplate },
      { nameof(Boolean), DefaultEditorTemplates.BooleanTemplate },
      { nameof(Decimal), DefaultEditorTemplates.DecimalTemplate },
      { nameof(String), DefaultEditorTemplates.StringTemplate },
      { nameof(Object), DefaultEditorTemplates.ObjectTemplate },
      { nameof(IFormFile), DefaultEditorTemplates.IFormFileTemplate }
   };

   static string
   CacheItemId = Guid.NewGuid().ToString();

   internal delegate void
   ExecuteTemplateDelegate(
         HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ViewDataDictionary viewData, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, GetViewNamesDelegate getViewNames, GetDefaultActionsDelegate getDefaultActions);

   internal delegate Dictionary<string, TemplateAction>
   GetDefaultActionsDelegate(DataBoundControlMode mode);

   internal delegate IEnumerable<string>
   GetViewNamesDelegate(ModelMetadata metadata, params string?[] templateHints);

   internal delegate void
   TemplateHelperDelegate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ModelExplorer modelExplorer, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData);

   public static void
   Template(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, string expression, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData) =>
      Template(
         html,
         package,
         output,
         expression: expression,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         mode,
         additionalViewData,
         TemplateHelper
      );

   internal static void
   Template(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, string expression, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData, TemplateHelperDelegate templateHelper) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, html.ViewData);

      templateHelper(
         html,
         package,
         output,
         modelExplorer,
         htmlFieldName: expression,
         templateName: templateName,
         membersNames,
         mode,
         additionalViewData
      );
   }

   public static void
   TemplateFor<TContainer, TValue>(HtmlHelper<TContainer> html, IXcstPackage package, ISequenceWriter<object> output, Expression<Func<TContainer, TValue>> expression,
         string? htmlFieldName, string? templateName, IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData) =>
      TemplateFor(
         html,
         package,
         output,
         expression: expression,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         mode,
         additionalViewData,
         TemplateHelper
      );

   internal static void
   TemplateFor<TContainer, TValue>(HtmlHelper<TContainer> html, IXcstPackage package, ISequenceWriter<object> output, Expression<Func<TContainer, TValue>> expression,
         string? htmlFieldName, string? templateName, IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData, TemplateHelperDelegate templateHelper) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData);

      htmlFieldName ??= ExpressionHelper.GetExpressionText(expression);

      templateHelper(
         html,
         package,
         output,
         modelExplorer,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         mode,
         additionalViewData
      );
   }

   public static void
   TemplateHelper(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ModelExplorer modelExplorer, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData) =>
      TemplateHelper(
         html,
         package,
         output,
         modelExplorer,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         mode,
         additionalViewData,
         ExecuteTemplate
      );

   internal static void
   TemplateHelper(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ModelExplorer modelExplorer, string? htmlFieldName, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, object? additionalViewData, ExecuteTemplateDelegate executeTemplate) {

      var displayMode = mode == DataBoundControlMode.ReadOnly;
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

      executeTemplate(html, package, output, viewData, templateName, membersNames, mode, GetViewNames, GetDefaultActions);
   }

   static void
   ExecuteTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ViewDataDictionary viewData, string? templateName,
         IList<string>? membersNames, DataBoundControlMode mode, GetViewNamesDelegate getViewNames, GetDefaultActionsDelegate getDefaultActions) {

      var actionCache = GetActionCache(html);
      var defaultActions = getDefaultActions(mode);
      var modeViewPath = _modeViewPaths[mode];

      var metadata = viewData.ModelMetadata;
      var options = DefaultEditorTemplates.Options(viewData);

      string?[] templateHints = {
         templateName,
         metadata.TemplateHint,
         ((options != null) ?
            TypeHelpers.IsIEnumerableNotString(metadata.ModelType) ? "ListBox"
            : "DropDownList"
            : null),
         metadata.DataTypeName
      };

      var config = XcstWebOptions.Instance;

      foreach (var viewName in getViewNames(metadata, templateHints)) {

         var viewPage = ((mode == DataBoundControlMode.ReadOnly) ?
            config.DisplayTemplateFactory
            : config.EditorTemplateFactory)?.Invoke(viewName, html.ViewContext);

         if (viewPage != null) {
            RenderViewPage(html, output, viewData, viewPage);
            return;
         }

         var fullViewName = modeViewPath + "/" + viewName;

         if (actionCache.TryGetValue(fullViewName, out var cacheItem)) {

            if (cacheItem != null) {
               cacheItem.Execute(html, package, output, viewData);
               return;
            }

         } else {

            if (defaultActions.TryGetValue(viewName, out var defaultAction)) {

               var item = new ActionCacheCodeItem {
                  Action = defaultAction
               };

               actionCache[fullViewName] = item;

               item.Execute(html, package, output, viewData);
               return;
            }

            actionCache[fullViewName] = null;
         }
      }

      throw new InvalidOperationException($"Unable to locate an appropriate template for type {metadata.UnderlyingOrModelType.FullName}.");
   }

   static Dictionary<string, ActionCacheItem?>
   GetActionCache(HtmlHelper html) {

      var context = html.ViewContext.HttpContext;
      Dictionary<string, ActionCacheItem?> result;

      if (context.Items.TryGetValue(CacheItemId, out var item)) {
         result = (Dictionary<string, ActionCacheItem?>)item!;
      } else {
         result = new Dictionary<string, ActionCacheItem?>();
         context.Items[CacheItemId] = result;
      }

      return result;
   }

   static Dictionary<string, TemplateAction>
   GetDefaultActions(DataBoundControlMode mode) =>
      (mode == DataBoundControlMode.ReadOnly) ? _defaultDisplayActions
         : _defaultEditorActions;

   static IEnumerable<string>
   GetViewNames(ModelMetadata metadata, params string?[] templateHints) {

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

      } else if (!metadata.IsComplexType) {

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
   MakeHtmlHelper(HtmlHelper currentHtml, ViewDataDictionary viewData) =>
      new HtmlHelper(new ViewContext(currentHtml.ViewContext), new ViewDataContainer(viewData));

   static void
   RenderViewPage(HtmlHelper html, ISequenceWriter<object> output, ViewDataDictionary viewData, XcstViewPage viewPage) {

      viewPage.ViewContext = new ViewContext(html.ViewContext);
      viewPage.ViewData = viewData;

      var evaluator = XcstEvaluator.Using((object)viewPage);

      foreach (var item in viewData) {
         evaluator.WithParam(item.Key, item.Value);
      }

      evaluator.CallInitialTemplate()
         .OutputToRaw(output)
         .Run();
   }

   abstract class ActionCacheItem {

      public abstract void
      Execute(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ViewDataDictionary viewData);
   }

   class ActionCacheCodeItem : ActionCacheItem {

#pragma warning disable CS8618
      public TemplateAction
      Action { get; set; }
#pragma warning restore CS8618

      public override void
      Execute(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, ViewDataDictionary viewData) =>
         Action(MakeHtmlHelper(html, viewData), package, output);
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

enum DataBoundControlMode {
   ReadOnly = 0,
   Edit = 1
}
