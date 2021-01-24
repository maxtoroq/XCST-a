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

#region PrecompiledPageModule is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;

namespace Xcst.Web.Precompilation {

   public class PrecompiledPageModule : IHttpModule {

      static readonly object _hasBeenRegisteredKey = new object();
      static readonly Lazy<Dictionary<string, Type>> _pageMap = new Lazy<Dictionary<string, Type>>(InitializePageMap);

      static Dictionary<string, Type> InitializePageMap() {

         var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

         var pairs =
            from a in BuildManager.GetReferencedAssemblies().Cast<Assembly>()
            where a.IsDefined(typeof(PrecompiledModuleAttribute))
            from t in a.GetTypes()
            where t.IsDefined(typeof(PageVirtualPathAttribute))
            select new KeyValuePair<string, Type>(
               t.GetCustomAttribute<PageVirtualPathAttribute>().VirtualPath,
               t);

         foreach (var item in pairs) {

            if (map.ContainsKey(item.Key)) {
               throw new InvalidOperationException($"Ambiguous path '{item.Key}', is used by '{map[item.Key].AssemblyQualifiedName}' and '{item.Value.AssemblyQualifiedName}'.");
            }

            map.Add(item.Key, item.Value);
         }

         return map;
      }

      static bool PageExists(string pagePath) =>
         _pageMap.Value.ContainsKey(pagePath);

      static Type PageType(string pagePath) =>
         _pageMap.Value[pagePath];

      public void Dispose() { }

      public void Init(HttpApplication application) {

         if (application.Context.Items[_hasBeenRegisteredKey] is null) {
            application.Context.Items[_hasBeenRegisteredKey] = true;
            application.PostResolveRequestCache += OnApplicationPostResolveRequestCache;
         }
      }

      static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {

         HttpContext context = ((HttpApplication)sender).Context;
         HttpRequest request = context.Request;

         // Parse incoming URL (we trim off the first two chars since they're always "~/")

         string requestPath = request.AppRelativeCurrentExecutionFilePath.Substring(2) + request.PathInfo;

         if (MatchRequest(requestPath, out string? pagePath, out string? pathInfo)) {

            var page = (XcstPage)Activator.CreateInstance(PageType(pagePath));
            page.VirtualPath = "~/" + pagePath;
            page.PathInfo = pathInfo;

            if (page is ISessionStateAware sess) {
               context.SetSessionStateBehavior(sess.SessionStateBehavior);
            }

            IHttpHandler handler = page.CreateHttpHandler();

            context.RemapHandler(handler);

         } else {
            // let the next module try to find it
         }
      }

      static bool MatchRequest(string requestPath, [NotNullWhen(true)] out string? pagePath, out string? pathInfo) {

         Assert.IsNotNull(requestPath);
         Debug.Assert(!requestPath.StartsWith("~/"));

         // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages

         if (!String.IsNullOrEmpty(requestPath)) {

            // For each trimmed part of the path try to add a known extension and
            // check if it matches a file in the application.

            string currentLevel = requestPath;
            string currentPathInfo = String.Empty;

            while (true) {

               // Does the current route level patch any supported extension?

               if (PageExists(currentLevel)) {

                  pagePath = currentLevel;
                  pathInfo = currentPathInfo;
                  return true;
               }

               // Try to remove the last path segment (e.g. go from /foo/bar to /foo)

               int indexOfLastSlash = currentLevel.LastIndexOf('/');

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

      static bool MatchDefaultFile(string requestPath, [NotNullWhen(true)] out string? pagePath) {

         const string defaultDocument = "index";

         string currentLevel = requestPath;
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
}
