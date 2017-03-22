// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Web.Caching;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   interface IViewLocationCache {

      string GetViewLocation(HttpContextBase httpContext, string key);
      void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath);
   }

   class DefaultViewLocationCache : IViewLocationCache {

      static readonly TimeSpan _defaultTimeSpan = new TimeSpan(0, 15, 0);

      [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "The reference type is immutable. ")]
      public static readonly IViewLocationCache Null = new NullViewLocationCache();

      public TimeSpan TimeSpan { get; private set; }

      public DefaultViewLocationCache()
         : this(_defaultTimeSpan) { }

      public DefaultViewLocationCache(TimeSpan timeSpan) {

         if (timeSpan.Ticks < 0) {
            throw new InvalidOperationException(MvcResources.DefaultViewLocationCache_NegativeTimeSpan);
         }

         this.TimeSpan = timeSpan;
      }

      public string GetViewLocation(HttpContextBase httpContext, string key) {

         if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

         return (string)httpContext.Cache[key];
      }

      public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath) {

         if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

         httpContext.Cache.Insert(key, virtualPath, null /* dependencies */, Cache.NoAbsoluteExpiration, this.TimeSpan);
      }
   }

   sealed class NullViewLocationCache : IViewLocationCache {

      public string GetViewLocation(HttpContextBase httpContext, string key) {
         return null;
      }

      public void InsertViewLocation(HttpContextBase httpContext, string key, string virtualPath) { }
   }
}
