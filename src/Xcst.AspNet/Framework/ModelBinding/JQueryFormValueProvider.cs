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
         : base(controllerContext.HttpContext.Request.Form,
            CultureInfo.CurrentCulture,
            jQueryToMvcRequestNormalizationRequired: true) { }
   }

   /// <summary>
   /// Provides the necessary ValueProvider to handle JQuery Form data.
   /// </summary>
   public sealed class JQueryFormValueProviderFactory : ValueProviderFactory {

      /// <summary>
      /// Returns the suitable ValueProvider.
      /// </summary>
      /// <param name="controllerContext">The context on which the ValueProvider should operate.</param>
      /// <returns></returns>
      public override IValueProvider GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

         return new JQueryFormValueProvider(controllerContext);
      }
   }
}
