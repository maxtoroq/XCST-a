// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Web.Hosting;

namespace System.Web.WebPages {

   /// <summary>
   /// This class caches the result of VirtualPathProvider.FileExists for a short
   /// period of time, and recomputes it if necessary.
   /// 
   /// The default VPP MapPathBasedVirtualPathProvider caches the result of
   /// the FileExists call with the appropriate dependencies, so it is less
   /// expensive on subsequent calls, but it still needs to do MapPath which can 
   /// take quite some time.
   /// </summary>

   class FileExistenceCache {

      const int TicksPerMillisecond = 10000;
      readonly Func<VirtualPathProvider> _virtualPathProviderFunc;
      readonly Func<string, bool> _virtualPathFileExists;
      ConcurrentDictionary<string, bool> _cache;
      long _creationTick;
      int _ticksBeforeReset;

      // Use the VPP returned by the HostingEnvironment unless a custom vpp is passed in (mainly for testing purposes)

      public VirtualPathProvider VirtualPathProvider => _virtualPathProviderFunc();

      public int MilliSecondsBeforeReset {
         get { return _ticksBeforeReset / TicksPerMillisecond; }
         internal set { _ticksBeforeReset = value * TicksPerMillisecond; }
      }

      internal IDictionary<string, bool> CacheInternal => _cache;

      public bool TimeExceeded => (DateTime.UtcNow.Ticks - Interlocked.Read(ref _creationTick)) > _ticksBeforeReset;

      // Overload used mainly for testing

      public FileExistenceCache(VirtualPathProvider virtualPathProvider, int milliSecondsBeforeReset = 1000)
         : this(() => virtualPathProvider, milliSecondsBeforeReset) {

         Contract.Assert(virtualPathProvider != null);
      }

      public FileExistenceCache(Func<VirtualPathProvider> virtualPathProviderFunc, int milliSecondsBeforeReset = 1000) {

         Contract.Assert(virtualPathProviderFunc != null);

         _virtualPathProviderFunc = virtualPathProviderFunc;
         _virtualPathFileExists = path => _virtualPathProviderFunc().FileExists(path);
         _ticksBeforeReset = milliSecondsBeforeReset * TicksPerMillisecond;

         Reset();
      }

      public void Reset() {

         _cache = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

         DateTime now = DateTime.UtcNow;
         long tick = now.Ticks;

         Interlocked.Exchange(ref _creationTick, tick);
      }

      public bool FileExists(string virtualPath) {

         if (this.TimeExceeded) {
            Reset();
         }

         return _cache.GetOrAdd(virtualPath, _virtualPathFileExists);
      }
   }
}
