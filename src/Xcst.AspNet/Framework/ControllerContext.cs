// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Web.Routing;
using System.Web.WebPages;

namespace System.Web.Mvc {

   // Though many of the properties on ControllerContext and its subclassed types are virtual, there are still sealed
   // properties (like ControllerContext.RequestContext, ActionExecutingContext.Result, etc.). If these properties
   // were virtual, a mocking framework might override them with incorrect behavior (property getters would return
   // null, property setters would be no-ops). By sealing these properties, we are forcing them to have the default
   // "get or store a value" semantics that they were intended to have.

   public class ControllerContext {

      HttpContextBase _httpContext;
      RequestContext _requestContext;
      RouteData _routeData;
      IDependencyResolver _resolver;
      ITempDataProvider _tempDataProvider;

      internal IDisplayMode DisplayMode {
         get { return DisplayModeProvider.GetDisplayMode(HttpContext); }
         set { DisplayModeProvider.SetDisplayMode(HttpContext, value); }
      }

      public virtual HttpContextBase HttpContext {
         get {
            if (_httpContext == null) {
               _httpContext = (_requestContext != null) ? _requestContext.HttpContext : new EmptyHttpContext();
            }
            return _httpContext;
         }
         set { _httpContext = value; }
      }

      internal virtual bool IsChildAction => false;

      internal ViewContext ParentActionViewContext => null;

      public RequestContext RequestContext {
         get {
            if (_requestContext == null) {

               // still need explicit calls to constructors since the property getters are virtual and might return null

               HttpContextBase httpContext = HttpContext ?? new EmptyHttpContext();
               RouteData routeData = RouteData ?? new RouteData();

               _requestContext = new RequestContext(httpContext, routeData);
            }
            return _requestContext;
         }
         set { _requestContext = value; }
      }

      public virtual RouteData RouteData {
         get {
            if (_routeData == null) {
               _routeData = (_requestContext != null) ? _requestContext.RouteData : new RouteData();
            }
            return _routeData;
         }
         set { _routeData = value; }
      }

      public bool ValidateRequest { get; set; } = true;

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

      // parameterless constructor used for mocking

      public ControllerContext() { }

      // copy constructor - allows for subclassed types to take an existing ControllerContext as a parameter
      // and we'll automatically set the appropriate properties

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The virtual property setters are only to support mocking frameworks, in which case this constructor shouldn't be called anyway.")]
      protected ControllerContext(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         this.RequestContext = controllerContext.RequestContext;
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
