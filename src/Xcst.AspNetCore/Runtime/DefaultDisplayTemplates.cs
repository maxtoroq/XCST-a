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
using Xcst.PackageModel;
using Xcst.Runtime;
using Xcst.Web.Configuration;

namespace Xcst.Web.Runtime {

   static class DefaultDisplayTemplates {

      public static void
      BooleanTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var output = DocumentWriter.CastElement(package, seqOutput);
         var viewData = html.ViewData;

         bool? value = null;

         if (viewData.Model != null) {
            value = Convert.ToBoolean(viewData.Model, CultureInfo.InvariantCulture);
         }

         if (viewData.ModelMetadata.IsNullableValueType) {

            output.WriteStartElement("select");

            var className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "select"), "list-box tri-state");

            HtmlAttributeHelper.WriteClass(className, null, output);
            HtmlAttributeHelper.WriteBoolean("disabled", true, output);

            foreach (var item in DefaultEditorTemplates.TriStateValues(value)) {
               SelectInstructions.WriteOption(item, output);
            }

            output.WriteEndElement();

         } else {

            output.WriteStartElement("input");
            output.WriteAttributeString("type", "checkbox");

            var className = DefaultEditorTemplates.GetEditorCssClass(new EditorInfo("Boolean", "input", InputType.CheckBox), "check-box");

            HtmlAttributeHelper.WriteClass(className, null, output);
            HtmlAttributeHelper.WriteBoolean("disabled", true, output);
            HtmlAttributeHelper.WriteBoolean("checked", value.GetValueOrDefault(), output);

            output.WriteEndElement();
         }
      }

      public static void
      CollectionTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
         CollectionTemplate(html, package, seqOutput, TemplateHelpers.TemplateHelper);

      internal static void
      CollectionTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         var viewData = html.ViewData;
         var model = viewData.ModelMetadata.Model;

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

         try {

            viewData.TemplateInfo.HtmlFieldPrefix = String.Empty;

            var fieldNameBase = oldPrefix;
            var index = 0;

            foreach (var item in collection) {

               var itemType = typeInCollection;

               if (item != null
                  && !typeInCollectionIsNullableValueType) {

                  itemType = item.GetType();
               }

               var metadata = ModelMetadataProviders.Current.GetMetadataForType(() => item, itemType);
               var fieldName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", fieldNameBase, index++);

               templateHelper(
                  html,
                  package,
                  seqOutput,
                  metadata,
                  htmlFieldName: fieldName,
                  templateName: null,
                  membersNames: null,
                  DataBoundControlMode.ReadOnly,
                  additionalViewData: null
               );
            }

         } finally {
            viewData.TemplateInfo.HtmlFieldPrefix = oldPrefix;
         }
      }

      public static void
      DecimalTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var viewData = html.ViewData;

         if (viewData.TemplateInfo.FormattedModelValue == viewData.ModelMetadata.Model) {

            viewData.TemplateInfo.FormattedModelValue =
               package.Context.SimpleContent.Format("{0:0.00}", viewData.ModelMetadata.Model);
         }

         StringTemplate(html, package, seqOutput);
      }

      public static void
      EmailAddressTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var output = DocumentWriter.CastElement(package, seqOutput);
         var viewData = html.ViewData;

         output.WriteStartElement("a");
         output.WriteAttributeString("href", "mailto:" + Convert.ToString(viewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(output.SimpleContent.Convert(viewData.TemplateInfo.FormattedModelValue));
         output.WriteEndElement();
      }

      public static void
      HiddenInputTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         if (!html.ViewData.ModelMetadata.HideSurroundingHtml) {
            StringTemplate(html, package, seqOutput);
         }
      }

      public static void
      HtmlTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
         seqOutput.WriteRaw(package.Context.SimpleContent.Convert(html.ViewData.TemplateInfo.FormattedModelValue));

      public static void
      ObjectTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
         ObjectTemplate(html, package, seqOutput, TemplateHelpers.TemplateHelper);

      internal static void
      ObjectTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput, TemplateHelpers.TemplateHelperDelegate templateHelper) {

         var viewData = html.ViewData;
         var modelMetadata = viewData.ModelMetadata;

         if (modelMetadata.Model is null
            || viewData.TemplateInfo.TemplateDepth > 1) {

            MetadataInstructions.DisplayTextHelper(html, seqOutput, modelMetadata);
            return;
         }

         var filteredProperties = DisplayInstructions.DisplayProperties(html);
         var groupedProperties = filteredProperties.GroupBy(p => p.GroupName);

         bool createFieldset = groupedProperties.Any(g => g.Key != null);

         foreach (var group in groupedProperties) {

            XcstWriter? fieldsetWriter = null;

            if (createFieldset) {

               fieldsetWriter = DocumentWriter.CastElement(package, seqOutput);

               fieldsetWriter.WriteStartElement("fieldset");
               fieldsetWriter.WriteStartElement("legend");
               fieldsetWriter.WriteString(group.Key);
               fieldsetWriter.WriteEndElement();
            }

            foreach (ModelMetadata propertyMeta in group) {

               XcstWriter? fieldWriter = null;

               if (!propertyMeta.HideSurroundingHtml) {

                  var memberTemplate = DisplayInstructions.MemberTemplate(html, propertyMeta);

                  if (memberTemplate != null) {
                     memberTemplate(null!/* argument is not used */, fieldsetWriter ?? seqOutput);
                     continue;
                  }

                  var labelWriter = fieldsetWriter
                     ?? DocumentWriter.CastElement(package, seqOutput);

                  labelWriter.WriteStartElement("div");
                  labelWriter.WriteAttributeString("class", "display-label");
                  labelWriter.WriteString(propertyMeta.GetDisplayName() ?? String.Empty);
                  labelWriter.WriteEndElement();

                  fieldWriter = fieldsetWriter
                     ?? DocumentWriter.CastElement(package, seqOutput);

                  fieldWriter.WriteStartElement("div");
                  fieldWriter.WriteAttributeString("class", "display-field");
               }

               templateHelper(
                  html,
                  package,
                  fieldWriter ?? fieldsetWriter ?? seqOutput,
                  propertyMeta,
                  htmlFieldName: propertyMeta.PropertyName,
                  templateName: null,
                  membersNames: null,
                  DataBoundControlMode.ReadOnly,
                  additionalViewData: null
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
      StringTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) =>
         seqOutput.WriteString(package.Context.SimpleContent.Convert(html.ViewData.TemplateInfo.FormattedModelValue));

      public static void
      UrlTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var output = DocumentWriter.CastElement(package, seqOutput);
         var viewData = html.ViewData;

         output.WriteStartElement("a");
         output.WriteAttributeString("href", Convert.ToString(viewData.Model, CultureInfo.InvariantCulture));
         output.WriteString(output.SimpleContent.Convert(viewData.TemplateInfo.FormattedModelValue));
         output.WriteEndElement();
      }

      public static void
      ImageUrlTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var viewData = html.ViewData;

         if (viewData.Model != null) {

            var output = DocumentWriter.CastElement(package, seqOutput);

            output.WriteStartElement("img");
            output.WriteAttributeString("src", Convert.ToString(viewData.Model, CultureInfo.InvariantCulture));
            output.WriteEndElement();
         }
      }

      public static void
      EnumTemplate(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> seqOutput) {

         var viewData = html.ViewData;
         var modelMetadata = viewData.ModelMetadata;

         if (modelMetadata.Model != null) {

            if (modelMetadata.EditFormatString != null) {
               // undo formatting if applicable to edit mode, for consistency with editor template
               viewData.TemplateInfo.FormattedModelValue = modelMetadata.Model;
            }

            if (viewData.TemplateInfo.FormattedModelValue == modelMetadata.Model) {
               viewData.TemplateInfo.FormattedModelValue = modelMetadata.SimpleDisplayText;
            }
         }

         StringTemplate(html, package, seqOutput);
      }
   }
}
