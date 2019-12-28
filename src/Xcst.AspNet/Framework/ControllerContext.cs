// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Web.Routing;

namespace System.Web.Mvc {

   // Though many of the properties on ControllerContext and its subclassed types are virtual, there are still sealed
   // properties (like ControllerContext.RequestContext, ActionExecutingContext.Result, etc.). If these properties
   // were virtual, a mocking framework might override them with incorrect behavior (property getters would return
   // null, property setters would be no-ops). By sealing these properties, we are forcing them to have the default
   // "get or store a value" semantics that they were intended to have.

   public class ControllerContext {

      HttpContextBase _httpContext;
      IDependencyResolver _resolver;
      ITempDataProvider _tempDataProvider;

      public virtual HttpContextBase HttpContext {
         get {
            if (_httpContext == null) {
               _httpContext = RequestContext?.HttpContext;
            }
            return _httpContext;
         }
         set { _httpContext = value; }
      }

      public RequestContext RequestContext { get; set; }

      /// <summary>
      /// Represents a replaceable dependency resolver providing services.
      /// By default, it uses the <see cref="DependencyResolver.CurrentCache"/>. 
      /// </summary>
      internal IDependencyResolver Resolver {
         get { return _resolver ?? DependencyResolver.CurrentCache; }
         set { _resolver = value; }
      }

      public ITempDataProvider TempDataProvider {
         get {
            if (_tempDataProvider == null) {
               _tempDataProvider = CreateTempDataProvider();
            }
            return _tempDataProvider;
         }
         set { _tempDataProvider = value; }
      }

      public ControllerContext()
         : this(new RequestContext(new EmptyHttpContext(), new RouteData())) { }

      // copy constructor - allows for subclassed types to take an existing ControllerContext as a parameter
      // and we'll automatically set the appropriate properties

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The virtual property setters are only to support mocking frameworks, in which case this constructor shouldn't be called anyway.")]
      protected ControllerContext(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         this.RequestContext = controllerContext.RequestContext;

         _resolver = controllerContext._resolver;
         _tempDataProvider = controllerContext._tempDataProvider;
      }

      public ControllerContext(HttpContextBase httpContext, RouteData routeData)
         : this(new RequestContext(httpContext, routeData)) { }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The virtual property setters are only to support mocking frameworks, in which case this constructor shouldn't be called anyway.")]
      public ControllerContext(RequestContext requestContext) {

         if (requestContext == null) throw new ArgumentNullException(nameof(requestContext));

         this.RequestContext = requestContext;
      }

      ITempDataProvider CreateTempDataProvider() {

         // The factory can be customized in order to create an ITempDataProvider for the controller.

         ITempDataProviderFactory tempDataProviderFactory = this.Resolver.GetService<ITempDataProviderFactory>();

         if (tempDataProviderFactory != null) {
            return tempDataProviderFactory.CreateInstance();
         }

         // Note that getting a service from the current cache will return the same instance for every controller.

         return this.Resolver.GetService<ITempDataProvider>()
            ?? new SessionStateTempDataProvider();
      }

      sealed class EmptyHttpContext : HttpContextBase { }
   }
}
