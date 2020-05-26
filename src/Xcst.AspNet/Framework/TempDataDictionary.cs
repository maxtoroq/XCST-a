// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   public class TempDataDictionary : IDictionary<string, object?> {

      internal const string TempDataSerializationKey = "__tempData";

      Dictionary<string, object?> _data;
      HashSet<string> _initialKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      HashSet<string> _retainedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      public int Count => _data.Count;

      public ICollection<string> Keys => _data.Keys;

      public ICollection<object?> Values => _data.Values;

      bool ICollection<KeyValuePair<string, object?>>.IsReadOnly =>
         ((ICollection<KeyValuePair<string, object?>>)_data).IsReadOnly;

      public object? this[string key] {
         get {
            object? value;
            if (TryGetValue(key, out value)) {
               _initialKeys.Remove(key);
               return value;
            }
            return null;
         }
         set {
            _data[key] = value;
            _initialKeys.Add(key);
         }
      }

      public TempDataDictionary() {
         _data = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      }

      public void Keep() {
         _retainedKeys.Clear();
         _retainedKeys.UnionWith(_data.Keys);
      }

      public void Keep(string key) =>
         _retainedKeys.Add(key);

      public void Load(ControllerContext controllerContext, ITempDataProvider tempDataProvider) {

         IDictionary<string, object?> providerDictionary = tempDataProvider.LoadTempData(controllerContext);

         _data = (providerDictionary != null) ?
            new Dictionary<string, object?>(providerDictionary, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

         _initialKeys = new HashSet<string>(_data.Keys, StringComparer.OrdinalIgnoreCase);
         _retainedKeys.Clear();
      }

      public object? Peek(string key) {

         object? value;
         _data.TryGetValue(key, out value);

         return value;
      }

      public void Save(ControllerContext controllerContext, ITempDataProvider tempDataProvider) {

         // Frequently called so ensure delegate is stateless

         RemoveFromDictionary(_data, (KeyValuePair<string, object?> entry, TempDataDictionary tempData) => {
            string key = entry.Key;
            return !tempData._initialKeys.Contains(key)
                && !tempData._retainedKeys.Contains(key);
         }, this);

         tempDataProvider.SaveTempData(controllerContext, _data);
      }

      static void RemoveFromDictionary<TKey, TValue, TState>(IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, TState, bool> removeCondition, TState state) {

         // Because it is not possible to delete while enumerating, a copy of the keys must be taken. Use the size of the dictionary as an upper bound
         // to avoid creating more than one copy of the keys.

         int removeCount = 0;
         TKey[] keys = new TKey[dictionary.Count];

         foreach (var entry in dictionary) {
            if (removeCondition(entry, state)) {
               keys[removeCount] = entry.Key;
               removeCount++;
            }
         }

         for (int i = 0; i < removeCount; i++) {
            dictionary.Remove(keys[i]);
         }
      }

      public void Add(string key, object? value) {

         _data.Add(key, value);
         _initialKeys.Add(key);
      }

      public void Clear() {

         _data.Clear();
         _retainedKeys.Clear();
         _initialKeys.Clear();
      }

      public bool ContainsKey(string key) =>
         _data.ContainsKey(key);

      public bool ContainsValue(object? value) =>
         _data.ContainsValue(value);

      public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
         new TempDataDictionaryEnumerator(this);

      public bool Remove(string key) {

         _retainedKeys.Remove(key);
         _initialKeys.Remove(key);

         return _data.Remove(key);
      }

      public bool TryGetValue(string key, out object? value) {

         _initialKeys.Remove(key);

         return _data.TryGetValue(key, out value);
      }

      void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int index) =>
         ((ICollection<KeyValuePair<string, object?>>)_data).CopyTo(array, index);

      void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> keyValuePair) {
         _initialKeys.Add(keyValuePair.Key);
         ((ICollection<KeyValuePair<string, object?>>)_data).Add(keyValuePair);
      }

      bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> keyValuePair) =>
         ((ICollection<KeyValuePair<string, object?>>)_data).Contains(keyValuePair);

      bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> keyValuePair) {
         _initialKeys.Remove(keyValuePair.Key);
         return ((ICollection<KeyValuePair<string, object?>>)_data).Remove(keyValuePair);
      }

      IEnumerator IEnumerable.GetEnumerator() =>
         new TempDataDictionaryEnumerator(this);

      private sealed class TempDataDictionaryEnumerator : IEnumerator<KeyValuePair<string, object?>> {

         IEnumerator<KeyValuePair<string, object?>> _enumerator;
         TempDataDictionary _tempData;

         public KeyValuePair<string, object?> Current {
            get {
               KeyValuePair<string, object?> kvp = _enumerator.Current;
               _tempData._initialKeys.Remove(kvp.Key);
               return kvp;
            }
         }

         object IEnumerator.Current => Current;

         public TempDataDictionaryEnumerator(TempDataDictionary tempData) {
            _tempData = tempData;
            _enumerator = _tempData._data.GetEnumerator();
         }

         public bool MoveNext() =>
            _enumerator.MoveNext();

         public void Reset() =>
            _enumerator.Reset();

         void IDisposable.Dispose() =>
            _enumerator.Dispose();
      }
   }

   public interface ITempDataProvider {

      IDictionary<string, object?> LoadTempData(ControllerContext controllerContext);
      void SaveTempData(ControllerContext controllerContext, IDictionary<string, object?> values);
   }

   /// <summary>
   /// Used to create an <see cref="ITempDataProvider"/> instance for the controller.
   /// </summary>
   public interface ITempDataProviderFactory {

      /// <summary>
      /// Creates an instance of <see cref="ITempDataProvider"/> for the controller.
      /// </summary>
      /// <returns>The created <see cref="ITempDataProvider"/>.</returns>
      ITempDataProvider CreateInstance();
   }

   public class SessionStateTempDataProvider : ITempDataProvider {

      internal const string TempDataSessionStateKey = "__ControllerTempData";

      public virtual IDictionary<string, object?> LoadTempData(ControllerContext controllerContext) {

         HttpSessionStateBase session = controllerContext.HttpContext.Session;

         if (session != null) {

            if (session[TempDataSessionStateKey] is Dictionary<string, object?> tempDataDictionary) {

               // If we got it from Session, remove it so that no other request gets it

               session.Remove(TempDataSessionStateKey);
               return tempDataDictionary;
            }
         }

         return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      }

      public virtual void SaveTempData(ControllerContext controllerContext, IDictionary<string, object?> values) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         HttpSessionStateBase session = controllerContext.HttpContext.Session;
         bool isDirty = (values != null && values.Count > 0);

         if (session is null) {

            if (isDirty) {
               throw new InvalidOperationException(MvcResources.SessionStateTempDataProvider_SessionStateDisabled);
            }

         } else {

            if (isDirty) {
               session[TempDataSessionStateKey] = values;
            } else {

               // Since the default implementation of Remove() (from SessionStateItemCollection) dirties the
               // collection, we shouldn't call it unless we really do need to remove the existing key.

               if (session[TempDataSessionStateKey] != null) {
                  session.Remove(TempDataSessionStateKey);
               }
            }
         }
      }
   }
}
