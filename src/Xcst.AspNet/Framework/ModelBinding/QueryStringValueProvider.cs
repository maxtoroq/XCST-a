// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;

namespace System.Web.Mvc {

   public sealed class QueryStringValueProvider : NameValueCollectionValueProvider {

      // QueryString should use the invariant culture since it's part of the URL, and the URL should be
      // interpreted in a uniform fashion regardless of the origin of a particular request.

      public QueryStringValueProvider(ControllerContext controllerContext)
         : this(controllerContext, new UnvalidatedRequestValuesWrapper(controllerContext.HttpContext.Request.Unvalidated)) { }

      // For unit testing

      internal QueryStringValueProvider(ControllerContext controllerContext, IUnvalidatedRequestValues unvalidatedValues)
         : base(controllerContext.HttpContext.Request.QueryString, unvalidatedValues.QueryString, CultureInfo.InvariantCulture) { }
   }

   public sealed class QueryStringValueProviderFactory : ValueProviderFactory {

      readonly UnvalidatedRequestValuesAccessor _unvalidatedValuesAccessor;

      public QueryStringValueProviderFactory()
         : this(null) { }

      // For unit testing

      internal QueryStringValueProviderFactory(UnvalidatedRequestValuesAccessor unvalidatedValuesAccessor) {
         _unvalidatedValuesAccessor = unvalidatedValuesAccessor ?? (cc => new UnvalidatedRequestValuesWrapper(cc.HttpContext.Request.Unvalidated));
      }

      public override IValueProvider GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         return new QueryStringValueProvider(controllerContext, _unvalidatedValuesAccessor(controllerContext));
      }
   }
}
