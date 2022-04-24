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

using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Xcst;

namespace System.Web.Helpers {

   public static class AntiForgery {

      [EditorBrowsable(EditorBrowsableState.Never)]
      public static void
      GetHtml(HttpContext httpContext, XcstWriter output) {

         var antiforgery = GetAntiforgeryService(httpContext);
         var tokenSet = antiforgery.GetAndStoreTokens(httpContext);

         output.WriteStartElement("input");
         output.WriteAttributeString("type", "hidden");
         output.WriteAttributeString("name", tokenSet.FormFieldName);
         output.WriteAttributeString("value", tokenSet.RequestToken);
         output.WriteEndElement();
      }

      public async static Task<bool>
      TryValidateAsync(HttpContext httpContext) {

         var antiforgery = GetAntiforgeryService(httpContext);

         return await antiforgery.IsRequestValidAsync(httpContext);
      }

      static IAntiforgery
      GetAntiforgeryService(HttpContext httpContext) =>
         httpContext.RequestServices.GetService(typeof(IAntiforgery)) as IAntiforgery
            ?? throw new InvalidOperationException("IAntiforgery service is not available.");
   }
}
