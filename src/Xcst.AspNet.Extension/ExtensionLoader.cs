// Copyright 2019 Max Toro Q.
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
using System.Collections.Generic;
using System.IO;
using Xcst.Compiler;
using Xcst.Web;

[assembly: XcstExtension(XmlNamespaces.XcstApplication, typeof(Xcst.Web.Extension.ExtensionLoader))]

namespace Xcst.Web.Extension {

   partial class ExtensionLoader : XcstExtensionLoader {

      public bool DefaultModelDynamic { get; set; }

      public Uri? ApplicationUri { get; set; }

      public bool GenerateHref { get; set; }

      public bool GenerateLinkTo { get; set; }

      public bool AnnotateVirtualPath { get; set; }

      public override Stream LoadSource() {

         Type thisType = GetType();

         return thisType.Assembly
            .GetManifestResourceStream($"{thisType.Namespace}.xcst-app.xsl");
      }

      public override IEnumerable<KeyValuePair<string, object?>> GetParameters() {

#if ASPNETMVC
         yield return Param("aspnetmvc", true);
#endif
         yield return Param("make-relative-uri", new Func<Uri, Uri, Uri>(MakeRelativeUri));
         yield return Param("remove-extension", new Func<string, string>(RemoveExtension));

         if (this.ApplicationUri != null) {
            yield return Param("application-uri", this.ApplicationUri);
         }

         if (this.DefaultModelDynamic != default) {
            yield return Param("default-model-dynamic", this.DefaultModelDynamic);
         }

         if (this.GenerateHref != default) {
            yield return Param("generate-href", this.GenerateHref);
         }

         if (this.GenerateLinkTo != default) {
            yield return Param("generate-linkto", this.GenerateLinkTo);
         }

         if (this.AnnotateVirtualPath != default) {
            yield return Param("annotate-virtual-path", this.AnnotateVirtualPath);
         }
      }

      static KeyValuePair<string, object?> Param(string name, object? value) =>
         new KeyValuePair<string, object?>(name, value);

      static Uri MakeRelativeUri(Uri current, Uri compare) =>
         current.MakeRelativeUri(compare);

      static string RemoveExtension(string path) =>
         Path.ChangeExtension(path, null);
   }
}
