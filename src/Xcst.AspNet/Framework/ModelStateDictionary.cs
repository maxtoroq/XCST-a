// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Web.Mvc {

   [Serializable]
   public class ModelStateDictionary : IDictionary<string, ModelState> {

      readonly IDictionary<string, ModelState> _innerDictionary;

      public int Count => _innerDictionary.Count;

      public bool IsReadOnly => _innerDictionary.IsReadOnly;

      public bool IsValid => Values.All(modelState => modelState.Errors.Count == 0);

      public ICollection<string> Keys => _innerDictionary.Keys;

      public ICollection<ModelState> Values => _innerDictionary.Values;

      public ModelState this[string key] {
         get {
            ModelState value;
            _innerDictionary.TryGetValue(key, out value);
            return value;
         }
         set => _innerDictionary[key] = value;
      }

      // For unit testing
      internal IDictionary<string, ModelState> InnerDictionary => _innerDictionary;

      public ModelStateDictionary() {
         _innerDictionary = new Dictionary<string, ModelState>(StringComparer.OrdinalIgnoreCase);
      }

      public ModelStateDictionary(ModelStateDictionary dictionary) {

         if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

         _innerDictionary = new CopyOnWriteDictionary<string, ModelState>(dictionary, StringComparer.OrdinalIgnoreCase);
      }

      public void Add(KeyValuePair<string, ModelState> item) =>
         _innerDictionary.Add(item);

      public void Add(string key, ModelState value) =>
         _innerDictionary.Add(key, value);

      public void AddModelError(string key, Exception exception) =>
         GetModelStateForKey(key).Errors.Add(exception);

      public void AddModelError(string key, string errorMessage) =>
         GetModelStateForKey(key).Errors.Add(errorMessage);

      public void Clear() =>
         _innerDictionary.Clear();

      public bool Contains(KeyValuePair<string, ModelState> item) =>
         _innerDictionary.Contains(item);

      public bool ContainsKey(string key) =>
         _innerDictionary.ContainsKey(key);

      public void CopyTo(KeyValuePair<string, ModelState>[] array, int arrayIndex) =>
         _innerDictionary.CopyTo(array, arrayIndex);

      public IEnumerator<KeyValuePair<string, ModelState>> GetEnumerator() =>
         _innerDictionary.GetEnumerator();

      private ModelState GetModelStateForKey(string key) {

         if (key is null) throw new ArgumentNullException(nameof(key));

         ModelState modelState;

         if (!TryGetValue(key, out modelState)) {
            modelState = new ModelState();
            this[key] = modelState;
         }

         return modelState;
      }

      public bool IsValidField(string key) {

         if (key is null) throw new ArgumentNullException(nameof(key));

         // if the key is not found in the dictionary, we just say that it's valid (since there are no errors)

         return FindKeysWithPrefix(this, key)
            .All(entry => entry.Value.Errors.Count == 0);
      }

      static IEnumerable<KeyValuePair<string, TValue>> FindKeysWithPrefix<TValue>(IDictionary<string, TValue> dictionary, string prefix) {

         if (dictionary.TryGetValue(prefix, out TValue exactMatchValue)) {
            yield return new KeyValuePair<string, TValue>(prefix, exactMatchValue);
         }

         foreach (var entry in dictionary) {

            string key = entry.Key;

            if (key.Length <= prefix.Length) {
               continue;
            }

            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
               continue;
            }

            // Everything is prefixed by the empty string

            if (prefix.Length == 0) {
               yield return entry;
            } else {
               char charAfterPrefix = key[prefix.Length];
               switch (charAfterPrefix) {
                  case '[':
                  case '.':
                     yield return entry;
                     break;
               }
            }
         }
      }

      public void Merge(ModelStateDictionary dictionary) {

         if (dictionary is null) {
            return;
         }

         foreach (var entry in dictionary) {
            this[entry.Key] = entry.Value;
         }
      }

      public bool Remove(KeyValuePair<string, ModelState> item) =>
         _innerDictionary.Remove(item);

      public bool Remove(string key) =>
         _innerDictionary.Remove(key);

      public void SetModelValue(string key, ValueProviderResult value) =>
         GetModelStateForKey(key).Value = value;

      public bool TryGetValue(string key, out ModelState value) =>
         _innerDictionary.TryGetValue(key, out value);

      IEnumerator IEnumerable.GetEnumerator() {
         return GetEnumerator();
      }
   }

   [Serializable]
   public class ModelState {

      ModelErrorCollection _errors = new ModelErrorCollection();

      public ValueProviderResult? Value { get; set; }

      public ModelErrorCollection Errors => _errors;
   }

   [Serializable]
   public class ModelErrorCollection : Collection<ModelError> {

      public void Add(Exception exception) =>
         Add(new ModelError(exception));

      public void Add(string errorMessage) =>
         Add(new ModelError(errorMessage));
   }

   [Serializable]
   public class ModelError {

      public Exception? Exception { get; private set; }

      public string ErrorMessage { get; private set; }

      public ModelError(Exception exception)
         : this(exception, errorMessage: null) { }

      public ModelError(Exception exception, string? errorMessage)
         : this(errorMessage) {

         if (exception is null) throw new ArgumentNullException(nameof(exception));

         this.Exception = exception;
      }

      public ModelError(string? errorMessage) {
         this.ErrorMessage = errorMessage ?? String.Empty;
      }
   }
}
