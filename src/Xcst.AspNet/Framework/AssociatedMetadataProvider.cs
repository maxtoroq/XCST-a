// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   // This class provides a good implementation of ModelMetadataProvider for people who will be
   // using traditional classes with properties. It uses the buddy class support from
   // DataAnnotations, and consolidates the three operations down to a single override
   // for reading the attribute values and creating the metadata class.

   abstract class AssociatedMetadataProvider : ModelMetadataProvider {

      protected abstract ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName);

      protected virtual IEnumerable<Attribute> FilterAttributes(Type containerType, PropertyDescriptor propertyDescriptor, IEnumerable<Attribute> attributes) =>
         attributes;

      public override IEnumerable<ModelMetadata> GetMetadataForProperties(object container, Type containerType) {

         if (containerType == null) throw new ArgumentNullException(nameof(containerType));

         PropertyDescriptorCollection properties = GetTypeDescriptor(containerType).GetProperties();

         // The return value is sorted from the ModelMetadata type, so returning as an array is best for performance
         ModelMetadata[] metadata = new ModelMetadata[properties.Count];

         for (int i = 0; i < properties.Count; i++) {

            PropertyDescriptor property = properties[i];
            Func<object> modelAccessor = (container == null) ? null : GetPropertyValueAccessor(container, property);
            ModelMetadata propertyMetadata = GetMetadataForProperty(modelAccessor, containerType, property);

            if (propertyMetadata != null) {
               propertyMetadata.Container = container;
            }

            metadata[i] = propertyMetadata;
         }

         return metadata;
      }

      public override ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, string propertyName) {

         if (containerType == null) throw new ArgumentNullException(nameof(containerType));
         if (String.IsNullOrEmpty(propertyName)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(propertyName));

         ICustomTypeDescriptor typeDescriptor = GetTypeDescriptor(containerType);

         PropertyDescriptor property = typeDescriptor.GetProperties().Find(propertyName, ignoreCase: true)
            ?? throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, MvcResources.Common_PropertyNotFound,
                  containerType.FullName, propertyName));

         return GetMetadataForProperty(modelAccessor, containerType, property);
      }

      protected virtual ModelMetadata GetMetadataForProperty(Func<object> modelAccessor, Type containerType, PropertyDescriptor propertyDescriptor) {

         IEnumerable<Attribute> attributes = FilterAttributes(containerType, propertyDescriptor, new AttributeList(propertyDescriptor.Attributes));
         ModelMetadata result = CreateMetadata(attributes, containerType, modelAccessor, propertyDescriptor.PropertyType, propertyDescriptor.Name);
         ApplyMetadataAwareAttributes(attributes, result);

         return result;
      }

      public override ModelMetadata GetMetadataForType(Func<object> modelAccessor, Type modelType) {

         if (modelType == null) throw new ArgumentNullException(nameof(modelType));

         AttributeList attributes = new AttributeList(GetTypeDescriptor(modelType).GetAttributes());
         ModelMetadata result = CreateMetadata(attributes, null /* containerType */, modelAccessor, modelType, null /* propertyName */);
         ApplyMetadataAwareAttributes(attributes, result);

         return result;
      }

      static void ApplyMetadataAwareAttributes(IEnumerable<Attribute> attributes, ModelMetadata result) {

         foreach (IMetadataAware awareAttribute in attributes.OfType<IMetadataAware>()) {
            awareAttribute.OnMetadataCreated(result);
         }
      }

      static Func<object> GetPropertyValueAccessor(object container, PropertyDescriptor property) =>
         () => property.GetValue(container);

      protected virtual ICustomTypeDescriptor GetTypeDescriptor(Type type) =>
         TypeDescriptorHelper.Get(type);
   }

   // This interface is implemented by attributes which wish to contribute to the
   // ModelMetadata creation process without needing to write a custom metadata
   // provider. It is consumed by AssociatedMetadataProvider, so this behavior is
   // automatically inherited by all classes which derive from it (notably, the
   // DataAnnotationsModelMetadataProvider).

   public interface IMetadataAware {
      void OnMetadataCreated(ModelMetadata metadata);
   }

   class EmptyModelMetadataProvider : AssociatedMetadataProvider {

      protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName) =>
         new ModelMetadata(this, containerType, modelAccessor, modelType, propertyName);
   }
}
