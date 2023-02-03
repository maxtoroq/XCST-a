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

using System;
using System.Linq;

namespace Xcst.Web.Runtime;

using UrlBuilder = UrlHelper.UrlBuilder;

/// <exclude/>
public static class LinkToHelper {

   public static string
   LinkTo(string path, params object?[]? pathParts) =>
      UrlUtil.GenerateClientUrl(null, path, pathParts);

   public static string
   LinkToDefault(string path, string defaultPath, params object?[]? pathParts) {

      if (pathParts is null
         || pathParts.Length == 0
         || pathParts.All(p => p is null || !UrlBuilder.IsDisplayableType(p.GetType()))) {

         return LinkTo(defaultPath, pathParts);
      }

      return LinkTo(path, pathParts);
   }
}
