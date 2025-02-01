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
using System.ComponentModel;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   Input(XcstWriter output, string name, object? value = null, string? type = null,
         string? format = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateInput(output, type, modelExplorer, name, value, format, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   InputForModel(XcstWriter output, object? value = null, string? type = null,
         string? format = null, string? @class = null) =>
      GenerateInput(output, type, this.ViewData.ModelExplorer, name: String.Empty, value, format, @class);

   protected internal IDisposable
   GenerateInput(XcstWriter output, string? type, ModelExplorer modelExplorer, string name, object? value,
         string? format, string? @class) {

      ArgumentNullException.ThrowIfNull(name);

      var fullName = FullNameNonEmpty(name);
      var inputType = GetInputType(type) ?? InputType.Text;
      var valueOrModel = value ?? modelExplorer.Model;

      var valueAttr = (string?)GetModelStateValue(fullName, typeof(string));

      valueAttr ??= (inputType == InputType.Hidden
         && valueOrModel is byte[] byteArrayValue) ? Convert.ToBase64String(byteArrayValue)
         : FormatValue(valueOrModel, format);

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", type ?? GetInputTypeString(inputType));
      output.WriteAttributeString("name", fullName);

      if (inputType == InputType.Password
         || String.Equals(type, "file", StringComparison.OrdinalIgnoreCase)
         || String.Equals(type, "image", StringComparison.OrdinalIgnoreCase)) {

         if (value != null) {
            output.WriteAttributeString("value", valueAttr);
         }

      } else {
         output.WriteAttributeString("value", valueAttr);
      }

      var cssClass = (this.ViewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteBoolean("readonly", modelExplorer.Metadata.IsReadOnly, output);

      if (!String.IsNullOrEmpty(modelExplorer.Metadata.Placeholder)) {
         output.WriteAttributeString("placeholder", modelExplorer.Metadata.Placeholder);
      }

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
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   InputFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, string? type = null,
         string? format = null, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var exprString = ExpressionHelper.GetExpressionText(expression);

      return GenerateInput(output, type, modelExplorer, exprString, value: null, format, @class);
   }
}
