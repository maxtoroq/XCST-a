// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Mvc.ExpressionUtil;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   public class ModelMetadata {

      public const int DefaultOrder = 10000;

      readonly Type? _containerType;
      readonly Type _modelType;
      readonly string? _propertyName;

      Dictionary<string, object?> _additionalValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      bool _convertEmptyStringToNull = true;
      bool _htmlEncode = true;
      bool _isRequired;
      object? _model;
      Func<object?>? _modelAccessor;
      int _order = DefaultOrder;
      IEnumerable<ModelMetadata>? _properties;
      ModelMetadata[]? _propertiesInternal;
      Type? _realModelType;
      bool _showForDisplay = true;
      bool _showForEdit = true;
      string? _simpleDisplayText;

      public virtual Dictionary<string, object?> AdditionalValues => _additionalValues;

      /// <summary>
      /// A reference to the model's container object. Will be non-null if the model represents a property.
      /// </summary>
      public object? Container { get; set; }

      public Type? ContainerType => _containerType;

      public virtual bool ConvertEmptyStringToNull {
         get => _convertEmptyStringToNull;
         set => _convertEmptyStringToNull = value;
      }

      public virtual string? DataTypeName { get; set; }

      public virtual string? Description { get; set; }

      public virtual string? DisplayFormatString { get; set; }

      [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "The method is a delegating helper to choose among multiple property values")]
      public virtual string? DisplayName { get; set; }

      public virtual string? EditFormatString { get; set; }

      internal virtual bool HasNonDefaultEditFormat { get; set; }

      public virtual bool HideSurroundingHtml { get; set; }

      public virtual bool HtmlEncode {
         get => _htmlEncode;
         set => _htmlEncode = value;
      }

      public virtual bool IsComplexType => !(TypeDescriptor.GetConverter(ModelType).CanConvertFrom(typeof(string)));

      public bool IsNullableValueType => TypeHelpers.IsNullableValueType(ModelType);

      public virtual bool IsReadOnly { get; set; }

      public virtual bool IsRequired {
         get => _isRequired;
         set => _isRequired = value;
      }

      public object? Model {
         get {
            if (_modelAccessor != null) {
               _model = _modelAccessor();
               _modelAccessor = null;
            }
            return _model;
         }
         set {
            _model = value;
            _modelAccessor = null;
            _properties = null;
            _realModelType = null;
         }
      }

      public Type ModelType => _modelType;

      public virtual string? NullDisplayText { get; set; }

      public virtual int Order {
         get => _order;
         set => _order = value;
      }

      public virtual IEnumerable<ModelMetadata> Properties {
         get {
            if (_properties is null) {

               IEnumerable<ModelMetadata> originalProperties = Provider.GetMetadataForProperties(Model, RealModelType);

               // This will be returned as a copied out array in the common case, so reuse the returned array for performance.

               _propertiesInternal = SortProperties(originalProperties.AsArray());
               _properties = new ReadOnlyCollection<ModelMetadata>(_propertiesInternal);
            }
            return _properties;
         }
      }

      internal ModelMetadata[] PropertiesAsArray {
         get {

            IEnumerable<ModelMetadata> virtualProperties = Properties;

            if (Object.ReferenceEquals(virtualProperties, _properties)) {
               Assert.IsNotNull(_propertiesInternal);
               return _propertiesInternal!;
            }

            return virtualProperties.AsArray();
         }
      }

#pragma warning disable CS8603
      public string PropertyName => _propertyName;
#pragma warning restore CS8603

      internal ModelMetadataProvider Provider { get; set; }

      internal Type RealModelType {
         get {
            if (_realModelType is null) {

               _realModelType = ModelType;

               // Don't call GetType() if the model is Nullable<T>, because it will
               // turn Nullable<T> into T for non-null values
               if (Model != null
                  && !TypeHelpers.IsNullableValueType(ModelType)) {

                  _realModelType = Model.GetType();
               }
            }

            return _realModelType;
         }
      }

      public virtual string? ShortDisplayName { get; set; }

      public virtual bool ShowForDisplay {
         get => _showForDisplay;
         set => _showForDisplay = value;
      }

      public virtual bool ShowForEdit {
         get => _showForEdit;
         set => _showForEdit = value;
      }

      [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "This property delegates to the method when the user has not yet set a simple display text value.")]
      public virtual string SimpleDisplayText {
         get => _simpleDisplayText ??= GetSimpleDisplayText();
         set => _simpleDisplayText = value;
      }

      public virtual string? TemplateHint { get; set; }

      public virtual string? Watermark { get; set; }

      public virtual string? GroupName { get; set; }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static ModelMetadata FromLambdaExpression<TParameter, TValue>(Expression<Func<TParameter, TValue>> expression, ViewDataDictionary<TParameter> viewData) =>
         FromLambdaExpression(expression, viewData, metadataProvider: null);

      internal static ModelMetadata FromLambdaExpression<TParameter, TValue>(Expression<Func<TParameter, TValue>> expression, ViewDataDictionary<TParameter> viewData, ModelMetadataProvider? metadataProvider) {

         if (expression is null) throw new ArgumentNullException(nameof(expression));
         if (viewData is null) throw new ArgumentNullException(nameof(viewData));

         string? propertyName = null;
         Type? containerType = null;
         bool legalExpression = false;

         // Need to verify the expression is valid; it needs to at least end in something
         // that we can convert to a meaningful string for model binding purposes

         switch (expression.Body.NodeType) {
            case ExpressionType.ArrayIndex:
               // ArrayIndex always means a single-dimensional indexer; multi-dimensional indexer is a method call to Get()
               legalExpression = true;
               break;

            case ExpressionType.Call:
               // Only legal method call is a single argument indexer/DefaultMember call
               legalExpression = ExpressionHelper.IsSingleArgumentIndexer(expression.Body);
               break;

            case ExpressionType.MemberAccess:
               // Property/field access is always legal
               MemberExpression memberExpression = (MemberExpression)expression.Body;
               propertyName = memberExpression.Member is PropertyInfo ? memberExpression.Member.Name : null;
               containerType = memberExpression.Expression.Type;
               legalExpression = true;
               break;

            case ExpressionType.Parameter:
               // Parameter expression means "model => model", so we delegate to FromModel
               return FromModel(viewData, metadataProvider);
         }

         if (!legalExpression) {
            throw new InvalidOperationException(MvcResources.TemplateHelpers_TemplateLimitations);
         }

         TParameter container = viewData.Model;
         Func<object?> modelAccessor = () => {
            try {
               return CachedExpressionCompiler.Process(expression)(container);
            } catch (NullReferenceException) {
               return null;
            }
         };

         return GetMetadataFromProvider(modelAccessor, typeof(TValue), propertyName, container, containerType, metadataProvider);
      }

      static ModelMetadata FromModel(ViewDataDictionary viewData, ModelMetadataProvider? metadataProvider) =>
         viewData.ModelMetadata
            ?? GetMetadataFromProvider(null, typeof(string), null, null, null, metadataProvider);

      public static ModelMetadata FromStringExpression(string expression, ViewDataDictionary viewData) =>
         FromStringExpression(expression, viewData, metadataProvider: null);

      internal static ModelMetadata FromStringExpression(string expression, ViewDataDictionary viewData, ModelMetadataProvider? metadataProvider) {

         if (expression is null) throw new ArgumentNullException(nameof(expression));
         if (viewData is null) throw new ArgumentNullException(nameof(viewData));

         if (expression.Length == 0) {
            // Empty string really means "model metadata for the current model"
            return FromModel(viewData, metadataProvider);
         }

         ViewDataInfo? vdi = viewData.GetViewDataInfo(expression);
         object? container = null;
         Type? containerType = null;
         Type? modelType = null;
         Func<object?>? modelAccessor = null;
         string? propertyName = null;

         if (vdi != null) {

            if (vdi.Container != null) {
               container = vdi.Container;
               containerType = vdi.Container.GetType();
            }

            modelAccessor = () => vdi.Value;

            if (vdi.PropertyDescriptor != null) {
               propertyName = vdi.PropertyDescriptor.Name;
               modelType = vdi.PropertyDescriptor.PropertyType;
            } else if (vdi.Value != null) {
               // We only need to delay accessing properties (for LINQ to SQL)
               modelType = vdi.Value.GetType();
            }

         } else if (viewData.ModelMetadata != null) {

            //  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData
            ModelMetadata propertyMetadata = viewData.ModelMetadata.Properties.Where(p => p.PropertyName == expression).FirstOrDefault();

            if (propertyMetadata != null) {
               return propertyMetadata;
            }
         }

         return GetMetadataFromProvider(modelAccessor, modelType ?? typeof(string), propertyName, container, containerType, metadataProvider);
      }

      public ModelMetadata(ModelMetadataProvider provider, Type? containerType, Func<object?>? modelAccessor, Type modelType, string? propertyName) {

         if (provider is null) throw new ArgumentNullException(nameof(provider));
         if (modelType is null) throw new ArgumentNullException(nameof(modelType));

         this.Provider = provider;

         _containerType = containerType;
         _isRequired = !TypeHelpers.TypeAllowsNullValue(modelType);
         _modelAccessor = modelAccessor;
         _modelType = modelType;
         _propertyName = propertyName;
      }

      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "The method is a delegating helper to choose among multiple property values")]
      public string GetDisplayName() =>
         this.DisplayName
            ?? this.PropertyName
            ?? this.ModelType.Name;

      private static ModelMetadata GetMetadataFromProvider(Func<object?>? modelAccessor, Type modelType, string? propertyName, object? container, Type? containerType, ModelMetadataProvider? metadataProvider) {

         metadataProvider = metadataProvider ?? ModelMetadataProviders.Current;

         if (containerType != null
            && !String.IsNullOrEmpty(propertyName)) {

            ModelMetadata metadata = metadataProvider.GetMetadataForProperty(modelAccessor, containerType, propertyName!);
            metadata.Container = container;

            return metadata;
         }

         return metadataProvider.GetMetadataForType(modelAccessor, modelType);
      }

      [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This method is used to resolve the simple display text when it was not explicitly set through other means.")]
      protected virtual string GetSimpleDisplayText() {

         if (this.Model is null) {
            return this.NullDisplayText;
         }

         string toStringResult = Convert.ToString(this.Model, CultureInfo.CurrentCulture);

         if (toStringResult is null) {
            return String.Empty;
         }

         if (!toStringResult.Equals(this.Model.GetType().FullName, StringComparison.Ordinal)) {
            return toStringResult;
         }

         ModelMetadata firstProperty = this.Properties.FirstOrDefault();

         if (firstProperty is null) {
            return String.Empty;
         }

         if (firstProperty.Model is null) {
            return firstProperty.NullDisplayText;
         }

         return Convert.ToString(firstProperty.Model, CultureInfo.CurrentCulture);
      }

      internal virtual IEnumerable<ModelValidator> GetValidators(ControllerContext context) =>
         ModelValidatorProviders.Providers.GetValidators(this, context);

      static ModelMetadata[] SortProperties(ModelMetadata[] properties) {

         // Performance-senstive
         // Common case is that properties do not need sorting

         int? previousOrder = null;
         bool needSort = false;

         for (int i = 0; i < properties.Length; i++) {

            ModelMetadata metadata = properties[i];

            if (previousOrder != null && previousOrder > metadata.Order) {
               needSort = true;
               break;
            }
            previousOrder = metadata.Order;
         }

         if (!needSort) {
            return properties;
         }

         // For compatibility the sort must be stable so use OrderBy rather than Array.Sort
         return properties.OrderBy(m => m.Order).ToArray();
      }
   }

   public abstract class ModelMetadataProvider {

      public abstract IEnumerable<ModelMetadata> GetMetadataForProperties(object? container, Type containerType);

      public abstract ModelMetadata GetMetadataForProperty(Func<object?>? modelAccessor, Type containerType, string propertyName);

      public abstract ModelMetadata GetMetadataForType(Func<object?>? modelAccessor, Type modelType);
   }

   public class ModelMetadataProviders {

      static ModelMetadataProviders _instance = new ModelMetadataProviders();

      ModelMetadataProvider? _currentProvider;
      IResolver<ModelMetadataProvider> _resolver;

      internal ModelMetadataProviders(IResolver<ModelMetadataProvider>? resolver = null) {
         _resolver = resolver
            ?? new SingleServiceResolver<ModelMetadataProvider>(() => _currentProvider, new CachedDataAnnotationsModelMetadataProvider(), "ModelMetadataProviders.Current");
      }

      public static ModelMetadataProvider Current {
         get => _instance.CurrentInternal;
         set => _instance.CurrentInternal = value;
      }

      internal ModelMetadataProvider CurrentInternal {
         get => _resolver.Current;
         set => _currentProvider = value ?? new EmptyModelMetadataProvider();
      }
   }
}
