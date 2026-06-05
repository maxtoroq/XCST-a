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

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xcst.Web.Mvc;

[GeneratedCodeReference]
[EditorBrowsable(EditorBrowsableState.Never)]
public class OptionList : IEnumerable<SelectListItem> {

   readonly List<SelectListItem>
   _staticList;

   readonly HtmlHelper
   _htmlHelper;

   List<SelectListItem>?
   _dynamicList;

   public bool
   AddBlankOption =>
      _staticList.Count == 0
         && _dynamicList != null;

   internal
   OptionList(int staticOptionsCount, HtmlHelper htmlHelper) {
      _staticList = new List<SelectListItem>(staticOptionsCount);
      _htmlHelper = htmlHelper;
   }

   [GeneratedCodeReference]
   public OptionList
   AddStaticOption(object? value = null, string? text = null, bool selected = false, bool disabled = false) {

      var item = new SelectListItem {
         Text = text,
         Selected = selected,
         Disabled = disabled
      };

      if (value != null) {
         item.Value = _htmlHelper.SelectValueString(value);
      }

      _staticList.Add(item);

      return this;
   }

   [GeneratedCodeReference]
   public OptionList
   ConcatDynamicList(IEnumerable<SelectListItem>? list) {

      if (list != null) {

         _dynamicList = new List<SelectListItem>();

         foreach (var item in list) {

            _dynamicList.Add(new SelectListItem {
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
