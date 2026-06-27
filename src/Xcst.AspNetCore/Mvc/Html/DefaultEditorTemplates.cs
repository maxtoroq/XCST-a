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
using System.Reflection;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

using ModelBinding;

static class DefaultEditorTemplates {

   public static void
   BooleanTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var value = default(bool?);

      if (html.Model != null) {
         // FIX: conversion logic duplicated with GenerateCheckbox()
         value = Convert.ToBoolean(html.Model, CultureInfo.InvariantCulture);
      }

      if (html.ModelMetadata.IsNullableValueType) {

         var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);
         var className = GetEditorCssClass(html, "select", null);
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

         var className = GetEditorCssClass(html, "input", "checkbox");
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

      var model = html.Model;

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

      var elementMetadata = html.ModelMetadata.ElementMetadata;

      try {

         html.ViewContext.HtmlFieldPrefix = String.Empty;

         var fieldNameBase = oldPrefix;
         var index = 0;

         foreach (var item in collection) {

            var itemMetadata = elementMetadata;

            if (item != null
               && !typeInCollectionIsNullableValueType) {

               itemMetadata = html.MetadataProvider.GetMetadataForType(item.GetType());
            }

            var itemExplorer = new ModelExplorer(html.MetadataProvider, html.ModelExplorer, itemMetadata, item);
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
   DropDownListTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(html, "select", null);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      string? optionLabel = null;

      var options = html.ViewContext.OptionsForModel();

      if (options is OptionList { AddBlankOption: true }) {
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

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(html, "select", null);
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

      var className = GetEditorCssClass(html, "input", "hidden");
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
   ListBoxTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(html, "select", null);
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
   MultilineTextTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var className = GetEditorCssClass(html, "textarea", null);
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
   ObjectTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (html.ViewContext.TemplateDepth > 1) {
         html.DisplayTextHelper(seqOutput, html.ModelExplorer);
         return;
      }

      var filteredProperties = html.EditorProperties();
      var groupCssClass = html.ViewContext.Options.EditorGroupCssClass;

      foreach (var propertyExplorer in filteredProperties) {

         var propertyMeta = propertyExplorer.Metadata;
         var propertyName = propertyMeta.PropertyName!;

         if (propertyMeta.HideSurroundingHtml) {
            renderPropertyTmpl(html, propertyExplorer, seqOutput);
            continue;
         }

         if (html.MemberTemplate(propertyExplorer) is { } memberTemplate) {
            memberTemplate.Invoke(null!/* argument is not used */, seqOutput!);
            continue;
         }

         var writer = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

         writer.WriteStartElement("div");

         try {

            if (groupCssClass != null) {
               writer.WriteAttributeString("class", groupCssClass);
            }

            html.GenerateLabel(writer, propertyExplorer, propertyName, default)
               .NoConstructor()
               .Dispose();

            renderPropertyTmpl(html, propertyExplorer, writer);

            html.GenerateValidationMessage(writer, propertyExplorer, propertyName, default)
               .NoConstructor()
               .Dispose();

         } finally {
            writer.WriteEndElement();
         }
      }

      static void renderPropertyTmpl(
            HtmlHelper html, ModelExplorer propertyExplorer, ISequenceWriter<object> output) {

         new TemplateHelper(html, false, String.Empty, propertyExplorer)
            .Render(
               output,
               new TemplateHelper.RenderArgs {
                  htmlFieldName = propertyExplorer.Metadata.PropertyName,
               });
      }
   }

   public static void
   PasswordTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "password";
      var className = GetEditorCssClass(html, "input", inputType);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            type = inputType,
            @class = htmlAttributes.RemoveClass(html.SimpleContent)
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   public static void
   StringTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      // String is the fallback template for non-complex types. Not using an explicit
      // input type allows GenerateInput() to infer from metadata.

      var templateName = html.ViewContext.ViewName!;
      var inputTypeDefault = HtmlHelper.GetInputType(templateName);

      // Don't use FormattedModelValue (current culture) for cases where
      // the input type requires invariant formatting, and other cases
      // (see GetDataTypeFormat())

      var value = (html.ModelMetadata.HasNonDefaultEditFormat) ?
         html.ViewContext.FormattedModelValue : null;

      var className = GetEditorCssClass(html, "input", inputTypeDefault);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            value = value,
            @class = htmlAttributes.RemoveClass(html.SimpleContent)
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   public static void
   UploadTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var inputType = "file";
      var className = GetEditorCssClass(html, "input", inputType);
      var htmlAttributes = CreateHtmlAttributes(html, className);

      using var disp = html.GenerateInput(
         seqOutput,
         html.ModelExplorer,
         String.Empty,
         new HtmlHelper.InputArgs {
            type = inputType,
            @class = htmlAttributes.RemoveClass(html.SimpleContent)
         });

      htmlAttributes.WriteTo(disp.ElementOutput);
      disp.EndOfConstructor();
   }

   static HtmlAttributeDictionary
   CreateHtmlAttributes(HtmlHelper html, string? className) {

      var htmlAttributes = new HtmlAttributeDictionary();

      htmlAttributes.AddClass(className);
      htmlAttributes.SetAttributes(html.ViewContext.HtmlAttributes);

      return htmlAttributes;
   }

   internal static string?
   GetEditorCssClass(HtmlHelper html, string elementName, string? inputType) =>
      html.ViewContext.Options.EditorCssClass?.Invoke(elementName, inputType);

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
