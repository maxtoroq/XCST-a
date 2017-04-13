﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web;

namespace Microsoft.Internal.Web.Utils {

   interface IVirtualPathUtility {

      string Combine(string basePath, string relativePath);

      string ToAbsolute(string virtualPath);
   }

   sealed class VirtualPathUtilityWrapper : IVirtualPathUtility {

      public string Combine(string basePath, string relativePath) {
         return VirtualPathUtility.Combine(basePath, relativePath);
      }

      public string ToAbsolute(string virtualPath) {
         return VirtualPathUtility.ToAbsolute(virtualPath);
      }
   }
}