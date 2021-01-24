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

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace System.Web.Mvc {

   sealed class EmptyHttpContext : HttpContext {

      public override IFeatureCollection
      Features => throw new NotImplementedException();

      public override HttpRequest
      Request => throw new NotImplementedException();

      public override HttpResponse
      Response => throw new NotImplementedException();

      public override ConnectionInfo
      Connection => throw new NotImplementedException();

      public override WebSocketManager
      WebSockets => throw new NotImplementedException();

      public override ClaimsPrincipal
      User {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override IDictionary<object, object?>
      Items {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override IServiceProvider
      RequestServices {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override CancellationToken
      RequestAborted {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override string
      TraceIdentifier {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override ISession
      Session {
         get => throw new NotImplementedException();
         set => throw new NotImplementedException();
      }

      public override void
      Abort() => throw new NotImplementedException();
   }
}
