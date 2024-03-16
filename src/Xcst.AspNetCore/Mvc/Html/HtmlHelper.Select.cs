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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   public SelectDisposable
   Select(XcstWriter output, string name, object? value = null, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) =>
      GenerateSelect(output, default(ModelExplorer), name, value, selectList, default(string), multiple, @class);

   [GeneratedCodeReference]
   public SelectDisposable
   SelectForModel(XcstWriter output, object? value = null, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) =>
      GenerateSelect(output, this.ViewData.ModelExplorer, String.Empty, value, selectList, default(string), multiple, @class);

   protected internal SelectDisposable
   GenerateSelect(XcstWriter output, ModelExplorer? modelExplorer, string name, object? value, IEnumerable<SelectListItem>? selectList,
         string? optionLabel, bool multiple, string? @class) {

      var viewData = this.ViewData;
      var fullName = viewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      var usedViewData = false;

      // If we got a null selectList, try to use ViewData to get the list of items.

      if (selectList is null) {
         //selectList = getSelectData(name);
         //usedViewData = true;
      }

      var defaultValue = (multiple) ?
         GetModelStateValue(fullName, typeof(string[]))
         : GetModelStateValue(fullName, typeof(string));

      // If we haven't already used ViewData to get the entire list of items then we need to
      // use the ViewData-supplied value before using the parameter-supplied value.

      if (defaultValue is null) {

         if (modelExplorer != null) {
            defaultValue = modelExplorer.Model;

         } else if (value != null) {
            defaultValue = value;

         } else {

            if (!usedViewData
               && !String.IsNullOrEmpty(name)) {

               defaultValue = viewData.Eval(name);
            }
         }
      }

      var selectedValues = getSelectedValues(defaultValue, multiple);

      output.WriteStartElement("select");
      WriteId(fullName, output);
      output.WriteAttributeString("name", fullName);
      WriteBoolean("multiple", multiple, output);

      var cssClass = (viewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(name, modelExplorer, excludeMinMaxLength: !multiple, output);

      return new SelectDisposable(output, writeList, isSelected);

      IEnumerable<SelectListItem> getSelectData(string name) {

         var o = this.ViewData?.Eval(name)
            ?? throw new InvalidOperationException($"There is no ViewData item of type 'IEnumerable<SelectListItem>' that has the key '{name}'.");

         var selectList = o as IEnumerable<SelectListItem>
            ?? throw new InvalidOperationException($"The ViewData item that has the key '{name}' is of type '{o.GetType().FullName}' but must be of type 'IEnumerable<SelectListItem>'.");

         return selectList;
      }

      static HashSet<string> getSelectedValues(object? defaultValue, bool allowMultiple) {

         if (defaultValue is null) {
            return new HashSet<string>(0);
         }

         IEnumerable defaultValues;

         if (allowMultiple) {

            var defaultEnumerable = defaultValue as IEnumerable;

            if (defaultEnumerable is null || defaultEnumerable is string) {
               throw new InvalidOperationException("The parameter 'expression' must evaluate to an IEnumerable when multiple selection is allowed.");
            }

            defaultValues = defaultEnumerable;

         } else {
            defaultValues = new[] { defaultValue };
         }

         var values = from object value in defaultValues
                      select SelectValueString(value);

         // ToString() by default returns an enum value's name.  But selectList may use numeric values.

         var enumValues = from value in defaultValues.OfType<Enum>()
                          select value.ToString("d");

         values = values.Concat(enumValues);

         return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
      }

      bool isSelected(string value, bool selectedDefault) =>
         (selectedValues.Count > 0) ?
            selectedValues.Contains(value)
            : selectedDefault;

      void writeList(XcstWriter output) {

         // Make optionLabel the first item that gets rendered.

         if (optionLabel != null) {
            WriteOption(new SelectListItem {
               Text = optionLabel,
               Value = String.Empty
            }, null, output);
         }

         if (selectList is null) {
            return;
         }

         // Group items in the SelectList if requested.
         // Treat each item with Group == null as a member of a unique group
         // so they are added according to the original order.

         var groupedSelectList = selectList.GroupBy(i =>
            (i.Group is null) ? i.GetHashCode() : i.Group.GetHashCode());

         foreach (var group in groupedSelectList) {

            var optGroup = group.First().Group;

            if (optGroup != null) {

               output.WriteStartElement("optgroup");

               if (optGroup.Name != null) {
                  output.WriteAttributeString("label", optGroup.Name);
               }

               WriteBoolean("disabled", optGroup.Disabled, output);
            }

            foreach (var item in group) {

               var value = item.Value ?? item.Text ?? String.Empty;
               var selected = isSelected(value, item.Selected);

               WriteOption(item, selected, output);
            }

            if (optGroup != null) {
               output.WriteEndElement(); // </optgroup>
            }
         }
      }
   }

   internal void
   WriteOption(SelectListItem item, bool? selected, XcstWriter output) {

      output.WriteStartElement("option");

      if (item.Value != null) {
         output.WriteAttributeString("value", item.Value);
      }

      WriteBoolean("selected", selected ?? item.Selected, output);
      WriteBoolean("disabled", item.Disabled, output);

      output.WriteString(item.Text);
      output.WriteEndElement();
   }

   [GeneratedCodeReference]
   public void
   SelectOption(XcstWriter output, SelectDisposable? disp,
         object? value = null, bool selected = false, bool disabled = false, string text = "") {

      var valueStr = SelectValueString(value);
      var valueOrText = (value != null) ? valueStr : text;

      output.WriteStartElement("option");

      if (value != null) {
         output.WriteAttributeString("value", valueStr);
      }

      WriteBoolean("selected", disp?.IsSelected(valueOrText, selected) ?? selected, output);
      WriteBoolean("disabled", disabled, output);
      output.WriteString(text);
      output.WriteEndElement();
   }

   static string
   SelectValueString(object? value) =>
      Convert.ToString(value, CultureInfo.CurrentCulture)
         ?? String.Empty;

   [GeneratedCodeReference]
   public class SelectDisposable : ElementEndingDisposable {

      readonly XcstWriter
      _output;

      readonly Action<XcstWriter>
      _listBuilder;

      readonly Func<string, bool, bool>
      _isSelectedFn;

      bool
      _eoc;

      bool
      _disposed;

      internal
      SelectDisposable(XcstWriter output, Action<XcstWriter> listBuilder, Func<string, bool, bool> isSelectedFn)
         : base(output) {

         _output = output;
         _listBuilder = listBuilder;
         _isSelectedFn = isSelectedFn;
      }

      public bool
      IsSelected(string value, bool selectedDefault) =>
         _isSelectedFn.Invoke(value, selectedDefault);

      public void
      EndOfConstructor() {
         _eoc = true;
      }

      public SelectDisposable
      NoConstructor() {
         _eoc = this.ElementStarted;
         return this;
      }

      protected override void
      Dispose(bool disposing) {

         if (_disposed) {
            return;
         }

         // don't write list when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (disposing
            && _eoc) {

            _listBuilder?.Invoke(_output);
         }

         base.Dispose(disposing);

         _disposed = true;
      }
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   public SelectDisposable
   SelectFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateSelect(output, modelExplorer, expressionString, null, selectList, default(string), multiple, @class);
   }
}
