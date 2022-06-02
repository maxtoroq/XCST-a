// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace System.Web.Mvc;

static class UrlHelperImpl {

   // this method can accept an app-relative path or an absolute path for contentPath
   public static string
   GenerateClientUrl(HttpContext httpContext, string contentPath) {

      if (String.IsNullOrEmpty(contentPath)) {
         return contentPath;
      }

      // many of the methods we call internally can't handle query strings properly, so just strip it out for
      // the time being

      contentPath = StripQuery(contentPath, out var query);

      // many of the methods we call internally can't handle query strings properly, so tack it on after processing
      // the virtual app path and url rewrites

      if (String.IsNullOrEmpty(query)) {
         return GenerateClientUrlInternal(httpContext, contentPath);
      } else {
         return GenerateClientUrlInternal(httpContext, contentPath) + query;
      }
   }

   internal static string
   GenerateClientUrlInternal(HttpContext httpContext, string contentPath) {

      if (String.IsNullOrEmpty(contentPath)) {
         return contentPath;
      }

      var isAppRelative = contentPath[0] == '~';

      if (isAppRelative) {

         // See also Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase.Content
         var other = new PathString(contentPath.Substring(1));
         return httpContext.Request.PathBase.Add(other).Value!;
      }

      return contentPath;
   }

   static string
   StripQuery(string path, out string? query) {

      var queryIndex = path.IndexOf('?');

      if (queryIndex >= 0) {
         query = path.Substring(queryIndex);
         return path.Substring(0, queryIndex);
      } else {
         query = null;
         return path;
      }
   }
}
