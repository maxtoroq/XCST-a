﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.IO;
using System.Web.Compilation;

namespace System.Web.Mvc {

   internal interface IBuildManager {
      bool FileExists(string virtualPath);
      Type GetCompiledType(string virtualPath);
      ICollection GetReferencedAssemblies();
      Stream ReadCachedFile(string fileName);
      Stream CreateCachedFile(string fileName);
   }

   sealed class BuildManagerWrapper : IBuildManager {

      bool IBuildManager.FileExists(string virtualPath) {
         return BuildManager.GetObjectFactory(virtualPath, throwIfNotFound: false) != null;
      }

      Type IBuildManager.GetCompiledType(string virtualPath) {
         return BuildManager.GetCompiledType(virtualPath);
      }

      ICollection IBuildManager.GetReferencedAssemblies() {
         return BuildManager.GetReferencedAssemblies();
      }

      Stream IBuildManager.ReadCachedFile(string fileName) {
         return BuildManager.ReadCachedFile(fileName);
      }

      Stream IBuildManager.CreateCachedFile(string fileName) {
         return BuildManager.CreateCachedFile(fileName);
      }
   }
}
