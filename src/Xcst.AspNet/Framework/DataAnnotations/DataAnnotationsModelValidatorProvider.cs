﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Mvc.Properties;
using System.Web.Security;
using DataAnnotationsCompareAttribute = System.ComponentModel.DataAnnotations.CompareAttribute;

namespace System.Web.Mvc {

   // A factory for validators based on ValidationAttribute

   delegate ModelValidator DataAnnotationsModelValidationFactory(ModelMetadata metadata, ControllerContext context, ValidationAttribute attribute);

   // A factory for validators based on IValidatableObject

   delegate ModelValidator DataAnnotationsValidatableObjectAdapterFactory(ModelMetadata metadata, ControllerContext context);

   /// <summary>
   /// An implementation of <see cref="ModelValidatorProvider"/> which providers validators
   /// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
   /// a validator for types which implement <see cref="IValidatableObject"/>. To support
   /// client side validation, you can either register adapters through the static methods
   /// on this class, or by having your validation attributes implement
   /// <see cref="IClientValidatable"/>. The logic to support IClientValidatable
   /// is implemented in <see cref="DataAnnotationsModelValidator"/>.
   /// </summary>
   class DataAnnotationsModelValidatorProvider : AssociatedValidatorProvider {

      static bool _addImplicitRequiredAttributeForValueTypes = true;
      static ReaderWriterLockSlim _adaptersLock = new ReaderWriterLockSlim();

      // Factories for validation attributes

      internal static DataAnnotationsModelValidationFactory DefaultAttributeFactory =
         (metadata, context, attribute) => new DataAnnotationsModelValidator(metadata, context, attribute);

      internal static Dictionary<Type, DataAnnotationsModelValidationFactory> AttributeFactories = BuildAttributeFactoriesDictionary();

      // Factories for IValidatableObject models

      internal static DataAnnotationsValidatableObjectAdapterFactory DefaultValidatableFactory =
         (metadata, context) => new ValidatableObjectAdapter(metadata, context);

      internal static Dictionary<Type, DataAnnotationsValidatableObjectAdapterFactory> ValidatableFactories = new Dictionary<Type, DataAnnotationsValidatableObjectAdapterFactory>();

      public static bool AddImplicitRequiredAttributeForValueTypes {
         get { return _addImplicitRequiredAttributeForValueTypes; }
         set { _addImplicitRequiredAttributeForValueTypes = value; }
      }

      protected override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, ControllerContext context, IEnumerable<Attribute> attributes) {

         _adaptersLock.EnterReadLock();

         try {
            var results = new List<ModelValidator>();

            // Add an implied [Required] attribute for any non-nullable value type,
            // unless they've configured us not to do that.

            if (AddImplicitRequiredAttributeForValueTypes
               && metadata.IsRequired
               && !attributes.Any(a => a is RequiredAttribute)) {

               attributes = attributes.Concat(new[] { new RequiredAttribute() });
            }

            // Produce a validator for each validation attribute we find

            foreach (ValidationAttribute attribute in attributes.OfType<ValidationAttribute>()) {

               DataAnnotationsModelValidationFactory factory;

               Type attrType = attribute.GetType();

               if (!AttributeFactories.TryGetValue(attrType, out factory)
                   && (factory = AttributeFactories.Where(pair => pair.Key.IsAssignableFrom(attrType))
                       .Select(pair => pair.Value)
                       .FirstOrDefault()) == null) {
                  factory = DefaultAttributeFactory;
               }

               results.Add(factory(metadata, context, attribute));
            }

            // Produce a validator if the type supports IValidatableObject

            if (typeof(IValidatableObject).IsAssignableFrom(metadata.ModelType)) {

               DataAnnotationsValidatableObjectAdapterFactory factory;

               if (!ValidatableFactories.TryGetValue(metadata.ModelType, out factory)) {
                  factory = DefaultValidatableFactory;
               }

               results.Add(factory(metadata, context));
            }

            return results;

         } finally {
            _adaptersLock.ExitReadLock();
         }
      }

      #region Validation attribute adapter registration

      public static void RegisterAdapter(Type attributeType, Type adapterType) {

         ValidateAttributeType(attributeType);
         ValidateAttributeAdapterType(adapterType);
         ConstructorInfo constructor = GetAttributeAdapterConstructor(attributeType, adapterType);

         _adaptersLock.EnterWriteLock();

         try {
            AttributeFactories[attributeType] = (metadata, context, attribute) => (ModelValidator)constructor.Invoke(new object[] { metadata, context, attribute });
         } finally {
            _adaptersLock.ExitWriteLock();
         }
      }

      public static void RegisterAdapterFactory(Type attributeType, DataAnnotationsModelValidationFactory factory) {

         ValidateAttributeType(attributeType);
         ValidateAttributeFactory(factory);

         _adaptersLock.EnterWriteLock();

         try {
            AttributeFactories[attributeType] = factory;
         } finally {
            _adaptersLock.ExitWriteLock();
         }
      }

      public static void RegisterDefaultAdapter(Type adapterType) {

         ValidateAttributeAdapterType(adapterType);
         ConstructorInfo constructor = GetAttributeAdapterConstructor(typeof(ValidationAttribute), adapterType);

         DefaultAttributeFactory = (metadata, context, attribute) => (ModelValidator)constructor.Invoke(new object[] { metadata, context, attribute });
      }

      public static void RegisterDefaultAdapterFactory(DataAnnotationsModelValidationFactory factory) {

         ValidateAttributeFactory(factory);

         DefaultAttributeFactory = factory;
      }

      // Helpers 

      static ConstructorInfo GetAttributeAdapterConstructor(Type attributeType, Type adapterType) {

         ConstructorInfo constructor = adapterType.GetConstructor(new[] { typeof(ModelMetadata), typeof(ControllerContext), attributeType })
            ?? throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, MvcResources.DataAnnotationsModelValidatorProvider_ConstructorRequirements,
                  adapterType.FullName, typeof(ModelMetadata).FullName, typeof(ControllerContext).FullName, attributeType.FullName), nameof(adapterType));

         return constructor;
      }

      static void ValidateAttributeAdapterType(Type adapterType) {

         if (adapterType == null) throw new ArgumentNullException(nameof(adapterType));

         if (!typeof(ModelValidator).IsAssignableFrom(adapterType)) {
            throw new ArgumentException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.Common_TypeMustDriveFromType,
                  adapterType.FullName,
                  typeof(ModelValidator).FullName),
               nameof(adapterType));
         }
      }

      static void ValidateAttributeType(Type attributeType) {

         if (attributeType == null) throw new ArgumentNullException(nameof(attributeType));

         if (!typeof(ValidationAttribute).IsAssignableFrom(attributeType)) {
            throw new ArgumentException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.Common_TypeMustDriveFromType,
                  attributeType.FullName,
                  typeof(ValidationAttribute).FullName),
               nameof(attributeType));
         }
      }

      static void ValidateAttributeFactory(DataAnnotationsModelValidationFactory factory) {

         if (factory == null) throw new ArgumentNullException(nameof(factory));
      }

      #endregion

      #region IValidatableObject adapter registration

      /// <summary>
      /// Registers an adapter type for the given <paramref name="modelType"/>, which must
      /// implement <see cref="IValidatableObject"/>. The adapter type must derive from
      /// <see cref="ModelValidator"/> and it must contain a public constructor
      /// which takes two parameters of types <see cref="ModelMetadata"/> and
      /// <see cref="ControllerContext"/>.
      /// </summary>
      public static void RegisterValidatableObjectAdapter(Type modelType, Type adapterType) {

         ValidateValidatableModelType(modelType);
         ValidateValidatableAdapterType(adapterType);
         ConstructorInfo constructor = GetValidatableAdapterConstructor(adapterType);

         _adaptersLock.EnterWriteLock();

         try {
            ValidatableFactories[modelType] = (metadata, context) => (ModelValidator)constructor.Invoke(new object[] { metadata, context });
         } finally {
            _adaptersLock.ExitWriteLock();
         }
      }

      /// <summary>
      /// Registers an adapter factory for the given <paramref name="modelType"/>, which must
      /// implement <see cref="IValidatableObject"/>.
      /// </summary>
      public static void RegisterValidatableObjectAdapterFactory(Type modelType, DataAnnotationsValidatableObjectAdapterFactory factory) {

         ValidateValidatableModelType(modelType);
         ValidateValidatableFactory(factory);

         _adaptersLock.EnterWriteLock();

         try {
            ValidatableFactories[modelType] = factory;
         } finally {
            _adaptersLock.ExitWriteLock();
         }
      }

      /// <summary>
      /// Registers the default adapter type for objects which implement
      /// <see cref="IValidatableObject"/>. The adapter type must derive from
      /// <see cref="ModelValidator"/> and it must contain a public constructor
      /// which takes two parameters of types <see cref="ModelMetadata"/> and
      /// <see cref="ControllerContext"/>.
      /// </summary>
      public static void RegisterDefaultValidatableObjectAdapter(Type adapterType) {

         ValidateValidatableAdapterType(adapterType);
         ConstructorInfo constructor = GetValidatableAdapterConstructor(adapterType);

         DefaultValidatableFactory = (metadata, context) => (ModelValidator)constructor.Invoke(new object[] { metadata, context });
      }

      /// <summary>
      /// Registers the default adapter factory for objects which implement
      /// <see cref="IValidatableObject"/>.
      /// </summary>
      public static void RegisterDefaultValidatableObjectAdapterFactory(DataAnnotationsValidatableObjectAdapterFactory factory) {

         ValidateValidatableFactory(factory);

         DefaultValidatableFactory = factory;
      }

      // Helpers 

      static ConstructorInfo GetValidatableAdapterConstructor(Type adapterType) {

         ConstructorInfo constructor = adapterType.GetConstructor(new[] { typeof(ModelMetadata), typeof(ControllerContext) })
            ?? throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, MvcResources.DataAnnotationsModelValidatorProvider_ValidatableConstructorRequirements,
                  adapterType.FullName, typeof(ModelMetadata).FullName, typeof(ControllerContext).FullName), nameof(adapterType));

         return constructor;
      }

      static void ValidateValidatableAdapterType(Type adapterType) {

         if (adapterType == null) throw new ArgumentNullException(nameof(adapterType));

         if (!typeof(ModelValidator).IsAssignableFrom(adapterType)) {
            throw new ArgumentException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.Common_TypeMustDriveFromType,
                  adapterType.FullName,
                  typeof(ModelValidator).FullName),
               nameof(adapterType));
         }
      }

      static void ValidateValidatableModelType(Type modelType) {

         if (modelType == null) throw new ArgumentNullException(nameof(modelType));

         if (!typeof(IValidatableObject).IsAssignableFrom(modelType)) {
            throw new ArgumentException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.Common_TypeMustDriveFromType,
                  modelType.FullName,
                  typeof(IValidatableObject).FullName),
               nameof(modelType));
         }
      }

      static void ValidateValidatableFactory(DataAnnotationsValidatableObjectAdapterFactory factory) {

         if (factory == null) throw new ArgumentNullException(nameof(factory));
      }

      #endregion

      static Dictionary<Type, DataAnnotationsModelValidationFactory> BuildAttributeFactoriesDictionary() {

         var dict = new Dictionary<Type, DataAnnotationsModelValidationFactory>();

         AddValidationAttributeAdapter(dict, typeof(RangeAttribute),
            (metadata, context, attribute) => new RangeAttributeAdapter(metadata, context, (RangeAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(RegularExpressionAttribute),
            (metadata, context, attribute) => new RegularExpressionAttributeAdapter(metadata, context, (RegularExpressionAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(RequiredAttribute),
            (metadata, context, attribute) => new RequiredAttributeAdapter(metadata, context, (RequiredAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(StringLengthAttribute),
            (metadata, context, attribute) => new StringLengthAttributeAdapter(metadata, context, (StringLengthAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(MaxLengthAttribute),
            (metadata, context, attribute) => new MaxLengthAttributeAdapter(metadata, context, (MaxLengthAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(MinLengthAttribute),
            (metadata, context, attribute) => new MinLengthAttributeAdapter(metadata, context, (MinLengthAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(MembershipPasswordAttribute),
            (metadata, context, attribute) => new MembershipPasswordAttributeAdapter(metadata, context, (MembershipPasswordAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(DataAnnotationsCompareAttribute),
            (metadata, context, attribute) => new CompareAttributeAdapter(metadata, context, (DataAnnotationsCompareAttribute)attribute));

         AddValidationAttributeAdapter(dict, typeof(FileExtensionsAttribute),
            (metadata, context, attribute) => new FileExtensionsAttributeAdapter(metadata, context, (FileExtensionsAttribute)attribute));

         AddDataTypeAttributeAdapter(dict, typeof(CreditCardAttribute), "creditcard");
         AddDataTypeAttributeAdapter(dict, typeof(EmailAddressAttribute), "email");
         AddDataTypeAttributeAdapter(dict, typeof(PhoneAttribute), "phone");
         AddDataTypeAttributeAdapter(dict, typeof(UrlAttribute), "url");

         return dict;
      }

      static void AddValidationAttributeAdapter(Dictionary<Type, DataAnnotationsModelValidationFactory> dictionary, Type validataionAttributeType, DataAnnotationsModelValidationFactory factory) {

         Contract.Assert(dictionary != null);

         if (validataionAttributeType != null) {
            dictionary.Add(validataionAttributeType, factory);
         }
      }

      static void AddDataTypeAttributeAdapter(Dictionary<Type, DataAnnotationsModelValidationFactory> dictionary, Type attributeType, string ruleName) {

         AddValidationAttributeAdapter(
            dictionary,
            attributeType,
            (metadata, context, attribute) => new DataTypeAttributeAdapter(metadata, context, (DataTypeAttribute)attribute, ruleName));
      }
   }
}
