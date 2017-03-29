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

#region DefaultEditorTemplates is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;
using Xcst.Runtime;
using Xcst.Web.Configuration;

#if !ASPNETLIB
using System.Data.Linq; 
using Xcst.Web.Mvc.Html;
#endif

namespace Xcst.Web.Runtime {

   static class DefaultEditorTemplates {

      const string HtmlAttributeKey = "htmlAttributes";
      const string OptionsKeyPrefix = "__xcst_options:";

      public static void BooleanTemplate(HtmlHelper html, XcstWriter output) {

         bool? value = null;

         if (html.ViewData.Model != null) {
            value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
         }

         if (html.ViewData.ModelMetadata.IsNullableValueType) {

            string className = GetEditorCssClass(new EditorInfo("Boolean", "select"), "list-box tri-state");
            IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);

            SelectInstructions.DropDownList(html, output, String.Empty, TriStateValues(value), optionLabel: null, htmlAttributes: htmlAttributes);

         } else {

            string className = GetEditorCssClass(new EditorInfo("Boolean", "input", InputType.CheckBox), "check-box");

            InputInstructions.CheckBox(html, output, String.Empty, value.GetValueOrDefault(), CreateHtmlAttributes(html, className));
         }
      }

      public static void CollectionTemplate(HtmlHelper html, XcstWriter output) {
         CollectionTemplate(html, output, TemplateHelpers.TemplateHelper);
      }

      internal static void CollectionTemplate(HtmlHelper html, XcstWriter output, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         ViewDataDictionary viewData = html.ViewData;
         object model = viewData.ModelMetadata.Model;

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

         string oldPrefix = viewData.TemplateInfo.HtmlFieldPrefix;

         try {

            viewData.TemplateInfo.HtmlFieldPrefix = String.Empty;

            string fieldNameBase = oldPrefix;
            int index = 0;

            foreach (object item in collection) {

               Type itemType = typeInCollection;

               if (item != null && !typeInCollectionIsNullableValueType) {
                  itemType = item.GetType();
               }

               ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => item, itemType);
               string fieldName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);
               templateHelper(html, output, metadata, fieldName, null /* templateName */, DataBoundControlMode.Edit, null /* additionalViewData */);
            }

         } finally {
            viewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
         }
      }

      public static void DecimalTemplate(HtmlHelper html, XcstWriter output) {

         if (html.ViewData.TemplateInfo.FormattedModelValue == html.ViewData.ModelMetadata.Model) {
            html.ViewData.TemplateInfo.FormattedModelValue = String.Format(CultureInfo.CurrentCulture, "{0:0.00}", html.ViewData.ModelMetadata.Model);
         }

         HtmlInputTemplateHelper(html, output, "Decimal");
      }

      public static void HiddenInputTemplate(HtmlHelper html, XcstWriter output) {

         ViewDataDictionary viewData = html.ViewData;

         if (!viewData.ModelMetadata.HideSurroundingHtml) {
            DefaultDisplayTemplates.StringTemplate(html, output);
         }

         object model = viewData.Model;

#if !ASPNETLIB
         Binary modelAsBinary = model as Binary;

         if (modelAsBinary != null) {
            model = modelAsBinary.ToArray();
         } 
#endif

         byte[] modelAsByteArray = model as byte[];

         if (modelAsByteArray != null) {
            model = Convert.ToBase64String(modelAsByteArray);
         }

         string className = GetEditorCssClass(new EditorInfo("HiddenInput", "input", InputType.Hidden), null);
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);

         InputInstructions.Hidden(html, output, String.Empty, model, htmlAttributes);
      }

      public static void MultilineTextTemplate(HtmlHelper html, XcstWriter output) {

         object value = html.ViewData.TemplateInfo.FormattedModelValue;
         string className = GetEditorCssClass(new EditorInfo("MultilineText", "textarea"), "text-box multi-line");
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);

         AddInputAttributes(html, htmlAttributes);

         TextAreaInstructions.TextArea(html, output, String.Empty, value, 0, 0, htmlAttributes);
      }

      static IDictionary<string, object> CreateHtmlAttributes(HtmlHelper html, string className, string inputType = null) {

         object htmlAttributesObject = html.ViewData[HtmlAttributeKey];

         if (htmlAttributesObject != null) {
            return MergeHtmlAttributes(htmlAttributesObject, className, inputType);
         }

         var htmlAttributes = new Dictionary<string, object>();
         htmlAttributes.AddCssClass(className);

         if (inputType != null) {
            htmlAttributes.Add("type", inputType);
         }

         return htmlAttributes;
      }

      static IDictionary<string, object> MergeHtmlAttributes(object htmlAttributesObject, string className, string inputType) {

         IDictionary<string, object> htmlAttributesDict = htmlAttributesObject as IDictionary<string, object>;

         RouteValueDictionary htmlAttributes = (htmlAttributesDict != null) ? new RouteValueDictionary(htmlAttributesDict)
             : HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributesObject);

         htmlAttributes.AddCssClass(className);

         // The input type from the provided htmlAttributes overrides the inputType parameter.

         if (inputType != null
            && !htmlAttributes.ContainsKey("type")) {

            htmlAttributes.Add("type", inputType);
         }

         return htmlAttributes;
      }

      public static void ObjectTemplate(HtmlHelper html, XcstWriter output) {
         ObjectTemplate(html, output, TemplateHelpers.TemplateHelper);
      }

      internal static void ObjectTemplate(HtmlHelper html, XcstWriter output, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         ViewDataDictionary viewData = html.ViewData;
         ModelMetadata modelMetadata = viewData.ModelMetadata;

         if (viewData.TemplateInfo.TemplateDepth > 1) {

            if (modelMetadata.Model == null) {
               output.WriteString(modelMetadata.NullDisplayText);
               return;
            }

            // DDB #224751
            string text = modelMetadata.SimpleDisplayText;

            if (modelMetadata.HtmlEncode) {
               output.WriteString(text);
            } else {
               output.WriteRaw(text);
            }

            return;
         }

         foreach (ModelMetadata propertyMetadata in modelMetadata.Properties.Where(pm => html.ShowForEdit(pm))) {

            if (!propertyMetadata.HideSurroundingHtml) {

               Action<TemplateContext, XcstWriter> memberTemplate = html.MemberTemplate(propertyMetadata);

               if (memberTemplate != null) {
                  memberTemplate(null, output);
                  continue;
               }

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "editor-label");
               LabelInstructions.LabelHelper(html, output, propertyMetadata, propertyMetadata.PropertyName);
               output.WriteEndElement();

               output.WriteStartElement("div");
               output.WriteAttributeString("class", "editor-field");
            }

            templateHelper(html, output, propertyMetadata, propertyMetadata.PropertyName, null /* templateName */, DataBoundControlMode.Edit, null /* additionalViewData */);

            if (!propertyMetadata.HideSurroundingHtml) {
               output.WriteString(" ");
               ValidationInstructions.ValidationMessageHelper(html, output, propertyMetadata, propertyMetadata.PropertyName, null, null, null);
               output.WriteEndElement(); // </div>
            }
         }
      }

      public static void PasswordTemplate(HtmlHelper html, XcstWriter output) {

         object value = (!XcstWebConfiguration.Instance.Editors.OmitPasswordValue) ?
            html.ViewData.TemplateInfo.FormattedModelValue
            : null;

         string className = GetEditorCssClass(new EditorInfo("Password", "input", InputType.Password), "text-box single-line password");
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);

         InputInstructions.Password(html, output, String.Empty, value, htmlAttributes);
      }

      public static void StringTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "String");
      }

      public static void TextTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Text");
      }

      public static void PhoneNumberInputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "PhoneNumber", inputType: "tel");
      }

      public static void UrlInputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Url", inputType: "url");
      }

      public static void EmailAddressInputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "EmailAddress", inputType: "email");
      }

      public static void DateTimeInputTemplate(HtmlHelper html, XcstWriter output) {

         ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fffK}");
         HtmlInputTemplateHelper(html, output, "DateTime", inputType: "datetime");
      }

      public static void DateTimeLocalInputTemplate(HtmlHelper html, XcstWriter output) {

         ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fff}");
         HtmlInputTemplateHelper(html, output, "DateTime-local", inputType: "datetime-local");
      }

      public static void DateInputTemplate(HtmlHelper html, XcstWriter output) {

         ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-dd}");
         HtmlInputTemplateHelper(html, output, "Date", inputType: "date");
      }

      public static void TimeInputTemplate(HtmlHelper html, XcstWriter output) {

         ApplyRfc3339DateFormattingIfNeeded(html, "{0:HH:mm:ss.fff}");
         HtmlInputTemplateHelper(html, output, "Time", inputType: "time");
      }

      public static void ByteInputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Byte", inputType: "number");
      }

      public static void SByteInputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "SByte", inputType: "number");
      }

      public static void Int32InputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Int32", inputType: "number");
      }

      public static void UInt32InputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "UInt32", inputType: "number");
      }

      public static void Int64InputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Int64", inputType: "number");
      }

      public static void UInt64InputTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "UInt64", inputType: "number");
      }

      public static void ColorInputTemplate(HtmlHelper html, XcstWriter output) {

         object value = null;

         if (html.ViewData.Model != null) {

            if (html.ViewData.Model is Color) {
               Color color = (Color)html.ViewData.Model;
               value = String.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
            } else {
               value = html.ViewData.Model;
            }
         }

         HtmlInputTemplateHelper(html, output, "Color", "color", value);
      }

      public static void UploadTemplate(HtmlHelper html, XcstWriter output) {
         HtmlInputTemplateHelper(html, output, "Upload", inputType: "file");
      }

      public static void DropDownListTemplate(HtmlHelper html, XcstWriter output) {

         string className = GetEditorCssClass(new EditorInfo("DropDownList", "select"), null);
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);
         ViewDataDictionary viewData = html.ViewData;

         string optionLabel = null;

         IEnumerable<SelectListItem> options = Options(viewData);
         OptionList optionList = options as OptionList;

         if (optionList != null
            && optionList.AddBlankOption) {

            optionLabel = viewData.ModelMetadata.Watermark ?? String.Empty;
         }

         SelectInstructions.DropDownListHelper(html, output, viewData.ModelMetadata, String.Empty, options, optionLabel, htmlAttributes);
      }

      public static void ListBoxTemplate(HtmlHelper html, XcstWriter output) {

         string className = GetEditorCssClass(new EditorInfo("ListBox", "select"), null);
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className);
         ViewDataDictionary viewData = html.ViewData;

         IEnumerable<SelectListItem> options = Options(viewData);

         SelectInstructions.ListBoxHelper(html, output, viewData.ModelMetadata, String.Empty, options, htmlAttributes);
      }

      static void ApplyRfc3339DateFormattingIfNeeded(HtmlHelper html, string format) {

         if (html.Html5DateRenderingMode != Html5DateRenderingMode.Rfc3339) {
            return;
         }

         ModelMetadata metadata = html.ViewData.ModelMetadata;
         object value = metadata.Model;

         if (html.ViewData.TemplateInfo.FormattedModelValue != value
            && metadata.HasNonDefaultEditFormat()) {

            return;
         }

         if (value is DateTime
            || value is DateTimeOffset) {

            html.ViewData.TemplateInfo.FormattedModelValue = String.Format(CultureInfo.InvariantCulture, format, value);
         }
      }

      static void HtmlInputTemplateHelper(HtmlHelper html, XcstWriter output, string templateName, string inputType = null) {
         HtmlInputTemplateHelper(html, output, templateName, inputType, html.ViewData.TemplateInfo.FormattedModelValue);
      }

      static void HtmlInputTemplateHelper(HtmlHelper html, XcstWriter output, string templateName, string inputType, object value) {

         string className = GetEditorCssClass(new EditorInfo(templateName, "input", InputType.Text), "text-box single-line");
         IDictionary<string, object> htmlAttributes = CreateHtmlAttributes(html, className, inputType: inputType);

         AddInputAttributes(html, htmlAttributes);

         InputInstructions.TextBox(html, output, name: String.Empty, value: value, htmlAttributes: htmlAttributes);
      }

      static void AddInputAttributes(HtmlHelper html, IDictionary<string, object> htmlAttributes) {

         ModelMetadata metadata = html.ViewData.ModelMetadata;

         string placeholder = metadata.Watermark;

         if (!String.IsNullOrEmpty(placeholder)
            && !htmlAttributes.ContainsKey("placeholder")) {

            htmlAttributes["placeholder"] = placeholder;
         }

         if (metadata.IsReadOnly
            && !htmlAttributes.ContainsKey("readonly")) {

            htmlAttributes["readonly"] = "readonly";
         }
      }

      internal static string GetEditorCssClass(EditorInfo editorInfo, string defaultCssClass) {

         Func<EditorInfo, string, string> customFn = XcstWebConfiguration.Instance.Editors.EditorCssClass;

         if (customFn != null) {
            return customFn(editorInfo, defaultCssClass);
         }

         return defaultCssClass;
      }

      internal static List<SelectListItem> TriStateValues(bool? value) {
         return new List<SelectListItem> {
            new SelectListItem { Text = "Not Set", Value = String.Empty, Selected = !value.HasValue },
            new SelectListItem { Text = "True", Value = "true", Selected = value.HasValue && value.Value },
            new SelectListItem { Text = "False", Value = "false", Selected = value.HasValue && !value.Value },
         };
      }

      internal static IEnumerable<SelectListItem> Options(ViewDataDictionary viewData) {

         IEnumerable<SelectListItem> options;

         if (viewData.TryGetValue(OptionsKeyPrefix + viewData.TemplateInfo.HtmlFieldPrefix, out options)) {
            return options;
         }

         return null;
      }
   }
}