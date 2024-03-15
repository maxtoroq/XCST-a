// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace Xcst.Web.Mvc;

sealed class DynamicViewDataDictionary : DynamicObject {

   readonly Func<ViewDataDictionary>
   _viewDataThunk;

   private ViewDataDictionary
   ViewData {
      get {
         var viewData = _viewDataThunk.Invoke();
         Debug.Assert(viewData != null);
         return viewData;
      }
   }

   public
   DynamicViewDataDictionary(Func<ViewDataDictionary> viewDataThunk) {
      _viewDataThunk = viewDataThunk;
   }

   // Implementing this function improves the debugging experience as it provides the debugger with the list of all
   // the properties currently defined on the object

   public override IEnumerable<string>
   GetDynamicMemberNames() =>
      this.ViewData.Keys;

   public override bool
   TryGetMember(GetMemberBinder binder, out object? result) {

      result = this.ViewData[binder.Name];

      // since ViewDataDictionary always returns a result even if the key does not exist, always return true

      return true;
   }

   public override bool
   TrySetMember(SetMemberBinder binder, object? value) {

      this.ViewData[binder.Name] = value;

      // you can always set a key in the dictionary so return true

      return true;
   }
}
