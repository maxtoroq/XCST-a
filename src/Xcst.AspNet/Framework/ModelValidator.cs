// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Web.Mvc {

   abstract class ModelValidator {

      protected internal ControllerContext ControllerContext { get; private set; }

      protected internal ModelMetadata Metadata { get; private set; }

      public virtual bool IsRequired => false;

      protected ModelValidator(ModelMetadata metadata, ControllerContext controllerContext) {

         if (metadata is null) throw new ArgumentNullException(nameof(metadata));
         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         this.Metadata = metadata;
         this.ControllerContext = controllerContext;
      }

      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may perform non-trivial work.")]
      public virtual IEnumerable<ModelClientValidationRule> GetClientValidationRules() =>
         Enumerable.Empty<ModelClientValidationRule>();

      public static ModelValidator GetModelValidator(ModelMetadata metadata, ControllerContext context) =>
         new CompositeModelValidator(metadata, context);

      public abstract IEnumerable<ModelValidationResult> Validate(object? container);

      class CompositeModelValidator : ModelValidator {

         public CompositeModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
            : base(metadata, controllerContext) { }

         static ModelValidationResult CreateSubPropertyResult(ModelMetadata propertyMetadata, ModelValidationResult propertyResult) =>
            new ModelValidationResult {
               MemberName = CreateSubPropertyName(propertyMetadata.PropertyName, propertyResult.MemberName),
               Message = propertyResult.Message
            };

         static string CreateSubPropertyName(string? prefix, string propertyName) {

            if (String.IsNullOrEmpty(prefix)) {
               return propertyName;
            } else if (String.IsNullOrEmpty(propertyName)) {
               return prefix!;
            } else {
               return prefix + "." + propertyName;
            }
         }

         public override IEnumerable<ModelValidationResult> Validate(object? container) {

            bool propertiesValid = true;

            ModelMetadata[] properties = Metadata.PropertiesAsArray;

            // Performance sensitive loops

            for (int propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++) {

               ModelMetadata propertyMetadata = properties[propertyIndex];

               foreach (ModelValidator propertyValidator in propertyMetadata.GetValidators(this.ControllerContext)) {

                  foreach (ModelValidationResult propertyResult in propertyValidator.Validate(this.Metadata.Model)) {
                     propertiesValid = false;
                     yield return CreateSubPropertyResult(propertyMetadata, propertyResult);
                  }
               }
            }

            if (propertiesValid) {
               foreach (ModelValidator typeValidator in Metadata.GetValidators(this.ControllerContext)) {
                  foreach (ModelValidationResult typeResult in typeValidator.Validate(container)) {
                     yield return typeResult;
                  }
               }
            }
         }
      }
   }

   class ModelValidationResult {

      string? _memberName;
      string? _message;

      [AllowNull]
      public string MemberName {
         get => _memberName ?? String.Empty;
         set => _memberName = value;
      }

      public string Message {
         get => _message ?? String.Empty;
         set => _message = value;
      }
   }

   abstract class ModelValidatorProvider {
      public abstract IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context);
   }

   class EmptyModelValidatorProvider : ModelValidatorProvider {

      public override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context) =>
         Enumerable.Empty<ModelValidator>();
   }

   class ModelValidatorProviderCollection : Collection<ModelValidatorProvider> {

      ModelValidatorProvider[]? _combinedItems;
      IDependencyResolver? _dependencyResolver;

      internal ModelValidatorProvider[] CombinedItems {
         get {
            ModelValidatorProvider[]? combinedItems = _combinedItems;

            if (combinedItems is null) {
               combinedItems = MultiServiceResolver.GetCombined<ModelValidatorProvider>(Items, _dependencyResolver);
               _combinedItems = combinedItems;
            }

            return combinedItems;
         }
      }

      public ModelValidatorProviderCollection() { }

      public ModelValidatorProviderCollection(IList<ModelValidatorProvider> list)
         : base(list) { }

      internal ModelValidatorProviderCollection(IList<ModelValidatorProvider> list, IDependencyResolver dependencyResolver)
         : base(list) {

         _dependencyResolver = dependencyResolver;
      }

      protected override void ClearItems() {
         _combinedItems = null;
         base.ClearItems();
      }

      protected override void InsertItem(int index, ModelValidatorProvider item) {

         if (item is null) throw new ArgumentNullException(nameof(item));

         _combinedItems = null;
         base.InsertItem(index, item);
      }

      protected override void RemoveItem(int index) {
         _combinedItems = null;
         base.RemoveItem(index);
      }

      protected override void SetItem(int index, ModelValidatorProvider item) {

         if (item is null) throw new ArgumentNullException(nameof(item));

         _combinedItems = null;
         base.SetItem(index, item);
      }

      public IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context) {

         ModelValidatorProvider[] combined = this.CombinedItems;

         for (int i = 0; i < combined.Length; i++) {

            ModelValidatorProvider provider = combined[i];

            foreach (ModelValidator validator in provider.GetValidators(metadata, context)) {
               yield return validator;
            }
         }
      }
   }

   static class ModelValidatorProviders {

      public static ModelValidatorProviderCollection Providers { get; } = new ModelValidatorProviderCollection {
         new DataAnnotationsModelValidatorProvider(),
         new DataErrorInfoModelValidatorProvider(),
         new ClientDataTypeModelValidatorProvider()
      };
   }
}
