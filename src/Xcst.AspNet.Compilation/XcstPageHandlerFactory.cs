﻿// Copyright 2016 Max Toro Q.
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

using System;
using System.Web;
using System.Web.Compilation;

namespace Xcst.Web {

   public class XcstPageHandlerFactory : IHttpHandlerFactory {

      public IHttpHandler? GetHandler(HttpContext context, string requestType, string url, string pathTranslated) {

         if (url is null) throw new ArgumentNullException(nameof(url));

         string virtualPath = url;
         var page = (XcstPage?)BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(XcstPage));

         if (page != null) {
            page.VirtualPath = virtualPath;
         }

         return page?.CreateHttpHandler();
      }

      public void ReleaseHandler(IHttpHandler handler) { }
   }
}
