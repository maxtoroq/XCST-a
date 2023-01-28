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
using System.Web.Mvc;

namespace Xcst.Web.Runtime;

using HtmlAttribs = IDictionary<string, object>;

/// <exclude/>
public static class SelectInstructions {

   // Select

   public static void
   Select(HtmlHelper htmlHelper, XcstWriter output, string name, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, HtmlAttribs? htmlAttributes = null) =>
      SelectHelper(htmlHelper, output, default(ModelExplorer), name, selectList, default(string), multiple, htmlAttributes);

   [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Users cannot use anonymous methods with the LambdaExpression type")]
   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static void
   SelectFor<TModel, TProperty>(HtmlHelper<TModel> htmlHelper, XcstWriter output, Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, HtmlAttribs? htmlAttributes = null) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, htmlHelper.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      SelectHelper(htmlHelper, output, modelExplorer, expressionString, selectList, default(string), multiple, htmlAttributes);
   }

   public static void
   SelectForModel(HtmlHelper htmlHelper, XcstWriter output, IEnumerable<SelectListItem>? selectList = null,
         bool multiple = false, HtmlAttribs? htmlAttributes = null) =>

      SelectHelper(htmlHelper, output, htmlHelper.ViewData.ModelExplorer, String.Empty, selectList, default(string), multiple, htmlAttributes);

   internal static void
   SelectHelper(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer? modelExplorer, string expression, IEnumerable<SelectListItem>? selectList,
         string? optionLabel, bool multiple, HtmlAttribs? htmlAttributes) {

      if (!multiple
         && optionLabel is null
         && selectList != null) {

         var optionList = selectList as OptionList;

         if (optionList?.AddBlankOption == true) {
            optionLabel = String.Empty;
         }
      }

      SelectInternal(htmlHelper, output, modelExplorer, optionLabel, expression, selectList, multiple, htmlAttributes);
   }

   // Helper methods

   static IEnumerable<SelectListItem>
   GetSelectData(HtmlHelper htmlHelper, string name) {

      var o = htmlHelper.ViewData?.Eval(name)
         ?? throw new InvalidOperationException($"There is no ViewData item of type 'IEnumerable<SelectListItem>' that has the key '{name}'.");

      var selectList = o as IEnumerable<SelectListItem>
         ?? throw new InvalidOperationException($"The ViewData item that has the key '{name}' is of type '{o.GetType().FullName}' but must be of type 'IEnumerable<SelectListItem>'.");

      return selectList;
   }

   static IEnumerable<SelectListItem>
   GetSelectListWithDefaultValue(IEnumerable<SelectListItem> selectList, object defaultValue, bool allowMultiple) {

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
                   select Convert.ToString(value, CultureInfo.CurrentCulture);

      // ToString() by default returns an enum value's name.  But selectList may use numeric values.

      var enumValues = from Enum value in defaultValues.OfType<Enum>()
                       select value.ToString("d");

      values = values.Concat(enumValues);

      var selectedValues = new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
      var newSelectList = new List<SelectListItem>();

      foreach (var item in selectList) {

         item.Selected = (item.Value != null) ?
            selectedValues.Contains(item.Value)
            : selectedValues.Contains(item.Text);

         newSelectList.Add(item);
      }

      return newSelectList;
   }

   static void
   SelectInternal(HtmlHelper htmlHelper, XcstWriter output, ModelExplorer? modelExplorer, string? optionLabel, string name, IEnumerable<SelectListItem>? selectList,
         bool allowMultiple, HtmlAttribs? htmlAttributes) {

      var viewData = htmlHelper.ViewData;
      var fullName = viewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentNullException(nameof(name));
      }

      var usedViewData = false;

      // If we got a null selectList, try to use ViewData to get the list of items.

      if (selectList is null) {
         selectList = GetSelectData(htmlHelper, name);
         usedViewData = true;
      }

      var defaultValue = (allowMultiple) ?
         htmlHelper.GetModelStateValue(fullName, typeof(string[]))
         : htmlHelper.GetModelStateValue(fullName, typeof(string));

      // If we haven't already used ViewData to get the entire list of items then we need to
      // use the ViewData-supplied value before using the parameter-supplied value.

      if (defaultValue is null) {

         if (modelExplorer is null) {

            if (!usedViewData
               && !String.IsNullOrEmpty(name)) {

               defaultValue = viewData.Eval(name);
            }

         } else {
            defaultValue = modelExplorer.Model;
         }
      }

      if (defaultValue != null) {
         selectList = GetSelectListWithDefaultValue(selectList, defaultValue, allowMultiple);
      }

      output.WriteStartElement("select");
      HtmlAttributeHelper.WriteId(fullName, output);
      output.WriteAttributeString("name", fullName);
      HtmlAttributeHelper.WriteBoolean("multiple", allowMultiple, output);

      // If there are any errors for a named field, we add the css attribute.

      var cssClass = (viewData.ModelState.TryGetValue(fullName, out var modelState)
         && modelState.Errors.Count > 0) ? HtmlHelper.ValidationInputCssClassName : null;

      var validationAttribs = htmlHelper
         .GetUnobtrusiveValidationAttributes(name, modelExplorer?.Metadata, excludeMinMaxLength: !allowMultiple);

      HtmlAttributeHelper.WriteClass(cssClass, htmlAttributes, output);
      HtmlAttributeHelper.WriteAttributes(validationAttribs, output);

      // name cannnot be overridden, and class was already written

      HtmlAttributeHelper.WriteAttributes(htmlAttributes, output, excludeFn: n => n == "name" || n == "class");

      WriteOptions(optionLabel, selectList, output);

      output.WriteEndElement(); // </select>
   }

   static void
   WriteOptions(string? optionLabel, IEnumerable<SelectListItem> selectList, XcstWriter output) {

      // Make optionLabel the first item that gets rendered.

      if (optionLabel != null) {
         WriteOption(new SelectListItem {
            Text = optionLabel,
            Value = String.Empty,
            Selected = false
         }, output);
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
            WriteOption(item, output);
         }

         if (optGroup != null) {
            output.WriteEndElement(); // </optgroup>
         }
      }
   }

   internal static void
   WriteOption(SelectListItem item, XcstWriter output) {

      output.WriteStartElement("option");

      if (item.Value != null) {
         output.WriteAttributeString("value", item.Value);
      }

      HtmlAttributeHelper.WriteBoolean("selected", item.Selected, output);
      HtmlAttributeHelper.WriteBoolean("disabled", item.Disabled, output);

      output.WriteString(item.Text);
      output.WriteEndElement();
   }
}
