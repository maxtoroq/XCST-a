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

#region HtmlHelper is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   public IDisposable
   Input(XcstWriter output, string name, object? value = null, string? type = null, string? format = null, string? @class = null) =>
      InputImpl(output, type, modelExplorer: null, name, value, useViewData: null, format, @class);

   [GeneratedCodeReference]
   public IDisposable
   InputForModel(XcstWriter output, object? value = null, string? type = null,
         string? format = null, string? @class = null) =>
      InputForModelExplorer(output, type, this.ViewData.ModelExplorer, expression: String.Empty, value, format, @class);

   internal IDisposable
   InputForModelExplorer(XcstWriter output, string? type, ModelExplorer? modelExplorer, string expression, object? value,
         string? format, string? @class) {

      if (value is null
         && modelExplorer != null
         && GetInputType(type) != InputType.Password) {

         value = modelExplorer.Model;
      }

      return InputImpl(output, type, modelExplorer, expression, value, useViewData: false, format, @class);
   }

   IDisposable
   InputImpl(XcstWriter output, string? type, ModelExplorer? modelExplorer, string expression, object? value,
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

      return GenerateInput(
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

   protected IDisposable
   GenerateInput(XcstWriter output, InputType inputType, string? type, ModelExplorer? modelExplorer, string name, object? value,
         bool useViewData, bool isChecked, bool setId, bool isExplicitValue, string? format, string? @class) {

      var fullName = Name(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      output.WriteStartElement("input");

      if (setId) {
         WriteId(fullName, output);
      }

      output.WriteAttributeString("type", type ?? GetInputTypeString(inputType));
      output.WriteAttributeString("name", fullName);

      var valueParameter = FormatValue(value, format);
      var usedModelState = false;

      switch (inputType) {
         case InputType.CheckBox:

            bool? modelStateWasChecked;

            try {
               modelStateWasChecked = GetModelStateValue(fullName, typeof(bool)) as bool?;
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
               if (GetModelStateValue(fullName, typeof(string)) is string modelStateValue) {

                  // for CheckBox, valueParameter is "true"

                  isChecked = String.Equals(modelStateValue, valueParameter, StringComparison.Ordinal);
                  usedModelState = true;
               }
            }

            // useViewData is always false when called from RadioButton
            // the following condition is for CheckBox

            if (!usedModelState && useViewData) {
               isChecked = EvalBoolean(fullName);
            }

            output.WriteAttributeString("value", valueParameter);

            WriteBoolean("checked", isChecked, output);

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

               var attemptedValue = (string?)GetModelStateValue(fullName, typeof(string));

               var valueAttr = attemptedValue
                  ?? ((useViewData) ? EvalString(fullName, format) : valueParameter);

               output.WriteAttributeString("value", valueAttr);
            }

            break;
      }

      // If there are any errors for a named field, we add the css attribute.

      var cssClass = (ViewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);

      return new ElementEndingDisposable(output);
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
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public IDisposable
   InputFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, string? type = null,
         string? format = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var exprString = ExpressionHelper.GetExpressionText(expression);

      return InputForModelExplorer(output, type, modelExplorer, exprString, value: null, format, @class);
   }
}
