// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Web.Mvc.Properties;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace System.Web.Mvc;

public interface IModelBinder {

   object?
   BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext);
}

public class ModelBindingContext {

   static readonly Predicate<string>
   _defaultPropertyFilter = _ => true;

   string
   _modelName = String.Empty;

   ModelStateDictionary?
   _modelState;

   Predicate<string>?
   _propertyFilter;

   Dictionary<string, ModelMetadata>?
   _propertyMetadata;

   public bool
   FallbackToEmptyPrefix { get; set; }

   [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Cannot remove setter as that's a breaking change")]
   public object?
   Model {
      get => ModelMetadata.Model;
      set => throw new InvalidOperationException(MvcResources.ModelMetadata_PropertyNotSettable);
   }

   public ModelMetadata
   ModelMetadata { get; set; }

   public string?
   ModelName {
      get => _modelName;
      set => _modelName = value ?? String.Empty;
   }

   [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "The containing type is mutable.")]
   public ModelStateDictionary
   ModelState {
      get => _modelState ??= new ModelStateDictionary();
      set => _modelState = value;
   }

   [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value", Justification = "Cannot remove setter as that's a breaking change")]
   public Type
   ModelType {
      get => ModelMetadata.ModelType;
      set => throw new InvalidOperationException(MvcResources.ModelMetadata_PropertyNotSettable);
   }

   public Predicate<string>
   PropertyFilter {
      get => _propertyFilter ??= _defaultPropertyFilter;
      set => _propertyFilter = value;
   }

   public IDictionary<string, ModelMetadata>
   PropertyMetadata =>
      _propertyMetadata ??= ModelMetadata.PropertiesAsArray
         .ToDictionaryFast(m => m.PropertyName, StringComparer.OrdinalIgnoreCase);

   public IValueProvider
   ValueProvider { get; set; }

   public
   ModelBindingContext()
      : this(null) { }

   // copies certain values that won't change between parent and child objects,
   // e.g. ValueProvider, ModelState

   public
   ModelBindingContext(ModelBindingContext? bindingContext) {

      if (bindingContext != null) {
         this.ModelState = bindingContext.ModelState;
         this.ValueProvider = bindingContext.ValueProvider;
      }
   }
}

public static class ModelBinders {

   public static ModelBinderDictionary
   Binders { get; } = CreateDefaultBinderDictionary();

   internal static IModelBinder?
   GetBinderFromAttributes(Type type, Action<Type> errorAction) {

      var allAttrs = new AttributeList(TypeDescriptorHelper.Get(type).GetAttributes());
      var binder = allAttrs.SingleOfTypeDefaultOrError<Attribute, CustomModelBinderAttribute, Type>(errorAction, type);

      return binder?.GetBinder();
   }

   internal static IModelBinder?
   GetBinderFromAttributes(ICustomAttributeProvider element, Action<ICustomAttributeProvider> errorAction) {

      var attrs = (CustomModelBinderAttribute[])element.GetCustomAttributes(typeof(CustomModelBinderAttribute), inherit: true);

      // For compatibility, return null if no attributes.

      if (attrs is null) {
         return null;
      }

      var binder = attrs.SingleDefaultOrError(errorAction, element);

      return binder?.GetBinder();
   }

   static ModelBinderDictionary
   CreateDefaultBinderDictionary() {

      // We can't add a binder to the HttpPostedFileBase type as an attribute, so we'll just
      // prepopulate the dictionary as a convenience to users.

      var binders = new ModelBinderDictionary() {
         { typeof(IFormFile), new HttpPostedFileBaseModelBinder() },
         { typeof(byte[]), new ByteArrayModelBinder() },
      };

      return binders;
   }
}

public class ModelBinderDictionary : IDictionary<Type, IModelBinder> {

   readonly Dictionary<Type, IModelBinder>
   _innerDictionary = new();

   IModelBinder?
   _defaultBinder;

   ModelBinderProviderCollection
   _modelBinderProviders;

   public int
   Count => _innerDictionary.Count;

   public IModelBinder
   DefaultBinder {
      get => _defaultBinder ??= new DefaultModelBinder();
      set => _defaultBinder = value;
   }

   public bool
   IsReadOnly =>
      ((IDictionary<Type, IModelBinder>)_innerDictionary).IsReadOnly;

   public ICollection<Type>
   Keys => _innerDictionary.Keys;

   public ICollection<IModelBinder>
   Values => _innerDictionary.Values;

   public IModelBinder
   this[Type key] {
      get {
         _innerDictionary.TryGetValue(key, out var binder);
         return binder;
      }
      set => _innerDictionary[key] = value;
   }

   public
   ModelBinderDictionary()
      : this(ModelBinderProviders.BinderProviders) { }

   internal
   ModelBinderDictionary(ModelBinderProviderCollection modelBinderProviders) {
      _modelBinderProviders = modelBinderProviders;
   }

   public void
   Add(KeyValuePair<Type, IModelBinder> item) =>
      ((IDictionary<Type, IModelBinder>)_innerDictionary).Add(item);

   public void
   Add(Type key, IModelBinder value) =>
      _innerDictionary.Add(key, value);

   public void
   Clear() =>
      _innerDictionary.Clear();

   public bool
   Contains(KeyValuePair<Type, IModelBinder> item) =>
      ((IDictionary<Type, IModelBinder>)_innerDictionary).Contains(item);

   public bool
   ContainsKey(Type key) =>
      _innerDictionary.ContainsKey(key);

   public void
   CopyTo(KeyValuePair<Type, IModelBinder>[] array, int arrayIndex) =>
      ((IDictionary<Type, IModelBinder>)_innerDictionary).CopyTo(array, arrayIndex);

   public IModelBinder
   GetBinder(Type modelType) =>
      GetBinder(modelType, fallbackToDefault: true)!;

   public virtual IModelBinder?
   GetBinder(Type modelType, bool fallbackToDefault) {

      if (modelType is null) throw new ArgumentNullException(nameof(modelType));

      return GetBinder(modelType, (fallbackToDefault) ? this.DefaultBinder : null);
   }

   private IModelBinder?
   GetBinder(Type modelType, IModelBinder? fallbackBinder) {

      // Try to look up a binder for this type. We use this order of precedence:
      // 1. Binder returned from provider
      // 2. Binder registered in the global table
      // 3. Binder attribute defined on the type
      // 4. Supplied fallback binder

      var binder = _modelBinderProviders.GetBinder(modelType);

      if (binder != null) {
         return binder;
      }

      if (_innerDictionary.TryGetValue(modelType, out binder)) {
         return binder;
      }

      // Function is called frequently, so ensure the error delegate is stateless

      binder = ModelBinders.GetBinderFromAttributes(modelType, (Type errorModel) => {
         throw new InvalidOperationException(
             String.Format(CultureInfo.CurrentCulture, MvcResources.ModelBinderDictionary_MultipleAttributes, errorModel.FullName));
      });

      return binder ?? fallbackBinder;
   }

   public IEnumerator<KeyValuePair<Type, IModelBinder>>
   GetEnumerator() => _innerDictionary.GetEnumerator();

   public bool
   Remove(KeyValuePair<Type, IModelBinder> item) =>
      ((IDictionary<Type, IModelBinder>)_innerDictionary).Remove(item);

   public bool
   Remove(Type key) =>
      _innerDictionary.Remove(key);

   public bool
   TryGetValue(Type key, [MaybeNullWhen(returnValue: false)] out IModelBinder value) =>
      _innerDictionary.TryGetValue(key, out value);

   IEnumerator
   IEnumerable.GetEnumerator() =>
      ((IEnumerable)_innerDictionary).GetEnumerator();
}
