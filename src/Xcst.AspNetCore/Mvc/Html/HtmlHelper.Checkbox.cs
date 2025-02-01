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
   public CheckboxDisposable
   Checkbox(ISequenceWriter<XElement> output, string name, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateCheckbox(output, modelExplorer, name, isChecked: null, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public CheckboxDisposable
   Checkbox(ISequenceWriter<XElement> output, string name, bool isChecked, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateCheckbox(output, modelExplorer, name, isChecked, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public CheckboxDisposable
   CheckboxForModel(ISequenceWriter<XElement> output, string? @class = null) =>
      GenerateCheckbox(output, this.ViewData.ModelExplorer, name: String.Empty, isChecked: null, @class);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public CheckboxDisposable
   CheckboxForModel(ISequenceWriter<XElement> output, bool isChecked, string? @class = null) =>
      GenerateCheckbox(output, this.ViewData.ModelExplorer, name: String.Empty, isChecked, @class);

   protected internal CheckboxDisposable
   GenerateCheckbox(ISequenceWriter<XElement> output, ModelExplorer modelExplorer, string name,
         bool? isChecked, string? @class) {

      var inputWriter = DocumentWriter.CastElement(this.CurrentPackage, output);

      GenerateCheckboxInput(
         inputWriter,
         modelExplorer,
         name,
         isChecked,
         @class,
         out var fullName);

      return new CheckboxDisposable(inputWriter, writeHiddenInput);

      // Render an additional <input type="hidden".../> for checkboxes. This
      // addresses scenarios where unchecked checkboxes are not sent in the request.
      // Sending a hidden input makes it possible to know that the checkbox was present
      // on the page when the request was submitted.

      void writeHiddenInput() {

         var hiddenWriter = DocumentWriter.CastElement(this.CurrentPackage, output);

         hiddenWriter.WriteStartElement("input");
         hiddenWriter.WriteAttributeString("type", "hidden");
         hiddenWriter.WriteAttributeString("name", fullName);
         hiddenWriter.WriteAttributeString("value", "false");
         hiddenWriter.WriteEndElement();
      }
   }

   void
   GenerateCheckboxInput(XcstWriter output, ModelExplorer modelExplorer, string name,
         bool? isChecked, string? @class, out string fullName) {

      ArgumentNullException.ThrowIfNull(name);

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

      output.WriteAttributeString("type", GetInputTypeString(InputType.CheckBox));
      output.WriteAttributeString("name", fullName);
      output.WriteAttributeString("value", value);

      WriteBoolean("checked", checkedAttr.GetValueOrDefault(), output);

      var cssClass = (ViewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, default, output);
   }

   [EditorBrowsable(EditorBrowsableState.Never)]
   public class CheckboxDisposable : ElementEndingDisposable {

      readonly Action
      _hiddenFn;

      bool
      _eoc;

      [GeneratedCodeReference]
      public XcstWriter
      CheckboxOutput { get; }

      public
      CheckboxDisposable(XcstWriter output, Action hiddenFn)
         : base(output, elementStarted: true) {

         _hiddenFn = hiddenFn;
         this.CheckboxOutput = output;
      }

      [GeneratedCodeReference]
      public void
      EndOfConstructor() {
         _eoc = true;
      }

      [GeneratedCodeReference]
      public CheckboxDisposable
      NoConstructor() {
         _eoc = true;
         return this;
      }

      protected override void
      Dispose(bool disposing) {

         base.Dispose(disposing);

         // don't write hidden input when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (disposing
            && _eoc) {

            _hiddenFn.Invoke();
         }
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public CheckboxDisposable
   CheckboxFor(ISequenceWriter<XElement> output, Expression<Func<TModel, bool>> expression, string? @class = null) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateCheckbox(output, modelExplorer, expressionString, isChecked: null, @class);
   }
}
