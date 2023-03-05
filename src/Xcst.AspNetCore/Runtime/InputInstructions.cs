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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Xml.Linq;
using Xcst.Runtime;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class InputInstructions {

   //////////////////////////
   // CheckBox
   //////////////////////////

   public static CheckBoxDisposable
   CheckBox(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, string name, HtmlAttributeDictionary? htmlAttributes = null) =>
      CheckBoxHelper(htmlHelper, package, output, modelExplorer: null, name, isChecked: null, htmlAttributes);

   public static CheckBoxDisposable
   CheckBox(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, string name, bool isChecked, HtmlAttributeDictionary? htmlAttributes = null) =>
      CheckBoxHelper(htmlHelper, package, output, modelExplorer: null, name, isChecked, htmlAttributes);

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static CheckBoxDisposable
   CheckBoxFor<TModel>(HtmlHelper<TModel> htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output,
         Expression<Func<TModel, bool>> expression, HtmlAttributeDictionary? htmlAttributes = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return CheckBoxForModelExplorer(htmlHelper, package, output, modelExplorer, expressionString, isChecked: null, htmlAttributes);
   }

   public static CheckBoxDisposable
   CheckBoxForModel(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, HtmlAttributeDictionary? htmlAttributes = null) {

      var modelExplorer = htmlHelper.ViewData.ModelExplorer;

      return CheckBoxForModelExplorer(htmlHelper, package, output, modelExplorer, expression: String.Empty, isChecked: null, htmlAttributes);
   }

   public static CheckBoxDisposable
   CheckBoxForModel(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, bool isChecked, HtmlAttributeDictionary? htmlAttributes = null) {

      var modelExplorer = htmlHelper.ViewData.ModelExplorer;

      return CheckBoxForModelExplorer(htmlHelper, package, output, modelExplorer, expression: String.Empty, isChecked, htmlAttributes);
   }

   static CheckBoxDisposable
   CheckBoxForModelExplorer(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, ModelExplorer? modelExplorer, string expression,
         bool? isChecked, HtmlAttributeDictionary? htmlAttributes) {

      var model = modelExplorer?.Model;

      if (isChecked is null
         && model != null) {

         if (Boolean.TryParse(model.ToString(), out bool modelChecked)) {
            isChecked = modelChecked;
         }
      }

      return CheckBoxHelper(htmlHelper, package, output, modelExplorer, expression, isChecked, htmlAttributes);
   }

   internal static CheckBoxDisposable
   CheckBoxHelper(HtmlHelper htmlHelper, IXcstPackage package, ISequenceWriter<XElement> output, ModelExplorer? modelExplorer, string name,
         bool? isChecked, HtmlAttributeDictionary? htmlAttributes) {

      var inputWriter = DocumentWriter.CastElement(package, output);
      var hiddenWriter = DocumentWriter.CastElement(package, output);

      var explicitChecked = isChecked.HasValue;

      var inputDisposable = InputHelper(
         htmlHelper,
         inputWriter,
         InputType.CheckBox,
         type: null,
         modelExplorer,
         name,
         value: "true",
         useViewData: !explicitChecked,
         isChecked: isChecked ?? false,
         setId: true,
         isExplicitValue: false,
         format: null,
         @class: htmlAttributes?.GetClassOrNull());

      htmlAttributes?.WriteTo(inputWriter, excludeClass: true);

      var fullName = htmlHelper.Name(name);

      // Render an additional <input type="hidden".../> for checkboxes. This
      // addresses scenarios where unchecked checkboxes are not sent in the request.
      // Sending a hidden input makes it possible to know that the checkbox was present
      // on the page when the request was submitted.

      return new CheckBoxDisposable(inputDisposable, hiddenWriter, fullName);
   }

   //////////////////////////
   // RadioButton
   //////////////////////////

   public static IDisposable
   RadioButton(HtmlHelper htmlHelper, XcstWriter output, string name, object value, string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      var isChecked = RadioButtonValueEquals(value, htmlHelper.EvalString(name));

      return RadioButtonHelper(htmlHelper, output, modelExplorer: null, name, value, isChecked, @class);
   }

   public static IDisposable
   RadioButton(HtmlHelper htmlHelper, XcstWriter output, string name, object value, bool isChecked,
         string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      return RadioButtonHelper(htmlHelper, output, modelExplorer: null, name, value, isChecked, @class);
   }

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static IDisposable
   RadioButtonFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, object value,
         string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return RadioButtonForModelExplorer(htmlHelper, output, modelExplorer, expressionString, value, isChecked: null, @class);
   }

   public static IDisposable
   RadioButtonForModel(HtmlHelper htmlHelper, XcstWriter output, object value, string? @class = null) {

      var modelExplorer = htmlHelper.ViewData.ModelExplorer;

      return RadioButtonForModelExplorer(htmlHelper, output, modelExplorer, String.Empty, value, isChecked: null, @class);
   }

   public static IDisposable
   RadioButtonForModel(HtmlHelper htmlHelper, XcstWriter output, object value, bool isChecked, string? @class = null) {

      var modelExplorer = htmlHelper.ViewData.ModelExplorer;

      return RadioButtonForModelExplorer(htmlHelper, output, modelExplorer, String.Empty, value, isChecked, @class);
   }

   static IDisposable
   RadioButtonForModelExplorer(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer? modelExplorer, string expression, object value,
         bool? isChecked, string? @class) {

      var model = modelExplorer?.Model;

      if (isChecked is null
         && model != null) {

         isChecked = RadioButtonValueEquals(value, model.ToString());
      }

      return RadioButtonHelper(htmlHelper, output, modelExplorer, expression, value, isChecked, @class);
   }

   static IDisposable
   RadioButtonHelper(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer? modelExplorer, string name, object value,
         bool? isChecked, string? @class) {

      return InputHelper(
         htmlHelper,
         output,
         InputType.Radio,
         type: null,
         modelExplorer,
         name,
         value,
         useViewData: false,
         isChecked: isChecked.GetValueOrDefault(),
         setId: true,
         isExplicitValue: true,
         format: null,
         @class);
   }

   static bool
   RadioButtonValueEquals(object value, string viewDataValue) {

      var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);

      return String.Equals(viewDataValue, valueString, StringComparison.OrdinalIgnoreCase);
   }

   //////////////////////////
   // Input
   //////////////////////////

   public static IDisposable
   Input(HtmlHelper htmlHelper, XcstWriter output, string name, object? value = null, string? type = null, string? format = null, string? @class = null) =>
      InputImpl(htmlHelper, output, type, modelExplorer: null, name, value, useViewData: null, format, @class);

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static IDisposable
   InputFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, string? type = null,
         string? format = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var exprString = ExpressionHelper.GetExpressionText(expression);

      return InputForModelExplorer(htmlHelper, output, type, modelExplorer, exprString, value: null, format, @class);
   }

   public static IDisposable
   InputForModel(HtmlHelper htmlHelper, XcstWriter output, object? value = null, string? type = null,
         string? format = null, string? @class = null) {

      var modelExplorer = htmlHelper.ViewData.ModelExplorer;

      return InputForModelExplorer(htmlHelper, output, type, modelExplorer, expression: String.Empty, value, format, @class);
   }

   static IDisposable
   InputForModelExplorer(HtmlHelper htmlHelper, XcstWriter output, string? type, ModelExplorer? modelExplorer, string expression, object? value,
         string? format, string? @class) {

      if (value is null
         && modelExplorer != null
         && GetInputType(type) != InputType.Password) {

         value = modelExplorer.Model;
      }

      return InputImpl(htmlHelper, output, type, modelExplorer, expression, value, useViewData: false, format, @class);
   }

   static IDisposable
   InputImpl(HtmlHelper htmlHelper, XcstWriter output, string? type, ModelExplorer? modelExplorer, string expression, object? value,
         bool? useViewData, string? format, string? @class) {

      var inputType = GetInputType(type);
      var checkBoxOrRadio = inputType is InputType.CheckBox or InputType.Radio;

      if (checkBoxOrRadio) {
         // Don't want Input() to behave like RadioButton() or CheckBox()
         inputType = null;
      }

      if (inputType == InputType.Hidden) {

         if (value is byte[] byteArrayValue) {
            value = Convert.ToBase64String(byteArrayValue);
         }
      }

      useViewData ??= (value is null);

      return InputHelper(
         htmlHelper,
         output,
         inputType ?? InputType.Text,
         type,
         modelExplorer,
         name: expression,
         value,
         useViewData: useViewData.Value,
         isChecked: false,
         setId: true,
         isExplicitValue: true,
         format,
         @class);
   }

   static InputType?
   GetInputType(string? type) =>
      type switch {
         "checkbox" => InputType.CheckBox,
         "hidden" => InputType.Hidden,
         "password" => InputType.Password,
         "radio" => InputType.Radio,
         "text" => InputType.Text,
         _ => null,
      };

   static IDisposable
   InputHelper(HtmlHelper htmlHelper, XcstWriter output, InputType inputType, string? type, ModelExplorer? modelExplorer, string name, object? value,
         bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string? format, string? @class) {

      var fullName = htmlHelper.Name(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      output.WriteStartElement("input");

      if (setId) {
         HtmlAttributeHelper.WriteId(fullName, output);
      }

      output.WriteAttributeString("type", type ?? HtmlHelper.GetInputTypeString(inputType));
      output.WriteAttributeString("name", fullName);

      var valueParameter = htmlHelper.FormatValue(value, format);
      var usedModelState = false;

      switch (inputType) {
         case InputType.CheckBox:

            bool? modelStateWasChecked;

            try {
               modelStateWasChecked = htmlHelper.GetModelStateValue(fullName, typeof(bool)) as bool?;
            } catch (InvalidOperationException) {
               modelStateWasChecked = null;
            }

            if (modelStateWasChecked.HasValue) {
               isChecked = modelStateWasChecked.Value;
               usedModelState = true;
            }

            goto case InputType.Radio;

         case InputType.Radio:

            if (!usedModelState) {
               if (htmlHelper.GetModelStateValue(fullName, typeof(string)) is string modelStateValue) {

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

            HtmlAttributeHelper.WriteBoolean("checked", isChecked, output);

            break;

         case InputType.Password:

            if (value != null) {
               output.WriteAttributeString("value", valueParameter);
            }

            break;

         default:

            var writeValue = true;

            if (String.Equals(type, "file", StringComparison.OrdinalIgnoreCase)
               || String.Equals(type, "image", StringComparison.OrdinalIgnoreCase)) {

               // 'value' attribute is not needed for 'file' and 'image' input types.
               writeValue = false;
            }

            if (writeValue) {

               var attemptedValue = (string?)htmlHelper.GetModelStateValue(fullName, typeof(string));

               var valueAttr = attemptedValue
                  ?? ((useViewData) ? htmlHelper.EvalString(fullName, format) : valueParameter);

               output.WriteAttributeString("value", valueAttr);
            }

            break;
      }

      // If there are any errors for a named field, we add the css attribute.

      var cssClass = (htmlHelper.ViewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? HtmlHelper.ValidationInputCssClassName : null;

      HtmlAttributeHelper.WriteCssClass(@class, cssClass, output);
      HtmlAttributeHelper.WriteAttributes(htmlHelper.GetUnobtrusiveValidationAttributes(name, modelExplorer), output);

      return new ElementEndingDisposable(output);
   }
}

public class CheckBoxDisposable : IDisposable {

   readonly IDisposable
   _checkBoxOutput;

   readonly XcstWriter
   _hiddenOutput;

   readonly string
   _fullName;

   bool
   _eoc;

   bool
   _disposed;

   public
   CheckBoxDisposable(IDisposable checkBoxOutput, XcstWriter hiddenOutput, string fullName) {
      _checkBoxOutput = checkBoxOutput;
      _hiddenOutput = hiddenOutput;
      _fullName = fullName;
   }

   public void
   EndOfConstructor() {
      _eoc = true;
   }

   public CheckBoxDisposable
   NoConstructor() {
      _eoc = true;
      return this;
   }

   void
   WriteHiddenInput() {

      _hiddenOutput.WriteStartElement("input");
      _hiddenOutput.WriteAttributeString("type", "hidden");
      _hiddenOutput.WriteAttributeString("name", _fullName);
      _hiddenOutput.WriteAttributeString("value", "false");
      _hiddenOutput.WriteEndElement();
   }

   public void
   Dispose() {

      if (_disposed) {
         return;
      }

      _checkBoxOutput.Dispose();

      // don't write hidden input when end of constructor is not reached
      // e.g. an exception occurred, c:return, etc.

      if (_eoc) {
         WriteHiddenInput();
      }

      _disposed = true;
   }
}
