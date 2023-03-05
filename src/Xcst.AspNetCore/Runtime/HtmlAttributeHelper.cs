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
using Xcst.Web.Mvc;

namespace Xcst.Web.Runtime;

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

   internal static void
   WriteCssClass(string? userClass, string? libClass, XcstWriter output) {

      // NOTE: For backcompat, userClass must be a non-null string to be joined
      // with libClass, otherwise it's ignored. If there's no libClass,
      // userClass can be null, resulting in an empty attribute.
      // 
      // libClass must not be null or empty, which allows you to call this method
      // without having to make that check.
      // 
      // See also HtmlAttributeDictionary.SetAttributes

      var libClassHasValue = !String.IsNullOrEmpty(libClass);
      var userClassHasValue = userClass != null;

      if (libClassHasValue
         || userClassHasValue) {

         var joinedClass =
            (libClassHasValue && userClassHasValue) ? userClass + " " + libClass
            : (libClassHasValue) ? libClass
            : userClass;

         output.WriteAttributeString("class", joinedClass);
      }
   }

   internal static void
   WriteAttributes(IDictionary<string, object>? htmlAttributes, XcstWriter output) {

      if (htmlAttributes != null) {

         foreach (var item in htmlAttributes) {
            WriteAttribute(item.Key, item.Value, output);
         }
      }
   }

   public static void
   WriteAttribute(string key, object? value, XcstWriter output) =>
      output.WriteAttributeString(key, output.SimpleContent.Convert(value));
}
