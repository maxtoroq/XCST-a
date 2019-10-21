// Copyright 2015 Max Toro Q.
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

#region ExtensionlessUrlModule is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Xcst.Web.Compilation;

namespace Xcst.Web {

   public class ExtensionlessUrlModule : IHttpModule {

      static readonly object _hasBeenRegisteredKey = new object();
      static readonly string _supportedExtension = PageBuildProvider.FileExtension;

      public void Dispose() { }

      public void Init(HttpApplication application) {

         if (application.Context.Items[_hasBeenRegisteredKey] == null) {
            application.Context.Items[_hasBeenRegisteredKey] = true;
            application.PostResolveRequestCache += OnApplicationPostResolveRequestCache;
         }
      }

      static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {

         HttpContext context = ((HttpApplication)sender).Context;
         HttpRequest request = context.Request;

         // Parse incoming URL (we trim off the first two chars since they're always "~/")

         string requestPath = request.AppRelativeCurrentExecutionFilePath.Substring(2) + request.PathInfo;

         if (MatchRequest(requestPath, out string pagePath, out string pathInfo)) {

            if (Path.GetFileName(pagePath).StartsWith("_", StringComparison.OrdinalIgnoreCase)) {
               throw new HttpException(404, "Files with leading underscores (\"_\") cannot be served.");
            }

            string virtualPath = "~/" + pagePath;
            XcstPage page = CreateFromVirtualPath(virtualPath, pathInfo);

            if (page != null) {

               if (page is ISessionStateAware sess) {
                  context.SetSessionStateBehavior(sess.SessionStateBehavior);
               }

               context.RemapHandler(page.CreateHttpHandler());
            }

         } else {

            // If its not a match, but to a supported extension, we want to return a 404 instead of a 403

            string extension = PathExtension(requestPath);

            if (String.Equals("." + _supportedExtension, extension, StringComparison.OrdinalIgnoreCase)) {
               throw new HttpException(404, null);
            }
         }
      }

      static XcstPage CreateFromVirtualPath(string virtualPath, string pathInfo) {

         if (virtualPath == null) throw new ArgumentNullException(nameof(virtualPath));

         var page = (XcstPage)BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(XcstPage));

         if (page != null) {
            page.VirtualPath = virtualPath;
            page.PathInfo = pathInfo;
         }

         return page;
      }

      /// <summary>
      /// Path.GetExtension performs a CheckInvalidPathChars(path) which blows up for paths that do not translate to valid physical paths but are valid paths in ASP.NET
      /// This method is a near clone of Path.GetExtension without a call to CheckInvalidPathChars(path);
      /// </summary>

      static string PathExtension(string path) {

         if (String.IsNullOrEmpty(path)) {
            return path;
         }

         int current = path.Length;

         while (--current >= 0) {

            char ch = path[current];

            if (ch == '.') {

               if (current == path.Length - 1) {
                  break;
               }

               return path.Substring(current);
            }

            if (ch == Path.DirectorySeparatorChar
               || ch == Path.AltDirectorySeparatorChar) {

               break;
            }
         }

         return String.Empty;
      }

      static bool MatchRequest(string requestPath, out string pagePath, out string pathInfo) {

         Debug.Assert(requestPath != null);
         Debug.Assert(!requestPath.StartsWith("~/"));

         // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages

         if (!String.IsNullOrEmpty(requestPath)) {

            // If the file exists and its not a supported extension, let the request go through
            // TODO: Look into switching to RawURL to eliminate the need for this issue

            if (FileExists(requestPath)
               && !PathEndsWithExtension(requestPath, _supportedExtension)) {

               pagePath = null;
               pathInfo = null;
               return false;
            }

            // For each trimmed part of the path try to add a known extension and
            // check if it matches a file in the application.

            string currentLevel = requestPath;
            string currentPathInfo = String.Empty;

            while (true) {

               // Does the current route level patch any supported extension?

               string routeLevelMatch = GetRouteLevelMatch(currentLevel);

               if (routeLevelMatch != null) {

                  pagePath = routeLevelMatch;
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

      static string GetRouteLevelMatch(string pathValue) {

         // For performance, avoid multiple calls to String.Concat
         // Only add the extension if it's not already there

         if (!PathEndsWithExtension(pathValue, _supportedExtension)) {
            pathValue = pathValue + "." + _supportedExtension;
         }

         if (FileExists(pathValue)) {
            return pathValue;
         }

         return null;
      }

      static bool MatchDefaultFile(string requestPath, out string pagePath) {

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

         // Does the current route level match any supported extension?

         string indexMatch = GetRouteLevelMatch(currentLevelIndex);

         if (indexMatch != null) {

            pagePath = indexMatch;
            return true;
         }

         pagePath = null;
         return false;
      }

      static bool FileExists(string path) {

         string virtualPath = "~/" + path;

         return HostingEnvironment.VirtualPathProvider.FileExists(virtualPath);
      }

      static bool PathEndsWithExtension(string path, string extension) {

         Debug.Assert(path != null);
         Debug.Assert(extension != null && extension.Length > 0);

         if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) {

            int extensionLength = extension.Length;
            int pathLength = path.Length;

            return (pathLength > extensionLength && path[pathLength - extensionLength - 1] == '.');
         }

         return false;
      }
   }
}
