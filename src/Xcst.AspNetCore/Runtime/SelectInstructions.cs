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

#region SelectInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class SelectInstructions {

   public static SelectDisposable
   Select(HtmlHelper htmlHelper, XcstWriter output, string name, object? value = null, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) =>
      SelectHelper(htmlHelper, output, default(ModelExplorer), name, value, selectList, default(string), multiple, @class);

   [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Users cannot use anonymous methods with the LambdaExpression type")]
   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static SelectDisposable
   SelectFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return SelectHelper(htmlHelper, output, modelExplorer, expressionString, null, selectList, default(string), multiple, @class);
   }

   public static SelectDisposable
   SelectForModel(HtmlHelper htmlHelper, XcstWriter output, object? value = null, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, string? @class = null) =>

      SelectHelper(htmlHelper, output, htmlHelper.ViewData.ModelExplorer, String.Empty, value, selectList, default(string), multiple, @class);

   internal static SelectDisposable
   SelectHelper(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer? modelExplorer, string name, object? value, IEnumerable<SelectListItem>? selectList,
         string? optionLabel, bool multiple, string? @class) {

      var viewData = htmlHelper.ViewData;
      var fullName = viewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      var usedViewData = false;

      // If we got a null selectList, try to use ViewData to get the list of items.

      if (selectList is null) {
         //selectList = GetSelectData(htmlHelper, name);
         //usedViewData = true;
      }

      var defaultValue = (multiple) ?
         htmlHelper.GetModelStateValue(fullName, typeof(string[]))
         : htmlHelper.GetModelStateValue(fullName, typeof(string));

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

      var selectedValues = GetSelectedValues(defaultValue, multiple);

      output.WriteStartElement("select");
      HtmlAttributeHelper.WriteId(fullName, output);
      output.WriteAttributeString("name", fullName);
      HtmlAttributeHelper.WriteBoolean("multiple", multiple, output);

      var cssClass = (viewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? HtmlHelper.ValidationInputCssClassName : null;

      HtmlAttributeHelper.WriteCssClass(@class, cssClass, output);
      htmlHelper.WriteUnobtrusiveValidationAttributes(name, modelExplorer, excludeMinMaxLength: !multiple, output);

      bool isSelected(string value, bool selectedDefault) =>
         (selectedValues.Count > 0) ?
            selectedValues.Contains(value)
            : selectedDefault;

      void writeList(XcstWriter output) =>
         WriteOptions(optionLabel, selectList, isSelected, output);

      return new SelectDisposable(output, writeList, isSelected);
   }

   static IEnumerable<SelectListItem>
   GetSelectData(HtmlHelper htmlHelper, string name) {

      var o = htmlHelper.ViewData?.Eval(name)
         ?? throw new InvalidOperationException($"There is no ViewData item of type 'IEnumerable<SelectListItem>' that has the key '{name}'.");

      var selectList = o as IEnumerable<SelectListItem>
         ?? throw new InvalidOperationException($"The ViewData item that has the key '{name}' is of type '{o.GetType().FullName}' but must be of type 'IEnumerable<SelectListItem>'.");

      return selectList;
   }

   static HashSet<string>
   GetSelectedValues(object? defaultValue, bool allowMultiple) {

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
                   select ValueString(value);

      // ToString() by default returns an enum value's name.  But selectList may use numeric values.

      var enumValues = from value in defaultValues.OfType<Enum>()
                       select value.ToString("d");

      values = values.Concat(enumValues);

      return new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
   }

   static string
   ValueString(object? value) =>
      Convert.ToString(value, CultureInfo.CurrentCulture)
         ?? String.Empty;

   public static void
   OptionHelper(HtmlHelper htmlHelper, XcstWriter output, SelectDisposable? disp,
         object? value = null, bool selected = false, bool disabled = false, string text = "") {

      var valueStr = ValueString(value);
      var valueOrText = (value != null) ? valueStr : text;

      output.WriteStartElement("option");

      if (value != null) {
         output.WriteAttributeString("value", valueStr);
      }

      HtmlAttributeHelper.WriteBoolean("selected", disp?.IsSelected(valueOrText, selected) ?? selected, output);
      HtmlAttributeHelper.WriteBoolean("disabled", disabled, output);
      output.WriteString(text);
      output.WriteEndElement();
   }

   static void
   WriteOptions(string? optionLabel, IEnumerable<SelectListItem>? selectList, Func<string, bool, bool> isSelectedFn, XcstWriter output) {

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

            HtmlAttributeHelper.WriteBoolean("disabled", optGroup.Disabled, output);
         }

         foreach (var item in group) {

            var value = item.Value ?? item.Text ?? String.Empty;
            var selected = isSelectedFn.Invoke(value, item.Selected);

            WriteOption(item, selected, output);
         }

         if (optGroup != null) {
            output.WriteEndElement(); // </optgroup>
         }
      }
   }

   internal static void
   WriteOption(SelectListItem item, bool? selected, XcstWriter output) {

      output.WriteStartElement("option");

      if (item.Value != null) {
         output.WriteAttributeString("value", item.Value);
      }

      HtmlAttributeHelper.WriteBoolean("selected", selected ?? item.Selected, output);
      HtmlAttributeHelper.WriteBoolean("disabled", item.Disabled, output);

      output.WriteString(item.Text);
      output.WriteEndElement();
   }
}

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
