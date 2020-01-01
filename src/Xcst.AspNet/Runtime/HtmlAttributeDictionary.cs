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
using System.Web.Mvc;

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public class HtmlAttributeDictionary : Dictionary<string, object> {

      static readonly IDictionary<string, object> EmptyDictionary = new Dictionary<string, object>();

      public HtmlAttributeDictionary() { }

      public HtmlAttributeDictionary(object/*?*/ htmlAttributes)
         : base(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)) { }

      public HtmlAttributeDictionary(IDictionary<string, object>/*?*/ htmlAttributes)
         : base(htmlAttributes ?? EmptyDictionary) { }

      public HtmlAttributeDictionary AddCssClass(string/*?*/ cssClass) {

         if (!String.IsNullOrEmpty(cssClass)) {

            if (DictionaryExtensions.TryGetValue(this, "class", out string existingClass)) {
               this["class"] = existingClass + " " + cssClass;
            } else {
               this["class"] = cssClass;
            }
         }

         return this;
      }

      public HtmlAttributeDictionary MergeAttribute(string key, object/*?*/ value, bool replaceExisting = false) {

         if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

         if (replaceExisting || !ContainsKey(key)) {
            this[key] = value;
         }

         return this;
      }

      public HtmlAttributeDictionary MergeAttributes<TValue>(IDictionary<string, TValue>/*?*/ attributes, bool replaceExisting) {

         if (attributes != null) {
            foreach (var entry in attributes) {
               MergeAttribute(entry.Key, entry.Value, replaceExisting);
            }
         }

         return this;
      }

      public HtmlAttributeDictionary MergeBoolean(string key, bool value, bool replaceExisting = false) {

         if (value) {
            MergeAttribute(key, key, replaceExisting);
         }

         return this;
      }

      internal HtmlAttributeDictionary GenerateId(string name) {

         if (!ContainsKey("id")) {

            string sanitizedId = TagBuilder.CreateSanitizedId(name);

            if (!String.IsNullOrEmpty(sanitizedId)) {
               this["id"] = sanitizedId;
            }
         }

         return this;
      }

      public void WriteTo(XcstWriter output) {

         foreach (var item in this) {
            output.WriteAttributeString(item.Key, output.SimpleContent.Convert(item.Value));
         }
      }
   }
}
