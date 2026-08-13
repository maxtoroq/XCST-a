// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Xcst.Web;

public static class HttpRequestExtensions {

   public static string
   GetHttpMethodOverride(this HttpRequest request) =>
      GetHttpMethodOverride(request, null);

   public static string
   GetHttpMethodOverride(this HttpRequest request, IFormCollection? form) {

      ArgumentNullException.ThrowIfNull(request);

      const string key = "X-HTTP-Method-Override";

      var incomingVerb = request.Method;

      if (!String.Equals(incomingVerb, "POST", StringComparison.OrdinalIgnoreCase)) {
         return incomingVerb;
      }

      var verbOverride = SingleValue(request.Headers[key])
         ?? SingleValue((FormOrDefault(request, form) is { } f) ? f[key] : StringValues.Empty)
         ?? SingleValue(request.Query[key]);

      if (!String.IsNullOrEmpty(verbOverride)
         && !String.Equals(verbOverride, "GET", StringComparison.OrdinalIgnoreCase)
         && !String.Equals(verbOverride, "POST", StringComparison.OrdinalIgnoreCase)) {

         incomingVerb = verbOverride;
      }

      return incomingVerb;

      static IFormCollection? FormOrDefault(HttpRequest request, IFormCollection? form) =>
         form ?? (request.HasFormContentType ? request.Form : null);

      static string? SingleValue(StringValues values) {

         if (values.Count == 0) {
            return null;
         }

         // if not empty don't return null, for coalescing

         if (values.Count > 1) {
            return String.Empty;
         }

         return values.ToString();
      }
   }

   public static bool
   IsAjaxRequest(this HttpRequest request) {

      ArgumentNullException.ThrowIfNull(request);

      return request.Headers.XRequestedWith == "XMLHttpRequest";
   }
}
