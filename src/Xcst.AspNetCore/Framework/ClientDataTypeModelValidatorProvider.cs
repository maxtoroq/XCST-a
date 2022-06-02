// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc.Properties;
using Xcst.Web.Configuration;

namespace System.Web.Mvc;

class ClientDataTypeModelValidatorProvider : ModelValidatorProvider {

   static readonly HashSet<Type>
   _numericTypes = new(new Type[] {
      typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
      typeof(int), typeof(uint), typeof(long), typeof(ulong),
      typeof(float), typeof(double), typeof(decimal)
   });

   public override IEnumerable<ModelValidator>
   GetValidators(ModelMetadata metadata, ControllerContext context) {

      if (metadata is null) throw new ArgumentNullException(nameof(metadata));
      if (context is null) throw new ArgumentNullException(nameof(context));

      return GetValidatorsImpl(metadata, context);
   }

   static IEnumerable<ModelValidator>
   GetValidatorsImpl(ModelMetadata metadata, ControllerContext context) {

      var type = metadata.ModelType;

      if (IsDateTimeType(type, metadata)) {
         yield return new DateModelValidator(metadata, context);
      }

      if (IsNumericType(type)) {
         yield return new NumericModelValidator(metadata, context);
      }
   }

   static bool
   IsNumericType(Type type) =>
      _numericTypes.Contains(GetTypeToValidate(type));

   static bool
   IsDateTimeType(Type type, ModelMetadata metadata) =>
      typeof(DateTime) == GetTypeToValidate(type)
         && String.Equals(metadata.DataTypeName, "Date", StringComparison.OrdinalIgnoreCase);

   static Type
   GetTypeToValidate(Type type) =>
      // strip off the Nullable<>
      Nullable.GetUnderlyingType(type) ?? type;

   static string
   GetFieldMustBeNumericResource(ControllerContext controllerContext) =>
      XcstWebConfiguration.Instance.EditorTemplates.NumberValidationMessage?.Invoke()
         ?? MvcResources.ClientDataTypeModelValidatorProvider_FieldMustBeNumeric;

   static string
   GetFieldMustBeDateResource(ControllerContext controllerContext) =>
      XcstWebConfiguration.Instance.EditorTemplates.DateValidationMessage?.Invoke()
         ?? MvcResources.ClientDataTypeModelValidatorProvider_FieldMustBeDate;

   internal sealed class DateModelValidator : ClientModelValidator {

      public
      DateModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
         : base(metadata, controllerContext, "date", GetFieldMustBeDateResource(controllerContext)) { }
   }

   internal sealed class NumericModelValidator : ClientModelValidator {

      public
      NumericModelValidator(ModelMetadata metadata, ControllerContext controllerContext)
         : base(metadata, controllerContext, "number", GetFieldMustBeNumericResource(controllerContext)) { }
   }

   internal class ClientModelValidator : ModelValidator {

      string
      _errorMessage;

      string
      _validationType;

      public
      ClientModelValidator(ModelMetadata metadata, ControllerContext controllerContext, string validationType, string errorMessage)
         : base(metadata, controllerContext) {

         if (String.IsNullOrEmpty(validationType)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(validationType));
         if (String.IsNullOrEmpty(errorMessage)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(errorMessage));

         _validationType = validationType;
         _errorMessage = errorMessage;
      }

      public sealed override IEnumerable<ModelClientValidationRule>
      GetClientValidationRules() {

         var rule = new ModelClientValidationRule {
            ValidationType = _validationType,
            ErrorMessage = FormatErrorMessage(this.Metadata.GetDisplayName())
         };

         return new ModelClientValidationRule[] { rule };
      }

      string
      FormatErrorMessage(string displayName) =>
         // use CurrentCulture since this message is intended for the site visitor
         String.Format(CultureInfo.CurrentCulture, _errorMessage, displayName);

      public sealed override IEnumerable<ModelValidationResult>
      Validate(object? container) =>
         // this is not a server-side validator
         Enumerable.Empty<ModelValidationResult>();
   }
}
