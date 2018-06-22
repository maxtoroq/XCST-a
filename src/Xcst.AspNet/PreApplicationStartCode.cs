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

using System;
using System.ComponentModel;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Mvc;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Xcst.Compiler;
using Xcst.Web.Compilation;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;

namespace Xcst.Web {

   /// <exclude/>

   [EditorBrowsable(EditorBrowsableState.Never)]
   public static class PreApplicationStartCode {

      static bool startWasCalled;

      public static void Start() {

         if (!startWasCalled) {

            startWasCalled = true;

            System.Web.Mvc.PreApplicationStartCode.Start();

            XcstWebConfiguration config = XcstWebConfiguration.Instance;

            BuildProvider.RegisterBuildProvider("." + XcstWebConfiguration.FileExtension, typeof(PageBuildProvider<XcstViewPage>));
            ViewEngines.Engines.Add(new XcstViewEngine());

#if ASPNETLIB
            config.RegisterHandlerFactory(XcstPageHttpHandler.Create);
            config.RegisterHandlerFactory(XcstViewPageHttpHandler.Create);
            DynamicModuleUtility.RegisterModule(typeof(XcstPageHttpModule));
#endif
         }
      }
   }
}
