// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using RouteValueDictionary = Microsoft.AspNetCore.Routing.RouteValueDictionary;

namespace System.Web.Mvc;

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

      if (result is null && typeof(IDictionary).IsAssignableFrom(targetType)) {
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

   public static object?
   GetDefaultValue(Type type) =>
      TypeAllowsNullValue(type) ? null
         : Activator.CreateInstance(type);

   public static bool
   IsCompatibleObject<T>(object? value) =>
      (value is T || (value is null && TypeAllowsNullValue(typeof(T))));

   public static bool
   IsNullableValueType(Type type) =>
      Nullable.GetUnderlyingType(type) != null;

   /// <summary>
   /// Provide a new <see cref="MissingMethodException"/> if original Message does not contain given full Type name.
   /// </summary>
   /// <param name="originalException"><see cref="MissingMethodException"/> to check.</param>
   /// <param name="fullTypeName">Full Type name which Message should contain.</param>
   /// <returns>New <see cref="MissingMethodException"/> if an update is required; null otherwise.</returns>
   public static MissingMethodException?
   EnsureDebuggableException(MissingMethodException originalException, string fullTypeName) {

      MissingMethodException? replacementException = null;

      if (!originalException.Message.Contains(fullTypeName)) {

         var message = String.Format(
            CultureInfo.CurrentCulture,
            "{0} Object type '{1}'.",
            originalException.Message,
            fullTypeName);

         replacementException = new MissingMethodException(message, originalException);
      }

      return replacementException;
   }

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
   public static RouteValueDictionary
   ObjectToDictionary(object? value) {

      var dictionary = new RouteValueDictionary();

      if (value != null) {
         foreach (var helper in PropertyHelper.GetProperties(value)) {
            dictionary.Add(helper.Name, helper.GetValue(value));
         }
      }

      return dictionary;
   }

   /// <summary>
   /// Given an object of anonymous type, add each property as a key and associated with its value to a dictionary.
   ///
   /// This helper will not cache accessors and types, and is intended when the anonymous object is accessed once
   /// or very few times throughout the lifetime of the web application.
   /// </summary>
   public static RouteValueDictionary
   ObjectToDictionaryUncached(object? value) {

      var dictionary = new RouteValueDictionary();

      if (value != null) {
         foreach (var helper in PropertyHelper.GetProperties(value)) {
            dictionary.Add(helper.Name, helper.GetValue(value));
         }
      }

      return dictionary;
   }

   /// <remarks>This code is copied from http://www.liensberger.it/web/blog/?p=191 </remarks>
   public static bool
   IsAnonymousType(Type type) {

      if (type is null) throw new ArgumentNullException(nameof(type));

      // TODO: The only way to detect anonymous types right now.

      return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
         && type.IsGenericType && type.Name.Contains("AnonymousType")
         && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) || type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
         && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
   }

   public static bool
   IsIEnumerableNotString(Type type) =>
      typeof(IEnumerable).IsAssignableFrom(type)
         && type != typeof(string);
}
