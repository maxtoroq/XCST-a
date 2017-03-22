// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;

namespace System.Web.Mvc {

   /// <summary>
   /// The JQuery Form Value provider is used to handle JQuery formatted data in
   /// request Forms.
   /// </summary>

   public class JQueryFormValueProvider : NameValueCollectionValueProvider {

      /// <summary>
      /// Constructs a new instance of the JQuery form ValueProvider
      /// </summary>
      /// <param name="controllerContext">The context on which the ValueProvider operates.</param>

      public JQueryFormValueProvider(ControllerContext controllerContext)
         : this(controllerContext, new UnvalidatedRequestValuesWrapper(controllerContext.HttpContext.Request.Unvalidated)) { }

      // For unit testing

      internal JQueryFormValueProvider(ControllerContext controllerContext, IUnvalidatedRequestValues unvalidatedValues)
         : base(controllerContext.HttpContext.Request.Form,
            unvalidatedValues.Form,
            CultureInfo.CurrentCulture,
            jQueryToMvcRequestNormalizationRequired: true) { }
   }

   /// <summary>
   /// Provides the necessary ValueProvider to handle JQuery Form data.
   /// </summary>

   public sealed class JQueryFormValueProviderFactory : ValueProviderFactory {

      readonly UnvalidatedRequestValuesAccessor _unvalidatedValuesAccessor;

      /// <summary>
      /// Constructs a new instance of the factory which provides JQuery form ValueProviders.
      /// </summary>

      public JQueryFormValueProviderFactory()
         : this(unvalidatedValuesAccessor: null) { }

      // For unit testing

      internal JQueryFormValueProviderFactory(UnvalidatedRequestValuesAccessor unvalidatedValuesAccessor) {

         _unvalidatedValuesAccessor = unvalidatedValuesAccessor
            ?? (cc => new UnvalidatedRequestValuesWrapper(cc.HttpContext.Request.Unvalidated));
      }

      /// <summary>
      /// Returns the suitable ValueProvider.
      /// </summary>
      /// <param name="controllerContext">The context on which the ValueProvider should operate.</param>
      /// <returns></returns>

      public override IValueProvider GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         return new JQueryFormValueProvider(controllerContext, _unvalidatedValuesAccessor(controllerContext));
      }
   }
}
