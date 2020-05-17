﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

namespace System.Web.Helpers.Claims {

   // Represents a ClaimsIdentity; serves as an abstraction around the WIF SDK and 4.5
   // ClaimIdentity types since we can't compile directly against them.

   abstract class ClaimsIdentity {

      public abstract IEnumerable<Claim> GetClaims();

      // Attempts to convert an IIdentity into a ClaimsIdentity;
      // returns null if the conversion fails (duck typing).
      //
      // The TClaimsIdentity must have the following shape:
      // class TClaimsIdentity : IIdentity {
      //   TClaimsCollection Claims { get; }
      // }
      // where TClaimsCollection is assignable to IEnumerable<TClaim>,
      // and where TClaim is valid for Claim.Create<TClaim>.

      static ClaimsIdentity TryConvert<TClaimsIdentity, TClaim>(IIdentity identity)
            where TClaimsIdentity : class, IIdentity =>
         (identity is TClaimsIdentity castClaimsIdentity) ?
            new ClaimsIdentityImpl<TClaimsIdentity, TClaim>(castClaimsIdentity)
            : null;

      sealed class ClaimsIdentityImpl<TClaimsIdentity, TClaim> : ClaimsIdentity
            where TClaimsIdentity : class, IIdentity {

         static readonly Func<TClaimsIdentity, IEnumerable<TClaim>> _claimsGetter = CreateClaimsGetter();
         readonly TClaimsIdentity _claimsIdentity;

         public ClaimsIdentityImpl(TClaimsIdentity claimsIdentity) {
            _claimsIdentity = claimsIdentity;
         }

         static Func<TClaimsIdentity, IEnumerable<TClaim>> CreateClaimsGetter() {

            PropertyInfo propInfo = typeof(TClaimsIdentity).GetProperty("Claims", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo propGetter = propInfo.GetGetMethod();

            // For improved perf, instance methods can be treated as static methods by leaving
            // the 'this' parameter unbound. Virtual dispatch for the property getter will
            // still take place as expected.

            return (Func<TClaimsIdentity, IEnumerable<TClaim>>)Delegate.CreateDelegate(typeof(Func<TClaimsIdentity, IEnumerable<TClaim>>), propGetter);
         }

         public override IEnumerable<Claim> GetClaims() =>
            _claimsGetter(_claimsIdentity).Select(Claim.Create);
      }
   }
}
