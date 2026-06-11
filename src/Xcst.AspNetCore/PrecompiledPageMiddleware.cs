// Copyright 2020 Max Toro Q.
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

#region PrecompiledPageMiddleware is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web;

sealed class PrecompiledPageMiddleware {

   readonly RequestDelegate
   _next;

   readonly Lazy<FrozenDictionary<string, Type>>
   _pageMap;

   public
   PrecompiledPageMiddleware(RequestDelegate next, IEnumerable<Assembly> appModules) {

      _next = next;
      _pageMap = new(() => {

         var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

         var pairs =
            from a in appModules
            from t in a.GetTypes()
            where t.IsDefined(typeof(PageVirtualPathAttribute))
            select new KeyValuePair<string, Type>(
               t.GetCustomAttribute<PageVirtualPathAttribute>()!.VirtualPath,
               t);

         foreach (var item in pairs) {

            if (!map.TryAdd(item.Key, item.Value)) {
               throw new InvalidOperationException(
                  $"Ambiguous path '{item.Key}', is used by '{map[item.Key].AssemblyQualifiedName}'" +
                  $" and '{item.Value.AssemblyQualifiedName}'.");
            }
         }

         return map.ToFrozenDictionary(map.Comparer);
      });
   }

   bool
   TryGetPageType(string pagePath, [NotNullWhen(returnValue: true)] out Type? pageType) =>
      _pageMap.Value.TryGetValue(pagePath, out pageType);

   static XcstPage
   CreatePage(Type pageType, IServiceProvider serviceProvider) =>
      (XcstPage)ActivatorUtilities.CreateInstance(serviceProvider, pageType);

   public async Task
   Invoke(HttpContext context) {

      if (context.Request.Path.Value is { Length: > 0 } requestPath
         && MatchRequest(requestPath, out var pagePath, out var pathInfo, out var pageType)) {

         var page = CreatePage(pageType, context.RequestServices);
         page.Contextualize(context, pagePath, pathInfo);

         await page.RenderPageAsync();
         return;
      }

      await _next.Invoke(context);
   }

   bool
   MatchRequest(string requestPath,
         [NotNullWhen(returnValue: true)] out string? pagePath,
         [NotNullWhen(returnValue: true)] out string? pathInfo,
         [NotNullWhen(returnValue: true)] out Type? pageType) {

      Debug.Assert(requestPath.StartsWith('/'));

      if (requestPath.Length > 1) {

         var currentLevel = requestPath;
         var currentPathInfo = String.Empty;

         while (true) {

            if (TryGetPageType(currentLevel, out pageType)) {
               pagePath = currentLevel;
               pathInfo = currentPathInfo;
               return true;
            }

            // Try to remove the last path segment (e.g. go from /foo/bar to /foo)
            var indexOfLastSlash = currentLevel.LastIndexOf('/');

            if (indexOfLastSlash <= 0) {
               break;
            }

            // Chop off the last path segment to get to the next one
            currentLevel = currentLevel.Substring(0, indexOfLastSlash);

            // And save the path info in case there is a match
            currentPathInfo = requestPath.Substring(indexOfLastSlash + 1);
         }
      }

      // If we haven't found anything yet, now try looking for index.* at the current url

      const string defaultDoc = "index";

      var defaultPath = (requestPath.Length == 1) ? $"/{defaultDoc}" // avoid concat
         : (requestPath[^1] == '/') ? requestPath + defaultDoc
         : $"{requestPath}/{defaultDoc}";

      if (TryGetPageType(defaultPath, out pageType)) {
         pagePath = defaultPath;
         pathInfo = String.Empty;
         return true;
      }

      pagePath = null;
      pathInfo = null;
      return false;
   }
}
