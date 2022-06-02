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

using System.Xml;
using System.Xml.Linq;

namespace Xcst.Web.Extension;

public partial class ExtensionPackage {

   public string
   ExtensionNamespace => a.NamespaceName;

   static object
   ErrorData(XObject node) {

      dynamic data = new System.Dynamic.ExpandoObject();
      data.LineNumber = LineNumber(node);
      data.ModuleUri = ModuleUri(node);

      return data;
   }

   static int
   LineNumber(XObject node) =>
      (node is IXmlLineInfo li) ?
         li.LineNumber
         : -1;

   static string
   ModuleUri(XObject node) =>
      node.Document?.BaseUri ?? node.BaseUri;

   string?
   AppRelativeUri(XElement module) {

      var moduleUri = new System.Uri(ModuleUri(module));
      var relativeUri = this.ApplicationUri!.MakeRelativeUri(moduleUri);

      if (!relativeUri.OriginalString.StartsWith("..")) {
         return relativeUri.OriginalString;
      }

      return null;
   }
}
