// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace System.Web.Mvc {

   public static class HttpRequestExtensions {

      internal const string XHttpMethodOverrideKey = "X-HTTP-Method-Override";

      public static string GetHttpMethodOverride(this HttpRequestBase request) {

         if (request is null) throw new ArgumentNullException(nameof(request));

         string incomingVerb = request.HttpMethod;

         if (!String.Equals(incomingVerb, "POST", StringComparison.OrdinalIgnoreCase)) {
            return incomingVerb;
         }

         string verbOverride = null;
         string headerOverrideValue = request.Headers[XHttpMethodOverrideKey];

         if (!String.IsNullOrEmpty(headerOverrideValue)) {
            verbOverride = headerOverrideValue;
         } else {

            string formOverrideValue = request.Form[XHttpMethodOverrideKey];

            if (!String.IsNullOrEmpty(formOverrideValue)) {
               verbOverride = formOverrideValue;
            } else {

               string queryStringOverrideValue = request.QueryString[XHttpMethodOverrideKey];

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

      public static bool IsAjaxRequest(this HttpRequestBase request) {

         if (request is null) throw new ArgumentNullException(nameof(request));

         return request["X-Requested-With"] == "XMLHttpRequest"
            || (request.Headers != null
               && request.Headers["X-Requested-With"] == "XMLHttpRequest");
      }

      [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "The request parameter is no longer being used but we do not want to break legacy callers.")]
      [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "1#", Justification = "Response.Redirect() takes its URI as a string parameter.")]
      public static bool IsUrlLocalToHost(this HttpRequestBase request, string url) =>
         !String.IsNullOrEmpty(url)
            && ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) // "/" or "/foo" but not "//" or "/\"
               || (url.Length > 1 && url[0] == '~' && url[1] == '/')); // "~/" or "~/foo"
   }
}
