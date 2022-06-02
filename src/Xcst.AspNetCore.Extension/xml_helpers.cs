// Copyright 2021 Max Toro Q.
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Xcst.Runtime;

namespace Xcst.Web.Extension;

partial class ExtensionPackage {

   static IEnumerable<XAttribute>
   attributes(XElement node) =>
      node.Attributes()
         .Where(p => !p.IsNamespaceDeclaration);

   static bool
   fn_empty<T>(T[] p) => p.Length == 0;

   static bool
   fn_empty<T>(IEnumerable<T> p) => !p.Any();

   static string
   fn_name(XObject node) =>
      node switch {
         XAttribute a => fn_substring_before(a.ToString(), '='),
         XElement el => fn_string(el.Name, el),
         XProcessingInstruction pi => pi.Target,
         _ => throw new System.NotImplementedException()
      };

   static IEnumerable<XElement>
   select(IEnumerable<XElement> nodes, params object[] names) =>
      nodes.SelectMany(p => select(p, names));

   static IEnumerable<XElement>
   select(XElement? node, params object[] names) {

      if (node is null) {
         return Enumerable.Empty<XElement>();
      }

      IEnumerable<XElement> selected = new XElement[] { node };

      for (int i = 0; i < names.Length; i++) {

         selected = names[i] switch {
            XName name => selected.SelectMany(p => p.Elements(name)),
            XNamespace ns => selected.SelectMany(p => p.Elements().Where(p2 => p2.Name.Namespace == ns)),
            _ => throw new System.ArgumentOutOfRangeException(),
         };
      }

      return selected;
   }

   static string
   fn_string(bool value) =>
      (value) ? "true" : "false";

   static string
   fn_string(int value) => XmlConvert.ToString(value);

   static string
   fn_string(decimal value) => XmlConvert.ToString(value);

   static string
   fn_string(XName qname, XElement? context) {

      if (context is null) {
         return qname.LocalName;
      }

      var prefix = context.GetPrefixOfNamespace(qname.Namespace);

      if (prefix != null) {
         return prefix + ":" + qname.LocalName;
      }

      return qname.LocalName;
   }

   static string
   fn_string(XObject node) =>
      node switch {
         XAttribute a => a.Value,
         XElement el => el.Value,
         XProcessingInstruction pi => pi.Data,
         _ => throw new System.NotImplementedException()
      };

   static string
   fn_substring_after(string str, char c) {

      var i = str.IndexOf(c);
      return str.Substring(i + 1);
   }

   static string
   fn_substring_before(string str, char c) {

      var i = str.IndexOf(c);
      return str.Substring(0, i);
   }

   static string[]
   fn_tokenize(string str) =>
      DataType.List(str, DataType.String)
         .ToArray();

   static string
   trim(string? str) =>
      SimpleContent.Trim(str);
}
