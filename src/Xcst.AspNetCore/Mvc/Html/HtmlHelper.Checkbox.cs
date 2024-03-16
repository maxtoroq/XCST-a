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
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   public CheckboxDisposable
   Checkbox(ISequenceWriter<XElement> output, string name, string? @class = null) =>
      GenerateCheckbox(output, modelExplorer: null, name, isChecked: null, @class);

   [GeneratedCodeReference]
   public CheckboxDisposable
   Checkbox(ISequenceWriter<XElement> output, string name, bool isChecked, string? @class = null) =>
      GenerateCheckbox(output, modelExplorer: null, name, isChecked, @class);

   [GeneratedCodeReference]
   public CheckboxDisposable
   CheckboxForModel(ISequenceWriter<XElement> output, string? @class = null) =>
      CheckboxForModelExplorer(output, this.ViewData.ModelExplorer, expression: String.Empty, isChecked: null, @class);

   [GeneratedCodeReference]
   public CheckboxDisposable
   CheckboxForModel(ISequenceWriter<XElement> output, bool isChecked, string? @class = null) =>
      CheckboxForModelExplorer(output, this.ViewData.ModelExplorer, expression: String.Empty, isChecked, @class);

   internal CheckboxDisposable
   CheckboxForModelExplorer(ISequenceWriter<XElement> output, ModelExplorer? modelExplorer, string expression,
         bool? isChecked, string? @class) {

      var model = modelExplorer?.Model;

      if (isChecked is null
         && model != null) {

         if (Boolean.TryParse(model.ToString(), out bool modelChecked)) {
            isChecked = modelChecked;
         }
      }

      return GenerateCheckbox(output, modelExplorer, expression, isChecked, @class);
   }

   protected internal CheckboxDisposable
   GenerateCheckbox(ISequenceWriter<XElement> output, ModelExplorer? modelExplorer, string name,
         bool? isChecked, string? @class) {

      var inputWriter = DocumentWriter.CastElement(this.CurrentPackage, output);
      var hiddenWriter = DocumentWriter.CastElement(this.CurrentPackage, output);

      var explicitChecked = isChecked.HasValue;

      var inputDisposable = GenerateInput(
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
         @class: @class);

      var fullName = Name(name);

      // Render an additional <input type="hidden".../> for checkboxes. This
      // addresses scenarios where unchecked checkboxes are not sent in the request.
      // Sending a hidden input makes it possible to know that the checkbox was present
      // on the page when the request was submitted.

      return new CheckboxDisposable(inputDisposable, inputWriter, hiddenWriter, fullName);
   }

   [GeneratedCodeReference]
   public class CheckboxDisposable : IDisposable {

      readonly IDisposable
      _checkboxCloser;

      readonly XcstWriter
      _hiddenOutput;

      readonly string
      _fullName;

      bool
      _eoc;

      bool
      _disposed;

      public XcstWriter
      CheckboxOutput { get; }

      public
      CheckboxDisposable(IDisposable checkboxCloser, XcstWriter checkboxOutput, XcstWriter hiddenOutput, string fullName) {
         _checkboxCloser = checkboxCloser;
         this.CheckboxOutput = checkboxOutput;
         _hiddenOutput = hiddenOutput;
         _fullName = fullName;
      }

      public void
      EndOfConstructor() {
         _eoc = true;
      }

      public CheckboxDisposable
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

         _checkboxCloser.Dispose();

         // don't write hidden input when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (_eoc) {
            WriteHiddenInput();
         }

         _disposed = true;
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public CheckboxDisposable
   CheckboxFor(ISequenceWriter<XElement> output, Expression<Func<TModel, bool>> expression, string? @class = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return CheckboxForModelExplorer(output, modelExplorer, expressionString, isChecked: null, @class);
   }
}
