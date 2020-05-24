// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;

namespace System.Web.Mvc {

   public sealed class RouteDataValueProvider : DictionaryValueProvider<object> {

      // RouteData should use the invariant culture since it's part of the URL, and the URL should be
      // interpreted in a uniform fashion regardless of the origin of a particular request.

      public RouteDataValueProvider(ControllerContext controllerContext)
         : base(controllerContext.HttpContext.Request.RequestContext.RouteData.Values, CultureInfo.InvariantCulture) { }
   }

   public sealed class RouteDataValueProviderFactory : ValueProviderFactory {

      public override IValueProvider GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         return new RouteDataValueProvider(controllerContext);
      }
   }
}
