﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web.Mvc.Properties;
using Xcst.Web.Configuration;

namespace System.Web.Mvc {

   public class DefaultModelBinder : IModelBinder {

      ModelBinderDictionary?
      _binders;

      [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Property is settable so that the dictionary can be provided for unit testing purposes.")]
      protected internal ModelBinderDictionary
      Binders {
         get => _binders ??= ModelBinders.Binders;
         set => _binders = value;
      }

      static void
      AddValueRequiredMessageToModelState(ControllerContext controllerContext, ModelStateDictionary modelState, string modelStateKey, Type elementType, object? value) {

         if (value is null
            && !TypeHelpers.TypeAllowsNullValue(elementType)
            && modelState.IsValidField(modelStateKey)) {

            modelState.AddModelError(modelStateKey, GetValueRequiredResource(controllerContext));
         }
      }

      internal void
      BindComplexElementalModel(ControllerContext controllerContext, ModelBindingContext bindingContext, object model) {

         // need to replace the property filter + model object and create an inner binding context

         var newBindingContext = CreateComplexElementalModelBindingContext(controllerContext, bindingContext, model);

         // validation

         if (OnModelUpdating(controllerContext, newBindingContext)) {
            BindProperties(controllerContext, newBindingContext);
            OnModelUpdated(controllerContext, newBindingContext);
         }
      }

      internal object?
      BindComplexModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         var model = bindingContext.Model;
         var modelType = bindingContext.ModelType;

         // if we're being asked to create an array, create a list instead, then coerce to an array after the list is created

         if (model is null && modelType.IsArray) {

            var elementType = modelType.GetElementType();
            var listType = typeof(List<>).MakeGenericType(elementType);
            var collection = CreateModel(controllerContext, bindingContext, listType);

            var arrayBindingContext = new ModelBindingContext {
               ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => collection, listType),
               ModelName = bindingContext.ModelName,
               ModelState = bindingContext.ModelState,
               PropertyFilter = bindingContext.PropertyFilter,
               ValueProvider = bindingContext.ValueProvider
            };

            var list = (IList?)UpdateCollection(controllerContext, arrayBindingContext, elementType);

            if (list is null) {
               return null;
            }

            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            return array;
         }

         if (model is null) {
            model = CreateModel(controllerContext, bindingContext, modelType);
         }

         // special-case IDictionary<,> and ICollection<>

         var dictionaryType = TypeHelpers.ExtractGenericInterface(modelType, typeof(IDictionary<,>));

         if (dictionaryType != null) {

            var genericArguments = dictionaryType.GetGenericArguments();
            var keyType = genericArguments[0];
            var valueType = genericArguments[1];

            var dictionaryBindingContext = new ModelBindingContext {
               ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, modelType),
               ModelName = bindingContext.ModelName,
               ModelState = bindingContext.ModelState,
               PropertyFilter = bindingContext.PropertyFilter,
               ValueProvider = bindingContext.ValueProvider
            };

            var dictionary = UpdateDictionary(controllerContext, dictionaryBindingContext, keyType, valueType);

            return dictionary;
         }

         var enumerableType = TypeHelpers.ExtractGenericInterface(modelType, typeof(IEnumerable<>));

         if (enumerableType != null) {

            var elementType = enumerableType.GetGenericArguments()[0];
            var collectionType = typeof(ICollection<>).MakeGenericType(elementType);

            if (collectionType.IsInstanceOfType(model)) {

               var collectionBindingContext = new ModelBindingContext {
                  ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, modelType),
                  ModelName = bindingContext.ModelName,
                  ModelState = bindingContext.ModelState,
                  PropertyFilter = bindingContext.PropertyFilter,
                  ValueProvider = bindingContext.ValueProvider
               };

               var collection = UpdateCollection(controllerContext, collectionBindingContext, elementType);

               return collection;
            }
         }

         // otherwise, just update the properties on the complex type

         BindComplexElementalModel(controllerContext, bindingContext, model);

         return model;
      }

      public virtual object?
      BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         RuntimeHelpers.EnsureSufficientExecutionStack();

         if (bindingContext is null) throw new ArgumentNullException(nameof(bindingContext));

         var performedFallback = false;

         if (!String.IsNullOrEmpty(bindingContext.ModelName)
            && !bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName!)) {

            // We couldn't find any entry that began with the prefix. If this is the top-level element, fall back
            // to the empty prefix.

            if (bindingContext.FallbackToEmptyPrefix) {

               bindingContext = new ModelBindingContext {
                  ModelMetadata = bindingContext.ModelMetadata,
                  ModelState = bindingContext.ModelState,
                  PropertyFilter = bindingContext.PropertyFilter,
                  ValueProvider = bindingContext.ValueProvider
               };

               performedFallback = true;

            } else {
               return null;
            }
         }

         // Simple model = int, string, etc.; determined by calling TypeConverter.CanConvertFrom(typeof(string))
         // or by seeing if a value in the request exactly matches the name of the model we're binding.
         // Complex type = everything else.

         if (!performedFallback) {

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult != null) {
               return BindSimpleModel(controllerContext, bindingContext, valueProviderResult);
            }
         }

         if (!bindingContext.ModelMetadata.IsComplexType) {
            return null;
         }

         return BindComplexModel(controllerContext, bindingContext);
      }

      void
      BindProperties(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         var properties = GetModelProperties(controllerContext, bindingContext);
         var propertyFilter = bindingContext.PropertyFilter;

         // Loop is a performance sensitive codepath so avoid using enumerators.

         for (int i = 0; i < properties.Count; i++) {

            var property = properties[i];

            if (ShouldUpdateProperty(property, propertyFilter)) {
               BindProperty(controllerContext, bindingContext, property);
            }
         }
      }

      protected virtual void
      BindProperty(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor) {

         // need to skip properties that aren't part of the request, else we might hit a StackOverflowException

         var fullPropertyKey = CreateSubPropertyName(bindingContext.ModelName, propertyDescriptor.Name);

         if (!bindingContext.ValueProvider.ContainsPrefix(fullPropertyKey)) {
            return;
         }

         // call into the property's model binder

         var propertyBinder = Binders.GetBinder(propertyDescriptor.PropertyType);
         var originalPropertyValue = propertyDescriptor.GetValue(bindingContext.Model);
         var propertyMetadata = bindingContext.PropertyMetadata[propertyDescriptor.Name];
         propertyMetadata.Model = originalPropertyValue;

         var innerBindingContext = new ModelBindingContext {
            ModelMetadata = propertyMetadata,
            ModelName = fullPropertyKey,
            ModelState = bindingContext.ModelState,
            ValueProvider = bindingContext.ValueProvider
         };

         var newPropertyValue = GetPropertyValue(controllerContext, innerBindingContext, propertyDescriptor, propertyBinder);

         propertyMetadata.Model = newPropertyValue;

         // validation

         var modelState = bindingContext.ModelState[fullPropertyKey];

         if (modelState is null
            || modelState.Errors.Count == 0) {

            if (OnPropertyValidating(controllerContext, bindingContext, propertyDescriptor, newPropertyValue)) {
               SetProperty(controllerContext, bindingContext, propertyDescriptor, newPropertyValue);
               OnPropertyValidated(controllerContext, bindingContext, propertyDescriptor, newPropertyValue);
            }

         } else {

            SetProperty(controllerContext, bindingContext, propertyDescriptor, newPropertyValue);

            // Convert FormatExceptions (type conversion failures) into InvalidValue messages

            foreach (var error in modelState.Errors.Where(err => String.IsNullOrEmpty(err.ErrorMessage) && err.Exception != null).ToList()) {

               for (Exception? exception = error.Exception; exception != null; exception = exception.InnerException) {

                  // We only consider "known" type of exception and do not make too aggressive changes here

                  if (exception is FormatException || exception is OverflowException) {

                     var displayName = propertyMetadata.GetDisplayName();
                     var errorMessageTemplate = GetValueInvalidResource(controllerContext);
                     var errorMessage = String.Format(CultureInfo.CurrentCulture, errorMessageTemplate, modelState.Value?.AttemptedValue, displayName);

                     modelState.Errors.Remove(error);
                     modelState.Errors.Add(errorMessage);

                     break;
                  }
               }
            }
         }
      }

      internal object?
      BindSimpleModel(ControllerContext controllerContext, ModelBindingContext bindingContext, ValueProviderResult valueProviderResult) {

         bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

         // if the value provider returns an instance of the requested data type, we can just short-circuit
         // the evaluation and return that instance

         if (bindingContext.ModelType.IsInstanceOfType(valueProviderResult.RawValue)) {
            return valueProviderResult.RawValue;
         }

         // since a string is an IEnumerable<char>, we want it to skip the two checks immediately following

         if (bindingContext.ModelType != typeof(string)) {

            // conversion results in 3 cases, as below

            if (bindingContext.ModelType.IsArray) {

               // case 1: user asked for an array
               // ValueProviderResult.ConvertTo() understands array types, so pass in the array type directly

               var modelArray = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult, bindingContext.ModelType);
               return modelArray;
            }

            var enumerableType = TypeHelpers.ExtractGenericInterface(bindingContext.ModelType, typeof(IEnumerable<>));

            if (enumerableType != null) {

               // case 2: user asked for a collection rather than an array
               // need to call ConvertTo() on the array type, then copy the array to the collection

               var modelCollection = CreateModel(controllerContext, bindingContext, bindingContext.ModelType);
               var elementType = enumerableType.GetGenericArguments()[0];
               var arrayType = elementType.MakeArrayType();
               var modelArray = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult, arrayType);

               var collectionType = typeof(ICollection<>).MakeGenericType(elementType);

               if (collectionType.IsInstanceOfType(modelCollection)) {
                  CollectionHelpers.ReplaceCollection(elementType, modelCollection, modelArray);
               }

               return modelCollection;
            }
         }

         // case 3: user asked for an individual element

         var model = ConvertProviderResult(bindingContext.ModelState, bindingContext.ModelName, valueProviderResult, bindingContext.ModelType);
         return model;
      }

      static bool
      CanUpdateReadonlyTypedReference(Type type) {

         // value types aren't strictly immutable, but because they have copy-by-value semantics
         // we can't update a value type that is marked readonly

         if (type.IsValueType) {
            return false;
         }

         // arrays are mutable, but because we can't change their length we shouldn't try
         // to update an array that is referenced readonly

         if (type.IsArray) {
            return false;
         }

         // special-case known common immutable types

         if (type == typeof(string)) {
            return false;
         }

         return true;
      }

      [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.Web.Mvc.ValueProviderResult.ConvertTo(System.Type)", Justification = "The target object should make the correct culture determination, not this method.")]
      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We're recording this exception so that we can act on it later.")]
      static object?
      ConvertProviderResult(ModelStateDictionary modelState, string modelStateKey, ValueProviderResult valueProviderResult, Type destinationType) {

         try {
            var convertedValue = valueProviderResult.ConvertTo(destinationType);
            return convertedValue;

         } catch (Exception ex) {
            modelState.AddModelError(modelStateKey, ex);
            return null;
         }
      }

      internal ModelBindingContext
      CreateComplexElementalModelBindingContext(ControllerContext controllerContext, ModelBindingContext bindingContext, object model) {

         var bindAttr = (BindAttribute?)GetTypeDescriptor(controllerContext, bindingContext)
            .GetAttributes()[typeof(BindAttribute)];

         var newPropertyFilter = (bindAttr != null) ?
            propertyName => bindAttr.IsPropertyAllowed(propertyName) && bindingContext.PropertyFilter(propertyName)
            : bindingContext.PropertyFilter;

         var newBindingContext = new ModelBindingContext {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => model, bindingContext.ModelType),
            ModelName = bindingContext.ModelName,
            ModelState = bindingContext.ModelState,
            PropertyFilter = newPropertyFilter,
            ValueProvider = bindingContext.ValueProvider
         };

         return newBindingContext;
      }

      protected virtual object
      CreateModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type modelType) {

         // fallback to the type's default constructor

         var typeToCreate = modelType;

         // we can understand some collection interfaces, e.g. IList<>, IDictionary<,>

         if (modelType.IsGenericType) {

            var genericTypeDefinition = modelType.GetGenericTypeDefinition();

            if (genericTypeDefinition == typeof(IDictionary<,>)) {
               typeToCreate = typeof(Dictionary<,>).MakeGenericType(modelType.GetGenericArguments());
            } else if (genericTypeDefinition == typeof(IEnumerable<>) || genericTypeDefinition == typeof(ICollection<>) || genericTypeDefinition == typeof(IList<>)) {
               typeToCreate = typeof(List<>).MakeGenericType(modelType.GetGenericArguments());
            }
         }

         try {
            return Activator.CreateInstance(typeToCreate);
         } catch (MissingMethodException exception) {

            // Ensure thrown exception contains the type name.  Might be down a few levels.

            var replacementException =
               TypeHelpers.EnsureDebuggableException(exception, typeToCreate.FullName);

            if (replacementException != null) {
               throw replacementException;
            }

            throw;
         }
      }

      protected static string
      CreateSubIndexName(string prefix, int index) =>
         String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", prefix, index);

      protected static string
      CreateSubIndexName(string prefix, string index) =>
         String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", prefix, index);

      protected internal static string
      CreateSubPropertyName(string? prefix, string? propertyName) {

         if (String.IsNullOrEmpty(prefix)) {
            return propertyName!;
         } else if (String.IsNullOrEmpty(propertyName)) {
            return prefix;
         } else {
            return prefix + "." + propertyName;
         }
      }

      protected IEnumerable<PropertyDescriptor>
      GetFilteredModelProperties(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         // Performance note: Retain for compatibility only. Faster version inlined

         var properties = GetModelProperties(controllerContext, bindingContext);
         var propertyFilter = bindingContext.PropertyFilter;

         return from PropertyDescriptor property in properties
                where ShouldUpdateProperty(property, propertyFilter)
                select property;
      }

      [SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.Web.Mvc.ValueProviderResult.ConvertTo(System.Type)", Justification = "ValueProviderResult already handles culture conversion appropriately.")]
      static void
      GetIndexes(ModelBindingContext bindingContext, out bool stopOnIndexNotFound, out IEnumerable<string> indexes) {

         var indexKey = CreateSubPropertyName(bindingContext.ModelName, "index");
         var valueProviderResult = bindingContext.ValueProvider.GetValue(indexKey);

         if (valueProviderResult != null) {

            var indexesArray = valueProviderResult.ConvertTo(typeof(string[])) as string[];

            if (indexesArray != null) {
               stopOnIndexNotFound = false;
               indexes = indexesArray;
               return;
            }
         }

         // just use a simple zero-based system

         stopOnIndexNotFound = true;
         indexes = GetZeroBasedIndexes();
      }

      protected virtual PropertyDescriptorCollection
      GetModelProperties(ControllerContext controllerContext, ModelBindingContext bindingContext) =>
         GetTypeDescriptor(controllerContext, bindingContext).GetProperties();

      protected virtual object?
      GetPropertyValue(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor, IModelBinder propertyBinder) {

         var value = propertyBinder.BindModel(controllerContext, bindingContext);

         if (bindingContext.ModelMetadata.ConvertEmptyStringToNull && Equals(value, String.Empty)) {
            return null;
         }

         return value;
      }

      protected virtual ICustomTypeDescriptor
      GetTypeDescriptor(ControllerContext controllerContext, ModelBindingContext bindingContext) =>
         TypeDescriptorHelper.Get(bindingContext.ModelType);

      static string
      GetValueInvalidResource(ControllerContext controllerContext) =>
         XcstWebConfiguration.Instance.ModelBinding.DefaultInvalidPropertyValueErrorMessage?.Invoke()
            ?? MvcResources.DefaultModelBinder_ValueInvalid;

      static string
      GetValueRequiredResource(ControllerContext controllerContext) =>
         XcstWebConfiguration.Instance.ModelBinding.DefaultRequiredPropertyValueErrorMessage?.Invoke()
            ?? MvcResources.DefaultModelBinder_ValueRequired;

      static IEnumerable<string>
      GetZeroBasedIndexes() {

         var i = 0;

         while (true) {
            yield return i.ToString(CultureInfo.InvariantCulture);
            i++;
         }
      }

      protected static bool
      IsModelValid(ModelBindingContext bindingContext) {

         if (bindingContext is null) throw new ArgumentNullException(nameof(bindingContext));

         if (String.IsNullOrEmpty(bindingContext.ModelName)) {
            return bindingContext.ModelState.IsValid;
         }

         return bindingContext.ModelState.IsValidField(bindingContext.ModelName!);
      }

      protected virtual void
      OnModelUpdated(ControllerContext controllerContext, ModelBindingContext bindingContext) {

         var startedValid = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

         foreach (ModelValidationResult validationResult in ModelValidator.GetModelValidator(bindingContext.ModelMetadata, controllerContext).Validate(null)) {

            var subPropertyName = CreateSubPropertyName(bindingContext.ModelName, validationResult.MemberName);

            if (!startedValid.ContainsKey(subPropertyName)) {
               startedValid[subPropertyName] = bindingContext.ModelState.IsValidField(subPropertyName);
            }

            if (startedValid[subPropertyName]) {
               bindingContext.ModelState.AddModelError(subPropertyName, validationResult.Message);
            }
         }
      }

      protected virtual bool
      OnModelUpdating(ControllerContext controllerContext, ModelBindingContext bindingContext) =>
         // default implementation does nothing
         true;

      protected virtual void
      OnPropertyValidated(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor, object? value) {
         // default implementation does nothing
      }

      protected virtual bool
      OnPropertyValidating(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor, object? value) =>
         // default implementation does nothing
         true;

      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We're recording this exception so that we can act on it later.")]
      protected virtual void
      SetProperty(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor, object? value) {

         var propertyMetadata = bindingContext.PropertyMetadata[propertyDescriptor.Name];
         propertyMetadata.Model = value;

         var modelStateKey = CreateSubPropertyName(bindingContext.ModelName, propertyMetadata.PropertyName);

         // If the value is null, and the validation system can find a Required validator for
         // us, we'd prefer to run it before we attempt to set the value; otherwise, property
         // setters which throw on null (f.e., Entity Framework properties which are backed by
         // non-nullable strings in the DB) will get their error message in ahead of us.
         //
         // We are effectively using the special validator -- Required -- as a helper to the
         // binding system, which is why this code is here instead of in the Validating/Validated
         // methods, which are really the old-school validation hooks.

         if (value is null
            && bindingContext.ModelState.IsValidField(modelStateKey)) {

            var requiredValidator = ModelValidatorProviders.Providers
               .GetValidators(propertyMetadata, controllerContext)
               .Where(v => v.IsRequired)
               .FirstOrDefault();

            if (requiredValidator != null) {
               foreach (var validationResult in requiredValidator.Validate(bindingContext.Model)) {
                  bindingContext.ModelState.AddModelError(modelStateKey, validationResult.Message);
               }
            }
         }

         var isNullValueOnNonNullableType = value is null
            && !TypeHelpers.TypeAllowsNullValue(propertyDescriptor.PropertyType);

         // Try to set a value into the property unless we know it will fail (read-only
         // properties and null values with non-nullable types)

         if (!propertyDescriptor.IsReadOnly
            && !isNullValueOnNonNullableType) {

            try {
               propertyDescriptor.SetValue(bindingContext.Model, value);
            } catch (Exception ex) {

               // Only add if we're not already invalid

               if (bindingContext.ModelState.IsValidField(modelStateKey)) {
                  bindingContext.ModelState.AddModelError(modelStateKey, ex);
               }
            }
         }

         // Last chance for an error on null values with non-nullable types, we'll use
         // the default "A value is required." message.

         if (isNullValueOnNonNullableType
            && bindingContext.ModelState.IsValidField(modelStateKey)) {

            bindingContext.ModelState.AddModelError(modelStateKey, GetValueRequiredResource(controllerContext));
         }
      }

      static bool
      ShouldUpdateProperty(PropertyDescriptor property, Predicate<string> propertyFilter) {

         if (property.IsReadOnly
            && !CanUpdateReadonlyTypedReference(property.PropertyType)) {

            return false;
         }

         // if this property is rejected by the filter, move on

         if (!propertyFilter(property.Name)) {
            return false;
         }

         // otherwise, allow

         return true;
      }

      internal object?
      UpdateCollection(ControllerContext controllerContext, ModelBindingContext bindingContext, Type elementType) {

         GetIndexes(bindingContext, out var stopOnIndexNotFound, out var indexes);
         var elementBinder = Binders.GetBinder(elementType);

         // build up a list of items from the request

         var modelList = new List<object?>();

         foreach (var currentIndex in indexes) {

            string subIndexKey = CreateSubIndexName(bindingContext.ModelName, currentIndex);

            if (!bindingContext.ValueProvider.ContainsPrefix(subIndexKey)) {
               if (stopOnIndexNotFound) {
                  // we ran out of elements to pull
                  break;
               } else {
                  continue;
               }
            }

            var innerContext = new ModelBindingContext {
               ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, elementType),
               ModelName = subIndexKey,
               ModelState = bindingContext.ModelState,
               PropertyFilter = bindingContext.PropertyFilter,
               ValueProvider = bindingContext.ValueProvider
            };

            var thisElement = elementBinder.BindModel(controllerContext, innerContext);

            // we need to merge model errors up

            AddValueRequiredMessageToModelState(controllerContext, bindingContext.ModelState, subIndexKey, elementType, thisElement);
            modelList.Add(thisElement);
         }

         // if there weren't any elements at all in the request, just return

         if (modelList.Count == 0) {
            return null;
         }

         // replace the original collection

         var collection = bindingContext.Model!;
         CollectionHelpers.ReplaceCollection(elementType, collection, modelList);

         return collection;
      }

      internal object
      UpdateDictionary(ControllerContext controllerContext, ModelBindingContext bindingContext, Type keyType, Type valueType) {

         GetIndexes(bindingContext, out var stopOnIndexNotFound, out var indexes);

         var keyBinder = Binders.GetBinder(keyType);
         var valueBinder = Binders.GetBinder(valueType);

         // build up a list of items from the request

         var modelList = new List<KeyValuePair<object, object?>>();

         foreach (string currentIndex in indexes) {

            var subIndexKey = CreateSubIndexName(bindingContext.ModelName, currentIndex);
            var keyFieldKey = CreateSubPropertyName(subIndexKey, "key");
            var valueFieldKey = CreateSubPropertyName(subIndexKey, "value");

            if (!(bindingContext.ValueProvider.ContainsPrefix(keyFieldKey) && bindingContext.ValueProvider.ContainsPrefix(valueFieldKey))) {
               if (stopOnIndexNotFound) {
                  // we ran out of elements to pull
                  break;
               } else {
                  continue;
               }
            }

            // bind the key

            var keyBindingContext = new ModelBindingContext {
               ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, keyType),
               ModelName = keyFieldKey,
               ModelState = bindingContext.ModelState,
               ValueProvider = bindingContext.ValueProvider
            };

            var thisKey = keyBinder.BindModel(controllerContext, keyBindingContext);

            // we need to merge model errors up

            AddValueRequiredMessageToModelState(controllerContext, bindingContext.ModelState, keyFieldKey, keyType, thisKey);

            if (!keyType.IsInstanceOfType(thisKey)) {

               // we can't add an invalid key, so just move on

               continue;
            }

            // bind the value

            modelList.Add(CreateEntryForModel(controllerContext, bindingContext, valueType, valueBinder, valueFieldKey, thisKey!));
         }

         // Let's try another method

         if (modelList.Count == 0) {

            if (bindingContext.ValueProvider is IEnumerableValueProvider enumerableValueProvider) {

               var keys = enumerableValueProvider.GetKeysFromPrefix(bindingContext.ModelName);

               foreach (var thisKey in keys) {
                  modelList.Add(CreateEntryForModel(controllerContext, bindingContext, valueType, valueBinder, thisKey.Value, thisKey.Key));
               }
            }
         }

         // replace the original collection

         var dictionary = bindingContext.Model!;
         CollectionHelpers.ReplaceDictionary(keyType, valueType, dictionary, modelList);

         return dictionary;
      }

      static KeyValuePair<object, object?>
      CreateEntryForModel(ControllerContext controllerContext, ModelBindingContext bindingContext, Type valueType, IModelBinder valueBinder, string modelName, object modelKey) {

         var valueBindingContext = new ModelBindingContext {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, valueType),
            ModelName = modelName,
            ModelState = bindingContext.ModelState,
            PropertyFilter = bindingContext.PropertyFilter,
            ValueProvider = bindingContext.ValueProvider
         };

         var thisValue = valueBinder.BindModel(controllerContext, valueBindingContext);
         AddValueRequiredMessageToModelState(controllerContext, bindingContext.ModelState, modelName, valueType, thisValue);

         return new KeyValuePair<object, object?>(modelKey, thisValue);
      }

      // This helper type is used because we're working with strongly-typed collections, but we don't know the Ts
      // ahead of time. By using the generic methods below, we can consolidate the collection-specific code in a
      // single helper type rather than having reflection-based calls spread throughout the DefaultModelBinder type.
      // There is a single point of entry to each of the methods below, so they're fairly simple to maintain.

      static class CollectionHelpers {

         static readonly MethodInfo
         _replaceCollectionMethod = typeof(CollectionHelpers)
            .GetMethod(nameof(ReplaceCollectionImpl), BindingFlags.Static | BindingFlags.NonPublic)!;

         static readonly MethodInfo
         _replaceDictionaryMethod = typeof(CollectionHelpers)
            .GetMethod(nameof(ReplaceDictionaryImpl), BindingFlags.Static | BindingFlags.NonPublic)!;

         [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
         public static void
         ReplaceCollection(Type collectionType, object collection, object? newContents) {

            var targetMethod = _replaceCollectionMethod.MakeGenericMethod(collectionType);
            targetMethod.Invoke(null, new object?[] { collection, newContents });
         }

         static void
         ReplaceCollectionImpl<T>(ICollection<T> collection, IEnumerable? newContents) {

            collection.Clear();

            if (newContents != null) {

               foreach (object item in newContents) {

                  // if the item was not a T, some conversion failed. the error message will be propagated,
                  // but in the meanwhile we need to make a placeholder element in the array.

                  T castItem = (item is T itemT) ? itemT : default(T);
                  collection.Add(castItem);
               }
            }
         }

         [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
         public static void
         ReplaceDictionary(Type keyType, Type valueType, object dictionary, object newContents) {

            var targetMethod = _replaceDictionaryMethod.MakeGenericMethod(keyType, valueType);
            targetMethod.Invoke(null, new object[] { dictionary, newContents });
         }

         static void
         ReplaceDictionaryImpl<TKey, TValue>(IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<object, object>> newContents) {

            dictionary.Clear();

            foreach (var item in newContents) {

               if (item.Key is TKey castKey) {

                  // if the item was not a T, some conversion failed. the error message will be propagated,
                  // but in the meanwhile we need to make a placeholder element in the dictionary.

                  var castValue = (item.Value is TValue valueT) ? valueT : default(TValue);
                  dictionary[castKey] = castValue;
               }
            }
         }
      }
   }
}