﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Xcst.Web {

   // based on <https://blogs.msdn.microsoft.com/ericwhite/2009/01/27/equality-semantics-of-linq-to-xml-trees/>

   class XDocumentNormalizer {

      public static bool DeepEqualsWithNormalization(XDocument doc1, XDocument doc2) {

         XDocument d1 = Normalize(doc1);
         XDocument d2 = Normalize(doc2);

         return XNode.DeepEquals(d1, d2);
      }

      public static XDocument Normalize(XDocument source) {

         return new XDocument(
             source.Declaration,
             source.Nodes().Select(n => {

                // Remove comments, processing instructions, and text nodes that are
                // children of XDocument.  Only white space text nodes are allowed as
                // children of a document, so we can remove all text nodes.

                if (n is XComment || n is XProcessingInstruction || n is XText) {
                   return null;
                }

                XElement e = n as XElement;

                if (e != null) {
                   return NormalizeElement(e);
                }

                return n;
             })
         );
      }

      static XNode NormalizeNode(XNode node) {

         // trim comments and processing instructions from normalized tree

         if (node is XComment || node is XProcessingInstruction) {
            return null;
         }

         XElement e = node as XElement;

         if (e != null) {
            return NormalizeElement(e);
         }
         // Only thing left is XCData and XText, so clone them

         return node;
      }

      static XElement NormalizeElement(XElement element) {

         return new XElement(element.Name,
            NormalizeAttributes(element),
            element.Nodes().Select(n => NormalizeNode(n))
         );
      }

      static IEnumerable<XAttribute> NormalizeAttributes(XElement element) {

         return element.Attributes()
            .Where(a => !a.IsNamespaceDeclaration
               && a.Name != Xsi.schemaLocation
               && a.Name != Xsi.noNamespaceSchemaLocation)
            .OrderBy(a => a.Name.NamespaceName)
            .ThenBy(a => a.Name.LocalName);
      }

      class Xsi {

         public static XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
         public static XName schemaLocation = xsi + "schemaLocation";
         public static XName noNamespaceSchemaLocation = xsi + "noNamespaceSchemaLocation";
      }
   }
}