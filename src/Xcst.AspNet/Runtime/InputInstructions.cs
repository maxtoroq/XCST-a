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

#region InputInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
#if ASPNETMVC
using System.Data.Linq;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Xml.Linq;
using Xcst.PackageModel;
using Xcst.Runtime;

namespace Xcst.Web.Runtime {

   using HtmlAttribs = IDictionary<string, object>;

   /// <exclude/>
   public static class InputInstructions {

      //////////////////////////
      // CheckBox
      //////////////////////////

      public static void CheckBox(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, string name, HtmlAttribs htmlAttributes = null) =>
         CheckBoxHelper(htmlHelper, package, output, default(ModelMetadata), name, isChecked: null, htmlAttributes: htmlAttributes);

      public static void CheckBox(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, string name, bool isChecked, HtmlAttribs htmlAttributes = null) =>
         CheckBoxHelper(htmlHelper, package, output, default(ModelMetadata), name, isChecked, htmlAttributes);

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void CheckBoxFor<TModel>(
            HtmlHelper<TModel> htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, Expression<Func<TModel, bool>> expression, HtmlAttribs htmlAttributes = null) {

         if (expression is null) throw new ArgumentNullException(nameof(expression));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         CheckBoxForMetadata(htmlHelper, package, output, metadata, expressionString, /*isChecked: */null, htmlAttributes);
      }

      public static void CheckBoxForModel(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = htmlHelper.ViewData.ModelMetadata;

         CheckBoxForMetadata(htmlHelper, package, output, metadata, /*expression: */String.Empty, /*isChecked: */null, htmlAttributes);
      }

      public static void CheckBoxForModel(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, bool isChecked, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = htmlHelper.ViewData.ModelMetadata;

         CheckBoxForMetadata(htmlHelper, package, output, metadata, /*expression: */String.Empty, isChecked, htmlAttributes);
      }

      static void CheckBoxForMetadata(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, ModelMetadata/*?*/ metadata, string expression,
            bool? isChecked, HtmlAttribs htmlAttributes) {

         object model = metadata?.Model;

         if (isChecked is null
            && model != null) {

            if (Boolean.TryParse(model.ToString(), out bool modelChecked)) {
               isChecked = modelChecked;
            }
         }

         CheckBoxHelper(htmlHelper, package, output, metadata, expression, isChecked, htmlAttributes);
      }

      static void CheckBoxHelper(
            HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, ModelMetadata/*?*/ metadata, string name, bool? isChecked, HtmlAttribs/*?*/ htmlAttributes) {

         XcstWriter inputWriter = DocumentWriter.CastElement(package, output);
         XcstWriter hiddenWriter = DocumentWriter.CastElement(package, output);

         bool explicitChecked = isChecked.HasValue;

         if (explicitChecked
            && htmlAttributes != null
            && htmlAttributes.ContainsKey("checked")) {

            htmlAttributes = htmlAttributes.Clone();
            htmlAttributes.Remove("checked"); // Explicit value must override dictionary
         }

         InputHelper(
            htmlHelper,
            inputWriter,
            InputType.CheckBox,
            metadata,
            name,
            value: "true",
            useViewData: !explicitChecked,
            isChecked: isChecked ?? false,
            setId: true,
            isExplicitValue: false,
            format: null,
            htmlAttributes: htmlAttributes);

         string fullName = Name(htmlHelper, name);

         // Render an additional <input type="hidden".../> for checkboxes. This
         // addresses scenarios where unchecked checkboxes are not sent in the request.
         // Sending a hidden input makes it possible to know that the checkbox was present
         // on the page when the request was submitted.

         hiddenWriter.WriteStartElement("input");
         hiddenWriter.WriteAttributeString("type", HtmlHelper.GetInputTypeString(InputType.Hidden));
         hiddenWriter.WriteAttributeString("name", fullName);
         hiddenWriter.WriteAttributeString("value", "false");
         hiddenWriter.WriteEndElement();
      }

      //////////////////////////
      // RadioButton
      //////////////////////////

      public static void RadioButton(
            HtmlHelper htmlHelper, XcstWriter output, string name, object value, HtmlAttribs htmlAttributes = null) {

         if (value is null) throw new ArgumentNullException(nameof(value));

         // checked attributes is implicit, so we need to ensure that the dictionary takes precedence.

         if (htmlAttributes?.ContainsKey("checked") == true) {
            RadioButtonHelper(htmlHelper, output, /*metadata: */null, name, value, /*isChecked: */null, htmlAttributes);
            return;
         }

         bool isChecked = RadioButtonValueEquals(value, htmlHelper.EvalString(name));

         RadioButtonHelper(htmlHelper, output, /*metadata: */null, name, value, isChecked, htmlAttributes);
      }

      public static void RadioButton(
            HtmlHelper htmlHelper, XcstWriter output, string name, object value, bool isChecked, HtmlAttribs htmlAttributes = null) {

         if (value is null) throw new ArgumentNullException(nameof(value));

         RadioButtonHelper(htmlHelper, output, /*metadata: */null, name, value, isChecked, htmlAttributes);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void RadioButtonFor<TModel, TProperty>(
            HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, object value, HtmlAttribs htmlAttributes = null) {

         if (value is null) throw new ArgumentNullException(nameof(value));

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string expressionString = ExpressionHelper.GetExpressionText(expression);

         RadioButtonForMetadata(htmlHelper, output, metadata, expressionString, value, /*isChecked: */null, htmlAttributes);
      }

      public static void RadioButtonForModel(
            HtmlHelper htmlHelper, XcstWriter output, object value, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = htmlHelper.ViewData.ModelMetadata;

         RadioButtonForMetadata(htmlHelper, output, metadata, String.Empty, value, /*isChecked: */null, htmlAttributes);
      }

      public static void RadioButtonForModel(
            HtmlHelper htmlHelper, XcstWriter output, object value, bool isChecked, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = htmlHelper.ViewData.ModelMetadata;

         RadioButtonForMetadata(htmlHelper, output, metadata, String.Empty, value, /*isChecked: */isChecked, htmlAttributes);
      }

      static void RadioButtonForMetadata(
            HtmlHelper htmlHelper, XcstWriter output, ModelMetadata/*?*/ metadata, string expression, object value, bool? isChecked,
            HtmlAttribs htmlAttributes) {

         object model = metadata?.Model;

         if (isChecked is null
            && model != null) {

            isChecked = RadioButtonValueEquals(value, model.ToString());
         }

         RadioButtonHelper(htmlHelper, output, metadata, expression, value, isChecked, htmlAttributes);
      }

      static void RadioButtonHelper(
            HtmlHelper htmlHelper, XcstWriter output, ModelMetadata/*?*/ metadata, string name, object value, bool? isChecked, HtmlAttribs/*?*/ htmlAttributes) {

         bool explicitChecked = isChecked.HasValue;

         if (explicitChecked
            && htmlAttributes != null
            && htmlAttributes.ContainsKey("checked")) {

            htmlAttributes = htmlAttributes.Clone();
            htmlAttributes.Remove("checked"); // Explicit value must override dictionary
         }

         InputHelper(
            htmlHelper,
            output,
            InputType.Radio,
            metadata,
            name,
            value,
            useViewData: false,
            isChecked: isChecked.GetValueOrDefault(),
            setId: true,
            isExplicitValue: true,
            format: null,
            htmlAttributes: htmlAttributes);
      }

      static bool RadioButtonValueEquals(object value, string viewDataValue) {

         string valueString = Convert.ToString(value, CultureInfo.CurrentCulture);

         return String.Equals(viewDataValue, valueString, StringComparison.OrdinalIgnoreCase);
      }

      //////////////////////////
      // Input
      //////////////////////////

      public static void Input(
            HtmlHelper htmlHelper, XcstWriter output, string name, object value = null, string type = null, string format = null, HtmlAttribs htmlAttributes = null) =>
         InputImpl(htmlHelper, output, type, /*metadata: */null, name, value, /*useViewData: */null, format, htmlAttributes);

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void InputFor<TModel, TProperty>(
            HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, string type = null, string format = null, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
         string exprString = ExpressionHelper.GetExpressionText(expression);

         InputForMetadata(htmlHelper, output, type, metadata, exprString, /*value: */null, format, htmlAttributes);
      }

      public static void InputForModel(
            HtmlHelper htmlHelper, XcstWriter output, object value = null, string type = null, string format = null, HtmlAttribs htmlAttributes = null) {

         ModelMetadata metadata = htmlHelper.ViewData.ModelMetadata;

         InputForMetadata(htmlHelper, output, type, metadata, /*expression: */String.Empty, value, format, htmlAttributes);
      }

      static void InputForMetadata(
            HtmlHelper htmlHelper, XcstWriter output, string/*?*/ type, ModelMetadata/*?*/ metadata, string expression, object/*?*/ value,
            string/*?*/ format, HtmlAttribs/*?*/ htmlAttributes) {

         if (value is null
            && metadata != null
            && GetInputType(type) != InputType.Password) {

            value = metadata.Model;
         }

         InputImpl(htmlHelper, output, type, metadata, expression, value, /*useViewData: */false, format, htmlAttributes);
      }

      static void InputImpl(
            HtmlHelper htmlHelper, XcstWriter output, string/*?*/ type, ModelMetadata/*?*/ metadata, string expression, object/*?*/ value,
            bool? useViewData, string/*?*/ format, HtmlAttribs/*?*/ htmlAttributes) {

         InputType? inputType = GetInputType(type);
         bool checkBoxOrRadio = inputType == InputType.CheckBox
            || inputType == InputType.Radio;

         if (type != null
            && (inputType is null || checkBoxOrRadio)
            && (htmlAttributes is null || !htmlAttributes.ContainsKey("type"))) {

            htmlAttributes = htmlAttributes?.Clone() ?? new HtmlAttributeDictionary();
            htmlAttributes["type"] = type;
         }

         if (checkBoxOrRadio) {
            // Don't want Input() to behave like RadioButton() or CheckBox()
            inputType = null;
         }

         if (inputType == InputType.Hidden) {

#if ASPNETMVC
            if (value is Binary binaryValue) {
               value = binaryValue.ToArray();
            }
#endif

            if (value is byte[] byteArrayValue) {
               value = Convert.ToBase64String(byteArrayValue);
            }
         }

         if (useViewData is null) {
            useViewData = (value is null);
         }

         InputHelper(
            htmlHelper,
            output,
            inputType ?? InputType.Text,
            metadata: metadata,
            name: expression,
            value: value,
            useViewData: useViewData.Value,
            isChecked: false,
            setId: true,
            isExplicitValue: true,
            format: format,
            htmlAttributes: htmlAttributes);
      }

      static InputType? GetInputType(string type) {

         switch (type) {
            case "checkbox":
               return InputType.CheckBox;
            case "hidden":
               return InputType.Hidden;
            case "password":
               return InputType.Password;
            case "radio":
               return InputType.Radio;
            case "text":
               return InputType.Text;
            default:
               return null;
         }
      }

      static void InputHelper(
            HtmlHelper htmlHelper, XcstWriter output, InputType inputType, ModelMetadata/*?*/ metadata, string name, object value,
            bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string format, HtmlAttribs/*?*/ htmlAttributes) {

         string fullName = Name(htmlHelper, name);

         if (String.IsNullOrEmpty(fullName)) {
            throw new ArgumentNullException(nameof(name));
         }

         output.WriteStartElement("input");

         if (setId) {
            HtmlAttributeHelper.WriteId(fullName, output);
         }

         string defaultType = HtmlHelper.GetInputTypeString(inputType);

         output.WriteAttributeString("type", defaultType);
         output.WriteAttributeString("name", fullName);

         string valueParameter = htmlHelper.FormatValue(value, format);
         bool usedModelState = false;
         bool valueWritten = false;

         switch (inputType) {
            case InputType.CheckBox:

               bool? modelStateWasChecked = htmlHelper.GetModelStateValue(fullName, typeof(bool)) as bool?;

               if (modelStateWasChecked.HasValue) {
                  isChecked = modelStateWasChecked.Value;
                  usedModelState = true;
               }

               goto case InputType.Radio;

            case InputType.Radio:

               if (!usedModelState) {

                  string modelStateValue = htmlHelper.GetModelStateValue(fullName, typeof(string)) as string;

                  if (modelStateValue != null) {

                     // for CheckBox, valueParameter is "true"

                     isChecked = String.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                     usedModelState = true;
                  }
               }

               // useViewData is always false when called from RadioButton
               // the following condition is for CheckBox

               if (!usedModelState && useViewData) {
                  isChecked = htmlHelper.EvalBoolean(fullName);
               }

               output.WriteAttributeString("value", valueParameter);
               valueWritten = true;

               HtmlAttributeHelper.WriteBoolean("checked", isChecked, output);

               break;

            case InputType.Password:

               if (value != null) {
                  output.WriteAttributeString("value", valueParameter);
                  valueWritten = true;
               }

               break;

            default:

               string type;

               if (htmlAttributes != null
                  && htmlAttributes.TryGetValue("type", out var t)) {

                  type = t.ToString();
               } else {
                  type = defaultType;
               }

               bool writeValue = true;

               if (String.Equals(type, "file", StringComparison.OrdinalIgnoreCase)
                  || String.Equals(type, "image", StringComparison.OrdinalIgnoreCase)) {

                  // 'value' attribute is not needed for 'file' and 'image' input types.
                  writeValue = false;
               }

               if (writeValue) {

                  string attemptedValue = (string)htmlHelper.GetModelStateValue(fullName, typeof(string));

                  string valueAttr = attemptedValue
                     ?? ((useViewData) ? htmlHelper.EvalString(fullName, format) : valueParameter);

                  output.WriteAttributeString("value", valueAttr);
                  valueWritten = true;
               }

               break;
         }

         // If there are any errors for a named field, we add the css attribute.

         string cssClass = (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out ModelState modelState)
            && modelState.Errors.Count > 0) ? HtmlHelper.ValidationInputCssClassName : null;

         HtmlAttributeHelper.WriteClass(cssClass, htmlAttributes, output);
         HtmlAttributeHelper.WriteAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, metadata), output);

         // name cannnot be overridden, and class was already written
         // explicit value cannot be overridden

         HtmlAttributeHelper.WriteAttributes(
            htmlAttributes,
            output,
            excludeFn: n => n == "name" || n == "class" || (isExplicitValue && valueWritten && (n == "value")));

         output.WriteEndElement();
      }

      //////////////////////////
      // Name
      //////////////////////////

      public static string Name(HtmlHelper html, string name) =>
         html.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

      public static string NameFor<TModel, TProperty>(HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression) =>
         Name(html, ExpressionHelper.GetExpressionText(expression));

      public static string NameForModel(HtmlHelper html) =>
         Name(html, String.Empty);
   }
}
