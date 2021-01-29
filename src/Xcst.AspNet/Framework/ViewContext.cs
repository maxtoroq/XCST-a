﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
#if NETCOREAPP
using HttpContextBase = Microsoft.AspNetCore.Http.HttpContext;
#endif

namespace System.Web.Mvc {

   public class ViewContext : ControllerContext {

      // Some values have to be stored in HttpContext.Items in order to be propagated between calls
      // to RenderPartial(), RenderAction(), etc.

      static readonly object _formContextKey = new object();

      // We need a default FormContext if the user uses html <form> instead of an MvcForm

      FormContext _defaultFormContext = new FormContext();

      public virtual bool ClientValidationEnabled { get; set; } = true;

      public virtual bool UnobtrusiveJavaScriptEnabled { get; set; } = true;

      /// <summary>
      /// Element name used to wrap a top-level message generated by
      /// <code>a:validation-summary</code>.
      /// </summary>
      public virtual string ValidationSummaryMessageElement { get; set; } = "span";

      /// <summary>
      /// Element name used to wrap a top-level message generated by
      /// <code>a:validation-message</code>.
      /// </summary>
      public virtual string ValidationMessageElement { get; set; } = "span";

      public virtual FormContext FormContext {
         get => HttpContext.Items[_formContextKey] as FormContext
            // Never return a null form context, this is important for validation purposes
            ?? _defaultFormContext;
         set => HttpContext.Items[_formContextKey] = value;
      }

      // parameterless constructor used for mocking
      public ViewContext() { }

      public ViewContext(HttpContextBase httpContext)
         : base(httpContext) { }

      public ViewContext(ControllerContext controllerContext)
         : base(controllerContext) { }

      internal FormContext? GetFormContextForClientValidation() =>
         (this.ClientValidationEnabled) ? this.FormContext : null;
   }

   public class FormContext {

      readonly Dictionary<string, bool> _renderedFields = new Dictionary<string, bool>();

      public bool RenderedField(string fieldName) {
         bool result;
         _renderedFields.TryGetValue(fieldName, out result);
         return result;
      }

      public void RenderedField(string fieldName, bool value) =>
         _renderedFields[fieldName] = value;
   }
}
