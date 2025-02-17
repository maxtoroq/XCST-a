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
   public SelectDisposable
   Select(XcstWriter output, string name, object? value = null, IEnumerable<SelectListItem>? options = null,
         bool multiple = false, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return GenerateSelect(output, modelExplorer, name, value, options, multiple, @class);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public SelectDisposable
   SelectForModel(XcstWriter output, object? value = null, IEnumerable<SelectListItem>? options = null,
         bool multiple = false, string? @class = null) =>
      GenerateSelect(output, this.ViewData.ModelExplorer, String.Empty, value, options, multiple, @class);

   protected internal SelectDisposable
   GenerateSelect(XcstWriter output, ModelExplorer modelExplorer, string name, object? value, IEnumerable<SelectListItem>? options,
         bool multiple, string? @class) {

      var viewData = this.ViewData;
      var fullName = FullNameNonEmpty(name);

      var defaultValue = (multiple) ?
         GetModelStateValue(fullName, typeof(string[]))
         : GetModelStateValue(fullName, typeof(string));

      defaultValue ??= value ?? modelExplorer.Model;

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
   SelectOption(XcstWriter output, SelectDisposable? disp,
         object? value = null, bool selected = false, bool disabled = false, string? text = null) {

      var valueStr = SelectValueString(value);
      var valueOrText = (value != null) ? valueStr : text ?? String.Empty;

      output.WriteStartElement("option");

      if (value != null) {
         output.WriteAttributeString("value", valueStr);
      }

      WriteBoolean("selected", disp?.IsSelected(valueOrText, selected) ?? selected, output);
      WriteBoolean("disabled", disabled, output);

      if (text != null) {
         output.WriteString(text);
      }

      return new ElementEndingDisposable(output);
   }

   string
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
   SelectFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, IEnumerable<SelectListItem>? options = null,
         bool multiple = false, string? @class = null) {

      ArgumentNullException.ThrowIfNull(expression);

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateSelect(output, modelExplorer, expressionString, value: null, options, multiple, @class);
   }
}
