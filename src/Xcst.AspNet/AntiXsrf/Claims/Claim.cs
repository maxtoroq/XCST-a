﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Reflection;

namespace System.Web.Helpers.Claims {

   // Represents a Claim; serves as an abstraction around the WIF SDK and 4.5 Claim types since
   // we can't compile directly against them.

   sealed class Claim {

      public string ClaimType { get; private set; }

      public string Value { get; private set; }

      public Claim(string claimType, string value) {
         ClaimType = claimType;
         Value = value;
      }

      // Creates a Claim from a TClaim object (duck typing).
      //
      // The TClaim must have the following shape:
      // class TClaim {
      //   string ClaimType { get; } // or just 'Type'
      //   string Value { get; }
      // }

      internal static Claim Create<TClaim>(TClaim claim) =>
         ClaimFactory<TClaim>.Create(claim);

      static class ClaimFactory<TClaim> {

         static readonly Func<TClaim, string> _claimTypeGetter = CreateClaimTypeGetter();
         static readonly Func<TClaim, string> _valueGetter = CreateValueGetter();

         public static Claim Create(TClaim claim) =>
            new Claim(_claimTypeGetter(claim), _valueGetter(claim));

         static Func<TClaim, string> CreateClaimTypeGetter() =>
            // the claim type might go by one of two different property names
            CreateGeneralPropertyGetter("ClaimType")
            ?? CreateGeneralPropertyGetter("Type")
            ?? throw new InvalidOperationException();

         static Func<TClaim, string>? CreateGeneralPropertyGetter(string propertyName) {

            PropertyInfo propInfo = typeof(TClaim).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance, null, typeof(string), Type.EmptyTypes, null);

            if (propInfo is null) {
               return null;
            }

            MethodInfo propGetter = propInfo.GetGetMethod();

            // For improved perf, instance methods can be treated as static methods by leaving
            // the 'this' parameter unbound. Virtual dispatch for the property getter will
            // still take place as expected.
            return (Func<TClaim, string>)Delegate.CreateDelegate(typeof(Func<TClaim, string>), propGetter);
         }

         static Func<TClaim, string> CreateValueGetter() =>
            CreateGeneralPropertyGetter("Value")
            ?? throw new InvalidOperationException();
      }
   }
}
