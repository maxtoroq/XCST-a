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
   public class SiblingContentDisposable : ElementEndingDisposable {

      readonly Action
      _siblingContentFn;

      bool
      _eoc;

      [GeneratedCodeReference]
      public XcstWriter
      ElementOutput { get; }

      public
      SiblingContentDisposable(XcstWriter output, Action siblingContentFn)
         : base(output, elementStarted: true) {

         _siblingContentFn = siblingContentFn;

         this.ElementOutput = output;
      }

      [GeneratedCodeReference]
      public void
      EndOfConstructor() {
         _eoc = true;
      }

      [GeneratedCodeReference]
      public SiblingContentDisposable
      NoConstructor() {
         _eoc = true;
         return this;
      }

      protected override void
      Dispose(bool disposing) {

         base.Dispose(disposing);

         // don't write hidden input when end of constructor is not reached
         // e.g. an exception occurred, c:return, etc.

         if (disposing
            && _eoc) {

            _siblingContentFn.Invoke();
         }
      }
   }
}
