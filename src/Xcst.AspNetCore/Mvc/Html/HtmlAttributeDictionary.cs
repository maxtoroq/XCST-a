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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

[GeneratedCodeReference]
[EditorBrowsable(EditorBrowsableState.Never)]
public class HtmlAttributeDictionary : Dictionary<string, object?> {

   // StringComparer consistent with HtmlHelper.AnonymousObjectToHtmlAttributes()

   [GeneratedCodeReference]
   public
   HtmlAttributeDictionary()
      : base(StringComparer.OrdinalIgnoreCase) { }

   public
   HtmlAttributeDictionary(IDictionary<string, object?> dictionary)
      : base(dictionary, StringComparer.OrdinalIgnoreCase) { }

   [GeneratedCodeReference]
   public HtmlAttributeDictionary
   AddClass(object? cssClass) {

      if (!(cssClass is null or string and { Length: 0 })) {

         if (TryGetValue("class", out var existingObj)
            && existingObj != null) {

            this["class"] = $"{existingObj} {cssClass}";

         } else {
            this["class"] = cssClass;
         }
      }

      return this;
   }

   [GeneratedCodeReference]
   public HtmlAttributeDictionary
   SetAttribute(string key, object? value) {

      ArgumentException.ThrowIfNullOrEmpty(key);

      this[key] = value;

      return this;
   }

   [GeneratedCodeReference]
   public HtmlAttributeDictionary
   SetBoolean(string key, bool value) {

      if (value) {
         SetAttribute(key, key);
      }

      return this;
   }

   [GeneratedCodeReference]
   public HtmlAttributeDictionary
   SetAttributes(object? attributes) {

      if (attributes != null) {

         SetAttributes((attributes is IDictionary<string, object?> dictionary) ?
            dictionary
            : HtmlHelper.AnonymousObjectToHtmlAttributes(attributes)
         );
      }

      return this;
   }

   [GeneratedCodeReference]
   public HtmlAttributeDictionary
   SetAttributes(IDictionary<string, object?>? attributes) {

      if (attributes != null) {

         foreach (var entry in attributes) {

            if (this.Comparer.Equals(entry.Key, "class")) {
               AddClass(entry.Value);
            } else {
               this[entry.Key] = entry.Value;
            }
         }
      }

      return this;
   }

   internal void
   WriteTo(XcstWriter output) {

      foreach (var item in this) {
         output.WriteAttributeString(item.Key, SerializeValue(item.Value, output.SimpleContent));
      }
   }

   static string
   SerializeValue(object? value, SimpleContent simpleContent) =>
      simpleContent.Convert(value);

   internal string?
   RemoveClass(SimpleContent simpleContent) {

      if (Remove("class", out var value)) {
         return SerializeValue(value, simpleContent);
      }

      return null;
   }
}
