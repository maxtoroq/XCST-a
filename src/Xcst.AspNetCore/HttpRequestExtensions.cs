// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Xcst.Web;

public static class HttpRequestExtensions {

   internal const string
   XHttpMethodOverrideKey = "X-HTTP-Method-Override";

   public static string
   GetHttpMethodOverride(this HttpRequest request) {

      ArgumentNullException.ThrowIfNull(request);

      var incomingVerb = request.Method;

      if (!String.Equals(incomingVerb, "POST", StringComparison.OrdinalIgnoreCase)) {
         return incomingVerb;
      }

      string? verbOverride = null;
      var headerOverrideValue = (string?)request.Headers[XHttpMethodOverrideKey];

      if (!String.IsNullOrEmpty(headerOverrideValue)) {
         verbOverride = headerOverrideValue;
      } else {

         var formOverrideValue = (string?)request.Form[XHttpMethodOverrideKey];

         if (!String.IsNullOrEmpty(formOverrideValue)) {
            verbOverride = formOverrideValue;
         } else {

            var queryStringOverrideValue = (string?)request.Query[XHttpMethodOverrideKey];

            if (!String.IsNullOrEmpty(queryStringOverrideValue)) {
               verbOverride = queryStringOverrideValue;
            }
         }
      }

      if (verbOverride != null) {
         if (!String.Equals(verbOverride, "GET", StringComparison.OrdinalIgnoreCase)
            && !String.Equals(verbOverride, "POST", StringComparison.OrdinalIgnoreCase)) {

            incomingVerb = verbOverride;
         }
      }

      return incomingVerb;
   }

   public static bool
   IsAjaxRequest(this HttpRequest request) {

      ArgumentNullException.ThrowIfNull(request);

      return request.Headers.XRequestedWith == "XMLHttpRequest";
   }

   public static bool
   IsUrlLocalToHost(this HttpRequest request, string? url) {

      return !String.IsNullOrEmpty(url) &&
         ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) || // "/" or "/foo" but not "//" or "/\"
         (url.Length > 1 && url[0] == '~' && url[1] == '/')); // "~/" or "~/foo"
   }
}
