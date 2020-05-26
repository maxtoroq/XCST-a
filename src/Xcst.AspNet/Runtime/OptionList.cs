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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public class OptionList : IEnumerable<SelectListItem> {

      readonly List<SelectListItem> staticList;
      readonly HashSet<string> selectedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      bool useSelectedValues;
      List<SelectListItem>? dynamicList;

      public bool AddBlankOption =>
         staticList.Count == 0
            && dynamicList != null;

      public static OptionList FromStaticList(int staticOptionsCount) {

         Debug.Assert(staticOptionsCount > 0);

         return new OptionList(staticOptionsCount);
      }

      public static OptionList Create() =>
         new OptionList(0);

      private OptionList(int staticOptionsCount) {
         this.staticList = new List<SelectListItem>(staticOptionsCount);
      }

      public OptionList WithSelectedValue(object selectedValue, bool multiple = false) {

         if (selectedValue != null) {

            if (multiple) {

               this.selectedValues.UnionWith(
                  ((IEnumerable)selectedValue).Cast<object>()
                     .Select(ValueString));

            } else {
               this.selectedValues.Add(ValueString(selectedValue));
            }
         }

         this.useSelectedValues = true;

         return this;
      }

      static string ValueString(object? value) =>
         Convert.ToString(value, CultureInfo.CurrentCulture);

      bool IsSelected(SelectListItem item) {

         if (this.useSelectedValues) {
            return this.selectedValues.Contains(item.Value ?? item.Text ?? String.Empty);
         }

         return item.Selected;
      }

      public OptionList AddStaticOption(object? value = null, string? text = null, bool selected = false, bool disabled = false) {

         var item = new SelectListItem {
            Text = text,
            Selected = selected,
            Disabled = disabled
         };

         if (value != null) {
            item.Value = ValueString(value);
         }

         item.Selected = IsSelected(item);

         this.staticList.Add(item);

         return this;
      }

      public OptionList ConcatDynamicList(IEnumerable<SelectListItem> list) {

         if (list != null) {

            this.dynamicList = new List<SelectListItem>();

            foreach (SelectListItem item in list) {

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

      public OptionList ConcatDynamicList<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> list) {

         if (list != null) {

            this.dynamicList = new List<SelectListItem>();

            foreach (var pair in list) {

               AddDynamicOption(new SelectListItem {
                  Text = ValueString(pair.Value),
                  Value = ValueString(pair.Key)
               });
            }
         }

         return this;
      }

      public OptionList ConcatDynamicList<TGroupKey, TKey, TValue>(IEnumerable<IGrouping<TGroupKey, KeyValuePair<TKey, TValue>>> list) {

         if (list != null) {

            this.dynamicList = new List<SelectListItem>();

            foreach (var group in list) {

               var g = new SelectListGroup {
                  Name = ValueString(group.Key)
               };

               foreach (KeyValuePair<TKey, TValue> pair in group) {

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

      public OptionList ConcatDynamicList<TKey, TElement>(IEnumerable<IGrouping<TKey, TElement>> list) {

         if (list != null) {

            this.dynamicList = new List<SelectListItem>();

            foreach (IGrouping<TKey, TElement> group in list) {

               var g = new SelectListGroup {
                  Name = ValueString(group.Key)
               };

               foreach (TElement item in group) {

                  AddDynamicOption(new SelectListItem {
                     Group = g,
                     Text = ValueString(item)
                  });
               }
            }
         }

         return this;
      }

      public OptionList ConcatDynamicList(IEnumerable list) {

         if (list != null) {

            this.dynamicList = new List<SelectListItem>();

            foreach (object item in list) {

               AddDynamicOption(new SelectListItem {
                  Text = ValueString(item),
               });
            }
         }

         return this;
      }

      void AddDynamicOption(SelectListItem item) {

         item.Selected = IsSelected(item);

         this.dynamicList!.Add(item);
      }

      public IEnumerator<SelectListItem> GetEnumerator() {

         if (this.dynamicList is null) {
            return this.staticList.GetEnumerator();
         }

         return this.staticList
            .Concat(this.dynamicList)
            .GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() =>
         GetEnumerator();
   }
}
