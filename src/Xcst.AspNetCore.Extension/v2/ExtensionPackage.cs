// Copyright 2022 Max Toro Q.
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Xcst.Web.Extension;

public partial class ExtensionPackageV2 {

   public record CompileErrorData(int LineNumber, string ModuleUri);

   const string
   _tunnelParamPrefix = "xcsta_";

   public string
   ExtensionNamespace => a.NamespaceName;

   public static void
   IsPage(Action<string, object?> setFn, bool isPage) =>
      setFn.Invoke(_tunnelParamPrefix + "is_page", isPage);

   static object
   ErrorData(XObject node) =>
      new CompileErrorData(LineNumber(node), ModuleUri(node));

   static int
   LineNumber(XObject node) =>
      (node is IXmlLineInfo li) ?
         li.LineNumber
         : -1;

   static string
   ModuleUri(XObject node) =>
      node.Document?.BaseUri ?? node.BaseUri;

   string?
   PagePath(XElement module) {

      var moduleUri = new Uri(ModuleUri(module));
      var relativeUri = this.ApplicationUri!.MakeRelativeUri(moduleUri).OriginalString;
      var modulePath = (!relativeUri.StartsWith("..")) ? "/" + relativeUri : null;

      if (modulePath != null) {

         var pagePath = Path.ChangeExtension(modulePath, null);

         if (module.Attribute(a + "slug") is { } slugAttr
            && xcst_non_string(slugAttr) is { } slug) {

            var parts = pagePath.Split('/').ToList();
            parts[^1] = slug;

            return String.Join('/', parts);
         }

         return pagePath;
      }

      return null;
   }

   static string?
   DefaultPagePath(string pagePath) {

      const string defaultSegment = "/index";

      if (pagePath.EndsWith(defaultSegment, StringComparison.Ordinal)) {
         var multiPart = pagePath.Length > defaultSegment.Length;
         return pagePath.Substring(0, pagePath.Length - defaultSegment.Length + (multiPart ? 0 : 1));
      }

      return null;
   }
}
