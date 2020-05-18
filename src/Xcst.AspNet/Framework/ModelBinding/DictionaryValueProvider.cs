// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;

namespace System.Web.Mvc {

   public class DictionaryValueProvider<TValue> : IValueProvider, IEnumerableValueProvider {

      PrefixContainer _prefixContainer;
      readonly Dictionary<string, ValueProviderResult> _values = new Dictionary<string, ValueProviderResult>(StringComparer.OrdinalIgnoreCase);

      private PrefixContainer PrefixContainer =>
         _prefixContainer
            // Race condition on initialization has no side effects
            ?? (_prefixContainer = new PrefixContainer(_values.Keys));

      public DictionaryValueProvider(IDictionary<string, TValue> dictionary, CultureInfo culture) {

         if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

         foreach (KeyValuePair<string, TValue> entry in dictionary) {

            object rawValue = entry.Value;
            string attemptedValue = Convert.ToString(rawValue, culture);
            _values[entry.Key] = new ValueProviderResult(rawValue, attemptedValue, culture);
         }
      }

      public virtual bool ContainsPrefix(string prefix) =>
         PrefixContainer.ContainsPrefix(prefix);

      public virtual ValueProviderResult GetValue(string key) {

         if (key is null) throw new ArgumentNullException(nameof(key));

         ValueProviderResult valueProviderResult;
         _values.TryGetValue(key, out valueProviderResult);

         return valueProviderResult;
      }

      public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix) =>
         PrefixContainer.GetKeysFromPrefix(prefix);
   }
}
