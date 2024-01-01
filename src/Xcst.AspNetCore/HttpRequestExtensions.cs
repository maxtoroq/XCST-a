// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Xcst.Web;

public static class HttpRequestExtensions {

   internal const string
   XHttpMethodOverrideKey = "X-HTTP-Method-Override";

   public static string
   GetHttpMethodOverride(this HttpRequest request) {

      if (request is null) throw new ArgumentNullException(nameof(request));

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

      if (request is null) throw new ArgumentNullException(nameof(request));

      return request.Item("X-Requested-With") == "XMLHttpRequest"
         || request.Headers["X-Requested-With"] == "XMLHttpRequest";
   }

   [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "The request parameter is no longer being used but we do not want to break legacy callers.")]
   [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "Response.Redirect() takes its URI as a string parameter.")]
   public static bool
   IsUrlLocalToHost(this HttpRequest request, string? url) {

      return !String.IsNullOrEmpty(url) &&
         ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) || // "/" or "/foo" but not "//" or "/\"
         (url.Length > 1 && url[0] == '~' && url[1] == '/')); // "~/" or "~/foo"
   }

   static string?
   Item(this HttpRequest request, string key) =>
      request.Query[key].ToString()
         ?? request.Form[key].ToString()
         ?? request.Cookies[key];
}
