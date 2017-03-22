// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Properties;
using System.Xml;

namespace System.Web.Mvc {

   static class TypeCacheUtil {

      public static List<Type> GetFilteredTypesFromAssemblies(string cacheName, Predicate<Type> predicate, IBuildManager buildManager) {

         var serializer = new TypeCacheSerializer();

         // first, try reading from the cache on disk

         List<Type> matchingTypes = ReadTypesFromCache(cacheName, predicate, buildManager, serializer);

         if (matchingTypes != null) {
            return matchingTypes;
         }

         // if reading from the cache failed, enumerate over every assembly looking for a matching type

         matchingTypes = FilterTypesInAssemblies(buildManager, predicate).ToList();

         // finally, save the cache back to disk

         SaveTypesToCache(cacheName, matchingTypes, buildManager, serializer);

         return matchingTypes;
      }

      static IEnumerable<Type> FilterTypesInAssemblies(IBuildManager buildManager, Predicate<Type> predicate) {

         // Go through all assemblies referenced by the application and search for types matching a predicate

         IEnumerable<Type> typesSoFar = Type.EmptyTypes;

         ICollection assemblies = buildManager.GetReferencedAssemblies();

         foreach (Assembly assembly in assemblies) {

            Type[] typesInAsm;

            try {
               typesInAsm = assembly.GetTypes();
            } catch (ReflectionTypeLoadException ex) {
               typesInAsm = ex.Types;
            }

            typesSoFar = typesSoFar.Concat(typesInAsm);
         }

         return typesSoFar.Where(type => TypeIsPublicClass(type) && predicate(type));
      }

      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Cache failures are not fatal, and the code should continue executing normally.")]
      internal static List<Type> ReadTypesFromCache(string cacheName, Predicate<Type> predicate, IBuildManager buildManager, TypeCacheSerializer serializer) {

         try {

            Stream stream = buildManager.ReadCachedFile(cacheName);

            if (stream != null) {

               using (StreamReader reader = new StreamReader(stream)) {

                  List<Type> deserializedTypes = serializer.DeserializeTypes(reader);

                  if (deserializedTypes != null
                     && deserializedTypes.All(type => TypeIsPublicClass(type) && predicate(type))) {

                     // If all read types still match the predicate, success!

                     return deserializedTypes;
                  }
               }
            }
         } catch { }

         return null;
      }

      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Cache failures are not fatal, and the code should continue executing normally.")]
      internal static void SaveTypesToCache(string cacheName, IList<Type> matchingTypes, IBuildManager buildManager, TypeCacheSerializer serializer) {

         try {

            Stream stream = buildManager.CreateCachedFile(cacheName);

            if (stream != null) {
               using (StreamWriter writer = new StreamWriter(stream)) {
                  serializer.SerializeTypes(matchingTypes, writer);
               }
            }
         } catch { }
      }

      static bool TypeIsPublicClass(Type type) {

         return type != null
            && type.IsPublic
            && type.IsClass
            && !type.IsAbstract;
      }
   }

   // Processes files with this format:
   //
   // <typeCache lastModified=... mvcVersionId=...>
   //   <assembly name=...>
   //     <module versionId=...>
   //       <type>...</type>
   //     </module>
   //   </assembly>
   // </typeCache>
   //
   // This is used to store caches of files between AppDomain resets, leading to improved cold boot time
   // and more efficient use of memory.

   sealed class TypeCacheSerializer {

      static readonly Guid _mvcVersionId = typeof(TypeCacheSerializer).Module.ModuleVersionId;

      // used for unit testing

      private DateTime CurrentDate => CurrentDateOverride ?? DateTime.Now;

      internal DateTime? CurrentDateOverride { get; set; }

      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "This is an instance method for consistency with the SerializeTypes() method.")]
      public List<Type> DeserializeTypes(TextReader input) {

         var doc = new XmlDocument();
         doc.Load(input);

         XmlElement rootElement = doc.DocumentElement;

         Guid readMvcVersionId = new Guid(rootElement.Attributes["mvcVersionId"].Value);

         if (readMvcVersionId != _mvcVersionId) {

            // The cache is outdated because the cache file was produced by a different version
            // of MVC.

            return null;
         }

         var deserializedTypes = new List<Type>();

         foreach (XmlNode assemblyNode in rootElement.ChildNodes) {

            string assemblyName = assemblyNode.Attributes["name"].Value;
            Assembly assembly = Assembly.Load(assemblyName);

            foreach (XmlNode moduleNode in assemblyNode.ChildNodes) {

               var moduleVersionId = new Guid(moduleNode.Attributes["versionId"].Value);

               foreach (XmlNode typeNode in moduleNode.ChildNodes) {

                  string typeName = typeNode.InnerText;

                  Type type = assembly.GetType(typeName);

                  if (type == null
                     || type.Module.ModuleVersionId != moduleVersionId) {

                     // The cache is outdated because we couldn't find a previously recorded
                     // type or the type's containing module was modified.

                     return null;

                  } else {
                     deserializedTypes.Add(type);
                  }
               }
            }
         }

         return deserializedTypes;
      }

      public void SerializeTypes(IEnumerable<Type> types, TextWriter output) {

         var groupedByAssembly =
            from type in types
            group type by type.Module into groupedByModule
            group groupedByModule by groupedByModule.Key.Assembly;

         var doc = new XmlDocument();
         doc.AppendChild(doc.CreateComment(MvcResources.TypeCache_DoNotModify));

         XmlElement typeCacheElement = doc.CreateElement("typeCache");
         doc.AppendChild(typeCacheElement);
         typeCacheElement.SetAttribute("lastModified", CurrentDate.ToString());
         typeCacheElement.SetAttribute("mvcVersionId", _mvcVersionId.ToString());

         foreach (var assemblyGroup in groupedByAssembly) {

            XmlElement assemblyElement = doc.CreateElement("assembly");
            typeCacheElement.AppendChild(assemblyElement);
            assemblyElement.SetAttribute("name", assemblyGroup.Key.FullName);

            foreach (var moduleGroup in assemblyGroup) {

               XmlElement moduleElement = doc.CreateElement("module");
               assemblyElement.AppendChild(moduleElement);
               moduleElement.SetAttribute("versionId", moduleGroup.Key.ModuleVersionId.ToString());

               foreach (Type type in moduleGroup) {
                  XmlElement typeElement = doc.CreateElement("type");
                  moduleElement.AppendChild(typeElement);
                  typeElement.AppendChild(doc.CreateTextNode(type.FullName));
               }
            }
         }

         doc.Save(output);
      }
   }
}
