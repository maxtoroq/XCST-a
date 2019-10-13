// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   abstract class AssociatedValidatorProvider : ModelValidatorProvider {

      protected virtual ICustomTypeDescriptor GetTypeDescriptor(Type type) {
         return TypeDescriptorHelper.Get(type);
      }

      public sealed override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context) {

         if (metadata == null) throw new ArgumentNullException(nameof(metadata));
         if (context == null) throw new ArgumentNullException(nameof(context));
         
         if (metadata.ContainerType != null
            && !String.IsNullOrEmpty(metadata.PropertyName)) {

            return GetValidatorsForProperty(metadata, context);
         }

         return GetValidatorsForType(metadata, context);
      }

      protected abstract IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context, IEnumerable<Attribute> attributes);

      IEnumerable<ModelValidator> GetValidatorsForProperty(ModelMetadata metadata, ControllerContext context) {

         ICustomTypeDescriptor typeDescriptor = GetTypeDescriptor(metadata.ContainerType);

         PropertyDescriptor property = typeDescriptor.GetProperties().Find(metadata.PropertyName, ignoreCase: true)
            ?? throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, MvcResources.Common_PropertyNotFound,
                  metadata.ContainerType.FullName, metadata.PropertyName), nameof(metadata));

         return GetValidators(metadata, context, new AttributeList(property.Attributes));
      }

      IEnumerable<ModelValidator> GetValidatorsForType(ModelMetadata metadata, ControllerContext context) {
         return GetValidators(metadata, context, new AttributeList(GetTypeDescriptor(metadata.ModelType).GetAttributes()));
      }
   }
}
