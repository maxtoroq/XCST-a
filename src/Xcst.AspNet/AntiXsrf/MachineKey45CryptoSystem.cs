// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Web.Security;

namespace System.Web.Helpers.AntiXsrf {

   // Interfaces with the System.Web.MachineKey static class using the 4.5 Protect / Unprotect methods.

   sealed class MachineKey45CryptoSystem : ICryptoSystem {

      static readonly string[] _purposes = new string[] { "System.Web.Helpers.AntiXsrf.AntiForgeryToken.v1" };
      static readonly MachineKey45CryptoSystem _singletonInstance = GetSingletonInstance();

      public static MachineKey45CryptoSystem Instance => _singletonInstance;

      static MachineKey45CryptoSystem GetSingletonInstance() =>
         new MachineKey45CryptoSystem();

      public string Protect(byte[] data) {
         byte[] rawProtectedBytes = MachineKey.Protect(data, _purposes);
         return HttpServerUtility.UrlTokenEncode(rawProtectedBytes);
      }

      public byte[] Unprotect(string protectedData) {
         byte[] rawProtectedBytes = HttpServerUtility.UrlTokenDecode(protectedData);
         return MachineKey.Unprotect(rawProtectedBytes, _purposes);
      }
   }
}
