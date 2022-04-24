﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc {

   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
   public sealed class HiddenInputAttribute : Attribute {

      public bool
      DisplayValue { get; set; } = true;
   }
}
