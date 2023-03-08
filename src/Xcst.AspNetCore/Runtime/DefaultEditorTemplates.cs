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
using Xcst.Web.Mvc;
using Xcst.Web.Mvc.ModelBinding;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Runtime;

static class DefaultEditorTemplates {

   public static void
   BooleanTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;
      var value = default(bool?);

      if (viewData.Model != null) {
         value = Convert.ToBoolean(viewData.Model, CultureInfo.InvariantCulture);
      }

      if (viewData.ModelMetadata.IsNullableValueType) {

         var output = DocumentWriter.CastElement(package, seqOutput);
         var className = GetEditorCssClass(new EditorInfo("Boolean", "select"), "list-box tri-state");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         using (var disp = SelectInstructions.Select(
               html,
               output,
               String.Empty,
               selectList: TriStateValues(value),
               @class: htmlAttributes.GetClassOrNull())) {

            htmlAttributes.WriteTo(output, excludeClass: true);
            disp.EndOfConstructor();
         }

      } else {

         var className = GetEditorCssClass(new EditorInfo("Boolean", "input", InputType.CheckBox), "check-box");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         InputInstructions.CheckBoxHelper(
               html,
               package,
               seqOutput,
               modelExplorer: null,
               name: String.Empty,
               value.GetValueOrDefault(),
               htmlAttributes)
            .NoConstructor()
            .Dispose();
      }
   }

   public static void
   CollectionTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      CollectionTemplate(html, package, seqOutput, TemplateHelpers.TemplateHelper);

   internal static void
   CollectionTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput, TemplateHelpers.TemplateHelperDelegate templateHelper) {

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

            templateHelper(
               html,
               package,
               seqOutput,
               itemExplorer,
               htmlFieldName: fieldName,
               templateName: null,
               membersNames: null,
               DataBoundControlMode.Edit,
               additionalViewData: null
            );
         }

      } finally {
         viewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
      }
   }

   public static void
   DateTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-dd}");
      HtmlInputTemplateHelper(html, package, seqOutput, "Date", inputType: "date");
   }

   public static void
   DateTimeTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fffK}");
      HtmlInputTemplateHelper(html, package, seqOutput, "DateTime", inputType: "datetime");
   }

   public static void
   DateTimeLocalTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:yyyy-MM-ddTHH:mm:ss.fff}");
      HtmlInputTemplateHelper(html, package, seqOutput, "DateTime-local", inputType: "datetime-local");
   }

   public static void
   DecimalTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;

      if (viewData.TemplateInfo.FormattedModelValue == viewData.ModelExplorer.Model) {

         viewData.TemplateInfo.FormattedModelValue =
            String.Format(CultureInfo.CurrentCulture, "{0:0.00}", viewData.ModelExplorer.Model);
      }

      HtmlInputTemplateHelper(html, package, seqOutput, "Decimal");
   }

   public static void
   DropDownListTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var className = GetEditorCssClass(new EditorInfo("DropDownList", "select"), null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var viewData = html.ViewData;

      string? optionLabel = null;

      var options = Options(viewData);

      if (options is OptionList and { AddBlankOption: true }) {
         optionLabel = viewData.ModelMetadata.Placeholder ?? String.Empty;
      }

      using (var disp = SelectInstructions.SelectHelper(
            html,
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
   EmailAddressTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "EmailAddress", inputType: "email");

   public static void
   EnumTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var className = GetEditorCssClass(new EditorInfo("Enum", "select"), null);
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

      using (var disp = SelectInstructions.SelectHelper(
            html,
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
   HiddenInputTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var viewData = html.ViewData;

      if (!viewData.ModelMetadata.HideSurroundingHtml) {
         DefaultDisplayTemplates.StringTemplate(html, package, seqOutput);
      }

      var output = DocumentWriter.CastElement(package, seqOutput);
      var model = viewData.Model;

      var className = GetEditorCssClass(new EditorInfo("HiddenInput", "input", InputType.Hidden), null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using (InputInstructions.Input(
            html,
            output,
            name: String.Empty,
            model,
            type: "hidden",
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
      }
   }

   public static void
   IFormFileTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      const string templateName = nameof(IFormFile);

      HtmlInputTemplateHelper(html, package, seqOutput, templateName, inputType: "file");
   }

   public static void
   ListBoxTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var className = GetEditorCssClass(new EditorInfo("ListBox", "select"), null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var viewData = html.ViewData;

      var options = Options(viewData);

      using (var disp = SelectInstructions.SelectHelper(
            html,
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
   MultilineTextTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var value = html.ViewData.TemplateInfo.FormattedModelValue;
      var className = GetEditorCssClass(new EditorInfo("MultilineText", "textarea"), "text-box multi-line");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (var disp = TextAreaInstructions.TextArea(
            html,
            output,
            name: String.Empty,
            value,
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
         disp.EndOfConstructor();
      }
   }

   public static void
   NumberTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "Number", inputType: "number");

   public static void
   ObjectTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      ObjectTemplate(html, package, seqOutput, TemplateHelpers.TemplateHelper);

   internal static void
   ObjectTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput, TemplateHelpers.TemplateHelperDelegate templateHelper) {

      var viewData = html.ViewData;

      if (viewData.TemplateInfo.TemplateDepth > 1) {
         MetadataInstructions.DisplayTextHelper(html, seqOutput, viewData.ModelExplorer);
         return;
      }

      var filteredProperties = EditorInstructions.EditorProperties(html);
      var groupedProperties = filteredProperties.GroupBy(p => MetadataDetailsProvider.GetGroupName(p.Metadata));

      var createFieldset = groupedProperties.Any(g => g.Key != null);

      foreach (var group in groupedProperties) {

         XcstWriter? fieldsetWriter = null;

         if (createFieldset) {

            fieldsetWriter = DocumentWriter.CastElement(package, seqOutput);

            fieldsetWriter.WriteStartElement("fieldset");
            fieldsetWriter.WriteStartElement("legend");
            fieldsetWriter.WriteString(group.Key);
            fieldsetWriter.WriteEndElement();
         }

         foreach (var propertyExplorer in group) {

            XcstWriter? fieldWriter = null;
            var propertyMeta = propertyExplorer.Metadata;

            if (!propertyMeta.HideSurroundingHtml) {

               var memberTemplate = EditorInstructions.MemberTemplate(html, propertyExplorer);

               if (memberTemplate != null) {
                  memberTemplate.Invoke(null!/* argument is not used */, fieldsetWriter ?? seqOutput);
                  continue;
               }

               var labelWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(package, seqOutput);

               labelWriter.WriteStartElement("div");
               labelWriter.WriteAttributeString("class", "editor-label");

               LabelInstructions.LabelHelper(html, labelWriter, propertyExplorer, propertyMeta.PropertyName!, default, default)
                  .Dispose();

               labelWriter.WriteEndElement();

               fieldWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(package, seqOutput);

               fieldWriter.WriteStartElement("div");
               fieldWriter.WriteAttributeString("class", "editor-field");
            }

            templateHelper(
               html,
               package,
               fieldWriter ?? fieldsetWriter ?? seqOutput,
               propertyExplorer,
               htmlFieldName: propertyMeta.PropertyName,
               templateName: null,
               membersNames: null,
               DataBoundControlMode.Edit,
               additionalViewData: null
            );

            if (!propertyMeta.HideSurroundingHtml) {

               fieldWriter!.WriteString(" ");

               ValidationInstructions.ValidationMessageHelper(html, fieldWriter, propertyExplorer, propertyMeta.PropertyName!, default, default)
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
   PasswordTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var className = GetEditorCssClass(new EditorInfo("Password", "input", InputType.Password), "text-box single-line password");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (InputInstructions.Input(
            html,
            output,
            name: String.Empty,
            value: null,
            type: "password",
            @class: htmlAttributes.GetClassOrNull())) {

         htmlAttributes.WriteTo(output, excludeClass: true);
      }
   }

   public static void
   PhoneNumberTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "PhoneNumber", inputType: "tel");

   public static void
   StringTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "String");

   public static void
   TextTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "Text");

   public static void
   TimeTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

      ApplyRfc3339DateFormattingIfNeeded(html, "{0:HH:mm:ss.fff}");
      HtmlInputTemplateHelper(html, package, seqOutput, "Time", inputType: "time");
   }

   public static void
   UploadTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "Upload", inputType: "file");

   public static void
   UrlTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, package, seqOutput, "Url", inputType: "url");

   static void
   HtmlInputTemplateHelper(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput, string templateName, string? inputType = null) {

      var output = DocumentWriter.CastElement(package, seqOutput);

      var value = html.ViewData.TemplateInfo.FormattedModelValue;

      var className = GetEditorCssClass(new EditorInfo(templateName, "input", InputType.Text), "text-box single-line");
      var htmlAttributes = CreateHtmlAttributes(html, className, addMetadataAttributes: true);

      using (InputInstructions.Input(
            html,
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
