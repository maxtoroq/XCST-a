// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Xcst.Web;

delegate bool
TryGetValueDelegate(object dictionary, string key, out object? value);

static class TypeHelpers {

   static readonly Dictionary<Type, TryGetValueDelegate?>
   _tryGetValueDelegateCache = new();

   static readonly ReaderWriterLockSlim
   _tryGetValueDelegateCacheLock = new();

   static readonly MethodInfo
   _strongTryGetValueImplInfo = typeof(TypeHelpers)
      .GetMethod(nameof(StrongTryGetValueImpl), BindingFlags.NonPublic | BindingFlags.Static)!;

   public static TryGetValueDelegate?
   CreateTryGetValueDelegate(Type targetType) {

      TryGetValueDelegate? result;

      _tryGetValueDelegateCacheLock.EnterReadLock();

      try {
         if (_tryGetValueDelegateCache.TryGetValue(targetType, out result)) {
            return result;
         }
      } finally {
         _tryGetValueDelegateCacheLock.ExitReadLock();
      }

      var dictionaryType = ExtractGenericInterface(targetType, typeof(IDictionary<,>));

      // just wrap a call to the underlying IDictionary<TKey, TValue>.TryGetValue() where string can be cast to TKey

      if (dictionaryType != null) {

         var typeArguments = dictionaryType.GetGenericArguments();
         var keyType = typeArguments[0];
         var returnType = typeArguments[1];

         if (keyType.IsAssignableFrom(typeof(string))) {
            var strongImplInfo = _strongTryGetValueImplInfo.MakeGenericMethod(keyType, returnType);
            result = (TryGetValueDelegate)Delegate.CreateDelegate(typeof(TryGetValueDelegate), strongImplInfo);
         }
      }

      // wrap a call to the underlying IDictionary.Item()

      if (result is null
         && typeof(IDictionary).IsAssignableFrom(targetType)) {

         result = TryGetValueFromNonGenericDictionary;
      }

      _tryGetValueDelegateCacheLock.EnterWriteLock();

      try {
         _tryGetValueDelegateCache[targetType] = result;
      } finally {
         _tryGetValueDelegateCacheLock.ExitWriteLock();
      }

      return result;
   }

   static bool
   StrongTryGetValueImpl<TKey, TValue>(object dictionary, string key, out object? value) {

      var strongDict = (IDictionary<TKey, TValue>)dictionary;

      var retVal = strongDict.TryGetValue((TKey)(object)key, out var strongValue);
      value = strongValue;

      return retVal;
   }

   static bool
   TryGetValueFromNonGenericDictionary(object dictionary, string key, out object? value) {

      var weakDict = (IDictionary)dictionary;

      var containsKey = weakDict.Contains(key);
      value = (containsKey) ? weakDict[key] : null;

      return containsKey;
   }

   public static Type?
   ExtractGenericInterface(Type queryType, Type interfaceType) {

      if (MatchesGenericType(queryType, interfaceType)) {
         return queryType;
      }

      var queryTypeInterfaces = queryType.GetInterfaces();
      return MatchGenericTypeFirstOrDefault(queryTypeInterfaces, interfaceType);
   }

   public static bool
   IsNullableValueType(Type type) =>
      Nullable.GetUnderlyingType(type) != null;

   static bool
   MatchesGenericType(Type type, Type matchType) =>
      type.IsGenericType && type.GetGenericTypeDefinition() == matchType;

   static Type?
   MatchGenericTypeFirstOrDefault(Type[] types, Type matchType) {

      for (int i = 0; i < types.Length; i++) {

         var type = types[i];

         if (MatchesGenericType(type, matchType)) {
            return type;
         }
      }

      return null;
   }

   public static bool
   TypeAllowsNullValue(Type type) =>
      (!type.IsValueType || IsNullableValueType(type));

   /// <summary>
   /// Given an object of anonymous type, add each property as a key and associated with its value to a dictionary.
   ///
   /// This helper will cache accessors and types, and is intended when the anonymous object is accessed multiple
   /// times throughout the lifetime of the web application.
   /// </summary>
   public static Dictionary<string, object?>
   ObjectToDictionary(object? value) {

      var comparer = StringComparer.OrdinalIgnoreCase;

      if (value is IDictionary<string, object?> dictionary) {
         return new Dictionary<string, object?>(dictionary, comparer);
      }

      var result = new Dictionary<string, object?>(comparer);

      if (value != null) {
         foreach (var helper in PropertyHelper.GetProperties(value)) {
            result.Add(helper.Name, helper.GetValue(value));
         }
      }

      return result;
   }
}
