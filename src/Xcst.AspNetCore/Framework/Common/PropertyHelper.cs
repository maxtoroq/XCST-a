// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace System.Web.Mvc;

class PropertyHelper {

   static ConcurrentDictionary<Type, PropertyHelper[]>
   _reflectionCache = new();

   Func<object, object?>
   _valueGetter;

   [AllowNull]
   public virtual string
   Name { get; protected set; }

   /// <summary>
   /// Initializes a fast property helper. This constructor does not cache the helper.
   /// </summary>
   [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "This is intended the Name is auto set differently per type and the type is internal")]
   public
   PropertyHelper(PropertyInfo property) {

      Debug.Assert(property != null);

      this.Name = property.Name;

      _valueGetter = MakeFastPropertyGetter(property);
   }

   /// <summary>
   /// Creates a single fast property setter. The result is not cached.
   /// </summary>
   /// <param name="propertyInfo">propertyInfo to extract the getter for.</param>
   /// <returns>a fast setter.</returns>
   /// <remarks>This method is more memory efficient than a dynamically compiled lambda, and about the same speed.</remarks>
   public static Action<TDeclaringType, object>
   MakeFastPropertySetter<TDeclaringType>(PropertyInfo propertyInfo) where TDeclaringType : class {

      Debug.Assert(propertyInfo != null);

      var setMethod = propertyInfo.GetSetMethod();

      Debug.Assert(setMethod != null);
      Debug.Assert(!setMethod.IsStatic);
      Debug.Assert(setMethod.GetParameters().Length == 1);
      Debug.Assert(!propertyInfo.ReflectedType.IsValueType);

      // Instance methods in the CLR can be turned into static methods where the first parameter
      // is open over "this". This parameter is always passed by reference, so we have a code
      // path for value types and a code path for reference types.

      var typeInput = propertyInfo.ReflectedType;
      var typeValue = setMethod.GetParameters()[0].ParameterType;

      // Create a delegate TValue -> "TDeclaringType.Property"

      var propertySetterAsAction = setMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(typeInput, typeValue));
      var callPropertySetterClosedGenericMethod = _callPropertySetterOpenGenericMethod.MakeGenericMethod(typeInput, typeValue);
      var callPropertySetterDelegate = Delegate.CreateDelegate(typeof(Action<TDeclaringType, object>), propertySetterAsAction, callPropertySetterClosedGenericMethod);

      return (Action<TDeclaringType, object>)callPropertySetterDelegate;
   }

   public object?
   GetValue(object instance) {

      Debug.Assert(_valueGetter != null, "Must call Initialize before using this object");

      return _valueGetter(instance);
   }

   /// <summary>
   /// Creates and caches fast property helpers that expose getters for every public get property on the underlying type.
   /// </summary>
   /// <param name="instance">the instance to extract property accessors for.</param>
   /// <returns>a cached array of all public property getters from the underlying type of this instance.</returns>
   public static PropertyHelper[]
   GetProperties(object instance) =>
      GetProperties(instance, CreateInstance, _reflectionCache);

   /// <summary>
   /// Creates a single fast property getter. The result is not cached.
   /// </summary>
   /// <param name="propertyInfo">propertyInfo to extract the getter for.</param>
   /// <returns>a fast getter.</returns>
   /// <remarks>This method is more memory efficient than a dynamically compiled lambda, and about the same speed.</remarks>
   public static Func<object, object?>
   MakeFastPropertyGetter(PropertyInfo propertyInfo) {

      Debug.Assert(propertyInfo != null);

      var getMethod = propertyInfo.GetGetMethod();

      Debug.Assert(getMethod != null);
      Debug.Assert(!getMethod.IsStatic);
      Debug.Assert(getMethod.GetParameters().Length == 0);

      // Instance methods in the CLR can be turned into static methods where the first parameter
      // is open over "this". This parameter is always passed by reference, so we have a code
      // path for value types and a code path for reference types.

      var typeInput = getMethod.ReflectedType;
      var typeOutput = getMethod.ReturnType;

      Delegate callPropertyGetterDelegate;

      if (typeInput.IsValueType) {

         // Create a delegate (ref TDeclaringType) -> TValue

         var propertyGetterAsFunc = getMethod.CreateDelegate(typeof(ByRefFunc<,>).MakeGenericType(typeInput, typeOutput));
         var callPropertyGetterClosedGenericMethod = _callPropertyGetterByReferenceOpenGenericMethod.MakeGenericMethod(typeInput, typeOutput);
         callPropertyGetterDelegate = Delegate.CreateDelegate(typeof(Func<object, object>), propertyGetterAsFunc, callPropertyGetterClosedGenericMethod);

      } else {

         // Create a delegate TDeclaringType -> TValue

         var propertyGetterAsFunc = getMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeInput, typeOutput));
         var callPropertyGetterClosedGenericMethod = _callPropertyGetterOpenGenericMethod.MakeGenericMethod(typeInput, typeOutput);
         callPropertyGetterDelegate = Delegate.CreateDelegate(typeof(Func<object, object>), propertyGetterAsFunc, callPropertyGetterClosedGenericMethod);
      }

      return (Func<object, object?>)callPropertyGetterDelegate;
   }

   static PropertyHelper
   CreateInstance(PropertyInfo property) =>
      new PropertyHelper(property);

   // Implementation of the fast getter.

   delegate TValue
   ByRefFunc<TDeclaringType, TValue>(ref TDeclaringType arg);

   static readonly MethodInfo
   _callPropertyGetterOpenGenericMethod = typeof(PropertyHelper)
      .GetMethod(nameof(CallPropertyGetter), BindingFlags.NonPublic | BindingFlags.Static);

   static readonly MethodInfo
   _callPropertyGetterByReferenceOpenGenericMethod = typeof(PropertyHelper)
      .GetMethod(nameof(CallPropertyGetterByReference), BindingFlags.NonPublic | BindingFlags.Static);

   static object?
   CallPropertyGetter<TDeclaringType, TValue>(Func<TDeclaringType, TValue> getter, object @this) =>
      getter((TDeclaringType)@this);

   static object?
   CallPropertyGetterByReference<TDeclaringType, TValue>(ByRefFunc<TDeclaringType, TValue> getter, object @this) {

      var unboxed = (TDeclaringType)@this;
      return getter(ref unboxed);
   }

   // Implementation of the fast setter.

   static readonly MethodInfo
   _callPropertySetterOpenGenericMethod = typeof(PropertyHelper)
      .GetMethod(nameof(CallPropertySetter), BindingFlags.NonPublic | BindingFlags.Static);

   static void
   CallPropertySetter<TDeclaringType, TValue>(Action<TDeclaringType, TValue> setter, object @this, object value) =>
      setter((TDeclaringType)@this, (TValue)value);

   protected static PropertyHelper[]
   GetProperties(object instance, Func<PropertyInfo, PropertyHelper> createPropertyHelper, ConcurrentDictionary<Type, PropertyHelper[]> cache) {

      // Using an array rather than IEnumerable, as this will be called on the hot path numerous times.

      var type = instance.GetType();

      if (!cache.TryGetValue(type, out var helpers)) {

         // We avoid loading indexed properties using the where statement.
         // Indexed properties are not useful (or valid) for grabbing properties off an anonymous object.

         var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod != null);

         var newHelpers = new List<PropertyHelper>();

         foreach (var property in properties) {

            var propertyHelper = createPropertyHelper(property);

            newHelpers.Add(propertyHelper);
         }

         helpers = newHelpers.ToArray();
         cache.TryAdd(type, helpers);
      }

      return helpers;
   }
}
