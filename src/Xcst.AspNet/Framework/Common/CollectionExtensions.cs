﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Web.Mvc {

   /// <summary>
   /// Helper extension methods for fast use of collections.
   /// </summary>
   static class CollectionExtensions {

      /// <summary>
      /// Return the enumerable as an Array, copying if required. Optimized for common case where it is an Array. 
      /// Avoid mutating the return value.
      /// </summary>
      public static T[] AsArray<T>(this IEnumerable<T> values) {

         Assert.IsNotNull(values);

         if (values is T[] array) {
            return array;
         }

         return values.ToArray();
      }

      /// <summary>
      /// Return the only value from list, the type's default value if empty, or call the errorAction for 2 or more.
      /// </summary>
      [return: MaybeNull]
      public static T SingleDefaultOrError<T, TArg1>(this IList<T> list, Action<TArg1> errorAction, TArg1 errorArg1) {

         Assert.IsNotNull(list);
         Assert.IsNotNull(errorAction);

         switch (list.Count) {
            case 0:
               return default;

            case 1:
               T value = list[0];
               return value;

            default:
               errorAction(errorArg1);
               return default;
         }
      }

      /// <summary>
      /// Returns a single value in list matching type TMatch if there is only one, null if there are none of type TMatch or calls the
      /// errorAction with errorArg1 if there is more than one.
      /// </summary>
      public static TMatch? SingleOfTypeDefaultOrError<TInput, TMatch, TArg1>(this IList<TInput> list, Action<TArg1> errorAction, TArg1 errorArg1)
            where TMatch : class {

         Assert.IsNotNull(list);
         Assert.IsNotNull(errorAction);

         TMatch? result = null;

         for (int i = 0; i < list.Count; i++) {

            var typedValue = list[i] as TMatch;

            if (typedValue != null) {
               if (result is null) {
                  result = typedValue;
               } else {
                  errorAction(errorArg1);
                  return null;
               }
            }
         }

         return result;
      }

      /// <summary>
      /// Convert an ICollection to an array, removing null values. Fast path for case where there are no null values.
      /// </summary>
      public static T[] ToArrayWithoutNulls<T>(this ICollection<T> collection)
            where T : class {

         Assert.IsNotNull(collection);

         T[] result = new T[collection.Count];
         int count = 0;

         foreach (T value in collection) {
            if (value != null) {
               result[count] = value;
               count++;
            }
         }

         if (count == collection.Count) {
            return result;
         } else {
            T[] trimmedResult = new T[count];
            Array.Copy(result, trimmedResult, count);
            return trimmedResult;
         }
      }

      /// <summary>
      /// Convert the array to a Dictionary using the keySelector to extract keys from values and the specified comparer. Optimized for array input.
      /// </summary>
      public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(this TValue[] array, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
            where TKey : notnull {

         Assert.IsNotNull(array);
         Assert.IsNotNull(keySelector);

         var dictionary = new Dictionary<TKey, TValue>(array.Length, comparer);

         for (int i = 0; i < array.Length; i++) {
            TValue value = array[i];
            dictionary.Add(keySelector(value), value);
         }

         return dictionary;
      }

      /// <summary>
      /// Convert the list to a Dictionary using the keySelector to extract keys from values and the specified comparer. Optimized for IList of T input with fast path for array.
      /// </summary>
      public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(this IList<TValue> list, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
            where TKey : notnull {

         Assert.IsNotNull(list);
         Assert.IsNotNull(keySelector);

         if (list is TValue[] array) {
            return ToDictionaryFast(array, keySelector, comparer);
         }

         return ToDictionaryFastNoCheck(list, keySelector, comparer);
      }

      /// <summary>
      /// Convert the enumerable to a Dictionary using the keySelector to extract keys from values and the specified comparer. Fast paths for array and IList of T.
      /// </summary>
      public static Dictionary<TKey, TValue> ToDictionaryFast<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
            where TKey : notnull {

         Assert.IsNotNull(enumerable);
         Assert.IsNotNull(keySelector);

         if (enumerable is TValue[] array) {
            return ToDictionaryFast(array, keySelector, comparer);
         }

         if (enumerable is IList<TValue> list) {
            return ToDictionaryFastNoCheck(list, keySelector, comparer);
         }

         var dictionary = new Dictionary<TKey, TValue>(comparer);

         foreach (TValue value in enumerable) {
            dictionary.Add(keySelector(value), value);
         }

         return dictionary;
      }

      /// <summary>
      /// Convert the list to a Dictionary using the keySelector to extract keys from values and the specified comparer. Optimized for IList of T input. No checking for other types.
      /// </summary>
      static Dictionary<TKey, TValue> ToDictionaryFastNoCheck<TKey, TValue>(IList<TValue> list, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
            where TKey : notnull {

         Assert.IsNotNull(list);
         Assert.IsNotNull(keySelector);

         int listCount = list.Count;

         var dictionary = new Dictionary<TKey, TValue>(listCount, comparer);

         for (int i = 0; i < listCount; i++) {
            TValue value = list[i];
            dictionary.Add(keySelector(value), value);
         }

         return dictionary;
      }
   }
}
