// Copyright 2021 Max Toro Q.
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
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Xcst.Web.Configuration;
using Xcst.Web.Precompilation;

namespace Xcst.Web.Builder;

public static class PrecompiledPageBuilderExtensions {

   public static IApplicationBuilder
   UseXcstPrecompiledPages(this IApplicationBuilder app, Assembly[] appModules) {

      if (app is null) throw new ArgumentNullException(nameof(app));
      if (appModules is null) throw new ArgumentNullException(nameof(appModules));

      app.UseMiddleware<PrecompiledPageMiddleware>((object)appModules);

      return app;
   }

   public static IApplicationBuilder
   UseXcstPrecompiledPages(this IApplicationBuilder app, Assembly[] appModules, Action<XcstWebConfiguration> config) {

      if (config is null) throw new ArgumentNullException(nameof(config));

      UseXcstPrecompiledPages(app, appModules);

      config(XcstWebConfiguration.Instance);

      return app;
   }
}
