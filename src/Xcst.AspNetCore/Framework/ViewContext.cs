﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using RouteData = Microsoft.AspNetCore.Routing.RouteData;

namespace Xcst.Web.Mvc;

public class ViewContext {

   // Some values have to be stored in HttpContext.Items in order to be propagated between calls
   // to RenderPartial(), RenderAction(), etc.

   static readonly object
   _formContextKey = new();

   // We need a default FormContext if the user uses html <form> instead of an MvcForm

   FormContext
   _defaultFormContext = new();

   HttpContext?
   _httpContext;

   ActionContext?
   _actionContext;

   public HttpContext
   HttpContext {
#pragma warning disable CS8603
      get => _httpContext;
#pragma warning restore CS8603
      set => _httpContext = value;
   }

   public ActionContext
   ActionContext => _actionContext ??= new() {
      HttpContext = HttpContext,
      RouteData = new RouteData()
   };

   public virtual FormContext
   FormContext {
      get {
         if (HttpContext.Items.TryGetValue(_formContextKey, out var formCtxObj)
            && formCtxObj is FormContext formCtx) {
            return formCtx;
         }
         return _defaultFormContext;
      }
      set => HttpContext.Items[_formContextKey] = value;
   }

   public virtual bool
   ClientValidationEnabled { get; set; } = true;

   /// <summary>
   /// Element name used to wrap a top-level message generated by
   /// <code>a:validation-message</code>.
   /// </summary>
   public virtual string
   ValidationMessageElement { get; set; } = "span";

   [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "The usage of the property is as an instance property of the helper.")]
   public Html5DateRenderingMode
   Html5DateRenderingMode { get; set; }

   // parameterless constructor used for mocking
   public
   ViewContext() { }

   public
   ViewContext(HttpContext httpContext) {

      ArgumentNullException.ThrowIfNull(httpContext);

      _httpContext = httpContext;
   }

   public
   ViewContext(ViewContext viewContext) {

      ArgumentNullException.ThrowIfNull(viewContext);

      _httpContext = viewContext._httpContext;
      _actionContext = viewContext._actionContext;

      this.ClientValidationEnabled = viewContext.ClientValidationEnabled;
      this.ValidationMessageElement = viewContext.ValidationMessageElement;
      this.Html5DateRenderingMode = viewContext.Html5DateRenderingMode;
   }

   internal FormContext?
   GetFormContextForClientValidation() =>
      (this.ClientValidationEnabled) ? this.FormContext : null;
}

/// <summary>
/// Controls the value-rendering method For HTML5 input elements of types such as date, time, datetime and datetime-local.
/// </summary>
public enum Html5DateRenderingMode {

   /// <summary>
   /// Render date and time values as Rfc3339 compliant strings to support HTML5 date and time types of input elements.
   /// </summary>
   Rfc3339 = 0,

   /// <summary>
   /// Render date and time values according to the current culture's ToString behavior.
   /// </summary>
   CurrentCulture
}

public class FormContext {

   readonly Dictionary<string, bool>
   _renderedFields = new();

   public bool
   RenderedField(string fieldName) {

      _renderedFields.TryGetValue(fieldName, out var result);
      return result;
   }

   public void
   RenderedField(string fieldName, bool value) =>
      _renderedFields[fieldName] = value;
}
