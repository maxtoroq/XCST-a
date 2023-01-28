// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web.Mvc;

abstract class ModelValidator {

   protected internal ControllerContext
   ControllerContext { get; private set; }

   protected internal ModelMetadata
   Metadata { get; private set; }

   public virtual bool
   IsRequired => false;

   protected
   ModelValidator(ModelMetadata metadata, ControllerContext controllerContext) {

      if (metadata is null) throw new ArgumentNullException(nameof(metadata));
      if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

      this.Metadata = metadata;
      this.ControllerContext = controllerContext;
   }

   [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method may perform non-trivial work.")]
   public virtual IEnumerable<ModelClientValidationRule>
   GetClientValidationRules() =>
      Enumerable.Empty<ModelClientValidationRule>();

   public static ModelValidator
   GetModelValidator(ModelMetadata metadata, ControllerContext context) =>
      new CompositeModelValidator(metadata, context);

   public abstract IEnumerable<ModelValidationResult>
   Validate(object? container);

   class CompositeModelValidator : ModelValidator {

      readonly IModelMetadataProvider
      _metadataProvider;

      public
      CompositeModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
         : base(metadata, controllerContext) {

         _metadataProvider = controllerContext.HttpContext.RequestServices
            .GetRequiredService<IModelMetadataProvider>();
      }

      static ModelValidationResult
      CreateSubPropertyResult(ModelExplorer propertyExplorer, ModelValidationResult propertyResult) =>
         new ModelValidationResult {
            MemberName = CreateSubPropertyName(propertyExplorer.Metadata.PropertyName, propertyResult.MemberName),
            Message = propertyResult.Message
         };

      static string
      CreateSubPropertyName(string? prefix, string propertyName) {

         if (String.IsNullOrEmpty(prefix)) {
            return propertyName;
         } else if (String.IsNullOrEmpty(propertyName)) {
            return prefix!;
         } else {
            return prefix + "." + propertyName;
         }
      }

      public override IEnumerable<ModelValidationResult>
      Validate(object? container) {

         var modelExplorer = new ModelExplorer(_metadataProvider, this.Metadata, container);

         var propertiesValid = true;
         var properties = modelExplorer.Properties.ToArray();

         // Performance sensitive loops

         for (int propertyIndex = 0; propertyIndex < properties.Length; propertyIndex++) {

            var propertyExplorer = properties[propertyIndex];

            foreach (var propertyValidator in GetValidators(propertyExplorer.Metadata, this.ControllerContext)) {

               foreach (var propertyResult in propertyValidator.Validate(propertyExplorer.Model)) {
                  propertiesValid = false;
                  yield return CreateSubPropertyResult(propertyExplorer, propertyResult);
               }
            }
         }

         if (propertiesValid) {
            foreach (var typeValidator in GetValidators(this.Metadata, this.ControllerContext)) {
               foreach (var typeResult in typeValidator.Validate(container)) {
                  yield return typeResult;
               }
            }
         }
      }

      static IEnumerable<ModelValidator>
      GetValidators(ModelMetadata metadata, ControllerContext context) =>
         ModelValidatorProviders.Providers.GetValidators(metadata, context);
   }
}

class ModelValidationResult {

   string?
   _memberName;

   string?
   _message;

   [AllowNull]
   public string
   MemberName {
      get => _memberName ?? String.Empty;
      set => _memberName = value;
   }

   [AllowNull]
   public string
   Message {
      get => _message ?? String.Empty;
      set => _message = value;
   }
}

abstract class ModelValidatorProvider {

   public abstract IEnumerable<ModelValidator>
   GetValidators(ModelMetadata metadata, ControllerContext context);
}

class EmptyModelValidatorProvider : ModelValidatorProvider {

   public override IEnumerable<ModelValidator>
   GetValidators(ModelMetadata metadata, ControllerContext context) =>
      Enumerable.Empty<ModelValidator>();
}

class ModelValidatorProviderCollection : Collection<ModelValidatorProvider> {

   ModelValidatorProvider[]?
   _combinedItems;

   IDependencyResolver?
   _dependencyResolver;

   internal ModelValidatorProvider[]
   CombinedItems {
      get {
         var combinedItems = _combinedItems;

         if (combinedItems is null) {
            combinedItems = MultiServiceResolver.GetCombined(Items, _dependencyResolver);
            _combinedItems = combinedItems;
         }

         return combinedItems;
      }
   }

   public
   ModelValidatorProviderCollection() { }

   public
   ModelValidatorProviderCollection(IList<ModelValidatorProvider> list)
      : base(list) { }

   internal
   ModelValidatorProviderCollection(IList<ModelValidatorProvider> list, IDependencyResolver dependencyResolver)
      : base(list) {

      _dependencyResolver = dependencyResolver;
   }

   protected override void
   ClearItems() {
      _combinedItems = null;
      base.ClearItems();
   }

   protected override void
   InsertItem(int index, ModelValidatorProvider item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      _combinedItems = null;
      base.InsertItem(index, item);
   }

   protected override void
   RemoveItem(int index) {
      _combinedItems = null;
      base.RemoveItem(index);
   }

   protected override void
   SetItem(int index, ModelValidatorProvider item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      _combinedItems = null;
      base.SetItem(index, item);
   }

   public IEnumerable<ModelValidator>
   GetValidators(ModelMetadata metadata, ControllerContext context) {

      var combined = this.CombinedItems;

      for (int i = 0; i < combined.Length; i++) {

         var provider = combined[i];

         foreach (var validator in provider.GetValidators(metadata, context)) {
            yield return validator;
         }
      }
   }
}

static class ModelValidatorProviders {

   public static ModelValidatorProviderCollection
   Providers { get; } = new() {
      new DataAnnotationsModelValidatorProvider(),
      new DataErrorInfoModelValidatorProvider(),
      new ClientDataTypeModelValidatorProvider()
   };
}
