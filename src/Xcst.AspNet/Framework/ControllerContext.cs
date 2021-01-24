// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if NETCOREAPP
using HttpContextBase = Microsoft.AspNetCore.Http.HttpContext;
#endif

namespace System.Web.Mvc {

   // Though many of the properties on ControllerContext and its subclassed types are virtual, there are still sealed
   // properties (like ControllerContext.RequestContext, ActionExecutingContext.Result, etc.). If these properties
   // were virtual, a mocking framework might override them with incorrect behavior (property getters would return
   // null, property setters would be no-ops). By sealing these properties, we are forcing them to have the default
   // "get or store a value" semantics that they were intended to have.

   public class ControllerContext {

      HttpContextBase? _httpContext;
      IDependencyResolver? _resolver;
      ITempDataProvider? _tempDataProvider;

      public HttpContextBase HttpContext {
         get => _httpContext ??= new EmptyHttpContext();
         set => _httpContext = value;
      }

      /// <summary>
      /// Represents a replaceable dependency resolver providing services.
      /// By default, it uses the <see cref="DependencyResolver.CurrentCache"/>. 
      /// </summary>
      internal IDependencyResolver Resolver {
         get => _resolver ?? DependencyResolver.CurrentCache;
         set => _resolver = value;
      }

      public ITempDataProvider TempDataProvider {
         get => _tempDataProvider ??= CreateTempDataProvider();
         set => _tempDataProvider = value;
      }

      public ControllerContext() { }

      // copy constructor - allows for subclassed types to take an existing ControllerContext as a parameter
      // and we'll automatically set the appropriate properties

      protected ControllerContext(ControllerContext controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         _httpContext = controllerContext._httpContext;
         _resolver = controllerContext._resolver;
         _tempDataProvider = controllerContext._tempDataProvider;
      }

      public ControllerContext(HttpContextBase httpContext) {

         if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

         _httpContext = httpContext;
      }

      ITempDataProvider CreateTempDataProvider() {

         // The factory can be customized in order to create an ITempDataProvider for the controller.

         ITempDataProviderFactory? tempDataProviderFactory = this.Resolver.GetService<ITempDataProviderFactory>();

         if (tempDataProviderFactory != null) {
            return tempDataProviderFactory.CreateInstance();
         }

         // Note that getting a service from the current cache will return the same instance for every controller.

         return this.Resolver.GetService<ITempDataProvider>()
            ?? new SessionStateTempDataProvider();
      }
   }
}
