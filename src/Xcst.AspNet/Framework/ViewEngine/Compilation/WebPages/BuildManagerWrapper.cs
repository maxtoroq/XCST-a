// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.Util;
using Microsoft.Internal.Web.Utils;

namespace System.Web.WebPages {

   /// <summary>
   /// Wraps the caching and instantiation of paths of the BuildManager. 
   /// In case of precompiled non-updateable sites, the only way to verify if a file exists is to call BuildManager.GetObjectFactory. However this method is less performant than
   /// VirtualPathProvider.FileExists which is used for all other scenarios. In this class, we optimize for the first scenario by storing the results of GetObjectFactory for a 
   /// long duration.
   /// </summary>

   sealed class BuildManagerWrapper {

      internal static readonly Guid KeyGuid = Guid.NewGuid();
      readonly IVirtualPathUtility _virtualPathUtility;
      readonly Func<VirtualPathProvider> _vppFunc;
      readonly bool _isPrecompiled;

      public BuildManagerWrapper()
         : this(() => HostingEnvironment.VirtualPathProvider, new VirtualPathUtilityWrapper()) { }

      public BuildManagerWrapper(VirtualPathProvider vpp, IVirtualPathUtility virtualPathUtility)
         : this(() => vpp, virtualPathUtility) {

         Contract.Assert(vpp != null);
      }

      public BuildManagerWrapper(Func<VirtualPathProvider> vppFunc, IVirtualPathUtility virtualPathUtility) {

         Contract.Assert(vppFunc != null);
         Contract.Assert(virtualPathUtility != null);

         _vppFunc = vppFunc;
         _virtualPathUtility = virtualPathUtility;
         _isPrecompiled = IsNonUpdatablePrecompiledApp();
      }

      internal bool IsNonUpdatablePrecompiledApp() {

         VirtualPathProvider vpp = _vppFunc();

         // VirtualPathProvider currently null in some test scenarios e.g. PreApplicationStartCodeTest.StartTest

         if (vpp == null) {
            return false;
         }

         return IsNonUpdateablePrecompiledApp(vpp, _virtualPathUtility);
      }

      /// <summary>
      /// An app's is precompiled for our purposes if 
      /// (a) it has a PreCompiledApp.config file in the site root, 
      /// (b) The PreCompiledApp.config says that the app is not Updatable.
      /// </summary>
      /// <remarks>
      /// This code is based on System.Web.DynamicData.Misc.IsNonUpdatablePrecompiledAppNoCache (DynamicData)
      /// </remarks>
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to replicate the behavior of BuildManager which catches all exceptions.")]
      internal static bool IsNonUpdateablePrecompiledApp(VirtualPathProvider vpp, IVirtualPathUtility virtualPathUtility) {

         return false;

         //var virtualPath = virtualPathUtility.ToAbsolute("~/PrecompiledApp.config");
         //if (!vpp.FileExists(virtualPath))
         //{
         //    return false;
         //}

         //XDocument document;
         //using (Stream stream = vpp.GetFile(virtualPath).Open())
         //{
         //    try
         //    {
         //        document = XDocument.Load(stream);
         //    }
         //    catch
         //    {
         //        // If we are unable to load the file, ignore it. The BuildManager behaves identically.
         //        return false;
         //    }
         //}

         //if (document.Root == null || !document.Root.Name.LocalName.Equals("precompiledApp", StringComparison.OrdinalIgnoreCase))
         //{
         //    return false;
         //}
         //var updatableAttribute = document.Root.Attribute("updatable");
         //if (updatableAttribute != null)
         //{
         //    bool result;
         //    return Boolean.TryParse(updatableAttribute.Value, out result) && (result == false);
         //}
         //return false;
      }

      public object CreateInstance(string virtualPath) {
         return CreateInstanceOfType<object>(virtualPath);
      }

      public T CreateInstanceOfType<T>(string virtualPath) where T : class {

         if (_isPrecompiled) {

            var buildManagerResult = (BuildManagerResult)HttpRuntime.Cache.Get(GetKeyFromVirtualPath(virtualPath));

            // The cache could have evicted our results. In this case, we'll simply fall through to CreateInstanceFromVirtualPath

            if (buildManagerResult != null) {
               Debug.Assert(buildManagerResult.Exists && buildManagerResult.ObjectFactory != null, "This method must only be called if the file exists.");
               return buildManagerResult.ObjectFactory.CreateInstance() as T;
            }
         }

         return (T)BuildManager.CreateInstanceFromVirtualPath(virtualPath, typeof(T));
      }

      /// <summary>
      /// Creates a reasonably unique key for a given virtual path by concatenating it with a Guid.
      /// </summary>

      static string GetKeyFromVirtualPath(string virtualPath) {
         return KeyGuid.ToString() + "_" + virtualPath;
      }

      class BuildManagerResult {

         public bool Exists { get; set; }

         public IWebObjectFactory ObjectFactory { get; set; }
      }
   }
}
