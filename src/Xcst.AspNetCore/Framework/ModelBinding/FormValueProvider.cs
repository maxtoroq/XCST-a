// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Xcst.Web.Mvc.ModelBinding;

public sealed class FormValueProvider : NameValueCollectionValueProvider {

   public
   FormValueProvider(ControllerContext controllerContext)
      : base(controllerContext.HttpContext.Request.Form, CultureInfo.CurrentCulture) { }
}

public sealed class FormValueProviderFactory : ValueProviderFactory {

   public override IValueProvider
   GetValueProvider(ControllerContext controllerContext) {

      if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

      return new FormValueProvider(controllerContext);
   }
}
