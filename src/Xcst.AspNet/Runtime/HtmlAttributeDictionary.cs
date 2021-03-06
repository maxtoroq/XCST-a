﻿// Copyright 2015 Max Toro Q.
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
using System.Web.Mvc;

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public class HtmlAttributeDictionary : Dictionary<string, object> {

      public HtmlAttributeDictionary SetClass(string? cssClass) {

         if (!String.IsNullOrEmpty(cssClass)) {
            this["class"] = cssClass!;
         }

         return this;
      }

      public HtmlAttributeDictionary SetAttribute(string key, object value) {

         if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

         this[key] = value;

         return this;
      }

      public HtmlAttributeDictionary SetBoolean(string key, bool value) {

         if (value) {
            SetAttribute(key, key);
         }

         return this;
      }

      public HtmlAttributeDictionary SetAttributes(object? attributes) {

         if (attributes != null) {

            var dict = attributes as IDictionary<string, object>
#pragma warning disable CS8619
               ?? HtmlHelper.AnonymousObjectToHtmlAttributes(attributes);
#pragma warning restore CS8619

            SetAttributes(dict);
         }

         return this;
      }

      public HtmlAttributeDictionary SetAttributes(IDictionary<string, object>? attributes) {

         // NOTE: For backcompat, the dictionary class must be a non-null string to be joined 
         // with the attribute (or library) class, otherwise it's ignored. If there's no attribute class,
         // the dictionary class can be null, resulting in an empty attribute.
         // 
         // See also HtmlAttributeHelper.WriteClass
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
                  && ContainsKey("class")) {

                  if (entry.Value is string s) {
                     this["class"] = (string)this["class"] + " " + s;
                  }

               } else {
                  SetAttribute(entry.Key, entry.Value);
               }
            }
         }

         return this;
      }

      public void WriteTo(XcstWriter output) {

         foreach (var item in this) {
            HtmlAttributeHelper.WriteAttribute(item.Key, item.Value, output);
         }
      }
   }
}
