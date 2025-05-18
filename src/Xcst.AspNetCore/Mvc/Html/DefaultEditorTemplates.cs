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

      var value = default(bool?);

      if (html.ViewData.Model != null) {
         // FIX: conversion logic duplicated with GenerateCheckbox()
         value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
      }

      if (html.ModelMetadata.IsNullableValueType) {

         var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);
         var className = GetEditorCssClass(_booleanSelectInfo, "list-box tri-state");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         using var disp = html.GenerateSelect(
            output,
            html.ModelExplorer,
            String.Empty,
            new HtmlHelper.SelectArgs {
               options = TriStateValues(value),
               @class = htmlAttributes.RemoveClass(output.SimpleContent),
            });

         htmlAttributes.WriteTo(output);
         disp.EndOfConstructor();

      } else {

         var className = GetEditorCssClass(_booleanCheckBoxInfo, "check-box");
         var htmlAttributes = CreateHtmlAttributes(html, className);

         using var disp = html.GenerateCheckbox(
            seqOutput,
            modelExplorer: html.ModelExplorer,
            String.Empty,
            new HtmlHelper.CheckboxArgs {
               @checked = value.GetValueOrDefault(),
               @class = htmlAttributes.RemoveClass(html.SimpleContent),
            });

         htmlAttributes.WriteTo(disp.ElementOutput);
         disp.EndOfConstructor();
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
         ?? throw new InvalidOperationException($"The Collection template was used with an object of type '{model.GetType()}', which does not implement System.IEnumerable.");

      var typeInCollection = typeof(string);
      var genericEnumerableType = TypeHelpers.ExtractGenericInterface(collection.GetType(), typeof(IEnumerable<>));

      if (genericEnumerableType != null) {
         typeInCollection = genericEnumerableType.GetGenericArguments()[0];
      }

      var typeInCollectionIsNullableValueType = TypeHelpers.IsNullableValueType(typeInCollection);
      var oldPrefix = html.ViewContext.HtmlFieldPrefix;

      var elementMetadata = viewData.ModelMetadata.ElementMetadata;

      try {

         html.ViewContext.HtmlFieldPrefix = String.Empty;

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

            new TemplateHelper(html, false, String.Empty, itemExplorer)
               .Render(seqOutput, new TemplateHelper.RenderArgs {
                  htmlFieldName = fieldName,
               });
         }

      } finally {
         html.ViewContext.HtmlFieldPrefix = oldPrefix;
      }
   }

   public static void
   DateTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "date";

      ApplyRfc3339DateFormattingIfNeeded(html, inputType);
      HtmlInputTemplateHelper(html, seqOutput, "Date", inputType);
   }

   public static void
   DateTimeLocalTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "datetime-local";

      ApplyRfc3339DateFormattingIfNeeded(html, inputType);
      HtmlInputTemplateHelper(html, seqOutput, "DateTime-local", inputType);
   }

   public static void
   DecimalTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var value = html.ModelExplorer.Model;

      var templateName = "Decimal";
      var inputType = "text";

      if (html.ViewContext.FormattedModelValue == value) {

         var format = html.GetDataTypeFormat(html.ModelMetadata, inputType, templateName)!;

         html.ViewContext.FormattedModelValue = html.FormatValue(value, format);
      }

      HtmlInputTemplateHelper(html, seqOutput, templateName, inputType);
   }

   public static void
   DropDownListTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_dropDownListInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      string? optionLabel = null;

      var options = html.ViewContext.OptionsForModel();

      if (options is OptionList and { AddBlankOption: true }) {
         optionLabel = html.ModelMetadata.Placeholder ?? String.Empty;
      }

      using var disp = html.GenerateSelect(
         output,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.SelectArgs {
            options = options,
            @class = htmlAttributes.RemoveClass(output.SimpleContent),
         });

      htmlAttributes.WriteTo(output);

      if (optionLabel != null) {
         html.WriteOption(new SelectListItem {
            Text = optionLabel,
            Value = String.Empty
         }, null, output);
      }

      disp.EndOfConstructor();
   }

   public static void
   EnumTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_enumInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);
      var metadata = html.ModelMetadata;

      var modelType = metadata.ModelType;
      var enumType = Nullable.GetUnderlyingType(modelType) ?? modelType;

      if (!enumType.IsEnum) {
         throw new InvalidOperationException("Enum template can only be used on Enum members.");
      }

      var formatString = metadata.EditFormatString
         ?? metadata.DisplayFormatString;

      var applyFormatInEdit = metadata.EditFormatString != null;

      var options = EnumOptions(enumType, output, formatString, applyFormatInEdit);
      var optionLabel = metadata.Placeholder ?? String.Empty;

      using var disp = html.GenerateSelect(
         output,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.SelectArgs {
            options = options,
            @class = htmlAttributes.RemoveClass(output.SimpleContent),
         });

      htmlAttributes.WriteTo(output);

      html.WriteOption(new SelectListItem {
         Text = optionLabel,
         Value = String.Empty
      }, null, output);

      disp.EndOfConstructor();
   }

   public static void
   HiddenInputTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (!html.ModelMetadata.HideSurroundingHtml) {
         DefaultDisplayTemplates.StringTemplate(html, seqOutput);
      }

      var className = GetEditorCssClass(_hiddenInputInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            type = "hidden",
            value = html.ViewContext.FormattedModelValue,
            @class = htmlAttributes.RemoveClass(html.SimpleContent),
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   public static void
   IFormFileTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, nameof(IFormFile), inputType: "file");

   public static void
   ListBoxTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_listBoxInfo, null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      var options = html.ViewContext.OptionsForModel();

      using var disp = html.GenerateSelect(
         output,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.SelectArgs {
            options = options,
            multiple = true,
            @class = htmlAttributes.RemoveClass(output.SimpleContent),
         });

      htmlAttributes.WriteTo(output);
      disp.EndOfConstructor();
   }

   public static void
   MonthTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "month";

      ApplyRfc3339DateFormattingIfNeeded(html, inputType);
      HtmlInputTemplateHelper(html, seqOutput, "Month", inputType);
   }

   public static void
   MultilineTextTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(_multilineTextInfo, "text-box multi-line");
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateTextarea(
         output,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.TextareaArgs {
            value = html.ViewContext.FormattedModelValue,
            @class = htmlAttributes.RemoveClass(output.SimpleContent),
         });

      htmlAttributes.WriteTo(output);
      disp.EndOfConstructor();
   }

   public static void
   NumberTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "Number", inputType: "number");

   public static void
   ObjectTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (html.ViewContext.TemplateDepth > 1) {
         html.DisplayTextHelper(seqOutput, html.ModelExplorer);
         return;
      }

      var filteredProperties = html.EditorProperties();
      var groupedProperties = filteredProperties.GroupBy(p => MetadataDetailsProvider.GetGroupName(p.Metadata));

      var createFieldset = groupedProperties.Any(g => g.Key != null);

      foreach (var group in groupedProperties) {

         XcstWriter? fieldsetWriter = null;

         if (createFieldset) {

            fieldsetWriter = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

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
                  ?? DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

               labelWriter.WriteStartElement("div");
               labelWriter.WriteAttributeString("class", "editor-label");

               html.GenerateLabel(labelWriter, propertyExplorer, propertyMeta.PropertyName!, default)
                  .NoConstructor()
                  .Dispose();

               labelWriter.WriteEndElement();

               fieldWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

               fieldWriter.WriteStartElement("div");
               fieldWriter.WriteAttributeString("class", "editor-field");
            }

            new TemplateHelper(html, false, String.Empty, propertyExplorer)
               .Render(
                  fieldWriter ?? fieldsetWriter ?? seqOutput,
                  new TemplateHelper.RenderArgs {
                     htmlFieldName = propertyMeta.PropertyName,
                  }
               );

            if (!propertyMeta.HideSurroundingHtml) {

               fieldWriter!.WriteString(" ");

               html.GenerateValidationMessage(fieldWriter, propertyExplorer, propertyMeta.PropertyName!, default)
                  .NoConstructor()
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

      var className = GetEditorCssClass(_passwordInfo, "text-box single-line password");
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ViewData.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            type = "password",
            @class = htmlAttributes.RemoveClass(html.SimpleContent)
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   public static void
   StringTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      // String is the fallback template for non-complex types. Not using an explicit
      // input type allows GenerateInput() to infer from metadata.
      HtmlInputTemplateHelper(html, seqOutput, "String");

   public static void
   TimeTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "time";

      ApplyRfc3339DateFormattingIfNeeded(html, inputType);
      HtmlInputTemplateHelper(html, seqOutput, "Time", inputType);
   }

   public static void
   UploadTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      HtmlInputTemplateHelper(html, seqOutput, "Upload", inputType: "file");

   static void
   HtmlInputTemplateHelper(HtmlHelper html, ISequenceWriter<object> seqOutput, string templateName, string? inputType = null) {

      var value = (HtmlHelper.InputOmitValue(inputType)) ? null
         : html.ViewContext.FormattedModelValue;

      var className = GetEditorCssClass(new EditorInfo(templateName, "input", InputType.Text), "text-box single-line");
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            type = inputType,
            value = value,
            @class = htmlAttributes.RemoveClass(html.SimpleContent)
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   static void
   ApplyRfc3339DateFormattingIfNeeded(HtmlHelper html, string inputType) {

      var value = html.ModelExplorer.Model;

      if (html.ViewContext.FormattedModelValue != value
         && html.ModelMetadata.HasNonDefaultEditFormat) {

         // non-default current culture formatting applied

         return;
      }

      var format = html.GetDataTypeFormat(html.ModelMetadata, inputType, null)!;

      if (value is DateTime
         || value is DateTimeOffset) {

         html.ViewContext.FormattedModelValue = String.Format(CultureInfo.InvariantCulture, format, value);
      }
   }

   static HtmlAttributeDictionary
   CreateHtmlAttributes(HtmlHelper html, string? className) {

      var htmlAttributes = new HtmlAttributeDictionary();

      htmlAttributes.AddClass(className);
      htmlAttributes.SetAttributes(html.ViewContext.HtmlAttributes);

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
