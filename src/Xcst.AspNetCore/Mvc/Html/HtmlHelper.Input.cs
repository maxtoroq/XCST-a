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
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   static readonly Dictionary<string, string>
   _defaultInputTypes = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType
      { nameof(DataType.Date), "date" },
      { nameof(DataType.DateTime), "datetime-local" },
      { nameof(DataType.DateTime) + "-local", "datetime-local" },
      { nameof(DataType.EmailAddress), "email" },
      { nameof(DataType.Password), "password" },
      { nameof(DataType.PhoneNumber), "tel" },
      { nameof(DataType.Text), "text" },
      { nameof(DataType.Time), "time" },
      { nameof(DataType.Upload), "file" },
      { nameof(DataType.Url), "url" },

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

   static readonly Dictionary<string, string>
   _rfc3339Formats = new(StringComparer.Ordinal) {
      { "date", "{0:yyyy-MM-dd}" },
      { "datetime", @"{0:yyyy-MM-ddTHH\:mm\:ss.fffK}" },
      { "datetime-local", @"{0:yyyy-MM-ddTHH\:mm\:ss.fff}" },
      { "month", "{0:yyyy-MM}" },
      { "time", @"{0:HH\:mm\:ss.fff}" },
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

      var inputTypeHint = default(string);
      var inputType = type ?? GetInputType(modelExplorer, out inputTypeHint);

      format ??= GetFormat(modelExplorer.Metadata, name, inputType, inputTypeHint);

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

      var cssClass = (this.ModelState.TryGetValue(fullName, out var modelState)
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

      foreach (var hint in GetInputTypeHints(modelExplorer.Metadata)) {
         if (_defaultInputTypes.TryGetValue(hint, out var inputType)) {
            inputTypeHint = hint;
            return inputType;
         }
      }

      inputTypeHint = nameof(String);

      return "text";
   }

   static IEnumerable<string>
   GetInputTypeHints(ModelMetadata metadata) {

      if (!String.IsNullOrEmpty(metadata.TemplateHint)) {
         yield return metadata.TemplateHint;
      }

      if (!String.IsNullOrEmpty(metadata.DataTypeName)) {
         yield return metadata.DataTypeName;
      }

      var fieldType = metadata.UnderlyingOrModelType;

      foreach (var typeName in TemplateRenderer.GetTypeNames(metadata, fieldType)) {
         yield return typeName;
      }
   }

   string?
   GetFormat(ModelMetadata metadata, string name, string inputType, string? inputTypeHint) {

      if (UsingFormattedModelValue(name)) {

         // Calling from an editor/display template, getting format for
         // the top model (not a property). Formatting is already done and should be
         // using TemplateInfo.FormattedModelValue as value.

         return null;
      }

      if (metadata.HasNonDefaultEditFormat) {
         return metadata.EditFormatString;
      }

      return GetDataTypeFormat(metadata, inputType, inputTypeHint)
         ?? metadata.EditFormatString;
   }

   internal string?
   GetDataTypeFormat(ModelMetadata metadata, string inputType, string? inputTypeHint) {

      // inputTypeHint should always take precedence over UnderlyingOrModelType

      if (_defaultInputTypes.Comparer.Equals(inputTypeHint, nameof(Decimal))
         || metadata.UnderlyingOrModelType == typeof(Decimal)) {

         return "{0:0.00}";
      }

      if (_rfc3339Formats.TryGetValue(inputType, out var rfc3339Format)) {
         return rfc3339Format;
      }

      return null;
   }

   bool
   UsingFormattedModelValue(string name) =>
      this.TemplateInfo.TemplateName != null
         && String.IsNullOrEmpty(name);
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
