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
   Radio(XcstWriter output, string name, object value, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateRadio(output, modelExplorer, name, value, isChecked: null, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   Radio(XcstWriter output, string name, object value, bool isChecked, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateRadio(output, modelExplorer, name, value, isChecked, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   RadioForModel(XcstWriter output, object value, string? @class = null) =>
      GenerateRadio(output, this.ViewData.ModelExplorer, String.Empty, value, isChecked: null, @class);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   RadioForModel(XcstWriter output, object value, bool isChecked, string? @class = null) =>
      GenerateRadio(output, this.ViewData.ModelExplorer, String.Empty, value, isChecked, @class);

   protected IDisposable
   GenerateRadio(XcstWriter output, ModelExplorer modelExplorer, string name, object value,
         bool? isChecked, string? @class) {

      ArgumentNullException.ThrowIfNull(name);
      ArgumentNullException.ThrowIfNull(value);

      var fullName = FullNameNonEmpty(name);
      var valueString = FormatValue(value, null);

      var checkedAttr = default(bool?);

      if (GetModelStateValue(fullName, typeof(string)) is string modelStateValue) {
         checkedAttr = String.Equals(modelStateValue, valueString, StringComparison.Ordinal);
      }

      checkedAttr ??= isChecked;

      if (checkedAttr is null
         && modelExplorer.Model is { } model) {

         checkedAttr = RadioValueEquals(value, model);
      }

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", GetInputTypeString(InputType.Radio));
      output.WriteAttributeString("name", fullName);
      output.WriteAttributeString("value", valueString);

      WriteBoolean("checked", checkedAttr.GetValueOrDefault(), output);

      var cssClass = (ViewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);

      return new ElementEndingDisposable(output);
   }

   bool
   RadioValueEquals(object value, object? viewDataValue) {

      var valueString = FormatValue(value, null);
      var vdString = FormatValue(viewDataValue, null);

      return String.Equals(vdString, valueString, StringComparison.OrdinalIgnoreCase);
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   RadioFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, object value,
         string? @class = null) {

      ArgumentNullException.ThrowIfNull(value);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateRadio(output, modelExplorer, expressionString, value, isChecked: null, @class);
   }
}
