// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Xcst.Web.Mvc;

class DataErrorInfoModelValidatorProvider : ModelValidatorProvider {

   public override IEnumerable<ModelValidator>
   GetValidators(ModelMetadata metadata, ControllerContext context) {

      if (metadata is null) throw new ArgumentNullException(nameof(metadata));
      if (context is null) throw new ArgumentNullException(nameof(context));

      return GetValidatorsImpl(metadata, context);
   }

   static IEnumerable<ModelValidator>
   GetValidatorsImpl(ModelMetadata metadata, ControllerContext context) {

      // If the metadata describes a model that implements IDataErrorInfo, we should call its
      // Error property at the appropriate time.

      if (TypeImplementsIDataErrorInfo(metadata.ModelType)) {
         yield return new DataErrorInfoClassModelValidator(metadata, context);
      }

      // If the metadata describes a property of a container that implements IDataErrorInfo,
      // we should call its Item indexer at the appropriate time.

      if (TypeImplementsIDataErrorInfo(metadata.ContainerType)) {
         yield return new DataErrorInfoPropertyModelValidator(metadata, context);
      }
   }

   static bool
   TypeImplementsIDataErrorInfo(Type? type) =>
      typeof(IDataErrorInfo).IsAssignableFrom(type);

   internal sealed class DataErrorInfoClassModelValidator : ModelValidator {

      public
      DataErrorInfoClassModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
         : base(metadata, controllerContext) { }

      public override IEnumerable<ModelValidationResult>
      Validate(object? container) {

         if (/*this.Metadata.Model*/container is IDataErrorInfo castModel) {

            var errorMessage = castModel.Error;

            if (!String.IsNullOrEmpty(errorMessage)) {
               return new[] { new ModelValidationResult { Message = errorMessage } };
            }
         }

         return Enumerable.Empty<ModelValidationResult>();
      }
   }

   internal sealed class DataErrorInfoPropertyModelValidator : ModelValidator {

      public
      DataErrorInfoPropertyModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
         : base(metadata, controllerContext) { }

      public override IEnumerable<ModelValidationResult>
      Validate(object? container) {

         if (container is IDataErrorInfo castContainer
            && !String.Equals(this.Metadata.PropertyName, "error", StringComparison.OrdinalIgnoreCase)) {

            var errorMessage = castContainer[this.Metadata.PropertyName];

            if (!String.IsNullOrEmpty(errorMessage)) {
               return new[] { new ModelValidationResult { Message = errorMessage } };
            }
         }

         return Enumerable.Empty<ModelValidationResult>();
      }
   }
}
