// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Caching;

namespace System.Web.Mvc {

   // This class assumes that model metadata is expensive to create, and allows the user to
   // stash a cache object that can be copied around as a prototype to make creation and
   // computation quicker. It delegates the retrieval of values to getter methods, the results
   // of which are cached on a per-metadata-instance basis.
   //
   // This allows flexible caching strategies: either caching the source of information across
   // instances or caching of the actual information itself, depending on what the developer
   // decides to put into the prototype cache.

   abstract class CachedModelMetadata<TPrototypeCache> : ModelMetadata {

      bool _convertEmptyStringToNull;
      string? _dataTypeName;
      string? _description;
      string? _displayFormatString;
      string? _displayName;
      string? _editFormatString;
      bool _hasNonDefaultEditFormat;
      bool _hideSurroundingHtml;
      bool _htmlEncode;
      bool _isReadOnly;
      bool _isRequired;
      string? _nullDisplayText;
      int _order;
      string? _shortDisplayName;
      bool _showForDisplay;
      bool _showForEdit;
      string? _templateHint;
      string? _watermark;
      string? _groupName;

      bool _convertEmptyStringToNullComputed;
      bool _dataTypeNameComputed;
      bool _descriptionComputed;
      bool _displayFormatStringComputed;
      bool _displayNameComputed;
      bool _editFormatStringComputed;
      bool _hasNonDefaultEditFormatComputed;
      bool _hideSurroundingHtmlComputed;
      bool _htmlEncodeComputed;
      bool _isReadOnlyComputed;
      bool _isRequiredComputed;
      bool _nullDisplayTextComputed;
      bool _orderComputed;
      bool _shortDisplayNameComputed;
      bool _showForDisplayComputed;
      bool _showForEditComputed;
      bool _templateHintComputed;
      bool _watermarkComputed;
      bool _groupNameComputed;

      public sealed override bool ConvertEmptyStringToNull {
         get {
            if (!_convertEmptyStringToNullComputed) {
               _convertEmptyStringToNull = ComputeConvertEmptyStringToNull();
               _convertEmptyStringToNullComputed = true;
            }
            return _convertEmptyStringToNull;
         }
         set {
            _convertEmptyStringToNull = value;
            _convertEmptyStringToNullComputed = true;
         }
      }

      public sealed override string? DataTypeName {
         get {
            if (!_dataTypeNameComputed) {
               _dataTypeName = ComputeDataTypeName();
               _dataTypeNameComputed = true;
            }
            return _dataTypeName;
         }
         set {
            _dataTypeName = value;
            _dataTypeNameComputed = true;
         }
      }

      public sealed override string? Description {
         get {
            if (!_descriptionComputed) {
               _description = ComputeDescription();
               _descriptionComputed = true;
            }
            return _description;
         }
         set {
            _description = value;
            _descriptionComputed = true;
         }
      }

      public sealed override string? DisplayFormatString {
         get {
            if (!_displayFormatStringComputed) {
               _displayFormatString = ComputeDisplayFormatString();
               _displayFormatStringComputed = true;
            }
            return _displayFormatString;
         }
         set {
            _displayFormatString = value;
            _displayFormatStringComputed = true;
         }
      }

      public sealed override string? DisplayName {
         get {
            if (!_displayNameComputed) {
               _displayName = ComputeDisplayName();
               _displayNameComputed = true;
            }
            return _displayName;
         }
         set {
            _displayName = value;
            _displayNameComputed = true;
         }
      }

      public sealed override string? EditFormatString {
         get {
            if (!_editFormatStringComputed) {
               _editFormatString = ComputeEditFormatString();
               _editFormatStringComputed = true;
            }
            return _editFormatString;
         }
         set {
            _editFormatString = value;
            _editFormatStringComputed = true;
         }
      }

      internal sealed override bool HasNonDefaultEditFormat {
         get {
            if (!_hasNonDefaultEditFormatComputed) {
               _hasNonDefaultEditFormat = ComputeHasNonDefaultEditFormat();
               _hasNonDefaultEditFormatComputed = true;
            }

            return _hasNonDefaultEditFormat;
         }
         set {
            _hasNonDefaultEditFormat = value;
            _hasNonDefaultEditFormatComputed = true;
         }
      }

      public sealed override bool HideSurroundingHtml {
         get {
            if (!_hideSurroundingHtmlComputed) {
               _hideSurroundingHtml = ComputeHideSurroundingHtml();
               _hideSurroundingHtmlComputed = true;
            }
            return _hideSurroundingHtml;
         }
         set {
            _hideSurroundingHtml = value;
            _hideSurroundingHtmlComputed = true;
         }
      }

      public sealed override bool HtmlEncode {
         get {
            if (!_htmlEncodeComputed) {
               _htmlEncode = ComputeHtmlEncode();
               _htmlEncodeComputed = true;
            }
            return _htmlEncode;
         }
         set {
            _htmlEncode = value;
            _htmlEncodeComputed = true;
         }
      }

      public sealed override bool IsReadOnly {
         get {
            if (!_isReadOnlyComputed) {
               _isReadOnly = ComputeIsReadOnly();
               _isReadOnlyComputed = true;
            }
            return _isReadOnly;
         }
         set {
            _isReadOnly = value;
            _isReadOnlyComputed = true;
         }
      }

      public sealed override bool IsRequired {
         get {
            if (!_isRequiredComputed) {
               _isRequired = ComputeIsRequired();
               _isRequiredComputed = true;
            }
            return _isRequired;
         }
         set {
            _isRequired = value;
            _isRequiredComputed = true;
         }
      }

      public sealed override string? NullDisplayText {
         get {
            if (!_nullDisplayTextComputed) {
               _nullDisplayText = ComputeNullDisplayText();
               _nullDisplayTextComputed = true;
            }
            return _nullDisplayText;
         }
         set {
            _nullDisplayText = value;
            _nullDisplayTextComputed = true;
         }
      }

      public sealed override int Order {
         get {
            if (!_orderComputed) {
               _order = ComputeOrder();
               _orderComputed = true;
            }
            return _order;
         }
         set {
            _order = value;
            _orderComputed = true;
         }
      }

      protected TPrototypeCache PrototypeCache { get; set; }

      public sealed override string? ShortDisplayName {
         get {
            if (!_shortDisplayNameComputed) {
               _shortDisplayName = ComputeShortDisplayName();
               _shortDisplayNameComputed = true;
            }
            return _shortDisplayName;
         }
         set {
            _shortDisplayName = value;
            _shortDisplayNameComputed = true;
         }
      }

      public sealed override bool ShowForDisplay {
         get {
            if (!_showForDisplayComputed) {
               _showForDisplay = ComputeShowForDisplay();
               _showForDisplayComputed = true;
            }
            return _showForDisplay;
         }
         set {
            _showForDisplay = value;
            _showForDisplayComputed = true;
         }
      }

      public sealed override bool ShowForEdit {
         get {
            if (!_showForEditComputed) {
               _showForEdit = ComputeShowForEdit();
               _showForEditComputed = true;
            }
            return _showForEdit;
         }
         set {
            _showForEdit = value;
            _showForEditComputed = true;
         }
      }

      public sealed override string SimpleDisplayText {
         // This is already cached in the base class with an appropriate override available
         get => base.SimpleDisplayText;
         set => base.SimpleDisplayText = value;
      }

      public sealed override string? TemplateHint {
         get {
            if (!_templateHintComputed) {
               _templateHint = ComputeTemplateHint();
               _templateHintComputed = true;
            }
            return _templateHint;
         }
         set {
            _templateHint = value;
            _templateHintComputed = true;
         }
      }

      public sealed override string? Watermark {
         get {
            if (!_watermarkComputed) {
               _watermark = ComputeWatermark();
               _watermarkComputed = true;
            }
            return _watermark;
         }
         set {
            _watermark = value;
            _watermarkComputed = true;
         }
      }

      public sealed override string? GroupName {
         get {
            if (!_groupNameComputed) {
               _groupName = ComputeGroupName();
               _groupNameComputed = true;
            }
            return _groupName;
         }
         set {
            _groupName = value;
            _groupNameComputed = true;
         }
      }

      // Constructor for creating real instances of the metadata class based on a prototype

      protected CachedModelMetadata(CachedModelMetadata<TPrototypeCache> prototype, Func<object?>? modelAccessor)
         : base(prototype.Provider, prototype.ContainerType, modelAccessor, prototype.ModelType, prototype.PropertyName) {

         this.PrototypeCache = prototype.PrototypeCache;
      }

      // Constructor for creating the prototype instances of the metadata class

      protected CachedModelMetadata(/*CachedDataAnnotations*/ModelMetadataProvider provider, Type? containerType, Type modelType, string? propertyName, TPrototypeCache prototypeCache)
         : base(provider, containerType, null /* modelAccessor */, modelType, propertyName) {

         this.PrototypeCache = prototypeCache;
      }

      protected virtual bool ComputeConvertEmptyStringToNull() =>
         base.ConvertEmptyStringToNull;

      protected virtual string? ComputeDataTypeName() =>
         base.DataTypeName;

      protected virtual string? ComputeDescription() =>
         base.Description;

      protected virtual string? ComputeDisplayFormatString() =>
         base.DisplayFormatString;

      protected virtual string? ComputeDisplayName() =>
         base.DisplayName;

      protected virtual string? ComputeEditFormatString() =>
         base.EditFormatString;

      protected virtual bool ComputeHasNonDefaultEditFormat() =>
         base.HasNonDefaultEditFormat;

      protected virtual bool ComputeHideSurroundingHtml() =>
         base.HideSurroundingHtml;

      protected virtual bool ComputeHtmlEncode() =>
         base.HtmlEncode;

      protected virtual bool ComputeIsReadOnly() =>
         base.IsReadOnly;

      protected virtual bool ComputeIsRequired() =>
         base.IsRequired;

      protected virtual string? ComputeNullDisplayText() =>
         base.NullDisplayText;

      protected virtual int ComputeOrder() =>
         base.Order;

      protected virtual string? ComputeShortDisplayName() =>
         base.ShortDisplayName;

      protected virtual bool ComputeShowForDisplay() =>
         base.ShowForDisplay;

      protected virtual bool ComputeShowForEdit() =>
         base.ShowForEdit;

      protected virtual string ComputeSimpleDisplayText() =>
         base.GetSimpleDisplayText();

      protected virtual string? ComputeTemplateHint() =>
         base.TemplateHint;

      protected virtual string? ComputeWatermark() =>
         base.Watermark;

      protected virtual string? ComputeGroupName() =>
         base.GroupName;

      protected sealed override string GetSimpleDisplayText() =>
         // Rename for consistency
         ComputeSimpleDisplayText();
   }

   abstract class CachedAssociatedMetadataProvider<TModelMetadata> : AssociatedMetadataProvider where TModelMetadata : ModelMetadata {

      static ConcurrentDictionary<Type, string> _typeIds = new ConcurrentDictionary<Type, string>();

      string? _cacheKeyPrefix;
      ObjectCache? _prototypeCache;

      protected internal CacheItemPolicy CacheItemPolicy { get; set; } =
         new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(20) };

      protected string CacheKeyPrefix =>
         _cacheKeyPrefix ??= "MetadataPrototypes::" + GetType().GUID.ToString("B");

      protected internal ObjectCache PrototypeCache {
         get => _prototypeCache ?? MemoryCache.Default;
         set => _prototypeCache = value;
      }

      protected sealed override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type? containerType, Func<object?>? modelAccessor, Type modelType, string? propertyName) {

         // If metadata is being created for a property then containerType != null && propertyName != null
         // If metadata is being created for a type then containerType is null && propertyName is null, so we have to use modelType for the cache key.

         Type typeForCache = containerType ?? modelType;
         string cacheKey = GetCacheKey(typeForCache, propertyName);
         TModelMetadata? prototype = PrototypeCache.Get(cacheKey) as TModelMetadata;

         if (prototype is null) {
            prototype = CreateMetadataPrototype(attributes, containerType, modelType, propertyName);
            PrototypeCache.Add(cacheKey, prototype, this.CacheItemPolicy);
         }

         return CreateMetadataFromPrototype(prototype, modelAccessor);
      }

      // New override for creating the prototype metadata (without the accessor)

      protected abstract TModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes, Type? containerType, Type modelType, string? propertyName);

      // New override for applying the prototype + modelAccess to yield the final metadata

      protected abstract TModelMetadata CreateMetadataFromPrototype(TModelMetadata prototype, Func<object?>? modelAccessor);

      internal string GetCacheKey(Type type, string? propertyName = null) {

         propertyName = propertyName ?? String.Empty;

         return this.CacheKeyPrefix + GetTypeId(type) + propertyName;
      }

      public sealed override ModelMetadata GetMetadataForProperty(Func<object?>? modelAccessor, Type containerType, string propertyName) =>
         base.GetMetadataForProperty(modelAccessor, containerType, propertyName);

      protected sealed override ModelMetadata GetMetadataForProperty(Func<object?>? modelAccessor, Type containerType, PropertyDescriptor propertyDescriptor) =>
         base.GetMetadataForProperty(modelAccessor, containerType, propertyDescriptor);

      public sealed override IEnumerable<ModelMetadata> GetMetadataForProperties(object? container, Type containerType) =>
         base.GetMetadataForProperties(container, containerType);

      public sealed override ModelMetadata GetMetadataForType(Func<object?>? modelAccessor, Type modelType) =>
         base.GetMetadataForType(modelAccessor, modelType);

      static string GetTypeId(Type type) =>
         // It's fine using a random Guid since we store the mapping for types to guids.
         _typeIds.GetOrAdd(type, _ => Guid.NewGuid().ToString("B"));
   }
}
