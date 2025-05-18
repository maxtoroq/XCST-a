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
using Xcst.Runtime;
using Xcst.Web.Builder;
using Xcst.Web.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

static class DefaultDisplayTemplates {

   static readonly EditorInfo
   _booleanSelectInfo = new("Boolean", "select");

   static readonly EditorInfo
   _booleanCheckboxInfo = new("Boolean", "input", "checkbox");

   public static void
   BooleanTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      var value = default(bool?);

      if (html.ViewData.Model != null) {
         value = Convert.ToBoolean(html.ViewData.Model, CultureInfo.InvariantCulture);
      }

      if (html.ModelMetadata.IsNullableValueType) {

         output.WriteStartElement("select");

         var className = DefaultEditorTemplates.GetEditorCssClass(_booleanSelectInfo, "list-box tri-state");

         html.WriteCssClass(null, className, output);
         html.WriteBoolean("disabled", true, output);

         foreach (var item in DefaultEditorTemplates.TriStateValues(value)) {
            html.WriteOption(item, null, output);
         }

         output.WriteEndElement();

      } else {

         output.WriteStartElement("input");
         output.WriteAttributeString("type", "checkbox");

         var className = DefaultEditorTemplates.GetEditorCssClass(_booleanCheckboxInfo, "check-box");

         html.WriteCssClass(null, className, output);
         html.WriteBoolean("disabled", true, output);
         html.WriteBoolean("checked", value.GetValueOrDefault(), output);

         output.WriteEndElement();
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

         var format = html.GetDataTypeFormat(html.ModelMetadata, "text", "Decimal")!;

         html.ViewContext.FormattedModelValue = html.SimpleContent.Format(format, value);
      }

      StringTemplate(html, seqOutput);
   }

   public static void
   EmailAddressTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      output.WriteStartElement("a");
      output.WriteAttributeString("href", "mailto:" + Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
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

      if (!html.ViewData.ModelMetadata.HideSurroundingHtml) {
         StringTemplate(html, seqOutput);
      }
   }

   public static void
   HtmlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      seqOutput.WriteRaw(html.SimpleContent.Convert(html.ViewContext.FormattedModelValue));

   public static void
   ImageUrlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      if (html.ViewData.Model != null) {

         var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

         output.WriteStartElement("img");
         output.WriteAttributeString("src", Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
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

      var modelExplorer = html.ModelExplorer;

      if (modelExplorer.Model is null
         || html.ViewContext.TemplateDepth > 1) {

         html.DisplayTextHelper(seqOutput, modelExplorer);
         return;
      }

      var filteredProperties = html.DisplayProperties();
      var groupedProperties = filteredProperties.GroupBy(p => MetadataDetailsProvider.GetGroupName(p.Metadata));

      bool createFieldset = groupedProperties.Any(g => g.Key != null);

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

            var propertyMeta = propertyExplorer.Metadata;
            XcstWriter? fieldWriter = null;

            if (!propertyMeta.HideSurroundingHtml) {

               var memberTemplate = html.MemberTemplate(propertyExplorer);

               if (memberTemplate != null) {
                  memberTemplate.Invoke(null!/* argument is not used */, fieldsetWriter ?? seqOutput);
                  continue;
               }

               var labelWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

               labelWriter.WriteStartElement("div");
               labelWriter.WriteAttributeString("class", "display-label");
               labelWriter.WriteString(propertyMeta.GetDisplayName() ?? String.Empty);
               labelWriter.WriteEndElement();

               fieldWriter = fieldsetWriter
                  ?? DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

               fieldWriter.WriteStartElement("div");
               fieldWriter.WriteAttributeString("class", "display-field");
            }

            new TemplateHelper(html, true, String.Empty, propertyExplorer)
               .Render(
                  fieldWriter ?? fieldsetWriter ?? seqOutput,
                  new TemplateHelper.RenderArgs {
                     htmlFieldName = propertyMeta.PropertyName,
                  }
               );

            if (!propertyMeta.HideSurroundingHtml) {
               fieldWriter!.WriteEndElement(); // </div>
            }
         }

         if (createFieldset) {
            fieldsetWriter!.WriteEndElement(); // </fieldset>
         }
      }
   }

   public static void
   StringTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) =>
      seqOutput.WriteString(html.SimpleContent.Convert(html.ViewContext.FormattedModelValue));

   public static void
   UrlTemplate(HtmlHelper html, ISequenceWriter<object> seqOutput) {

      var output = DocumentWriter.CastElement(html.ViewContext.CurrentPackage, seqOutput);

      output.WriteStartElement("a");
      output.WriteAttributeString("href", Convert.ToString(html.ViewData.Model, CultureInfo.InvariantCulture));
      output.WriteString(output.SimpleContent.Convert(html.ViewContext.FormattedModelValue));
      output.WriteEndElement();
   }
}
