// Copyright 2016 Max Toro Q.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Xcst.Web.Mvc;

[GeneratedCodeReference]
[EditorBrowsable(EditorBrowsableState.Never)]
public class OptionList : IEnumerable<SelectListItem> {

   readonly List<SelectListItem>
   _staticList;

   readonly HashSet<string>
   _selectedValues = new(StringComparer.OrdinalIgnoreCase);

   bool
   _useSelectedValues;

   List<SelectListItem>?
   _dynamicList;

   public bool
   AddBlankOption =>
      _staticList.Count == 0
         && _dynamicList != null;

   public static OptionList
   FromStaticList(int staticOptionsCount) {

      Debug.Assert(staticOptionsCount > 0);

      return new OptionList(staticOptionsCount);
   }

   public static OptionList
   Create() => new OptionList(0);

   private
   OptionList(int staticOptionsCount) {
      _staticList = new List<SelectListItem>(staticOptionsCount);
   }

   public OptionList
   WithSelectedValue(object? selectedValue, bool multiple = false) {

      if (selectedValue != null) {

         if (multiple) {

            _selectedValues.UnionWith(
               ((IEnumerable)selectedValue).Cast<object>()
                  .Select(ValueString));

         } else {
            _selectedValues.Add(ValueString(selectedValue));
         }
      }

      _useSelectedValues = true;

      return this;
   }

   static string
   ValueString(object? value) =>
      Convert.ToString(value, CultureInfo.CurrentCulture);

   bool
   IsSelected(SelectListItem item) {

      if (_useSelectedValues) {
         return _selectedValues.Contains(item.Value ?? item.Text ?? String.Empty);
      }

      return item.Selected;
   }

   public OptionList
   AddStaticOption(object? value = null, string? text = null, bool selected = false, bool disabled = false) {

      var item = new SelectListItem {
         Text = text,
         Selected = selected,
         Disabled = disabled
      };

      if (value != null) {
         item.Value = ValueString(value);
      }

      item.Selected = IsSelected(item);

      _staticList.Add(item);

      return this;
   }

   public OptionList
   ConcatDynamicList(IEnumerable<SelectListItem>? list) {

      if (list != null) {

         _dynamicList = new List<SelectListItem>();

         foreach (var item in list) {

            AddDynamicOption(new SelectListItem {
               Disabled = item.Disabled,
               Group = item.Group,
               Selected = item.Selected,
               Text = item.Text,
               Value = item.Value
            });
         }
      }

      return this;
   }

   public OptionList
   ConcatDynamicList<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? list) {

      if (list != null) {

         _dynamicList = new List<SelectListItem>();

         foreach (var pair in list) {

            AddDynamicOption(new SelectListItem {
               Text = ValueString(pair.Value),
               Value = ValueString(pair.Key)
            });
         }
      }

      return this;
   }

   public OptionList
   ConcatDynamicList<TGroupKey, TKey, TValue>(IEnumerable<IGrouping<TGroupKey, KeyValuePair<TKey, TValue>>>? list) {

      if (list != null) {

         _dynamicList = new List<SelectListItem>();

         foreach (var group in list) {

            var g = new SelectListGroup {
               Name = ValueString(group.Key)
            };

            foreach (var pair in group) {

               AddDynamicOption(new SelectListItem {
                  Group = g,
                  Text = ValueString(pair.Value),
                  Value = ValueString(pair.Key)
               });
            }
         }
      }

      return this;
   }

   public OptionList
   ConcatDynamicList<TKey, TElement>(IEnumerable<IGrouping<TKey, TElement>>? list) {

      if (list != null) {

         _dynamicList = new List<SelectListItem>();

         foreach (var group in list) {

            var g = new SelectListGroup {
               Name = ValueString(group.Key)
            };

            foreach (var item in group) {

               AddDynamicOption(new SelectListItem {
                  Group = g,
                  Text = ValueString(item)
               });
            }
         }
      }

      return this;
   }

   public OptionList
   ConcatDynamicList(IEnumerable? list) {

      if (list != null) {

         if (list is IEnumerable<SelectListItem> sList) {
            return ConcatDynamicList(sList);
         }

         _dynamicList = new List<SelectListItem>();

         foreach (var item in list) {

            AddDynamicOption(new SelectListItem {
               Text = ValueString(item),
            });
         }
      }

      return this;
   }

   void
   AddDynamicOption(SelectListItem item) {

      item.Selected = IsSelected(item);

      _dynamicList!.Add(item);
   }

   public IEnumerator<SelectListItem>
   GetEnumerator() {

      if (_dynamicList is null) {
         return _staticList.GetEnumerator();
      }

      return _staticList
         .Concat(_dynamicList)
         .GetEnumerator();
   }

   IEnumerator
   IEnumerable.GetEnumerator() =>
      GetEnumerator();
}
