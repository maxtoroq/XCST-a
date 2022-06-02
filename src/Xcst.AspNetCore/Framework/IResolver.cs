// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc;

interface IResolver<T> {

   T
   Current { get; }
}

class SingleServiceResolver<TService> : IResolver<TService> where TService : class {

   Lazy<TService?>
   _currentValueFromResolver;

   Func<TService?>
   _currentValueThunk;

   TService
   _defaultValue;

   Func<IDependencyResolver>
   _resolverThunk;

   string
   _callerMethodName;

   public TService
   Current => _currentValueFromResolver.Value
      ?? _currentValueThunk()
      ?? _defaultValue;

   public
   SingleServiceResolver(Func<TService?> currentValueThunk, TService defaultValue, string callerMethodName) {

      if (currentValueThunk is null) throw new ArgumentNullException(nameof(currentValueThunk));
      if (defaultValue is null) throw new ArgumentNullException(nameof(defaultValue));

      _resolverThunk = () => DependencyResolver.Current;
      _currentValueFromResolver = new Lazy<TService?>(GetValueFromResolver);
      _currentValueThunk = currentValueThunk;
      _defaultValue = defaultValue;
      _callerMethodName = callerMethodName;
   }

   internal
   SingleServiceResolver(Func<TService> staticAccessor, TService defaultValue, IDependencyResolver resolver, string callerMethodName)
      : this(staticAccessor, defaultValue, callerMethodName) {

      if (resolver != null) {
         _resolverThunk = () => resolver;
      }
   }

   TService?
   GetValueFromResolver() {

      var result = _resolverThunk()
         .GetService<TService>();

      if (result != null
         && _currentValueThunk() != null) {

         throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, MvcResources.SingleServiceResolver_CannotRegisterTwoInstances, typeof(TService).Name.ToString(), _callerMethodName));
      }

      return result;
   }
}
