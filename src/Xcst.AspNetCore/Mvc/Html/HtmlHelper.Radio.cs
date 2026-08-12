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
   public struct RadioArgs {

      [GeneratedCodeReference]
      public bool?
      @checked { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   Radio(XcstWriter output, object value, RadioArgs args = default) =>
      GenerateRadio(output, this.ModelExplorer, String.Empty, value, args);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   Radio(XcstWriter output, object value, string expression, RadioArgs args = default) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return GenerateRadio(output, modelExplorer, expression, value, args);
   }

   protected IDisposable
   GenerateRadio(XcstWriter output, ModelExplorer modelExplorer, string expression, object value,
         RadioArgs args) {

      ArgumentNullException.ThrowIfNull(output);
      ArgumentNullException.ThrowIfNull(modelExplorer);
      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(value);

      var @checked = args.@checked;
      var @class = args.@class;

      var fullName = FullNameNonEmpty(expression);
      var valueString = FormatValue(value, null);
      var modelState = this.ModelState[fullName];

      var checkedAttr = default(bool?);

      if (GetModelStateValue(modelState, typeof(string)) is string modelStateValue) {
         checkedAttr = String.Equals(modelStateValue, valueString, StringComparison.Ordinal);
      }

      checkedAttr ??= @checked;

      if (checkedAttr is null
         && modelExplorer.Model is { } model) {

         checkedAttr = RadioValueEquals(value, model);
      }

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", "radio");
      output.WriteAttributeString("name", fullName);
      output.WriteAttributeString("value", valueString);

      WriteBoolean("checked", checkedAttr.GetValueOrDefault(), output);

      var cssClass = (modelState?.Errors.Count > 0) ?
         ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(fullName, modelExplorer, default, output);

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
   RadioFor<TResult>(XcstWriter output, object value, Expression<Func<TModel, TResult>> expression,
         RadioArgs args = default) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateRadio(output, modelExplorer, expressionString, value, args);
   }
}
