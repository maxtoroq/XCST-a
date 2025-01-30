// Copyright 2025 Max Toro Q.
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

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [EditorBrowsable(EditorBrowsableState.Never)]
   public class DefaultContentDisposable : ElementEndingDisposable {

      readonly XcstWriter
      _output;

      readonly Action<XcstWriter>?
      _contentFn;

      bool
      _eoc;

      bool
      _disposed;

      internal
      DefaultContentDisposable(XcstWriter output, bool elementStarted, Action<XcstWriter>? contentFn)
         : base(output, elementStarted) {

         _output = output;
         _contentFn = contentFn;
      }

      [GeneratedCodeReference]
      public void
      EndOfConstructor() {
         _eoc = true;
      }

      [GeneratedCodeReference]
      public DefaultContentDisposable
      NoConstructor() {
         _eoc = this.ElementStarted;
         return this;
      }

      protected override void
      Dispose(bool disposing) {

         if (_disposed) {
            return;
         }

         // don't write content when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (disposing
            && _eoc
            && _contentFn != null) {

            _contentFn.Invoke(_output);
         }

         base.Dispose(disposing);

         _disposed = true;
      }
   }
}
