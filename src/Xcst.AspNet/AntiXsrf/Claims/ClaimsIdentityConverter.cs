﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;
using System.Web.Security;

namespace System.Web.Helpers.Claims {

   // Can convert IIdentity instances into our ClaimsIdentity wrapper.

   sealed class ClaimsIdentityConverter {

      static readonly MethodInfo _claimsIdentityTryConvertOpenMethod = typeof(ClaimsIdentity).GetMethod("TryConvert", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
      static readonly ClaimsIdentityConverter _default = new ClaimsIdentityConverter(GetDefaultConverters());

      readonly Func<IIdentity, ClaimsIdentity>[] _converters;

      // By default, we understand the ClaimsIdentity / Claim types included
      // with the WIF SDK and FX 4.5.

      public static ClaimsIdentityConverter Default => _default;

      // Internal for unit testing; nobody should ever be calling this in production.

      internal ClaimsIdentityConverter(Func<IIdentity, ClaimsIdentity>[] converters) {
         _converters = converters;
      }

      static bool IsGrandfatheredIdentityType(IIdentity claimsIdentity) =>

         // These specific types might also be claims-based types depending on
         // the version of the framework we're running, but we don't want to
         // treat them as claims-based types since we know their Name property
         // will suffice as a unique identifier within the security realm of the
         // current application.

         claimsIdentity is FormsIdentity
            || claimsIdentity is WindowsIdentity
            || claimsIdentity is GenericIdentity;

      public ClaimsIdentity TryConvert(IIdentity identity) {

         if (IsGrandfatheredIdentityType(identity)) {
            return null;
         }

         // loop through all registered converters until one matches

         for (int i = 0; i < _converters.Length; i++) {

            ClaimsIdentity retVal = _converters[i](identity);

            if (retVal != null) {
               return retVal;
            }
         }

         return null;
      }

      static void AddToList(IList<Func<IIdentity, ClaimsIdentity>> converters, Type claimsIdentityType, Type claimType) {

         if (claimsIdentityType != null
            && claimType != null) {

            MethodInfo tryConvertClosedMethod = _claimsIdentityTryConvertOpenMethod.MakeGenericMethod(claimsIdentityType, claimType);
            Func<IIdentity, ClaimsIdentity> converter = (Func<IIdentity, ClaimsIdentity>)Delegate.CreateDelegate(typeof(Func<IIdentity, ClaimsIdentity>), tryConvertClosedMethod);
            converters.Add(converter);
         }
      }

      static Func<IIdentity, ClaimsIdentity>[] GetDefaultConverters() {

         var converters = new List<Func<IIdentity, ClaimsIdentity>>();

         // WIF SDK is only available in full trust scenarios

         if (AppDomain.CurrentDomain.IsHomogenous && AppDomain.CurrentDomain.IsFullyTrusted) {
            Type claimsIdentityType = Type.GetType("Microsoft.IdentityModel.Claims.IClaimsIdentity, Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            Type claimType = Type.GetType("Microsoft.IdentityModel.Claims.Claim, Microsoft.IdentityModel, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            AddToList(converters, claimsIdentityType, claimType);
         }

         // 4.5 ClaimsIdentity type
         {
            Module mscorlibModule = typeof(object).Module;
            Type claimsIdentityType = mscorlibModule.GetType("System.Security.Claims.ClaimsIdentity");
            Type claimType = mscorlibModule.GetType("System.Security.Claims.Claim");
            AddToList(converters, claimsIdentityType, claimType);
         }

         return converters.ToArray();
      }
   }
}
