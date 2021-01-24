﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   public class SessionStateTempDataProvider : ITempDataProvider {

      internal const string TempDataSessionStateKey = "__ControllerTempData";

      public virtual IDictionary<string, object?> LoadTempData(ControllerContext controllerContext) {

         HttpSessionStateBase session = controllerContext.HttpContext.Session;

         if (session != null) {

            if (session[TempDataSessionStateKey] is Dictionary<string, object?> tempDataDictionary) {

               // If we got it from Session, remove it so that no other request gets it

               session.Remove(TempDataSessionStateKey);
               return tempDataDictionary;
            }
         }

         return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      }

      public virtual void SaveTempData(ControllerContext controllerContext, IDictionary<string, object?> values) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         HttpSessionStateBase session = controllerContext.HttpContext.Session;
         bool isDirty = (values != null && values.Count > 0);

         if (session is null) {

            if (isDirty) {
               throw new InvalidOperationException(MvcResources.SessionStateTempDataProvider_SessionStateDisabled);
            }

         } else {

            if (isDirty) {
               session[TempDataSessionStateKey] = values;
            } else {

               // Since the default implementation of Remove() (from SessionStateItemCollection) dirties the
               // collection, we shouldn't call it unless we really do need to remove the existing key.

               if (session[TempDataSessionStateKey] != null) {
                  session.Remove(TempDataSessionStateKey);
               }
            }
         }
      }
   }
}
