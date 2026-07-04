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
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

static class DefaultDisplayTemplates {

   public static void
   BooleanTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      var value = default(bool?);

      if (html.Model != null) {
         value = Convert.ToBoolean(html.Model, CultureInfo.InvariantCulture);
      }

      if (html.ModelMetadata.IsNullableValueType) {

         output.WriteStartElement("select");

         var className = DefaultEditorTemplates.GetEditorCssClass(html, "select", null);

         html.WriteCssClass(null, className, output);
         html.WriteBoolean("disabled", true, output);

         foreach (var item in DefaultEditorTemplates.TriStateValues(value)) {
            html.WriteOption(item, null, output);
         }

         output.WriteEndElement();

      } else {

         output.WriteStartElement("input");
         output.WriteAttributeString("type", "checkbox");

         var className = DefaultEditorTemplates.GetEditorCssClass(html, "input", "checkbox");

         html.WriteCssClass(null, className, output);
         html.WriteBoolean("disabled", true, output);
         html.WriteBoolean("checked", value.GetValueOrDefault(), output);

         output.WriteEndElement();
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
            var fieldName = String.Create(CultureInfo.InvariantCulture, $"{fieldNameBase}[{index++}]");

            new TemplateHelper(html, true, String.Empty, itemExplorer)
               .Render(seqOutput, new TemplateHelper.RenderArgs {
                  htmlFieldName = fieldName,
               });
         }

      } finally {
         html.ViewContext.HtmlFieldPrefix = oldPrefix;
      }
   }

   public static void
   DecimalTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var value = html.ModelExplorer.Model;

      if (html.ViewContext.FormattedModelValue == value) {

         var format = html.GetDataTypeFormat(html.ModelMetadata, null, html.ViewContext.ViewName)!;

         html.ViewContext.FormattedModelValue = html.SimpleContent.Format(format, value);
      }

      StringTemplate(html, seqOutput);
   }

   public static void
   EmailAddressTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      output.WriteStartElement("a");
      output.WriteAttributeString("href", String.Create(CultureInfo.InvariantCulture, $"mailto:{html.Model}"));
      output.WriteString(output.SimpleContent.Convert(html.ViewContext.FormattedModelValue));
      output.WriteEndElement();
   }

   public static void
   EnumTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var modelExplorer = html.ModelExplorer;

      if (modelExplorer.Model != null) {

         if (modelExplorer.Metadata.EditFormatString != null) {
            // undo formatting if applicable to edit mode, for consistency with editor template
            html.ViewContext.FormattedModelValue = modelExplorer.Model;
         }

         if (html.ViewContext.FormattedModelValue == modelExplorer.Model) {
            html.ViewContext.FormattedModelValue = modelExplorer.GetSimpleDisplayText();
         }
      }

      StringTemplate(html, seqOutput);
   }

   public static void
   HiddenInputTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (!html.ModelMetadata.HideSurroundingHtml) {
         StringTemplate(html, seqOutput);
      }
   }

   public static void
   HtmlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      seqOutput.WriteRaw(html.SimpleContent.Convert(html.ViewContext.FormattedModelValue));

   public static void
   ImageUrlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (html.Model != null) {

         var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

         output.WriteStartElement("img");
         output.WriteAttributeString("src", Convert.ToString(html.Model, CultureInfo.InvariantCulture));
         output.WriteEndElement();
      }
   }

   public static void
   MonthTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var value = html.ModelExplorer.Model;

      if (html.ViewContext.FormattedModelValue == value) {
         html.ViewContext.FormattedModelValue = html.SimpleContent.Format("{0:y}", value);
      }

      StringTemplate(html, seqOutput);
   }

   public static void
   ObjectTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (html.ModelExplorer.Model is null
         || html.ViewContext.TemplateDepth > 1) {

         html.DisplayTextHelper(seqOutput, html.ModelExplorer);
         return;
      }

      var filteredProperties = html.DisplayProperties();
      var groupCssClass = html.ViewContext.Options.DisplayGroupCssClass;

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

            writer.WriteStartElement("div");

            try {
               writer.WriteString(propertyMeta.GetDisplayName() ?? String.Empty);

            } finally {
               writer.WriteEndElement();
            }

            writer.WriteStartElement("div");

            try {
               renderPropertyTmpl(html, propertyExplorer, writer);

            } finally {
               writer.WriteEndElement();
            }

         } finally {
            writer.WriteEndElement();
         }
      }

      static void renderPropertyTmpl(
            HtmlHelper html, ModelExplorer propertyExplorer, ISequenceWriter<object> output) {

         new TemplateHelper(html, true, String.Empty, propertyExplorer)
            .Render(
               output,
               new TemplateHelper.RenderArgs {
                  htmlFieldName = propertyExplorer.Metadata.PropertyName,
               });
      }
   }

   public static void
   StringTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      seqOutput.WriteString(html.SimpleContent.Convert(html.ViewContext.FormattedModelValue));

   public static void
   UrlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.CurrentPackage, seqOutput);

      output.WriteStartElement("a");
      output.WriteAttributeString("href", Convert.ToString(html.Model, CultureInfo.InvariantCulture));
      output.WriteString(output.SimpleContent.Convert(html.ViewContext.FormattedModelValue));
      output.WriteEndElement();
   }
}
