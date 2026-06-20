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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xcst.Runtime;
using DataType = System.ComponentModel.DataAnnotations.DataType;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public struct InputArgs {

      [GeneratedCodeReference]
      public object?
      value { get; set; }

      [GeneratedCodeReference]
      public string?
      type { get; set; }

      [GeneratedCodeReference]
      public string?
      format { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   const string
   _fallbackInputType = "text";

   static readonly Dictionary<string, string>
   _defaultInputTypes = new(StringComparer.OrdinalIgnoreCase) {

      // System.ComponentModel.DataAnnotations.DataType
      { nameof(DataType.Date), "date" },
      { nameof(DataType.DateTime), "datetime-local" },
      { nameof(DataType.DateTime) + "-local", "datetime-local" },
      { nameof(DataType.EmailAddress), "email" },
      { nameof(DataType.Password), "password" },
      { nameof(DataType.PhoneNumber), "tel" },
      { nameof(DataType.Text), _fallbackInputType },
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
      { nameof(Decimal), _fallbackInputType },
      { nameof(Double), _fallbackInputType },
      { nameof(Single), _fallbackInputType },

      // other
      { "HiddenInput", "hidden" },
      { nameof(IFormFile), "file" },
      { "Month", "month" },
      { nameof(String), _fallbackInputType },
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

   static readonly HashSet<string>
   _invariantInputTypes = new(
      _rfc3339Formats.Keys.Append("number"), StringComparer.Ordinal);

   static readonly HashSet<string>
   _noValueInputTypes = new(new[] {
      "file",
      "image",
      "password",
   }, StringComparer.Ordinal);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SiblingContentDisposable
   Input(ISequenceWriter<XElement> output, string name, InputArgs args = default) {

      var modelExplorer = GetModelExplorerFromString(name);

      return GenerateInput(output, modelExplorer, name, args);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SiblingContentDisposable
   InputForModel(ISequenceWriter<XElement> output, InputArgs args = default) =>
      GenerateInput(output, this.ModelExplorer, String.Empty, args);

   protected internal SiblingContentDisposable
   GenerateInput(ISequenceWriter<XElement> output, ModelExplorer modelExplorer, string name, InputArgs args) {

      var inputWriter = DocumentWriter.CastElement(this.CurrentPackage, output);

      GenerateInputElement(
         inputWriter,
         modelExplorer,
         name,
         args,
         out var fullName,
         out var inputType);

      return new SiblingContentDisposable(inputWriter, writeHiddenInput);

      void writeHiddenInput() {

         if (!includeHiddenInvariantField(inputType)) {
            return;
         }

         var hiddenWriter = DocumentWriter.CastElement(this.CurrentPackage, output);

         hiddenWriter.WriteStartElement("input");
         hiddenWriter.WriteAttributeString("type", "hidden");
         hiddenWriter.WriteAttributeString("name", "__Invariant");
         hiddenWriter.WriteAttributeString("value", fullName);
         hiddenWriter.WriteEndElement();
      }

      bool
      includeHiddenInvariantField(string inputType) {

         if (this.ViewContext.FormMethod == FormMethod.Get) {
            return false;
         }

         return InputTypeIsInvariant(inputType);
      }
   }

   void
   GenerateInputElement(
         XcstWriter output, ModelExplorer modelExplorer, string name, InputArgs args,
         out string fullName,
         out string inputType) {

      ArgumentNullException.ThrowIfNull(name);

      var value = args.value;
      var type = args.type;
      var format = args.format;
      var @class = args.@class;

      fullName = FullNameNonEmpty(name);

      var inputTypeHint = default(string);
      inputType = type ?? GetInputType(modelExplorer, out inputTypeHint);

      format ??= GetFormat(modelExplorer.Metadata, inputType, inputTypeHint);

      var valueOrModel = value ?? modelExplorer.Model;
      var valueAttr = (string?)GetModelStateValue(fullName, typeof(string));

      var culture = InputTypeIsInvariant(inputType) ?
         CultureInfo.InvariantCulture : null;

      valueAttr ??= (InputTypeEquals(inputType, "hidden")
         && valueOrModel is byte[] byteArrayValue) ? Convert.ToBase64String(byteArrayValue)
         : FormatValue(valueOrModel, format, culture);

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", inputType);
      output.WriteAttributeString("name", fullName);

      if (!(value is null && InputOmitValue(inputType))) {
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
   }

   static string
   GetInputType(ModelExplorer modelExplorer, out string inputTypeHint) {

      foreach (var hint in GetInputTypeHints(modelExplorer.Metadata)) {
         if (_defaultInputTypes.TryGetValue(hint, out var inputType)) {
            inputTypeHint = hint;
            return inputType;
         }
      }

      inputTypeHint = nameof(String);

      return _fallbackInputType;
   }

   internal static string
   GetInputType(string inputTypeHint) {

      if (_defaultInputTypes.TryGetValue(inputTypeHint, out var inputType)) {
         return inputType;
      }

      return _fallbackInputType;
   }

   static IEnumerable<string>
   GetInputTypeHints(ModelMetadata metadata) {

      if (!String.IsNullOrEmpty(metadata.TemplateHint)) {
         yield return metadata.TemplateHint;
      }

      if (!String.IsNullOrEmpty(metadata.DataTypeName)) {
         yield return metadata.DataTypeName;
      }

      foreach (var typeName in TemplateRenderer.GetTypeNames(metadata)) {
         yield return typeName;
      }
   }

   string?
   GetFormat(ModelMetadata metadata, string inputType, string? inputTypeHint) {

      if (metadata.HasNonDefaultEditFormat) {
         return metadata.EditFormatString;
      }

      return GetDataTypeFormat(metadata, inputType, inputTypeHint)
         ?? metadata.EditFormatString;
   }

   internal string?
   GetDataTypeFormat(ModelMetadata metadata, string? inputType, string? inputTypeHint) {

      inputType ??= _fallbackInputType;

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

   static bool
   InputOmitValue(string inputType) =>
      _noValueInputTypes.Contains(inputType);

   static bool
   InputTypeEquals(string? a, string? b) =>
      String.Equals(a, b, StringComparison.OrdinalIgnoreCase);

   static bool
   InputTypeIsInvariant(string inputType) =>
      _invariantInputTypes.Contains(inputType);
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SiblingContentDisposable
   InputFor<TResult>(ISequenceWriter<XElement> output, Expression<Func<TModel, TResult>> expression, InputArgs args = default) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateInput(output, modelExplorer, expressionString, args);
   }
}
