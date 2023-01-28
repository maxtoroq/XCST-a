// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Xcst.Web.Mvc.Properties;

namespace Xcst.Web.Mvc.ModelBinding;

[AttributeUsage(ValidTargets, AllowMultiple = false, Inherited = false)]
public sealed class ModelBinderAttribute : CustomModelBinderAttribute {

   public Type
   BinderType { get; private set; }

   public
   ModelBinderAttribute(Type binderType) {

      if (binderType is null) throw new ArgumentNullException(nameof(binderType));

      if (!typeof(IModelBinder).IsAssignableFrom(binderType)) {

         var message = String.Format(CultureInfo.CurrentCulture,
            MvcResources.ModelBinderAttribute_TypeNotIModelBinder, binderType.FullName);

         throw new ArgumentException(message, nameof(binderType));
      }

      this.BinderType = binderType;
   }

   public override IModelBinder
   GetBinder() {

      try {
         return (IModelBinder)Activator.CreateInstance(this.BinderType)!;

      } catch (Exception ex) {

         throw new InvalidOperationException(
            String.Format(
               CultureInfo.CurrentCulture,
               MvcResources.ModelBinderAttribute_ErrorCreatingModelBinder,
               this.BinderType.FullName),
            ex);
      }
   }
}
