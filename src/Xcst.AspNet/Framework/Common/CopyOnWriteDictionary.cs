// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Web.Mvc {

   /// <summary>
   /// A <see cref="IDictionary{TKey, TValue}"/> that defers creating a shallow copy of the source dictionary until
   /// a mutative operation has been performed on it.
   /// </summary>
   class CopyOnWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue> {

      readonly IDictionary<TKey, TValue> _sourceDictionary;
      readonly IEqualityComparer<TKey> _comparer;
      IDictionary<TKey, TValue> _innerDictionary;

      private IDictionary<TKey, TValue> ReadDictionary =>
         _innerDictionary ?? _sourceDictionary;

      private IDictionary<TKey, TValue> WriteDictionary =>
         _innerDictionary ?? (_innerDictionary = new Dictionary<TKey, TValue>(_sourceDictionary, _comparer));

      public virtual ICollection<TKey> Keys => ReadDictionary.Keys;

      public virtual ICollection<TValue> Values => ReadDictionary.Values;

      public virtual int Count => ReadDictionary.Count;

      public virtual bool IsReadOnly => false;

      public virtual TValue this[TKey key] {
         get => ReadDictionary[key];
         set => WriteDictionary[key] = value;
      }

      public CopyOnWriteDictionary(IDictionary<TKey, TValue> sourceDictionary, IEqualityComparer<TKey> comparer) {

         Contract.Assert(sourceDictionary != null);
         Contract.Assert(comparer != null);

         _sourceDictionary = sourceDictionary;
         _comparer = comparer;
      }

      public virtual bool ContainsKey(TKey key) =>
         this.ReadDictionary.ContainsKey(key);

      public virtual void Add(TKey key, TValue value) =>
         this.WriteDictionary.Add(key, value);

      public virtual bool Remove(TKey key) =>
         this.WriteDictionary.Remove(key);

      public virtual bool TryGetValue(TKey key, out TValue value) =>
         this.ReadDictionary.TryGetValue(key, out value);

      public virtual void Add(KeyValuePair<TKey, TValue> item) =>
         this.WriteDictionary.Add(item);

      public virtual void Clear() =>
         this.WriteDictionary.Clear();

      public virtual bool Contains(KeyValuePair<TKey, TValue> item) =>
         this.ReadDictionary.Contains(item);

      public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
         this.ReadDictionary.CopyTo(array, arrayIndex);

      public bool Remove(KeyValuePair<TKey, TValue> item) =>
         this.WriteDictionary.Remove(item);

      public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
         this.ReadDictionary.GetEnumerator();

      IEnumerator IEnumerable.GetEnumerator() =>
         GetEnumerator();
   }
}
