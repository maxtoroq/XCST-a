// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;

namespace System.Web.Mvc {

   public sealed class FormValueProvider : NameValueCollectionValueProvider {

      public FormValueProvider(ControllerContext controllerContext)
         : this(controllerContext, new UnvalidatedRequestValuesWrapper(controllerContext.HttpContext.Request.Unvalidated)) { }

      // For unit testing

      internal FormValueProvider(ControllerContext controllerContext, IUnvalidatedRequestValues unvalidatedValues)
         : base(controllerContext.HttpContext.Request.Form, unvalidatedValues.Form, CultureInfo.CurrentCulture) { }
   }

   public sealed class FormValueProviderFactory : ValueProviderFactory {

      readonly UnvalidatedRequestValuesAccessor _unvalidatedValuesAccessor;

      public FormValueProviderFactory()
         : this(null) { }

      // For unit testing

      internal FormValueProviderFactory(UnvalidatedRequestValuesAccessor unvalidatedValuesAccessor) {

         _unvalidatedValuesAccessor = unvalidatedValuesAccessor
            ?? (cc => new UnvalidatedRequestValuesWrapper(cc.HttpContext.Request.Unvalidated));
      }

      public override IValueProvider GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         return new FormValueProvider(controllerContext, _unvalidatedValuesAccessor(controllerContext));
      }
   }
}
