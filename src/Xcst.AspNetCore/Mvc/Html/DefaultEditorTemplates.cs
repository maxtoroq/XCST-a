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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xcst.Runtime;
using Xcst.Web.Builder;
using Xcst.Web.Mvc.ModelBinding;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Mvc;

static class DefaultEditorTemplates {

   static readonly EditorInfo
   _booleanSelectInfo = new("Boolean", "select");

   static readonly EditorInfo
   _booleanCheckBoxInfo = new("Boolean", "input", InputType.CheckBox);

   static readonly EditorInfo
   _dropDownListInfo = new("DropDownList", "select");

   static readonly EditorInfo
   _enumInfo = new("Enum", "select");

   static readonly EditorInfo
   _hiddenInputInfo = new("HiddenInput", "input", InputType.Hidden);

   static readonly EditorInfo
   _listBoxInfo = new("ListBox", "select");

   static readonly EditorInfo
   _multilineTextInfo = new("MultilineText", "textarea");

   static readonly EditorInfo
   _passwordInfo = new("Password", "input", InputType.Password);

   public static void
   BooleanTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;
      var value = default(bool?);

      if (viewData.Model != null) {
         value = Convert.ToBoolean(viewData.Model, CultureInfo.InvariantCulture);
      }

      if (viewData.ModelMetadata.IsNullableValueType) {

         var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);
         var className = GetEditorCssClass(_booleanSelectInfo, "list-box tri-state");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         using var disp = html.Select(
               output,
               String.Empty,
               selectList: TriStateValues(value),
               @class: htmlAttributes.GetClassOrNull());

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();

      } else {

         var className = GetEditorCssClass(_booleanCheckBoxInfo, "check-box");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         using var disp = html.GenerateCheckbox(
               seqOutput,
               modelExplorer: null,
               name: String.Empty,
               value.GetValueOrDefault(),
               @class: htmlAttributes.GetClassOrNull());

         htmlAttributes.WriteTo(disp.CheckboxOutput, excludeClass: true);
         disp.NoConstructor();
      }
   }

   public static void
   CollectionTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;
      var model = viewData.ModelExplorer.Model;

      if (model is null) {
         return;
      }

      var collection = model as IEnumerable
         ?? throw new InvalidOperationException($"The Collection template was used with an object of type '{model.GetType().FullName}', which does not implement System.IEnumerable.");

      var typeInCollection = typeof(string);
      var genericEnumerableType = TypeHelpers.ExtractGenericInterface(collection.GetType(), typeof(IEnumerable<>));

      if (genericEnumerableType != null) {
         typeInCollection = genericEnumerableType.GetGenericArguments()[0];
      }

      var typeInCollectionIsNullableValueType = TypeHelpers.IsNullableValueType(typeInCollection);
      var oldPrefix = viewData.TemplateInfo.HtmlFieldPrefix;

      var elementMetadata = viewData.ModelMetadata.ElementMetadata;

      try {

         viewData.TemplateInfo.HtmlFieldPrefix = String.Empty;

         var fieldNameBase = oldPrefix;
         var index = 0;

         foreach (var item in collection) {

            var itemMetadata = elementMetadata;

            if (item != null
               && !typeInCollectionIsNullableValueType) {

               itemMetadata = viewData.MetadataProvider.GetMetadataForType(item.GetType());
            }

            var itemExplorer = new ModelExplorer(viewData.MetadataProvider, viewData.ModelExplorer, itemMetadata, item);
            var fieldName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);

            html.TemplateHelper(
               displayMode: false,
               itemExplorer,
               htmlFieldName: fieldName,
               templateName: null,
               membersNames: null,
               additionalViewData: null
            ).Render(seqOutput);
         }

      } finally {
         viewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
      }
   }

   public static void
   DateTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-dd}");
      HtmlInputTemplateHelper(html, seqOutput, "Date", inputType: "date");
   }

   public static void
   DateTimeLocalTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fff}");
      HtmlInputTemplateHelper(html, seqOutput, "DateTime-local", inputType: "datetime-local");
   }

   public static void
   DecimalTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;

      if (viewData.TemplateInfo.FormattedModelValue == viewData.ModelExplorer.Model) {

         viewData.TemplateInfo.FormattedModelValue =
            String.Format(CultureInfo.CurrentCulture, "{0:0.00}", viewData.ModelExplorer.Model);
      }

      HtmlInputTemplateHelper(html, seqOutput, "Decimal");
   }

   public static void
   DropDownListTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_dropDownListInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var viewData = html.ViewData;

      string? optionLabel = null;

      var options = Options(viewData);

      if (options is OptionList and { AddBlankOption: true }) {
         optionLabel = viewData.ModelMetadata.Placeholder ?? String.Empty;
      }

      using (var disp = html.GenerateSelect(
            output,
            viewData.ModelExplorer,
            String.Empty,
            value: null,
            options,
            optionLabel,
            multiple: false,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();
      }
   }

   public static void
   EmailAddressTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "EmailAddress", inputType: "email");

   public static void
   EnumTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_enumInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var viewData = html.ViewData;

      var modelType = viewData.ModelMetadata.ModelType;
      var enumType = Nullable.GetUnderlyingType(modelType) ?? modelType;

      if (!enumType.IsEnum) {
         throw new InvalidOperationException("Enum template can only be used on Enum members.");
      }

      var formatString = viewData.ModelMetadata.EditFormatString
         ?? viewData.ModelMetadata.DisplayFormatString;

      var applyFormatInEdit = viewData.ModelMetadata.EditFormatString != null;

      var options = EnumOptions(enumType, output, formatString, applyFormatInEdit);
      var optionLabel = viewData.ModelMetadata.Placeholder ?? String.Empty;

      using (var disp = html.GenerateSelect(
            output,
            viewData.ModelExplorer,
            String.Empty,
            value: null,
            options,
            optionLabel,
            multiple: false,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();
      }
   }

   public static void
   HiddenInputTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;

      if (!viewData.ModelMetadata.HideSurroundingHtml) {
         DefaultDisplayTemplates.StringTemplate(html, seqOutput);
      }

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);
      var model = viewData.Model;

      var className = GetEditorCssClass(_hiddenInputInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using (html.Input(
            output,
            name: String.Empty,
            model,
            type: "hidden",
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
      }
   }

   public static void
   IFormFileTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      const string templateName = nameof(IFormFile);

      HtmlInputTemplateHelper(html, seqOutput, templateName, inputType: "file");
   }

   public static void
   ListBoxTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_listBoxInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var viewData = html.ViewData;

      var options = Options(viewData);

      using (var disp = html.GenerateSelect(
            output,
            viewData.ModelExplorer,
            String.Empty,
            value: null,
            options,
            optionLabel: null,
            multiple: true,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();
      }
   }

   public static void
   MultilineTextTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var value = html.ViewData.TemplateInfo.FormattedModelValue;
      var className = GetEditorCssClass(_multilineTextInfo, "text-box multi-line");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (var disp = html.Textarea(
            output,
            name: String.Empty,
            value,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();
      }
   }

   public static void
   NumberTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "Number", inputType: "number");

   public static void
   ObjectTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;

      if (viewData.TemplateInfo.TemplateDepth > 1) {
         html.DisplayTextHelper(seqOutput, viewData.ModelExplorer);
         return;
      }

      var filteredProperties = html.EditorProperties();
      var groupedProperties = filteredProperties.GroupBy(p => MetadataDetailsProvider.GetGroupName(p.Metadata));

      var createFieldset = groupedProperties.Any(g => g.Key != null);

      foreach (var group in groupedProperties) {

         XcstWriter? fieldsetWriter = null;

         if (createFieldset) {

            fieldsetWriter = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

            fieldsetWriter.WriteStartElement("fieldset");
            fieldsetWriter.WriteStartElement("legend");
            fieldsetWriter.WriteString(group.Key);
            fieldsetWriter.WriteEndElement();
         }

         foreach (var propertyExplorer in group) {

            XcstWriter? fieldWriter = null;
            var propertyMeta = propertyExplorer.Metadata;

            if (!propertyMeta.HideSurroundingHtml) {

               var memberTemplate = html.MemberTemplate(propertyExplorer);

               if (memberTemplate != null) {
                  memberTemplate.Invoke(null!/* argument is not used */, fieldsetWriter ?? seqOutput);
                  continue;
               }

               var labelWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

               labelWriter.WriteStartElement("div");
               labelWriter.WriteAttributeString("class", "editor-label");

               html.GenerateLabel(labelWriter, propertyExplorer, propertyMeta.PropertyName!, default, default)
                  .Dispose();

               labelWriter.WriteEndElement();

               fieldWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

               fieldWriter.WriteStartElement("div");
               fieldWriter.WriteAttributeString("class", "editor-field");
            }

            html.TemplateHelper(
               displayMode: false,
               propertyExplorer,
               htmlFieldName: propertyMeta.PropertyName,
               templateName: null,
               membersNames: null,
               additionalViewData: null
            ).Render(fieldWriter ?? fieldsetWriter ?? seqOutput);

            if (!propertyMeta.HideSurroundingHtml) {

               fieldWriter!.WriteString(" ");

               html.GenerateValidationMessage(fieldWriter, propertyExplorer, propertyMeta.PropertyName!, default, default)
                  .Dispose();

               fieldWriter.WriteEndElement(); // </div>
            }
         }

         if (createFieldset) {
            fieldsetWriter!.WriteEndElement(); // </fieldset>
         }
      }
   }

   public static void
   PasswordTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_passwordInfo, "text-box single-line password");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (html.Input(
            output,
            name: String.Empty,
            value: null,
            type: "password",
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
      }
   }

   public static void
   PhoneNumberTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "PhoneNumber", inputType: "tel");

   public static void
   StringTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "String");

   public static void
   TimeTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:HH:mm:ss.fff}");
      HtmlInputTemplateHelper(html, seqOutput, "Time", inputType: "time");
   }

   public static void
   UploadTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "Upload", inputType: "file");

   public static void
   UrlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "Url", inputType: "url");

   static void
   HtmlInputTemplateHelper(HtmlHelper html, ISequenceWriter<object> seqOutput, string templateName, string? inputType = null) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var value = html.ViewData.TemplateInfo.FormattedModelValue;

      var className = GetEditorCssClass(new EditorInfo(templateName, "input", InputType.Text), "text-box single-line");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (html.Input(
            output,
            name: String.Empty,
            value,
            type: inputType,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
      }
   }

   static void
   ApplyRfc3339DateFormattingIfNeeded(HtmlHelper html, string format) {

      if (html.ViewContext.Html5DateRenderingMode != Html5DateRenderingMode.Rfc3339) {
         return;
      }

      var viewData = html.ViewData;
      var value = viewData.ModelExplorer.Model;

      if (viewData.TemplateInfo.FormattedModelValue != value
         && viewData.ModelMetadata.HasNonDefaultEditFormat) {

         return;
      }

      if (value is DateTime
         || value is DateTimeOffset) {

         viewData.TemplateInfo.FormattedModelValue = String.Format(CultureInfo.InvariantCulture, format, value);
      }
   }

   static HtmlAttributeDictionary
   CreateHtmlAttributes(HtmlHelper html, string? className, string? inputType = null,
         bool addMetadataAttributes = false) {

      var htmlAttributes = new HtmlAttributeDictionary();

      if (inputType != null) {
         htmlAttributes.Add("type", inputType);
      }

      htmlAttributes.SetClass(className);

      if (addMetadataAttributes) {

         var metadata = html.ViewData.ModelMetadata;

         if (!String.IsNullOrEmpty(metadata.Placeholder)) {
            htmlAttributes["placeholder"] = metadata.Placeholder;
         }

         htmlAttributes.SetBoolean("readonly", metadata.IsReadOnly);
      }

      var userAttribs = html.ViewData["htmlAttributes"];
      htmlAttributes.SetAttributes(userAttribs);

      return htmlAttributes;
   }

   internal static string?
   GetEditorCssClass(EditorInfo editorInfo, string? defaultCssClass) {

      var customFn = XcstWebOptions.Instance.EditorCssClass;

      if (customFn != null) {
         return customFn.Invoke(editorInfo, defaultCssClass);
      }

      return defaultCssClass;
   }

   internal static List<SelectListItem>
   TriStateValues(bool? value) =>
      new List<SelectListItem> {
         new SelectListItem {
            Text = "Not Set",
            Value = String.Empty,
            Selected = !value.HasValue
         },
         new SelectListItem {
            Text = "True",
            Value = "true",
            Selected = value.HasValue && value.Value
         },
         new SelectListItem {
            Text = "False",
            Value = "false",
            Selected = value.HasValue && !value.Value
         }
      };

   internal static IEnumerable<SelectListItem>?
   Options(ViewDataDictionary viewData) {

      var key = "__xcst_options:" + viewData.TemplateInfo.HtmlFieldPrefix;

      if (viewData.TryGetValue(key, out IEnumerable<SelectListItem>? options)) {
         return options;
      }

      return null;
   }

   internal static IList<SelectListItem>
   EnumOptions(Type enumType, XcstWriter output, string? formatString = null, bool applyFormatInEdit = false) {

      Debug.Assert(enumType.IsEnum);

      var selectList = new List<SelectListItem>();

      const BindingFlags bindingFlags = BindingFlags.DeclaredOnly
         | BindingFlags.GetField
         | BindingFlags.Public
         | BindingFlags.Static;

      foreach (var field in enumType.GetFields(bindingFlags)) {

         var enumValue = field.GetValue(null);

         var value = (formatString != null && applyFormatInEdit) ?
            String.Format(CultureInfo.CurrentCulture, formatString, enumValue)
            : field.Name;

         var text = (formatString != null && !applyFormatInEdit) ?
            output.SimpleContent.Format(formatString, enumValue)
            : MetadataDetailsProvider.GetDisplayName(field) ?? field.Name;

         selectList.Add(new SelectListItem {
            Value = value,
            Text = text,
         });
      }

      return selectList;
   }
}
