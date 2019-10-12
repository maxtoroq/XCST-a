// Copyright 2016 Max Toro Q.
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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Compilation;
using Xcst.Web.Mvc;

namespace Xcst.Web {

   public class XcstPageHandlerFactory : IHttpHandlerFactory {

      static IList<Func<object, IHttpHandler>> HttpHandlerFactories { get; } = new List<Func<object, IHttpHandler>> {
         CreateBuiltInHandler
      };

      static IHttpHandler CreateBuiltInHandler(object instance) {

         XcstViewPage viewPage = instance as XcstViewPage;

         if (instance != null) {
            return new XcstViewPageHandler(viewPage);
         }

         XcstPage page = instance as XcstPage;

         if (instance != null) {
            return new XcstPageHandler(page);
         }

         return null;
      }

      public static void RegisterHandlerFactory(Func<object, IHttpHandler> handlerFactory) {
         HttpHandlerFactories.Insert(0, handlerFactory);
      }

      public static IHttpHandler CreateFromVirtualPath(string virtualPath, string pathInfo = null) {

         object instance = BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(object));

         if (instance == null) {
            return null;
         }

         if (instance is XcstPage page) {
            page.VirtualPath = virtualPath;
            page.PathInfo = pathInfo;
         }

         return HttpHandlerFactories
            .Select(f => f(instance))
            .Where(p => p != null)
            .FirstOrDefault()
            ?? instance as IHttpHandler;
      }

      public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated) {
         return CreateFromVirtualPath(url);
      }

      public void ReleaseHandler(IHttpHandler handler) { }
   }
}
