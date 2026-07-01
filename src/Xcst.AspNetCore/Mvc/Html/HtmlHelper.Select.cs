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
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public struct SelectArgs {

      [GeneratedCodeReference]
      public object?
      value { get; set; }

      [GeneratedCodeReference]
      public IEnumerable<SelectListItem>?
      options { get; set; }

      [GeneratedCodeReference]
      public bool
      multiple { get; set; }

      [GeneratedCodeReference]
      public string?
      @class { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public struct OptionArgs {

      object?
      _value;

      bool
      _valueSet;

      [GeneratedCodeReference]
      public object?
      value {
         readonly get => _value;
         set {
            _value = value;
            _valueSet = true;
         }
      }

      internal readonly bool
      valueSet => _valueSet;

      [GeneratedCodeReference]
      public bool
      selected { get; set; }

      [GeneratedCodeReference]
      public bool
      disabled { get; set; }

      [GeneratedCodeReference]
      public string?
      text { get; set; }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SelectDisposable
   Select(XcstWriter output, string expression, SelectArgs args = default) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return GenerateSelect(output, modelExplorer, expression, args);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SelectDisposable
   SelectForModel(XcstWriter output, SelectArgs args = default) =>
      GenerateSelect(output, this.ModelExplorer, String.Empty, args);

   protected internal SelectDisposable
   GenerateSelect(XcstWriter output, ModelExplorer modelExplorer, string expression, SelectArgs args) {

      var value = args.value;
      var options = args.options;
      var multiple = args.multiple;
      var @class = args.@class;

      var fullName = FullNameNonEmpty(expression);
      var modelState = this.ModelState[fullName];

      var defaultValue = GetModelStateValue(modelState, (multiple) ? typeof(string[]) : typeof(string))
         ?? value ?? modelExplorer.Model;

      var selectedValues = getSelectedValues(defaultValue, multiple);

      output.WriteStartElement("select");

      WriteId(fullName, output);

      output.WriteAttributeString("name", fullName);
      WriteBoolean("multiple", multiple, output);

      var cssClass = (modelState?.Errors.Count > 0) ?
         ValidationInputCssClassName : null;

      WriteCssClass(@class, cssClass, output);
      WriteUnobtrusiveValidationAttributes(fullName, modelExplorer, excludeMinMaxLength: !multiple, output);

      return new SelectDisposable(output, writeList, isSelected);

      HashSet<string> getSelectedValues(object? defaultValue, bool allowMultiple) {

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

         // ToString() by default returns an enum value's name. But selectList may use numeric values.

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

         if (options is null) {
            return;
         }

         // Group items in the SelectList if requested.
         // Treat each item with Group == null as a member of a unique group
         // so they are added according to the original order.

         var groupedSelectList = options.GroupBy(i =>
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

               var valueOrText = item.Value ?? item.Text ?? String.Empty;
               var selected = isSelected(valueOrText, item.Selected);

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
   [EditorBrowsable(EditorBrowsableState.Never)]
   public IDisposable
   Option(XcstWriter output, SelectDisposable? disp, OptionArgs args = default) {

      var value = args.value;
      var selected = args.selected;
      var disabled = args.disabled;
      var text = args.text;

      var valueStr = SelectValueString(value);
      var valueOrText = (value != null) ? valueStr : text ?? String.Empty;

      output.WriteStartElement("option");

      if (args.valueSet) {
         output.WriteAttributeString("value", valueStr);
      }

      WriteBoolean("selected", disp?.IsSelected(valueOrText, selected) ?? selected, output);
      WriteBoolean("disabled", disabled, output);

      if (text != null) {
         output.WriteString(text);
      }

      return new ElementEndingDisposable(output);
   }

   internal string
   SelectValueString(object? value) =>
      FormatValue(value, null);

   [EditorBrowsable(EditorBrowsableState.Never)]
   public class SelectDisposable : DefaultContentDisposable {

      readonly Func<string, bool, bool>
      _isSelectedFn;

      internal
      SelectDisposable(XcstWriter output, Action<XcstWriter> listBuilder, Func<string, bool, bool> isSelectedFn)
         : base(output, elementStarted: true, listBuilder) {

         _isSelectedFn = isSelectedFn;
      }

      internal bool
      IsSelected(string value, bool selectedDefault) =>
         _isSelectedFn.Invoke(value, selectedDefault);
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SelectDisposable
   SelectFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, SelectArgs args = default) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateSelect(output, modelExplorer, expressionString, args);
   }
}
