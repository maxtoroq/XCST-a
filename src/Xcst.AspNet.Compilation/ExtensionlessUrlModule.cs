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

#region ExtensionlessUrlModule/PageRouter is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Hosting;
using Xcst.Web.Configuration;

namespace Xcst.Web {

   public class ExtensionlessUrlModule : IHttpModule {

      static readonly object _hasBeenRegisteredKey = new object();

      public void Dispose() { }

      public void Init(HttpApplication application) {

         if (application.Context.Items[_hasBeenRegisteredKey] != null) {
            // registration for this module has already run for this HttpApplication instance
            return;
         }

         application.Context.Items[_hasBeenRegisteredKey] = true;

         InitApplication(application);
      }

      static void InitApplication(HttpApplication application) {
         application.PostResolveRequestCache += OnApplicationPostResolveRequestCache;
      }

      static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {

         HttpContextBase context = new HttpContextWrapper(((HttpApplication)sender).Context);
         HttpRequestBase request = context.Request;

         // Parse incoming URL (we trim off the first two chars since they're always "~/")

         string requestPath = request.AppRelativeCurrentExecutionFilePath.Substring(2) + request.PathInfo;

         string[] pageMatch = PageRouter.MatchRequest(requestPath, HostingEnvironment.VirtualPathProvider.FileExists);

         if (pageMatch == null) {

            // If its not a match, but to a supported extension, we want to return a 404 instead of a 403

            string extension = PathExtension(requestPath);

            if (String.Equals("." + PageRouter.supportedExtension, extension, StringComparison.OrdinalIgnoreCase)) {
               throw new HttpException(404, null);
            }

            return;
         }

         string virtualPath = "~/" + pageMatch[0];
         string pathInfo = pageMatch[1];

         if (Path.GetFileName(virtualPath).StartsWith("_", StringComparison.OrdinalIgnoreCase)) {
            throw new HttpException(404, "Files with leading underscores (\"_\") cannot be served.");
         }

         IHttpHandler handler = XcstPageHandlerFactory.CreateFromVirtualPath(virtualPath, pathInfo);

         if (handler != null) {

            (handler as XcstPageHandler)?.SetUpSessionState(context);

            context.RemapHandler(handler);
         }
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
   }

   static class PageRouter {

      internal static readonly string supportedExtension = XcstWebConfiguration.FileExtension;
      static readonly string defaultDocument = "index";

      public static string[] MatchRequest(string pathValue, Func<string, bool> virtualPathExists) {

         Debug.Assert(pathValue != null);
         Debug.Assert(!pathValue.StartsWith("~/"));

         string currentLevel = String.Empty;
         string currentPathInfo = pathValue;

         // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages

         if (!String.IsNullOrEmpty(pathValue)) {

            // If the file exists and its not a supported extension, let the request go through
            // TODO: Look into switching to RawURL to eliminate the need for this issue

            if (FileExists(pathValue, virtualPathExists)
               && !PathEndsWithExtension(pathValue, supportedExtension)) {

               return null;
            }

            // For each trimmed part of the path try to add a known extension and
            // check if it matches a file in the application.

            currentLevel = pathValue;
            currentPathInfo = String.Empty;

            while (true) {

               // Does the current route level patch any supported extension?

               string routeLevelMatch = GetRouteLevelMatch(currentLevel, virtualPathExists);

               if (routeLevelMatch != null) {
                  return new string[2] { routeLevelMatch, currentPathInfo };
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

                  currentPathInfo = pathValue.Substring(indexOfLastSlash + 1);
               }
            }
         }

         return MatchDefaultFiles(pathValue, virtualPathExists, currentLevel);
      }

      static string GetRouteLevelMatch(string pathValue, Func<string, bool> virtualPathExists) {

         // For performance, avoid multiple calls to String.Concat
         // Only add the extension if it's not already there

         if (!PathEndsWithExtension(pathValue, supportedExtension)) {
            pathValue = pathValue + "." + supportedExtension;
         }

         if (!FileExists(pathValue, virtualPathExists)) {
            return null;
         }

         return pathValue;
      }

      static string[] MatchDefaultFiles(string pathValue, Func<string, bool> virtualPathExists, string currentLevel) {

         // If we haven't found anything yet, now try looking for index.* at the current url

         currentLevel = pathValue;
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

         string indexMatch = GetRouteLevelMatch(currentLevelIndex, virtualPathExists);

         if (indexMatch != null) {
            return new string[2] { indexMatch, String.Empty };
         }

         return null;
      }

      static bool FileExists(string path, Func<string, bool> virtualPathExists) {

         string virtualPath = "~/" + path;

         return virtualPathExists(virtualPath);
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
