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

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   /// <exclude/>
   public class ElementEndingDisposable : IDisposable {

      readonly XcstWriter
      _output;

      bool
      _disposed;

      public bool
      ElementStarted { get; }

      internal
      ElementEndingDisposable(XcstWriter output, bool elementStarted = true) {
         _output = output;
         this.ElementStarted = elementStarted;
      }

      public void
      Dispose() => Dispose(disposing: true);

      protected virtual void
      Dispose(bool disposing) {

         if (_disposed) {
            return;
         }

         if (disposing
            && this.ElementStarted) {

            _output.WriteEndElement();
         }

         _disposed = true;
      }
   }
}
