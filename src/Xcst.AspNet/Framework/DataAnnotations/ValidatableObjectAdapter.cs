﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   class ValidatableObjectAdapter : ModelValidator {

      public ValidatableObjectAdapter(ModelMetadata metadata, ControllerContext context)
         : base(metadata, context) { }

      public override IEnumerable<ModelValidationResult> Validate(object? container) {

         // NOTE: Container is never used here, because IValidatableObject doesn't give you
         // any way to get access to your container.

         object? model = this.Metadata.Model;

         if (model is null) {
            return Enumerable.Empty<ModelValidationResult>();
         }

         var validatable = model as IValidatableObject
            ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, MvcResources.ValidatableObjectAdapter_IncompatibleType,
                  typeof(IValidatableObject).FullName, model.GetType().FullName));

         var validationContext = new ValidationContext(validatable, null, null);

         return ConvertResults(validatable.Validate(validationContext));
      }

      IEnumerable<ModelValidationResult> ConvertResults(IEnumerable<ValidationResult> results) {

         foreach (ValidationResult result in results) {

            if (result != ValidationResult.Success) {
               if (result.MemberNames is null || !result.MemberNames.Any()) {
                  yield return new ModelValidationResult { Message = result.ErrorMessage };
               } else {
                  foreach (string memberName in result.MemberNames) {
                     yield return new ModelValidationResult { Message = result.ErrorMessage, MemberName = memberName };
                  }
               }
            }
         }
      }
   }
}
