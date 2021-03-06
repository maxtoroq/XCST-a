﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace System.Web.Mvc {

   sealed class DynamicViewDataDictionary : DynamicObject {

      readonly Func<ViewDataDictionary> _viewDataThunk;

      private ViewDataDictionary ViewData {
         get {
            ViewDataDictionary viewData = _viewDataThunk();
            Assert.IsNotNull(viewData);
            return viewData;
         }
      }

      public DynamicViewDataDictionary(Func<ViewDataDictionary> viewDataThunk) {
         _viewDataThunk = viewDataThunk;
      }

      // Implementing this function improves the debugging experience as it provides the debugger with the list of all
      // the properties currently defined on the object

      public override IEnumerable<string> GetDynamicMemberNames() =>
         this.ViewData.Keys;

      public override bool TryGetMember(GetMemberBinder binder, out object? result) {

         result = this.ViewData[binder.Name];

         // since ViewDataDictionary always returns a result even if the key does not exist, always return true

         return true;
      }

      public override bool TrySetMember(SetMemberBinder binder, object? value) {

         this.ViewData[binder.Name] = value;

         // you can always set a key in the dictionary so return true

         return true;
      }
   }
}
