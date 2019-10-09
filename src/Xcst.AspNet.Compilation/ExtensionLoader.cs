﻿// Copyright 2019 Max Toro Q.
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
using System.IO;
using Xcst.Compiler;
using Xcst.Web;

[assembly: XcstExtension(XmlNamespaces.XcstApplication, typeof(Xcst.Web.ExtensionLoader))]

namespace Xcst.Web {

   class ExtensionLoader : XcstExtensionLoader {

      public override Stream LoadSource() {

         return typeof(ExtensionLoader)
            .Assembly
            .GetManifestResourceStream($"{typeof(ExtensionLoader).Namespace}.xcst-app.xsl");
      }
   }
}
