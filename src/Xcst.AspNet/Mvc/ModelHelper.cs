// Copyright 2016 Max Toro Q.
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Xcst.Runtime;

namespace Xcst.Web.Mvc {

   public class ModelHelper {

      const string MemberTemplateKey = "__xcst_member_template";

      public HtmlHelper Html { get; }

      public object Model => Html.ViewData.Model;

      public ModelMetadata Metadata => Html.ViewData.ModelMetadata;

      public static ModelHelper<TModel> ForModel<TModel>(
            ModelHelper currentHelper,
            TModel model,
            string htmlFieldPrefix = null,
            object additionalViewData = null) {

         if (currentHelper == null) throw new ArgumentNullException(nameof(currentHelper));

         HtmlHelper currentHtml = currentHelper.Html;
         ViewDataDictionary currentViewData = currentHtml.ViewData;

         // Cannot call new ViewDataDictionary<TModel>(currentViewData)
         // because currentViewData.Model might be incompatible with TModel

         var tempDictionary = new ViewDataDictionary(currentViewData) {
            Model = model
         };

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary<TModel>(tempDictionary) {

               // setting new TemplateInfo clears VisitedObjects cache
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.HtmlFieldPrefix
               }
            }
         };

         if (!String.IsNullOrEmpty(htmlFieldPrefix)) {

            TemplateInfo templateInfo = container.ViewData.TemplateInfo;
            templateInfo.HtmlFieldPrefix = templateInfo.GetFullHtmlFieldName(htmlFieldPrefix);
         }

         if (additionalViewData != null) {

            IDictionary<string, object> additionalParams = additionalViewData as IDictionary<string, object>
               ?? TypeHelpers.ObjectToDictionary(additionalViewData);

            foreach (var kvp in additionalParams) {
               container.ViewData[kvp.Key] = kvp.Value;
            }
         }

         ViewContext currentViewContext = currentHtml.ViewContext;

         // new ViewContext resets FormContext

         var newViewContext = new ViewContext(
            currentViewContext,
#if !ASPNETLIB
            currentViewContext.View,
#endif
            container.ViewData,
            currentViewContext.TempData,
            currentViewContext.Writer
         );

         var html = new HtmlHelper<TModel>(newViewContext, container, currentHtml.RouteCollection);

         return new ModelHelper<TModel>(html);
      }

      internal static ModelHelper ForMemberTemplate(HtmlHelper currentHtml, ModelMetadata memberMetadata) {

         if (currentHtml == null) throw new ArgumentNullException(nameof(currentHtml));
         if (memberMetadata == null) throw new ArgumentNullException(nameof(memberMetadata));

         ViewDataDictionary currentViewData = currentHtml.ViewData;

         var container = new ViewDataContainer {
            ViewData = new ViewDataDictionary(currentViewData) {
               Model = memberMetadata.Model,
               ModelMetadata = memberMetadata,
               TemplateInfo = new TemplateInfo {
                  HtmlFieldPrefix = currentViewData.TemplateInfo.GetFullHtmlFieldName(memberMetadata.PropertyName)
               }
            }
         };

         // setting new TemplateInfo clears VisitedObjects cache, need to restore it
         currentViewData.TemplateInfo.VisitedObjects(new HashSet<object>(currentViewData.TemplateInfo.VisitedObjects()));

         var html = new HtmlHelper(currentHtml.ViewContext, container, currentHtml.RouteCollection);

         return new ModelHelper(html);
      }

      public ModelHelper(HtmlHelper htmlHelper) {

         if (htmlHelper == null) throw new ArgumentNullException(nameof(htmlHelper));

         this.Html = htmlHelper;
      }

      public string DisplayName() {
         return DisplayNameHelper(this.Html.ViewData.ModelMetadata, String.Empty);
      }

      public string DisplayName(string name) {

         ModelMetadata metadata = ModelMetadata.FromStringExpression(name, this.Html.ViewData);

         return DisplayNameHelper(metadata, name);
      }

      internal static string DisplayNameHelper(ModelMetadata metadata, string htmlFieldName) {

         // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
         // This is similar to how the LabelHelpers get the text of a label.

         string resolvedDisplayName = metadata.DisplayName
            ?? metadata.PropertyName
            ?? htmlFieldName.Split('.').Last();

         return resolvedDisplayName;
      }

      public string FieldId() {
         return FieldId(String.Empty);
      }

      public string FieldId(string name) {
         return this.Html.ViewData.TemplateInfo.GetFullHtmlFieldId(name);
      }

      public string FieldName() {
         return FieldName(String.Empty);
      }

      public string FieldName(string name) {
         return this.Html.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
      }

      public string FieldValue() {
         return FieldValueHelper(String.Empty, value: null, format: this.Metadata.EditFormatString, useViewData: true);
      }

      public string FieldValue(string name) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return FieldValueHelper(name, value: null, format: this.Metadata.EditFormatString, useViewData: true);
      }

      public string FieldValue(string name, string format) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         return FieldValueHelper(name, value: null, format: format, useViewData: true);
      }

      internal string FieldValueHelper(string name, object value, string format, bool useViewData) {

         string fullName = this.Html.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
         string attemptedValue = (string)this.Html.GetModelStateValue(fullName, typeof(string));
         string resolvedValue;

         if (attemptedValue != null) {

            // case 1: if ModelState has a value then it's already formatted so ignore format string

            resolvedValue = attemptedValue;

         } else if (useViewData) {

            if (name.Length == 0) {

               // case 2(a): format the value from ModelMetadata for the current model

               ModelMetadata metadata = ModelMetadata.FromStringExpression(String.Empty, this.Html.ViewData);
               resolvedValue = this.Html.FormatValue(metadata.Model, format);

            } else {

               // case 2(b): format the value from ViewData

               resolvedValue = this.Html.EvalString(name, format);
            }
         } else {

            // case 3: format the explicit value from ModelMetadata

            resolvedValue = this.Html.FormatValue(value, format);
         }

         return resolvedValue;
      }

      /// <summary>
      /// Determines whether a property should be shown in a display template, based on its metadata.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> display template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>

      public bool ShowForDisplay(ModelMetadata propertyMetadata) {
         return ShowForDisplayImpl(this.Html, propertyMetadata);
      }

      internal static bool ShowForDisplayImpl(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (!propertyMetadata.ShowForDisplay
            || html.ViewData.TemplateInfo.Visited(propertyMetadata)) {

            return false;
         }

         bool show;

         if (propertyMetadata.AdditionalValues.TryGetValue(nameof(propertyMetadata.ShowForDisplay), out show)) {
            return show;
         }

#if !ASPNETLIB
         if (propertyMetadata.ModelType == typeof(EntityState)) {
            return false;
         }
#endif
         return !propertyMetadata.IsComplexType;
      }

      /// <summary>
      /// Determines whether a property should be shown in an editor template, based on its metadata.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> editor template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>

      public bool ShowForEdit(ModelMetadata propertyMetadata) {
         return ShowForEditImpl(this.Html, propertyMetadata);
      }

      internal static bool ShowForEditImpl(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (!propertyMetadata.ShowForEdit
            || html.ViewData.TemplateInfo.Visited(propertyMetadata)) {

            return false;
         }

         bool show;

         if (propertyMetadata.AdditionalValues.TryGetValue(nameof(propertyMetadata.ShowForEdit), out show)) {
            return show;
         }

#if !ASPNETLIB
         if (propertyMetadata.ModelType == typeof(EntityState)) {
            return false;
         } 
#endif
         return !propertyMetadata.IsComplexType;
      }

      /// <summary>
      /// Returns the member template delegate for the provided property.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>

      public Action<TemplateContext, XcstWriter> MemberTemplate(ModelMetadata propertyMetadata) {
         return MemberTemplateImpl(this.Html, propertyMetadata);
      }

      internal static Action<TemplateContext, XcstWriter> MemberTemplateImpl(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (html == null) throw new ArgumentNullException(nameof(html));
         if (propertyMetadata == null) throw new ArgumentNullException(nameof(propertyMetadata));

         Action<ModelHelper, XcstWriter> memberTemplate;

         if (!html.ViewData.TryGetValue(MemberTemplateKey, out memberTemplate)) {
            return null;
         }

         ModelHelper modelHelper = ForMemberTemplate(html, propertyMetadata);

         return (c, o) => memberTemplate(modelHelper, o);
      }

      class ViewDataContainer : IViewDataContainer {

         public ViewDataDictionary ViewData { get; set; }
      }
   }
}
