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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Xcst.Web.Mvc.Html {

   static class TemplateHelpers {

      static readonly Dictionary<DataBoundControlMode, string> _modeViewPaths =
         new Dictionary<DataBoundControlMode, string> {
            { DataBoundControlMode.ReadOnly, "DisplayTemplates" },
            { DataBoundControlMode.Edit, "EditorTemplates" }
         };

      static readonly Dictionary<string, Action<HtmlHelper, XcstWriter>> _defaultDisplayActions =
         new Dictionary<string, Action<HtmlHelper, XcstWriter>>(StringComparer.OrdinalIgnoreCase) {
            { "EmailAddress", DefaultDisplayTemplates.EmailAddressTemplate },
            { "HiddenInput", DefaultDisplayTemplates.HiddenInputTemplate },
            { "Html", DefaultDisplayTemplates.HtmlTemplate },
            { "Text", DefaultDisplayTemplates.StringTemplate },
            { "Url", DefaultDisplayTemplates.UrlTemplate },
            { "ImageUrl", DefaultDisplayTemplates.ImageUrlTemplate },
            { "Collection", DefaultDisplayTemplates.CollectionTemplate },
            { nameof(Boolean), DefaultDisplayTemplates.BooleanTemplate },
            { nameof(Decimal), DefaultDisplayTemplates.DecimalTemplate },
            { nameof(String), DefaultDisplayTemplates.StringTemplate },
            { nameof(Object), DefaultDisplayTemplates.ObjectTemplate },
         };

      static readonly Dictionary<string, Action<HtmlHelper, XcstWriter>> _defaultEditorActions =
         new Dictionary<string, Action<HtmlHelper, XcstWriter>>(StringComparer.OrdinalIgnoreCase) {
            { "HiddenInput", DefaultEditorTemplates.HiddenInputTemplate },
            { "MultilineText", DefaultEditorTemplates.MultilineTextTemplate },
            { "Password", DefaultEditorTemplates.PasswordTemplate },
            { "Text", DefaultEditorTemplates.TextTemplate },
            { "Collection", DefaultEditorTemplates.CollectionTemplate },
            { "PhoneNumber", DefaultEditorTemplates.PhoneNumberInputTemplate },
            { "Url", DefaultEditorTemplates.UrlInputTemplate },
            { "EmailAddress", DefaultEditorTemplates.EmailAddressInputTemplate },
            { "DateTime", DefaultEditorTemplates.DateTimeInputTemplate },
            { "DateTime-local", DefaultEditorTemplates.DateTimeLocalInputTemplate },
            { "Date", DefaultEditorTemplates.DateInputTemplate },
            { "Time", DefaultEditorTemplates.TimeInputTemplate },
            { "Upload", DefaultEditorTemplates.UploadTemplate },
            { "DropDownList", DefaultEditorTemplates.DropDownListTemplate },
            { "ListBox", DefaultEditorTemplates.ListBoxTemplate },
            { nameof(Color), DefaultEditorTemplates.ColorInputTemplate },
            { nameof(Byte), DefaultEditorTemplates.ByteInputTemplate },
            { nameof(SByte), DefaultEditorTemplates.SByteInputTemplate },
            { nameof(Int32), DefaultEditorTemplates.Int32InputTemplate },
            { nameof(UInt32), DefaultEditorTemplates.UInt32InputTemplate },
            { nameof(Int64), DefaultEditorTemplates.Int64InputTemplate },
            { nameof(UInt64), DefaultEditorTemplates.UInt64InputTemplate },
            { nameof(Boolean), DefaultEditorTemplates.BooleanTemplate },
            { nameof(Decimal), DefaultEditorTemplates.DecimalTemplate },
            { nameof(String), DefaultEditorTemplates.StringTemplate },
            { nameof(Object), DefaultEditorTemplates.ObjectTemplate },
         };

      static string CacheItemId = Guid.NewGuid().ToString();

      internal delegate void ExecuteTemplateDelegate(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData, string templateName,
                                                     DataBoundControlMode mode, GetViewNamesDelegate getViewNames,
                                                     GetDefaultActionsDelegate getDefaultActions);

      internal delegate Dictionary<string, Action<HtmlHelper, XcstWriter>> GetDefaultActionsDelegate(DataBoundControlMode mode);

      internal delegate IEnumerable<string> GetViewNamesDelegate(ModelMetadata metadata, params string[] templateHints);

      internal delegate void TemplateHelperDelegate(HtmlHelper html, XcstWriter output, ModelMetadata metadata, string htmlFieldName,
                                                    string templateName, DataBoundControlMode mode, object additionalViewData);

      static void ExecuteTemplate(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData, string templateName, DataBoundControlMode mode, GetViewNamesDelegate getViewNames, GetDefaultActionsDelegate getDefaultActions) {

         Dictionary<string, ActionCacheItem> actionCache = GetActionCache(html);
         Dictionary<string, Action<HtmlHelper, XcstWriter>> defaultActions = getDefaultActions(mode);
         string modeViewPath = _modeViewPaths[mode];

         ModelMetadata metadata = viewData.ModelMetadata;
         var options = DefaultEditorTemplates.Options(viewData);

         string[] templateHints = {
            templateName,
            metadata.TemplateHint,
            ((options != null) ?
               TypeHelpers.IsIEnumerableNotString(metadata.ModelType) ? "ListBox"
               : "DropDownList"
               : null),
            metadata.DataTypeName
         };

         foreach (string viewName in getViewNames(metadata, templateHints)) {

            string fullViewName = modeViewPath + "/" + viewName;
            ActionCacheItem cacheItem;

            if (actionCache.TryGetValue(fullViewName, out cacheItem)) {

               if (cacheItem != null) {
                  cacheItem.Execute(html, output, viewData);
                  return;
               }

            } else {

               ViewEngineResult viewEngineResult = ViewEngines.Engines.FindPartialView(html.ViewContext, fullViewName);

               if (viewEngineResult.View != null) {

                  actionCache[fullViewName] = new ActionCacheViewItem { ViewName = fullViewName };

                  RenderView(html, output, viewData, viewEngineResult);
                  return;
               }

               Action<HtmlHelper, XcstWriter> defaultAction;

               if (defaultActions.TryGetValue(viewName, out defaultAction)) {

                  actionCache[fullViewName] = new ActionCacheCodeItem { Action = defaultAction };

                  defaultAction(MakeHtmlHelper(html, viewData), output);
                  return;
               }

               actionCache[fullViewName] = null;
            }
         }

         throw new InvalidOperationException($"Unable to locate an appropriate template for type {metadata.RealModelType().FullName}.");
      }

      static Dictionary<string, ActionCacheItem> GetActionCache(HtmlHelper html) {

         HttpContextBase context = html.ViewContext.HttpContext;
         Dictionary<string, ActionCacheItem> result;

         if (!context.Items.Contains(CacheItemId)) {
            result = new Dictionary<string, ActionCacheItem>();
            context.Items[CacheItemId] = result;
         } else {
            result = (Dictionary<string, ActionCacheItem>)context.Items[CacheItemId];
         }

         return result;
      }

      static Dictionary<string, Action<HtmlHelper, XcstWriter>> GetDefaultActions(DataBoundControlMode mode) {
         return mode == DataBoundControlMode.ReadOnly ? _defaultDisplayActions : _defaultEditorActions;
      }

      static IEnumerable<string> GetViewNames(ModelMetadata metadata, params string[] templateHints) {

         foreach (string templateHint in templateHints.Where(s => !String.IsNullOrEmpty(s))) {
            yield return templateHint;
         }

         // We don't want to search for Nullable<T>, we want to search for T (which should handle both T and Nullable<T>)

         Type fieldType = Nullable.GetUnderlyingType(metadata.RealModelType()) ?? metadata.RealModelType();

         // TODO: Make better string names for generic types

         yield return fieldType.Name;

         if (fieldType == typeof(string)) {

            // Nothing more to provide

            yield break;

         } else if (!metadata.IsComplexType) {

            // IsEnum is false for the Enum class itself

            if (fieldType.IsEnum) {

               // Same as fieldType.BaseType.Name in this case

               yield return "Enum";

            } else if (fieldType == typeof(DateTimeOffset)) {
               yield return "DateTime";
            }

            yield return "String";

         } else if (fieldType.IsInterface) {

            if (typeof(IEnumerable).IsAssignableFrom(fieldType)) {
               yield return "Collection";
            }

            yield return "Object";

         } else {

            bool isEnumerable = typeof(IEnumerable).IsAssignableFrom(fieldType);

            while (true) {

               fieldType = fieldType.BaseType;

               if (fieldType == null) {
                  break;
               }

               if (isEnumerable && fieldType == typeof(Object)) {
                  yield return "Collection";
               }

               yield return fieldType.Name;
            }
         }
      }

      public static void Template(HtmlHelper html, XcstWriter output, string expression, string templateName, string htmlFieldName, DataBoundControlMode mode, object additionalViewData) {
         Template(html, output, expression, templateName, htmlFieldName, mode, additionalViewData, TemplateHelper);
      }

      internal static void Template(HtmlHelper html, XcstWriter output, string expression, string templateName, string htmlFieldName,
                                    DataBoundControlMode mode, object additionalViewData, TemplateHelperDelegate templateHelper) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(expression, html.ViewData);

         if (htmlFieldName == null) {
            htmlFieldName = ExpressionHelper.GetExpressionText(expression);
         }

         templateHelper(html, output, metadata, htmlFieldName, templateName, mode, additionalViewData);
      }

      public static void TemplateFor<TContainer, TValue>(this HtmlHelper<TContainer> html, XcstWriter output, Expression<Func<TContainer, TValue>> expression,
                                                         string templateName, string htmlFieldName, DataBoundControlMode mode,
                                                         object additionalViewData) {

         TemplateFor(html, output, expression, templateName, htmlFieldName, mode, additionalViewData, TemplateHelper);
      }

      internal static void TemplateFor<TContainer, TValue>(this HtmlHelper<TContainer> html, XcstWriter output, Expression<Func<TContainer, TValue>> expression,
                                                           string templateName, string htmlFieldName, DataBoundControlMode mode,
                                                           object additionalViewData, TemplateHelperDelegate templateHelper) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);

         if (htmlFieldName == null) {
            htmlFieldName = ExpressionHelper.GetExpressionText(expression);
         }

         templateHelper(html, output, metadata, htmlFieldName, templateName, mode, additionalViewData);
      }

      public static void TemplateHelper(HtmlHelper html, XcstWriter output, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData) {
         TemplateHelper(html, output, metadata, htmlFieldName, templateName, mode, additionalViewData, ExecuteTemplate);
      }

      internal static void TemplateHelper(HtmlHelper html, XcstWriter output, ModelMetadata metadata, string htmlFieldName, string templateName, DataBoundControlMode mode, object additionalViewData, ExecuteTemplateDelegate executeTemplate) {

         bool displayMode = mode == DataBoundControlMode.ReadOnly;

         if (metadata.ConvertEmptyStringToNull
            && String.Empty.Equals(metadata.Model)) {

            metadata.Model = null;
         }

         object formattedModelValue = metadata.Model;

         if (metadata.Model == null
            && displayMode) {

            formattedModelValue = metadata.NullDisplayText;
         }

         string formatString = (displayMode) ?
            metadata.DisplayFormatString
            : metadata.EditFormatString;

         if (metadata.Model != null
            && !String.IsNullOrEmpty(formatString)) {

            formattedModelValue = (displayMode) ?
               output.SimpleContent.Format(formatString, metadata.Model)
               : String.Format(CultureInfo.CurrentCulture, formatString, metadata.Model);
         }

         // Normally this shouldn't happen, unless someone writes their own custom Object templates which
         // don't check to make sure that the object hasn't already been displayed

         object visitedObjectsKey = metadata.Model
            ?? metadata.RealModelType();

         if (html.ViewDataContainer.ViewData.TemplateInfo.VisitedObjects().Contains(visitedObjectsKey)) {
            // DDB #224750
            return;
         }

         var viewData = new ViewDataDictionary(html.ViewDataContainer.ViewData) {
            Model = metadata.Model,
            ModelMetadata = metadata,
            TemplateInfo = new TemplateInfo {
               FormattedModelValue = formattedModelValue,
               HtmlFieldPrefix = html.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName)
            }
         };

         viewData.TemplateInfo.VisitedObjects(new HashSet<object>(html.ViewData.TemplateInfo.VisitedObjects())); // DDB #224750

         if (additionalViewData != null) {

            IDictionary<string, object> additionalParams = additionalViewData as IDictionary<string, object>
               ?? TypeHelpers.ObjectToDictionary(additionalViewData);

            foreach (var kvp in additionalParams) {
               viewData[kvp.Key] = kvp.Value;
            }
         }

         viewData.TemplateInfo.VisitedObjects().Add(visitedObjectsKey); // DDB #224750

         executeTemplate(html, output, viewData, templateName, mode, GetViewNames, GetDefaultActions);
      }

      // Helpers

      static HtmlHelper MakeHtmlHelper(HtmlHelper html, ViewDataDictionary viewData) {

         var newHelper = new HtmlHelper(
            new ViewContext(
               html.ViewContext,
#if !ASPNETLIB
               html.ViewContext.View,
#endif
               viewData, html.ViewContext.TempData, html.ViewContext.Writer),
            new ViewDataContainer(viewData));

         newHelper.Html5DateRenderingMode = html.Html5DateRenderingMode;

         return newHelper;
      }

      static void RenderView(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData, ViewEngineResult viewEngineResult) {

         IView view = viewEngineResult.View;
         XcstView xcstView = view as XcstView;

         if (xcstView != null) {
            xcstView.RenderXcstView(
               new ViewContext(
                  html.ViewContext,
#if !ASPNETLIB
                  view,
#endif
                  viewData, html.ViewContext.TempData, html.ViewContext.Writer),
               output);

         } else {

            using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture)) {

               view.Render(
                  new ViewContext(
                     html.ViewContext,
#if !ASPNETLIB
                     view,
#endif
                     viewData, html.ViewContext.TempData, writer),
                  writer);

               output.WriteRaw(writer.ToString());
            }
         }
      }

      abstract class ActionCacheItem {
         public abstract void Execute(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData);
      }

      class ActionCacheCodeItem : ActionCacheItem {

         public Action<HtmlHelper, XcstWriter> Action { get; set; }

         public override void Execute(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData) {
            Action(MakeHtmlHelper(html, viewData), output);
         }
      }

      class ActionCacheViewItem : ActionCacheItem {

         public string ViewName { get; set; }

         public override void Execute(HtmlHelper html, XcstWriter output, ViewDataDictionary viewData) {

            ViewEngineResult viewEngineResult = ViewEngines.Engines.FindPartialView(html.ViewContext, this.ViewName);

            RenderView(html, output, viewData, viewEngineResult);
         }
      }

      class ViewDataContainer : IViewDataContainer {

         public ViewDataContainer(ViewDataDictionary viewData) {
            ViewData = viewData;
         }

         public ViewDataDictionary ViewData { get; set; }
      }
   }
}
