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

#region DefaultDisplayTemplates is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Xcst.Runtime;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime {

   static class DefaultDisplayTemplates {

      public static void BooleanTemplate(HtmlHelper html, XcstWriter output) {

         bool? value = null;

         if (html.ViewData.Model != null) {
            value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
         }

         if (html.ViewData.ModelMetadata.IsNullableValueType) {

            output.WriteStartElement("select");

            string className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "select"), "list-box tri-state");

            HtmlAttributesMerger.Create()
               .AddCssClass(className)
               .WriteTo(output);

            output.WriteAttributeString("disabled", "disabled");

            foreach (SelectListItem item in DefaultEditorTemplates.TriStateValues(value)) {
               SelectInstructions.ListItemToOption(output, item);
            }

            output.WriteEndElement();

         } else {

            output.WriteStartElement("input");
            output.WriteAttributeString("type", "checkbox");

            string className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "input", InputType.CheckBox), "check-box");

            HtmlAttributesMerger.Create()
               .AddCssClass(className)
               .WriteTo(output);

            output.WriteAttributeString("disabled", "disabled");

            if (value.GetValueOrDefault()) {
               output.WriteAttributeString("checked", "checked");
            }

            output.WriteEndElement();
         }
      }

      public static void CollectionTemplate(HtmlHelper html, XcstWriter output) {
         CollectionTemplate(html, output, TemplateHelpers.TemplateHelper);
      }

      internal static void CollectionTemplate(HtmlHelper html, XcstWriter output, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         object model = html.ViewData.ModelMetadata.Model;

         if (model == null) {
            return;
         }

         IEnumerable collection = model as IEnumerable;

         if (collection == null) {
            throw new InvalidOperationException($"The Collection template was used with an object of type '{model.GetType().FullName}', which does not implement System.IEnumerable.");
         }

         Type typeInCollection = typeof(string);
         Type genericEnumerableType = TypeHelpers.ExtractGenericInterface(collection.GetType(), typeof(IEnumerable<>));

         if (genericEnumerableType != null) {
            typeInCollection = genericEnumerableType.GetGenericArguments()[0];
         }

         bool typeInCollectionIsNullableValueType = TypeHelpers.IsNullableValueType(typeInCollection);

         string oldPrefix = html.ViewData.TemplateInfo.HtmlFieldPrefix;

         try {

            html.ViewData.TemplateInfo.HtmlFieldPrefix = String.Empty;

            string fieldNameBase = oldPrefix;
            int index = 0;

            foreach (object item in collection) {

               Type itemType = typeInCollection;

               if (item != null && !typeInCollectionIsNullableValueType) {
                  itemType = item.GetType();
               }

               ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => item, itemType);
               string fieldName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);
               templateHelper(html, output, metadata, fieldName, null /* templateName */, DataBoundControlMode.ReadOnly, null /* additionalViewData */);
            }

         } finally {
            html.ViewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
         }
      }

      public static void DecimalTemplate(HtmlHelper html, XcstWriter output) {

         if (html.ViewData.TemplateInfo.FormattedModelValue == html.ViewData.ModelMetadata.Model) {
            html.ViewData.TemplateInfo.FormattedModelValue = output.SimpleContent.Format("{0:0.00}", html.ViewData.ModelMetadata.Model);
         }

         StringTemplate(html, output);
      }

      public static void EmailAddressTemplate(HtmlHelper html, XcstWriter output) {

         output.WriteStartElement("a");
         output.WriteAttributeString("href", "mailto:" + Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(html.ViewData.TemplateInfo.FormattedModelValue);
         output.WriteEndElement();
      }

      public static void HiddenInputTemplate(HtmlHelper html, XcstWriter output) {

         if (html.ViewData.ModelMetadata.HideSurroundingHtml) {
            return;
         }

         StringTemplate(html, output);
      }

      public static void HtmlTemplate(HtmlHelper html, XcstWriter output) {

         output.WriteRaw(output.SimpleContent.Convert(html.ViewData.TemplateInfo.FormattedModelValue));
      }

      public static void ObjectTemplate(HtmlHelper html, XcstWriter output) {
         ObjectTemplate(html, output, TemplateHelpers.TemplateHelper);
      }

      internal static void ObjectTemplate(HtmlHelper html, XcstWriter output, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         ViewDataDictionary viewData = html.ViewData;
         ModelMetadata modelMetadata = viewData.ModelMetadata;

         if (modelMetadata.Model == null) {

            // DDB #225237

            output.WriteString(modelMetadata.NullDisplayText);
            return;
         }

         if (viewData.TemplateInfo.TemplateDepth > 1) {

            // DDB #224751

            string text = modelMetadata.SimpleDisplayText;

            if (modelMetadata.HtmlEncode) {
               output.WriteString(text);
            } else {
               output.WriteRaw(text);
            }

            return;
         }

         foreach (ModelMetadata propertyMetadata in modelMetadata.Properties.Where(pm => ModelHelper.ShowForDisplayImpl(html, pm))) {

            if (!propertyMetadata.HideSurroundingHtml) {

               Action<TemplateContext, XcstWriter> memberTemplate = ModelHelper.MemberTemplateImpl(html, propertyMetadata);

               if (memberTemplate != null) {
                  memberTemplate(null, output);
                  continue;
               }

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "display-label");
               output.WriteString(propertyMetadata.GetDisplayName() ?? String.Empty);
               output.WriteEndElement();

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "display-field");
            }

            templateHelper(html, output, propertyMetadata, propertyMetadata.PropertyName, null /* templateName */, DataBoundControlMode.ReadOnly, null /* additionalViewData */);

            if (!propertyMetadata.HideSurroundingHtml) {
               output.WriteEndElement(); // </div>
            }
         }
      }

      public static void StringTemplate(HtmlHelper html, XcstWriter output) {
         output.WriteString(html.ViewData.TemplateInfo.FormattedModelValue);
      }

      public static void UrlTemplate(HtmlHelper html, XcstWriter output) {

         output.WriteStartElement("a");
         output.WriteAttributeString("href", Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(html.ViewData.TemplateInfo.FormattedModelValue);
         output.WriteEndElement();
      }

      public static void ImageUrlTemplate(HtmlHelper html, XcstWriter output) {

         if (html.ViewData.Model != null) {
            output.WriteStartElement("img");
            output.WriteAttributeString("src", Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
            output.WriteEndElement();
         }
      }
   }
}
