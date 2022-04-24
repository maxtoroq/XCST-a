// Copyright 2020 Max Toro Q.
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
   public static class HtmlAttributeHelper {

      internal static void
      WriteId(string name, XcstWriter output) {

         var sanitizedId = TagBuilder.CreateSanitizedId(name);

         if (!String.IsNullOrEmpty(sanitizedId)) {
            output.WriteAttributeString("id", sanitizedId);
         }
      }

      public static void
      WriteBoolean(string key, bool value, XcstWriter output) {

         if (value) {
            output.WriteAttributeString(key, key);
         }
      }

      public static void
      WriteClass(string? cssClass, IDictionary<string, object>? htmlAttributes, XcstWriter output) {

         // NOTE: For backcompat, the dictionary class must be a non-null string to be joined
         // with the library class, otherwise it's ignored. If there's no library class,
         // the dictionary class can be null, resulting in an empty attribute.
         // 
         // The library class must not be null or empty, which allows you to call this method
         // without having to make that check.
         // 
         // See also HtmlAttributeDictionary.SetAttributes

         var cssClassHasValue = !String.IsNullOrEmpty(cssClass);
         object? dictClass = null;

         var dictHasClass = htmlAttributes != null
            && htmlAttributes.TryGetValue("class", out dictClass);

         if (cssClassHasValue
            || dictHasClass) {

            var joinedClass = (cssClassHasValue && dictClass is string s) ?
               s + " " + cssClass
               : (cssClassHasValue) ? cssClass!
               : output.SimpleContent.Convert(dictClass);

            output.WriteAttributeString("class", joinedClass);
         }
      }

      public static void
      WriteAttributes(IDictionary<string, object>? htmlAttributes, XcstWriter output, Func<string, bool>? excludeFn = null) {

         if (htmlAttributes != null) {

            foreach (var item in htmlAttributes) {

               if (excludeFn is null
                  || !excludeFn.Invoke(item.Key)) {

                  WriteAttribute(item.Key, item.Value, output);
               }
            }
         }
      }

      public static void
      WriteAttribute(string key, object? value, XcstWriter output) =>
         output.WriteAttributeString(key, output.SimpleContent.Convert(value));
   }
}
