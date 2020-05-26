// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Web.UI;

namespace System.Web.Mvc {

   [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "This is a shipped API")]
   public class SelectList : MultiSelectList {

      public object? SelectedValue { get; private set; }

      public SelectList(IEnumerable items)
         : this(items, selectedValue: null) { }

      public SelectList(IEnumerable items, object? selectedValue)
         : this(items, dataValueField: null, dataTextField: null, selectedValue: selectedValue) { }

      /// <summary>
      /// Initializes a new instance of the SelectList class by using the specified items for the list,
      /// the selected value, and the disabled values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="selectedValue">The selected value. Used to match the Selected property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      public SelectList(IEnumerable items, object? selectedValue, IEnumerable disabledValues)
         : this(items,
            dataValueField: null,
            dataTextField: null,
            selectedValue: selectedValue,
            disabledValues: disabledValues) { }

      public SelectList(IEnumerable items, string? dataValueField, string? dataTextField)
         : this(items, dataValueField, dataTextField, selectedValue: null) { }

      public SelectList(IEnumerable items, string? dataValueField, string? dataTextField, object? selectedValue)
         : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue)) {

         this.SelectedValue = selectedValue;
      }

      /// <summary>
      /// Initializes a new instance of the SelectList class by using the specified items for the list,
      /// the data value field, the data text field, the data group field, and the selected value.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValue">The selected value. Used to match the Selected property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      public SelectList(IEnumerable items, string? dataValueField, string? dataTextField, string? dataGroupField, object? selectedValue)
         : base(items, dataValueField, dataTextField, dataGroupField, ToEnumerable(selectedValue)) {

         this.SelectedValue = selectedValue;
      }

      /// <summary>
      /// Initializes a new instance of the SelectList class by using the specified items for the list,
      /// the data value field, the data text field, the selected value, and the disabled values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValue">The selected value. Used to match the Selected property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      public SelectList(IEnumerable items, string? dataValueField, string? dataTextField, object? selectedValue, IEnumerable? disabledValues)
         : base(items, dataValueField, dataTextField, ToEnumerable(selectedValue), disabledValues) {

         this.SelectedValue = selectedValue;
      }

      /// <summary>
      /// Initializes a new instance of the SelectList class by using the specified items for the list,
      /// the data value field, the data text field, the data group field, the selected value, and the disabled
      /// values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValue">The selected value. Used to match the Selected property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      public SelectList(IEnumerable items, string dataValueField, string dataTextField, string dataGroupField, object selectedValue,
            IEnumerable disabledValues)
         : base(items, dataValueField, dataTextField, dataGroupField, ToEnumerable(selectedValue), disabledValues) {

         this.SelectedValue = selectedValue;
      }

      /// <summary>
      /// Initializes a new instance of the SelectList class by using the specified items for the list,
      /// the data value field, the data text field, the data group field. the selected value, the disabled values,
      /// and the disabled groups.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValue">The selected value. Used to match the Selected property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledGroups">The disabled groups. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListGroup"/>.</param>
      public SelectList(IEnumerable items, string dataValueField, string dataTextField, string dataGroupField, object selectedValue,
            IEnumerable disabledValues, IEnumerable disabledGroups)
         : base(items, dataValueField, dataTextField, dataGroupField, ToEnumerable(selectedValue), disabledValues, disabledGroups) {

         this.SelectedValue = selectedValue;
      }

      static IEnumerable? ToEnumerable(object? selectedValue) =>
         (selectedValue != null) ? new object[] { selectedValue } : null;
   }

   [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Multi", Justification = "FxCop won't accept this in the custom dictionary, so we're suppressing it in source")]
   [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "This is a shipped API")]
   public class MultiSelectList : IEnumerable<SelectListItem> {

      IList<SelectListGroup>? _groups;

      /// <summary>
      /// Gets the data group field.
      /// </summary>
      public string? DataGroupField { get; private set; }

      public string? DataTextField { get; private set; }

      public string? DataValueField { get; private set; }

      /// <summary>
      /// Gets the disabled groups.
      /// </summary>
      public IEnumerable? DisabledGroups { get; private set; }

      /// <summary>
      /// Gets the disabled values.
      /// </summary>
      public IEnumerable? DisabledValues { get; private set; }

      public IEnumerable Items { get; private set; }

      public IEnumerable? SelectedValues { get; private set; }

      public MultiSelectList(IEnumerable items)
         : this(items, selectedValues: null) { }

      public MultiSelectList(IEnumerable items, IEnumerable? selectedValues)
         : this(items, dataValueField: null, dataTextField: null, selectedValues: selectedValues) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list, 
      /// the selected values, the disabled values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="selectedValues">The selected values field. Used to match the Selected property of the 
      /// corresponding <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>>
      public MultiSelectList(IEnumerable items, IEnumerable? selectedValues, IEnumerable? disabledValues)
         : this(items,
            dataValueField: null,
            dataTextField: null,
            selectedValues: selectedValues,
            disabledValues: disabledValues) { }

      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField)
         : this(items, dataValueField, dataTextField, selectedValues: null) { }

      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, IEnumerable? selectedValues)
         : this(items, dataValueField, dataTextField, dataGroupField: null, selectedValues: selectedValues) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list, 
      /// the data value field, the data text field, and the data group field.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, string? dataGroupField)
         : this(items, dataValueField, dataTextField, dataGroupField: dataGroupField, selectedValues: null) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list, 
      /// the data value field, the data text field, the selected values, and the disabled values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValues">The selected values field. Used to match the Selected property of the 
      /// corresponding <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>>
      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, IEnumerable? selectedValues, IEnumerable? disabledValues)
         : this(items,
            dataValueField,
            dataTextField,
            dataGroupField: null,
            selectedValues: selectedValues,
            disabledValues: disabledValues) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list, 
      /// the data value field, the data text field, the data group field, and the selected values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValues">The selected values field. Used to match the Selected property of the 
      /// corresponding <see cref="SelectListItem"/>.</param>
      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, string? dataGroupField, IEnumerable? selectedValues)
         : this(items, dataValueField, dataTextField, dataGroupField, selectedValues, disabledValues: null) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list,
      /// the data value field, the data text field, the data group field, the selected values, and the disabled
      /// values.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValues">The selected values field. Used to match the Selected property of the 
      /// corresponding <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, string? dataGroupField, IEnumerable? selectedValues,
            IEnumerable? disabledValues)
         : this(items,
            dataValueField,
            dataTextField,
            dataGroupField,
            selectedValues,
            disabledValues,
            disabledGroups: null) { }

      /// <summary>
      /// Initializes a new instance of the MultiSelectList class by using the items to include in the list, 
      /// the data value field, the data text field, the data group field, the selected values, the disabled values,
      /// and the disabled groups.
      /// </summary>
      /// <param name="items">The items used to build each <see cref="SelectListItem"/> of the list.</param>
      /// <param name="dataValueField">The data value field. Used to match the Value property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataTextField">The data text field. Used to match the Text property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="dataGroupField">The data group field. Used to match the Group property of the corresponding 
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="selectedValues">The selected values field. Used to match the Selected property of the 
      /// corresponding <see cref="SelectListItem"/>.</param>
      /// <param name="disabledValues">The disabled values. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListItem"/>.</param>
      /// <param name="disabledGroups">The disabled groups. Used to match the Disabled property of the corresponding
      /// <see cref="SelectListGroup"/>.</param>
      public MultiSelectList(IEnumerable items, string? dataValueField, string? dataTextField, string? dataGroupField, IEnumerable? selectedValues,
            IEnumerable? disabledValues, IEnumerable? disabledGroups) {

         if (items is null) throw new ArgumentNullException(nameof(items));

         this.Items = items;
         this.DataValueField = dataValueField;
         this.DataTextField = dataTextField;
         this.SelectedValues = selectedValues;
         this.DataGroupField = dataGroupField;
         this.DisabledValues = disabledValues;
         this.DisabledGroups = disabledGroups;

         if (this.DataGroupField != null) {
            _groups = new List<SelectListGroup>();
         }
      }

      public virtual IEnumerator<SelectListItem> GetEnumerator() =>
         GetListItems().GetEnumerator();

      internal IList<SelectListItem> GetListItems() =>
         (!String.IsNullOrEmpty(this.DataValueField)) ?
            GetListItemsWithValueField()
            : GetListItemsWithoutValueField();

      IList<SelectListItem> GetListItemsWithValueField() {

         HashSet<string> selectedValues = GetStringHashSet(this.SelectedValues);
         HashSet<string> disabledValues = GetStringHashSet(this.DisabledValues);
         HashSet<string> disabledGroups = GetStringHashSet(this.DisabledGroups);

         IEnumerable<SelectListItem> listItems = this.Items.Cast<object>().Select(item => {
            string value = Eval(item, this.DataValueField);
            return new SelectListItem {
               Group = GetGroup(item, disabledGroups),
               Value = value,
               Text = Eval(item, this.DataTextField),
               Selected = selectedValues.Contains(value),
               Disabled = disabledValues.Contains(value),
            };
         });

         return listItems.ToList();
      }

      IList<SelectListItem> GetListItemsWithoutValueField() {

         HashSet<object> selectedValues = GetObjectHashSet(this.SelectedValues);
         HashSet<object> disabledValues = GetObjectHashSet(this.DisabledValues);
         HashSet<string> disabledGroups = GetStringHashSet(this.DisabledGroups);

         IEnumerable<SelectListItem> listItems = this.Items.Cast<object>().Select(item => {
            return new SelectListItem {
               Group = GetGroup(item, disabledGroups),
               Text = Eval(item, this.DataTextField),
               Selected = selectedValues.Contains(item),
               Disabled = disabledValues.Contains(item),
            };
         });

         return listItems.ToList();
      }

      static string Eval(object container, string? expression) {

         object value = container;

         if (!String.IsNullOrEmpty(expression)) {
            value = DataBinder.Eval(container, expression);
         }

         return Convert.ToString(value, CultureInfo.CurrentCulture);
      }

      SelectListGroup? GetGroup(object container, HashSet<string> disabledGroups) {

         if (_groups is null) {
            return null;
         }

         string groupName = Eval(container, this.DataGroupField);

         if (String.IsNullOrEmpty(groupName)) {
            return null;
         }

         // We use StringComparison.CurrentCulture because the group name is used to display as the value of 
         // optgroup HTML tag's label attribute.
         SelectListGroup group = _groups.FirstOrDefault(
             g => String.Equals(g.Name, groupName, StringComparison.CurrentCulture));

         if (group is null) {

            group = new SelectListGroup {
               Name = groupName,
               Disabled = disabledGroups.Contains(groupName)
            };

            _groups.Add(group);
         }

         return group;
      }

      static HashSet<string> GetStringHashSet(IEnumerable? values) {

         var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

         if (values != null) {
            hashSet.UnionWith(
                values.Cast<object>().Select(value => Convert.ToString(value, CultureInfo.CurrentCulture)));
         }

         return hashSet;
      }

      static HashSet<object> GetObjectHashSet(IEnumerable? values) {

         var hashSet = new HashSet<object>();

         if (values != null) {
            hashSet.UnionWith(values.Cast<object>());
         }

         return hashSet;
      }

      IEnumerator IEnumerable.GetEnumerator() =>
         GetEnumerator();
   }

   public class SelectListItem {

      /// <summary>
      /// Gets or sets a value that indicates whether this <see cref="SelectListItem"/> is disabled.
      /// </summary>
      public bool Disabled { get; set; }

      /// <summary>
      /// Represents the optgroup HTML element this item is wrapped into.
      /// In a select list, multiple groups with the same name are supported.
      /// They are compared with reference equality.
      /// </summary>
      public SelectListGroup? Group { get; set; }

      public bool Selected { get; set; }

      public string? Text { get; set; }

      public string? Value { get; set; }
   }

   /// <summary>
   /// Represents the optgroup HTML element and its attributes.
   /// In a select list, multiple groups with the same name are supported.
   /// They are compared with reference equality.
   /// </summary>
   public class SelectListGroup {

      /// <summary>
      /// Gets or sets a value that indicates whether this <see cref="SelectListGroup"/> is disabled. 
      /// </summary>
      public bool Disabled { get; set; }

      /// <summary>
      /// Represents the value of the optgroup's label.
      /// </summary>
      public string? Name { get; set; }
   }
}
