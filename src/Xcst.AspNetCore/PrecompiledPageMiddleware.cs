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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web;

class PrecompiledPageMiddleware {

   readonly RequestDelegate
   _next;

   readonly Assembly[]
   _appModules;

   readonly Lazy<Dictionary<string, Type>>
   _pageMap;

   public
   PrecompiledPageMiddleware(RequestDelegate next, Assembly[] appModules) {
      _next = next;
      _appModules = appModules;
      _pageMap = new(InitializePageMap);
   }

   Dictionary<string, Type>
   InitializePageMap() {

      var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

      var pairs =
         from a in _appModules
         from t in a.GetTypes()
         where t.IsDefined(typeof(PageVirtualPathAttribute))
         select new KeyValuePair<string, Type>(
            t.GetCustomAttribute<PageVirtualPathAttribute>()!.VirtualPath,
            t);

      foreach (var item in pairs) {

         if (map.TryGetValue(item.Key, out var prevType)) {
            throw new InvalidOperationException($"Ambiguous path '{item.Key}', is used by '{prevType.AssemblyQualifiedName}' and '{item.Value.AssemblyQualifiedName}'.");
         }

         map.Add(item.Key, item.Value);
      }

      return map;
   }

   bool
   PageExists(string pagePath) =>
      _pageMap.Value.ContainsKey(pagePath);

   Type
   PageType(string pagePath) =>
      _pageMap.Value[pagePath];

   static XcstPage
   CreatePage(Type pageType, IServiceProvider serviceProvider) =>
      (XcstPage)ActivatorUtilities.CreateInstance(serviceProvider, pageType);

   public async Task
   Invoke(HttpContext context) {

      var request = context.Request;
      var requestPath = request.Path.Value!.Substring(1);

      if (MatchRequest(requestPath, out var pagePath, out var pathInfo)) {

         var page = CreatePage(PageType(pagePath), context.RequestServices);
         page.VirtualPath = "~/" + pagePath;
         page.PathInfo = pathInfo;
         page.HttpContext = context;

         await page.RenderPageAsync();
         return;
      }

      await _next.Invoke(context);
   }

   bool
   MatchRequest(string requestPath,
         [NotNullWhen(returnValue: true)] out string? pagePath,
         [NotNullWhen(returnValue: true)] out string? pathInfo) {

      Debug.Assert(requestPath != null);
      Debug.Assert(!requestPath.StartsWith("~/"));

      // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages

      if (!String.IsNullOrEmpty(requestPath)) {

         // For each trimmed part of the path try to add a known extension and
         // check if it matches a file in the application.

         var currentLevel = requestPath;
         var currentPathInfo = String.Empty;

         while (true) {

            // Does the current route level patch any supported extension?

            if (PageExists(currentLevel)) {

               pagePath = currentLevel;
               pathInfo = currentPathInfo;
               return true;
            }

            // Try to remove the last path segment (e.g. go from /foo/bar to /foo)

            var indexOfLastSlash = currentLevel.LastIndexOf('/');

            if (indexOfLastSlash == -1) {

               // If there are no more slashes, we're done

               break;

            } else {

               // Chop off the last path segment to get to the next one

               currentLevel = currentLevel.Substring(0, indexOfLastSlash);

               // And save the path info in case there is a match

               currentPathInfo = requestPath.Substring(indexOfLastSlash + 1);
            }
         }
      }

      // If we haven't found anything yet, now try looking for index.* at the current url

      if (MatchDefaultFile(requestPath, out pagePath)) {

         pathInfo = String.Empty;
         return true;
      }

      pagePath = null;
      pathInfo = null;
      return false;
   }

   bool
   MatchDefaultFile(string requestPath, [NotNullWhen(returnValue: true)] out string? pagePath) {

      const string defaultDocument = "index";

      var currentLevel = requestPath;
      string currentLevelIndex;

      if (String.IsNullOrEmpty(currentLevel)) {
         currentLevelIndex = defaultDocument;
      } else {

         if (currentLevel[currentLevel.Length - 1] != '/') {
            currentLevel += "/";
         }

         currentLevelIndex = currentLevel + defaultDocument;
      }

      if (PageExists(currentLevelIndex)) {

         pagePath = currentLevelIndex;
         return true;
      }

      pagePath = null;
      return false;
   }
}
