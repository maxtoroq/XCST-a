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
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class AntiforgeryInstructions {

   public static IDisposable
   HiddenToken(HttpContext httpContext, XcstWriter output) {

      var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
      var tokenSet = antiforgery.GetAndStoreTokens(httpContext);

      output.WriteStartElement("input");
      output.WriteAttributeString("type", "hidden");
      output.WriteAttributeString("name", tokenSet.FormFieldName);
      output.WriteAttributeString("value", tokenSet.RequestToken);

      return new ElementEndingDisposable(output);
   }
}
