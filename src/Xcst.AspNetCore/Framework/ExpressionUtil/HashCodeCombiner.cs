﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;

namespace Xcst.Web.Mvc.ExpressionUtil;

// based on System.Web.Util.HashCodeCombiner

class HashCodeCombiner {

   long
   _combinedHash64 = 0x1505L;

   public int
   CombinedHash => _combinedHash64.GetHashCode();

   public void
   AddFingerprint(ExpressionFingerprint? fingerprint) {

      if (fingerprint != null) {
         fingerprint.AddToHashCodeCombiner(this);
      } else {
         AddInt32(0);
      }
   }

   public void
   AddEnumerable(IEnumerable e) {

      if (e is null) {
         AddInt32(0);
      } else {

         var count = 0;

         foreach (object o in e) {
            AddObject(o);
            count++;
         }

         AddInt32(count);
      }
   }

   public void
   AddInt32(int i) =>
      _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ i;

   public void
   AddObject(object o) {

      var hashCode = (o != null) ? o.GetHashCode() : 0;
      AddInt32(hashCode);
   }
}
