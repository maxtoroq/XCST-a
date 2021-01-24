// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   interface IDependencyResolver {

      object? GetService(Type serviceType);
      IEnumerable<object> GetServices(Type serviceType);
   }

   static class DependencyResolverExtensions {

      public static TService? GetService<TService>(this IDependencyResolver resolver) where TService : class =>
         (TService?)resolver.GetService(typeof(TService));

      public static IEnumerable<TService> GetServices<TService>(this IDependencyResolver resolver) =>
         resolver.GetServices(typeof(TService)).Cast<TService>();
   }

   class DependencyResolver {

      static readonly DependencyResolver _instance = new DependencyResolver();

      public static IDependencyResolver Current =>
         _instance.InnerCurrent;

      internal static IDependencyResolver CurrentCache =>
         _instance.InnerCurrentCache;

      public IDependencyResolver InnerCurrent { get; private set; }

      /// <summary>
      /// Provides caching over results returned by Current.
      /// </summary>
      internal IDependencyResolver InnerCurrentCache { get; private set; }

#pragma warning disable CS8618
      public DependencyResolver() {
#pragma warning restore CS8618
         InnerSetResolver(new DefaultDependencyResolver());
      }

      public static void SetResolver(IDependencyResolver resolver) =>
         _instance.InnerSetResolver(resolver);

      public static void SetResolver(object commonServiceLocator) =>
         _instance.InnerSetResolver(commonServiceLocator);

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types.")]
      public static void SetResolver(Func<Type, object?> getService, Func<Type, IEnumerable<object>> getServices) =>
         _instance.InnerSetResolver(getService, getServices);

      public void InnerSetResolver(IDependencyResolver resolver) {

         if (resolver is null) throw new ArgumentNullException(nameof(resolver));

         this.InnerCurrent = resolver;
         this.InnerCurrentCache = new CacheDependencyResolver(this.InnerCurrent);
      }

      public void InnerSetResolver(object commonServiceLocator) {

         if (commonServiceLocator is null) throw new ArgumentNullException(nameof(commonServiceLocator));

         Type locatorType = commonServiceLocator.GetType();
         MethodInfo? getInstance = locatorType.GetMethod("GetInstance", new[] { typeof(Type) });
         MethodInfo? getInstances = locatorType.GetMethod("GetAllInstances", new[] { typeof(Type) });

         if (getInstance is null
            || getInstance.ReturnType != typeof(object)
            || getInstances is null
            || getInstances.ReturnType != typeof(IEnumerable<object>)) {

            throw new ArgumentException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.DependencyResolver_DoesNotImplementICommonServiceLocator,
                  locatorType.FullName),
               nameof(commonServiceLocator));
         }

         var getService = (Func<Type, object?>)Delegate.CreateDelegate(typeof(Func<Type, object?>), commonServiceLocator, getInstance);
         var getServices = (Func<Type, IEnumerable<object>>)Delegate.CreateDelegate(typeof(Func<Type, IEnumerable<object>>), commonServiceLocator, getInstances);

         InnerSetResolver(new DelegateBasedDependencyResolver(getService, getServices));
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types.")]
      public void InnerSetResolver(Func<Type, object?> getService, Func<Type, IEnumerable<object>> getServices) {

         if (getService is null) throw new ArgumentNullException(nameof(getService));
         if (getServices is null) throw new ArgumentNullException(nameof(getServices));

         InnerSetResolver(new DelegateBasedDependencyResolver(getService, getServices));
      }

      /// <summary>
      /// Wraps an IDependencyResolver and ensures single instance per-type.
      /// </summary>
      /// <remarks>
      /// Note it's possible for multiple threads to race and call the _resolver service multiple times.
      /// We'll pick one winner and ignore the others and still guarantee a unique instance.
      /// </remarks>
      sealed class CacheDependencyResolver : IDependencyResolver {

         readonly ConcurrentDictionary<Type, object?> _cache = new ConcurrentDictionary<Type, object?>();
         readonly ConcurrentDictionary<Type, IEnumerable<object>> _cacheMultiple = new ConcurrentDictionary<Type, IEnumerable<object>>();
         readonly Func<Type, object?> _getServiceDelegate;
         readonly Func<Type, IEnumerable<object>> _getServicesDelegate;

         readonly IDependencyResolver _resolver;

         public CacheDependencyResolver(IDependencyResolver resolver) {
            _resolver = resolver;
            _getServiceDelegate = _resolver.GetService;
            _getServicesDelegate = _resolver.GetServices;
         }

         public object? GetService(Type serviceType) =>
            // Use a saved delegate to prevent per-call delegate allocation
            _cache.GetOrAdd(serviceType, _getServiceDelegate);

         public IEnumerable<object> GetServices(Type serviceType) =>
            // Use a saved delegate to prevent per-call delegate allocation
            _cacheMultiple.GetOrAdd(serviceType, _getServicesDelegate);
      }

      class DefaultDependencyResolver : IDependencyResolver {

         [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method might throw exceptions whose type we cannot strongly link against; namely, ActivationException from common service locator")]
         public object? GetService(Type serviceType) {

            // Since attempting to create an instance of an interface or an abstract type results in an exception, immediately return null
            // to improve performance and the debugging experience with first-chance exceptions enabled.

            if (serviceType.IsInterface
               || serviceType.IsAbstract) {

               return null;
            }

            try {
               return Activator.CreateInstance(serviceType);
            } catch {
               return null;
            }
         }

         public IEnumerable<object> GetServices(Type serviceType) =>
            Enumerable.Empty<object>();
      }

      class DelegateBasedDependencyResolver : IDependencyResolver {

         readonly Func<Type, object?> _getService;
         readonly Func<Type, IEnumerable<object>> _getServices;

         public DelegateBasedDependencyResolver(Func<Type, object?> getService, Func<Type, IEnumerable<object>> getServices) {
            _getService = getService;
            _getServices = getServices;
         }

         [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This method might throw exceptions whose type we cannot strongly link against; namely, ActivationException from common service locator")]
         public object? GetService(Type type) {

            try {
               return _getService.Invoke(type);
            } catch {
               return null;
            }
         }

         public IEnumerable<object> GetServices(Type type) =>
            _getServices(type);
      }
   }

   static class MultiServiceResolver {

      internal static TService[] GetCombined<TService>(IList<TService> items, IDependencyResolver? resolver = null)
            where TService : class {

         if (resolver is null) {
            resolver = DependencyResolver.Current;
         }

         IEnumerable<TService> services = resolver.GetServices<TService>();

         return services.Concat(items).ToArray();
      }
   }
}
