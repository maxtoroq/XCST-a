// Copyright 2019 Max Toro Q.
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

#region DictionaryExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace Xcst.Web {

   /// <summary>
   /// Extension methods for <see cref="IDictionary{TKey,TValue}"/>.
   /// </summary>
   [EditorBrowsable(EditorBrowsableState.Never)]
   static class DictionaryExtensions {

      /// <summary>
      /// Gets the value of <typeparamref name="T"/> associated with the specified key or <c>default</c> value if
      /// either the key is not present or the value is not of type <typeparamref name="T"/>. 
      /// </summary>
      /// <typeparam name="T">The type of the value associated with the specified key.</typeparam>
      /// <param name="collection">The <see cref="IDictionary{TKey,TValue}"/> instance where <c>TValue</c> is <c>object</c>.</param>
      /// <param name="key">The key whose value to get.</param>
      /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
      /// <returns><c>true</c> if key was found, value is non-null, and value is of type <typeparamref name="T"/>; otherwise false.</returns>
      public static bool TryGetValue<T>(this IDictionary<string, object> collection, string key, out T value) {

         Contract.Assert(collection != null);

         if (collection.TryGetValue(key, out object valueObj)) {
            if (valueObj is T valueT) {
               value = valueT;
               return true;
            }
         }

         value = default(T);
         return false;
      }

      public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default) {

         if (dict.TryGetValue(key, out TValue value)) {
            return value;
         }

         return @default;
      }
   }
}
