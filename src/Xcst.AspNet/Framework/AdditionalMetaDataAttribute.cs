// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc {

   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property, AllowMultiple = true)]
   public sealed class AdditionalMetadataAttribute : Attribute, IMetadataAware {

      object _typeId = new object();

      public override object TypeId => _typeId;

      public string Name { get; private set; }

      public object Value { get; private set; }

      public AdditionalMetadataAttribute(string name, object value) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         this.Name = name;
         this.Value = value;
      }

      public void OnMetadataCreated(ModelMetadata metadata) {

         if (metadata is null) throw new ArgumentNullException(nameof(metadata));

         metadata.AdditionalValues[this.Name] = this.Value;
      }
   }
}
