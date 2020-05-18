// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Web.Mvc.Properties;
using System.Web.Security;
using DataAnnotationsCompareAttribute = System.ComponentModel.DataAnnotations.CompareAttribute;

namespace System.Web.Mvc {

   class DataAnnotationsModelValidator : ModelValidator {

      protected internal ValidationAttribute Attribute { get; private set; }

      protected internal string ErrorMessage => Attribute.FormatErrorMessage(Metadata.GetDisplayName());

      public override bool IsRequired => Attribute is RequiredAttribute;

      public DataAnnotationsModelValidator(ModelMetadata metadata, ControllerContext context, ValidationAttribute attribute)
         : base(metadata, context) {

         if (attribute is null) throw new ArgumentNullException(nameof(attribute));

         this.Attribute = attribute;
      }

      internal static ModelValidator Create(ModelMetadata metadata, ControllerContext context, ValidationAttribute attribute) =>
         new DataAnnotationsModelValidator(metadata, context, attribute);

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {

         IEnumerable<ModelClientValidationRule> results = base.GetClientValidationRules();

         if (this.Attribute is IClientValidatable clientValidatable) {
            results = results.Concat(clientValidatable.GetClientValidationRules(this.Metadata, ControllerContext));
         }

         return results;
      }

      public override IEnumerable<ModelValidationResult> Validate(object container) {

         // Per the WCF RIA Services team, instance can never be null (if you have
         // no parent, you pass yourself for the "instance" parameter).

         string memberName = this.Metadata.PropertyName ?? this.Metadata.ModelType.Name;

         var context = new ValidationContext(container ?? this.Metadata.Model) {
            DisplayName = this.Metadata.GetDisplayName(),
            MemberName = memberName
         };

         ValidationResult result = this.Attribute.GetValidationResult(this.Metadata.Model, context);

         if (result != ValidationResult.Success) {

            // ModelValidationResult.MemberName is used by invoking validators (such as ModelValidator) to 
            // construct the ModelKey for ModelStateDictionary. When validating at type level we want to append the 
            // returned MemberNames if specified (e.g. person.Address.FirstName). For property validation, the 
            // ModelKey can be constructed using the ModelMetadata and we should ignore MemberName (we don't want 
            // (person.Name.Name). However the invoking validator does not have a way to distinguish between these two 
            // cases. Consequently we'll only set MemberName if this validation returns a MemberName that is different
            // from the property being validated.

            string errorMemberName = result.MemberNames.FirstOrDefault();

            if (String.Equals(errorMemberName, memberName, StringComparison.Ordinal)) {
               errorMemberName = null;
            }

            var validationResult = new ModelValidationResult {
               Message = result.ErrorMessage,
               MemberName = errorMemberName
            };

            return new ModelValidationResult[] { validationResult };
         }

         return Enumerable.Empty<ModelValidationResult>();
      }
   }

   class DataAnnotationsModelValidator<TAttribute> : DataAnnotationsModelValidator
      where TAttribute : ValidationAttribute {

      protected new TAttribute Attribute => (TAttribute)base.Attribute;

      public DataAnnotationsModelValidator(ModelMetadata metadata, ControllerContext context, TAttribute attribute)
         : base(metadata, context, attribute) { }
   }

   class CompareAttributeAdapter : DataAnnotationsModelValidator<DataAnnotationsCompareAttribute> {

      public CompareAttributeAdapter(ModelMetadata metadata, ControllerContext context, DataAnnotationsCompareAttribute attribute)
         : base(metadata, context, new CompareAttributeWrapper(attribute, metadata)) {

         Contract.Assert(attribute.GetType() == typeof(DataAnnotationsCompareAttribute));
      }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
         yield return new ModelClientValidationEqualToRule(this.ErrorMessage, FormatPropertyForClientValidation(this.Attribute.OtherProperty));
      }

      static string FormatPropertyForClientValidation(string property) {

         Contract.Assert(property != null);

         return "*." + property;
      }

      // Wrapper for CompareAttribute that will eagerly get the OtherPropertyDisplayName and use it on the error message for client validation.
      // The System.ComponentModel.DataAnnotations.CompareAttribute doesn't populate the OtherPropertyDisplayName until after IsValid() 
      // is called. Therefore, by the time we get the error message for client validation, the display name is not populated and won't be used.

      sealed class CompareAttributeWrapper : DataAnnotationsCompareAttribute {

         readonly string _otherPropertyDisplayName;

         public CompareAttributeWrapper(DataAnnotationsCompareAttribute attribute, ModelMetadata metadata)
            : base(attribute.OtherProperty) {

            _otherPropertyDisplayName = attribute.OtherPropertyDisplayName;

            if (_otherPropertyDisplayName is null
               && metadata.ContainerType != null) {

               _otherPropertyDisplayName = ModelMetadataProviders.Current.GetMetadataForProperty(() => metadata.Model, metadata.ContainerType, attribute.OtherProperty).GetDisplayName();
            }

            if (_otherPropertyDisplayName is null) {
               _otherPropertyDisplayName = attribute.OtherProperty;
            }

            // Copy settable properties from wrapped attribute. Don't reset default message accessor (set as
            // CompareAttribute constructor calls ValidationAttribute constructor) when all properties are null to
            // preserve default error message. Reset the message accessor when just ErrorMessageResourceType is
            // non-null to ensure correct InvalidOperationException.

            if (!String.IsNullOrEmpty(attribute.ErrorMessage)
               || !String.IsNullOrEmpty(attribute.ErrorMessageResourceName)
               || attribute.ErrorMessageResourceType != null) {

               this.ErrorMessage = attribute.ErrorMessage;
               this.ErrorMessageResourceName = attribute.ErrorMessageResourceName;
               this.ErrorMessageResourceType = attribute.ErrorMessageResourceType;
            }
         }

         public override string FormatErrorMessage(string name) =>
            String.Format(CultureInfo.CurrentCulture, this.ErrorMessageString, name, _otherPropertyDisplayName);
      }
   }

   /// <summary>
   /// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation rule.
   /// </summary>
   class DataTypeAttributeAdapter : DataAnnotationsModelValidator {

      public string RuleName { get; set; }

      public DataTypeAttributeAdapter(ModelMetadata metadata, ControllerContext context, DataTypeAttribute attribute, string ruleName)
         : base(metadata, context, attribute) {

         if (String.IsNullOrEmpty(ruleName)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(ruleName));

         this.RuleName = ruleName;
      }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
         yield return new ModelClientValidationRule {
            ValidationType = RuleName,
            ErrorMessage = ErrorMessage
         };
      }
   }

   class FileExtensionsAttributeAdapter : DataAnnotationsModelValidator<FileExtensionsAttribute> {

      public FileExtensionsAttributeAdapter(ModelMetadata metadata, ControllerContext context, FileExtensionsAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {

         var rule = new ModelClientValidationRule {
            ValidationType = "extension",
            ErrorMessage = this.ErrorMessage
         };

         rule.ValidationParameters["extension"] = this.Attribute.Extensions;

         yield return rule;
      }
   }

   class MaxLengthAttributeAdapter : DataAnnotationsModelValidator<MaxLengthAttribute> {

      public MaxLengthAttributeAdapter(ModelMetadata metadata, ControllerContext context, MaxLengthAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
         return new[] { new ModelClientValidationMaxLengthRule(this.ErrorMessage, this.Attribute.Length) };
      }
   }

   class MembershipPasswordAttributeAdapter : DataAnnotationsModelValidator<MembershipPasswordAttribute> {

      public MembershipPasswordAttributeAdapter(ModelMetadata metadata, ControllerContext context, MembershipPasswordAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {
         yield return new ModelClientValidationMembershipPasswordRule(this.ErrorMessage, this.Attribute.MinRequiredPasswordLength, this.Attribute.MinRequiredNonAlphanumericCharacters, this.Attribute.PasswordStrengthRegularExpression);
      }
   }

   class MinLengthAttributeAdapter : DataAnnotationsModelValidator<MinLengthAttribute> {

      public MinLengthAttributeAdapter(ModelMetadata metadata, ControllerContext context, MinLengthAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() =>
         new[] { new ModelClientValidationMinLengthRule(this.ErrorMessage, this.Attribute.Length) };
   }

   class RangeAttributeAdapter : DataAnnotationsModelValidator<RangeAttribute> {

      public RangeAttributeAdapter(ModelMetadata metadata, ControllerContext context, RangeAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() {

         string errorMessage = this.ErrorMessage; // Per Dev10 Bug #923283, need to make sure ErrorMessage is called before Minimum/Maximum

         return new[] { new ModelClientValidationRangeRule(errorMessage, this.Attribute.Minimum, this.Attribute.Maximum) };
      }
   }

   class RegularExpressionAttributeAdapter : DataAnnotationsModelValidator<RegularExpressionAttribute> {

      public RegularExpressionAttributeAdapter(ModelMetadata metadata, ControllerContext context, RegularExpressionAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() =>
         new[] { new ModelClientValidationRegexRule(this.ErrorMessage, this.Attribute.Pattern) };
   }

   class RequiredAttributeAdapter : DataAnnotationsModelValidator<RequiredAttribute> {

      public RequiredAttributeAdapter(ModelMetadata metadata, ControllerContext context, RequiredAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() =>
         new[] { new ModelClientValidationRequiredRule(this.ErrorMessage) };
   }

   class StringLengthAttributeAdapter : DataAnnotationsModelValidator<StringLengthAttribute> {

      public StringLengthAttributeAdapter(ModelMetadata metadata, ControllerContext context, StringLengthAttribute attribute)
         : base(metadata, context, attribute) { }

      public override IEnumerable<ModelClientValidationRule> GetClientValidationRules() =>
         new[] { new ModelClientValidationStringLengthRule(this.ErrorMessage, this.Attribute.MinimumLength, this.Attribute.MaximumLength) };
   }
}
