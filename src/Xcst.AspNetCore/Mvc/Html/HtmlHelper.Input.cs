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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   static readonly Dictionary<string, string>
   _defaultInputTypes = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType
      { "Date", "date" },
      { "DateTime", "datetime-local" },
      { "DateTime-local", "datetime-local" },
      { "EmailAddress", "email" },
      { "Password", "password" },
      { "PhoneNumber", "tel" },
      { "Text", "text" },
      { "Time", "time" },
      { "Upload", "file" },
      { "Url", "url" },

      // integer
      { nameof(Byte), "number" },
      { nameof(Int16), "number" },
      { nameof(Int32), "number" },
      { nameof(Int64), "number" },
      { nameof(Int128), "number" },
      { nameof(SByte), "number" },
      { nameof(UInt16), "number" },
      { nameof(UInt32), "number" },
      { nameof(UInt64), "number" },
      { nameof(UInt128), "number" },

      // floating-point
      { nameof(Decimal), "text" },
      { nameof(Double), "text" },
      { nameof(Single), "text" },

      // other
      { "HiddenInput", "hidden" },
      { nameof(IFormFile), "file" },
      { "Month", "month" },
      { nameof(String), "text" },
      { "Week", "week" },
   };

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
      var inputType = type ?? GetInputType(modelExplorer, out var inputTypeHint);

      var valueOrModel = value ?? modelExplorer.Model;
      var valueAttr = (string?)GetModelStateValue(fullName, typeof(string));

      valueAttr ??= (InputTypeEquals(inputType, "hidden")
         && valueOrModel is byte[] byteArrayValue) ? Convert.ToBase64String(byteArrayValue)
         : FormatValue(valueOrModel, format);

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", inputType);
      output.WriteAttributeString("name", fullName);

      if (!(OmitInputValue(inputType) && value is null)) {
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

   internal static bool
   OmitInputValue(string? inputType) =>
      InputTypeEquals(inputType, "password")
         || InputTypeEquals(inputType, "file")
         || InputTypeEquals(inputType, "image");

   static bool
   InputTypeEquals(string? a, string? b) =>
      String.Equals(a, b, StringComparison.OrdinalIgnoreCase);

   static string
   GetInputType(ModelExplorer modelExplorer, out string inputTypeHint) {

      foreach (var hint in GetInputTypeHints(modelExplorer)) {
         if (_defaultInputTypes.TryGetValue(hint, out var inputType)) {
            inputTypeHint = hint;
            return inputType;
         }
      }

      inputTypeHint = "text";
      return inputTypeHint;
   }

   static IEnumerable<string>
   GetInputTypeHints(ModelExplorer modelExplorer) {

      if (!String.IsNullOrEmpty(modelExplorer.Metadata.TemplateHint)) {
         yield return modelExplorer.Metadata.TemplateHint;
      }

      if (!String.IsNullOrEmpty(modelExplorer.Metadata.DataTypeName)) {
         yield return modelExplorer.Metadata.DataTypeName;
      }

      var fieldType = modelExplorer.Metadata.UnderlyingOrModelType;

      foreach (var typeName in TemplateRenderer.GetTypeNames(modelExplorer.Metadata, fieldType)) {
         yield return typeName;
      }
   }
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
