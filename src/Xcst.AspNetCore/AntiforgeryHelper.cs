// Copyright 2023 Max Toro Q.
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web;

public class AntiforgeryHelper {

   readonly Func<HttpContext>
   _httpContextFn;

   readonly IAntiforgery
   _antiforgery;

   public
   AntiforgeryHelper(Func<HttpContext> httpContextFn) {

      ArgumentNullException.ThrowIfNull(httpContextFn);

      _httpContextFn = httpContextFn;

      _antiforgery = httpContextFn.Invoke()
         .RequestServices
         .GetRequiredService<IAntiforgery>();
   }

   public AntiforgeryTokenSet
   GetAndStoreTokens() =>
      _antiforgery.GetAndStoreTokens(_httpContextFn.Invoke());

   public AntiforgeryTokenSet
   GetTokens() =>
      _antiforgery.GetTokens(_httpContextFn.Invoke());

   public Task<bool>
   IsRequestValidAsync() =>
      _antiforgery.IsRequestValidAsync(_httpContextFn.Invoke());

   public Task
   ValidateRequestAsync() =>
      _antiforgery.ValidateRequestAsync(_httpContextFn.Invoke());

   public void
   SetCookieTokenAndHeader() =>
      _antiforgery.SetCookieTokenAndHeader(_httpContextFn.Invoke());
}
