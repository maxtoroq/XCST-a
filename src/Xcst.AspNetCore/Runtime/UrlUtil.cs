﻿// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#region UrlUtil is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using Microsoft.AspNetCore.Http;
using UrlBuilder = System.Web.Mvc.UrlHelper.UrlBuilder;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class UrlUtil {

   // Code generation uses static method for Href function,
   // therefore HttpContext cannot be provided dynamically

   public static Func<HttpContext>?
   CurrentHttpContext { get; set; }

   public static string
   GenerateClientUrl(string? basePath, string path, params object?[]? pathParts) =>
      GenerateClientUrl(CurrentHttpContext?.Invoke(), basePath, path, pathParts);

   static string
   GenerateClientUrl(HttpContext? httpContext, string? basePath, string path, params object?[]? pathParts) {

      if (String.IsNullOrEmpty(path)) {
         return path;
      }

      if (basePath != null) {
         path = new PathString(basePath).Add(path).Value!;
      }

      string query;
      var processedPath = UrlBuilder.BuildUrl(path, out query, pathParts);

      // many of the methods we call internally can't handle query strings properly, so tack it on after processing
      // the virtual app path and url rewrites

      if (String.IsNullOrEmpty(query)) {
         return GenerateClientUrlInternal(httpContext, processedPath);
      } else {
         return GenerateClientUrlInternal(httpContext, processedPath) + query;
      }
   }

   static string
   GenerateClientUrlInternal(HttpContext? httpContext, string contentPath) {

      if (String.IsNullOrEmpty(contentPath)) {
         return contentPath;
      }

      // can't call VirtualPathUtility.IsAppRelative since it throws on some inputs

      var isAppRelative = contentPath[0] == '~';

      if (isAppRelative) {

         var context = httpContext
            ?? throw new ArgumentException("httpContext cannot be null for app-relative paths.", nameof(httpContext));

         // See also Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase.Content
         var other = new PathString(contentPath.Substring(1));
         return context.Request.PathBase.Add(other).Value!;
      }

      return contentPath;
   }
}
