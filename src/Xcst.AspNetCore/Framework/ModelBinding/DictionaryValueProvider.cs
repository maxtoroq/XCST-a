// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;

namespace System.Web.Mvc;

public class DictionaryValueProvider<TValue> : IValueProvider, IEnumerableValueProvider {

   PrefixContainer?
   _prefixContainer;

   readonly Dictionary<string, ValueProviderResult>
   _values = new(StringComparer.OrdinalIgnoreCase);

   private PrefixContainer
   PrefixContainer =>
      // Race condition on initialization has no side effects
      _prefixContainer ??= new PrefixContainer(_values.Keys);

   public
   DictionaryValueProvider(IDictionary<string, TValue> dictionary, CultureInfo culture) {

      if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

      foreach (KeyValuePair<string, TValue> entry in dictionary) {

         var rawValue = (object?)entry.Value;
         var attemptedValue = Convert.ToString(rawValue, culture);
         _values[entry.Key] = new ValueProviderResult(rawValue, attemptedValue, culture);
      }
   }

   public virtual bool
   ContainsPrefix(string prefix) =>
      PrefixContainer.ContainsPrefix(prefix);

   public virtual ValueProviderResult?
   GetValue(string key) {

      if (key is null) throw new ArgumentNullException(nameof(key));

      _values.TryGetValue(key, out var valueProviderResult);

      return valueProviderResult;
   }

   public virtual IDictionary<string, string>
   GetKeysFromPrefix(string prefix) =>
      PrefixContainer.GetKeysFromPrefix(prefix);
}
