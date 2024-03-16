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

namespace Xcst.Web.Mvc;

[GeneratedCodeReference]
public class HtmlAttributeDictionary : Dictionary<string, object?> {

   public HtmlAttributeDictionary
   SetClass(string? cssClass) {

      if (!String.IsNullOrEmpty(cssClass)) {
         this["class"] = cssClass;
      }

      return this;
   }

   public HtmlAttributeDictionary
   SetAttribute(string key, object? value) {

      if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

      this[key] = value;

      return this;
   }

   public HtmlAttributeDictionary
   SetBoolean(string key, bool value) {

      if (value) {
         SetAttribute(key, key);
      }

      return this;
   }

   public HtmlAttributeDictionary
   SetAttributes(object? attributes) {

      if (attributes != null) {
         SetAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(attributes));
      }

      return this;
   }

   public HtmlAttributeDictionary
   SetAttributes(IDictionary<string, object?>? attributes) {

      // NOTE: For backcompat, the dictionary class must be a non-null string to be joined
      // with the attribute (or library) class, otherwise it's ignored. If there's no attribute class,
      // the dictionary class can be null, resulting in an empty attribute.
      // 
      // See also HtmlHelper.WriteCssClass
      // 
      // Previous logic:
      // if (!String.IsNullOrEmpty(cssClass)) {
      //    if (DictionaryExtensions.TryGetValue(this, "class", out string existingClass)) {
      //       this["class"] = existingClass + " " + cssClass;
      //    } else {
      //       this["class"] = cssClass;
      //    }
      // }

      if (attributes != null) {

         foreach (var entry in attributes) {

            if (entry.Key == "class"
               && TryGetValue("class", out var thisClassObj)) {

               if (entry.Value is string s) {
                  this["class"] = (string?)thisClassObj + " " + s;
               }

            } else {
               SetAttribute(entry.Key, entry.Value);
            }
         }
      }

      return this;
   }

   public void
   WriteTo(XcstWriter output) {

      foreach (var item in this) {
         WriteAttribute(item.Key, item.Value, output);
      }
   }

   internal void
   WriteTo(XcstWriter output, bool excludeClass) {

      foreach (var item in this) {

         if (excludeClass
            && item.Key == "class") {

            continue;
         }

         WriteAttribute(item.Key, item.Value, output);
      }
   }

   static void
   WriteAttribute(string key, object? value, XcstWriter output) =>
      output.WriteAttributeString(key, output.SimpleContent.Convert(value));

   internal string?
   GetClassOrNull() {

      if (TryGetValue("class", out var value)) {
         return value?.ToString();
      }

      return null;
   }
}
