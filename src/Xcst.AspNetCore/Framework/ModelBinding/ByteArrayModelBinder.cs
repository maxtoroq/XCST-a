// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc {

   public class ByteArrayModelBinder : IModelBinder {

      public virtual object?
      BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         if (bindingContext is null) throw new ArgumentNullException(nameof(bindingContext));

         var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

         // case 1: there was no <input ... /> element containing this data

         if (valueResult is null) {
            return null;
         }

         var value = valueResult.AttemptedValue;

         // case 2: there was an <input ... /> element but it was left blank

         if (String.IsNullOrEmpty(value)) {
            return null;
         }

         // Future proofing. If the byte array is actually an instance of System.Data.Linq.Binary
         // then we need to remove these quotes put in place by the ToString() method.

         var realValue = value.Replace("\"", String.Empty);

         return Convert.FromBase64String(realValue);
      }
   }
}
