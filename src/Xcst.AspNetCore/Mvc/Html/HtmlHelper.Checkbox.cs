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
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public struct CheckboxArgs {

      [GeneratedCodeReference]
      public bool?
      @checked { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SiblingContentDisposable
   Checkbox(ISequenceWriter<XElement> output, string name, CheckboxArgs args = default) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateCheckbox(output, modelExplorer, name, args);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SiblingContentDisposable
   CheckboxForModel(ISequenceWriter<XElement> output, CheckboxArgs args = default) =>
      GenerateCheckbox(output, this.ViewData.ModelExplorer, name: String.Empty, args);

   protected internal SiblingContentDisposable
   GenerateCheckbox(
         ISequenceWriter<XElement> output, ModelExplorer modelExplorer, string name,
         CheckboxArgs args) {

      var inputWriter = DocumentWriter.CastElement(this.ViewContext.CurrentPackage, output);

      GenerateCheckboxInput(
         inputWriter,
         modelExplorer,
         name,
         args,
         out var fullName);

      return new SiblingContentDisposable(inputWriter, writeHiddenInput);

      // Render an additional <input type="hidden".../> for checkboxes. This
      // addresses scenarios where unchecked checkboxes are not sent in the request.
      // Sending a hidden input makes it possible to know that the checkbox was present
      // on the page when the request was submitted.

      void writeHiddenInput() {

         var hiddenWriter = DocumentWriter.CastElement(this.ViewContext.CurrentPackage, output);

         hiddenWriter.WriteStartElement("input");
         hiddenWriter.WriteAttributeString("type", "hidden");
         hiddenWriter.WriteAttributeString("name", fullName);
         hiddenWriter.WriteAttributeString("value", "false");
         hiddenWriter.WriteEndElement();
      }
   }

   void
   GenerateCheckboxInput(XcstWriter output, ModelExplorer modelExplorer, string name,
         CheckboxArgs args, out string fullName) {

      ArgumentNullException.ThrowIfNull(name);

      var isChecked = args.@checked;
      var @class = args.@class;

      fullName = FullNameNonEmpty(name);

      var value = "true";

      bool? checkedAttr;

      try {
         checkedAttr = GetModelStateValue(fullName, typeof(bool)) as bool?;
      } catch (InvalidOperationException) {

         checkedAttr = (GetModelStateValue(fullName, typeof(string)) is string modelStateValue) ?
            String.Equals(modelStateValue, value, StringComparison.Ordinal)
            : null;
      }

      checkedAttr ??= isChecked;

      if (checkedAttr is null
         && modelExplorer.Model is { } model
         && Boolean.TryParse(model.ToString(), out var modelChecked)) {

         checkedAttr = modelChecked;
      }

      output.WriteStartElement("input");

      WriteId(fullName, output);

      output.WriteAttributeString("type", "checkbox");
      output.WriteAttributeString("name", fullName);
      output.WriteAttributeString("value", value);

      WriteBoolean("checked", checkedAttr.GetValueOrDefault(), output);

      var cssClass = (this.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public SiblingContentDisposable
   CheckboxFor(
         ISequenceWriter<XElement> output, Expression<Func<TModel, bool>> expression,
         CheckboxArgs args = default) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateCheckbox(output, modelExplorer, expressionString, args);
   }
}
