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
using TempDataSerializer = Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer;

namespace System.Web.Mvc {

   public class SessionStateTempDataProvider : ITempDataProvider {

      internal const string
      TempDataSessionStateKey = "__ControllerTempData";

      readonly TempDataSerializer
      _tempDataSerializer =
         (TempDataSerializer)Activator.CreateInstance(typeof(TempDataSerializer).Assembly
            .GetType("Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.DefaultTempDataSerializer", throwOnError: true)!)!;

      public virtual IDictionary<string, object?>
      LoadTempData(ControllerContext controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         // Accessing Session property will throw if the session middleware is not enabled.
         var session = controllerContext.HttpContext.Session;

         if (session.TryGetValue(TempDataSessionStateKey, out var value)) {
            // If we got it from Session, remove it so that no other request gets it
            session.Remove(TempDataSessionStateKey);

            return _tempDataSerializer.Deserialize(value);
         }

         return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      }

      public virtual void
      SaveTempData(ControllerContext controllerContext, IDictionary<string, object?> values) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         // Accessing Session property will throw if the session middleware is not enabled.
         var session = controllerContext.HttpContext.Session;
         var hasValues = (values != null && values.Count > 0);

         if (hasValues) {
            var bytes = _tempDataSerializer.Serialize(values);
            session.Set(TempDataSessionStateKey, bytes);
         } else {
            session.Remove(TempDataSessionStateKey);
         }
      }
   }
}
