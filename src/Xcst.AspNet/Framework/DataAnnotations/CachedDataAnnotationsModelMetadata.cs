// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   class CachedDataAnnotationsModelMetadata : CachedModelMetadata<CachedDataAnnotationsMetadataAttributes> {

      bool _isEditFormatStringFromCache;

      public CachedDataAnnotationsModelMetadata(CachedDataAnnotationsModelMetadata prototype, Func<object> modelAccessor)
         : base(prototype, modelAccessor) { }

      public CachedDataAnnotationsModelMetadata(CachedDataAnnotationsModelMetadataProvider provider, Type containerType, Type modelType, string propertyName, IEnumerable<Attribute> attributes)
         : base(provider, containerType, modelType, propertyName, new CachedDataAnnotationsMetadataAttributes(attributes.ToArray())) { }

      protected override bool ComputeConvertEmptyStringToNull() =>
         (this.PrototypeCache.DisplayFormat != null) ?
            this.PrototypeCache.DisplayFormat.ConvertEmptyStringToNull
            : base.ComputeConvertEmptyStringToNull();

      protected override string ComputeDataTypeName() {

         if (this.PrototypeCache.DataType != null) {
            return this.PrototypeCache.DataType.ToDataTypeName();
         }

         if (this.PrototypeCache.DisplayFormat != null
            && !this.PrototypeCache.DisplayFormat.HtmlEncode) {

            return DataTypeUtil.HtmlTypeName;
         }

         return base.ComputeDataTypeName();
      }

      protected override string ComputeDescription() =>
         (this.PrototypeCache.Display != null) ?
            this.PrototypeCache.Display.GetDescription()
            : base.ComputeDescription();

      protected override string ComputeDisplayFormatString() =>
         (this.PrototypeCache.DisplayFormat != null) ?
            this.PrototypeCache.DisplayFormat.DataFormatString
            : base.ComputeDisplayFormatString();

      protected override string ComputeDisplayName() =>
         this.PrototypeCache.Display?.GetName()
            ?? this.PrototypeCache.DisplayName?.DisplayName
            ?? base.ComputeDisplayName();

      protected override string ComputeEditFormatString() {

         if (this.PrototypeCache.DisplayFormat != null
            && this.PrototypeCache.DisplayFormat.ApplyFormatInEditMode) {

            _isEditFormatStringFromCache = true;

            return this.PrototypeCache.DisplayFormat.DataFormatString;
         }

         return base.ComputeEditFormatString();
      }

      protected override bool ComputeHasNonDefaultEditFormat() {

         if (!String.IsNullOrEmpty(this.EditFormatString)
            && _isEditFormatStringFromCache) {

            // Have a non-empty EditFormatString based on [DisplayFormat] from our cache

            if (this.PrototypeCache.DataType is null) {
               // Attributes include no [DataType]; [DisplayFormat] was applied directly
               return true;
            }

            if (this.PrototypeCache.DataType.DisplayFormat != this.PrototypeCache.DisplayFormat) {
               // Attributes include separate [DataType] and [DisplayFormat]; [DisplayFormat] provided override
               return true;
            }

            if (this.PrototypeCache.DataType.GetType() != typeof(DataTypeAttribute)) {
               // Attributes include [DisplayFormat] copied from [DataType] and [DataType] was of a subclass.
               // Assume the [DataType] constructor used the protected DisplayFormat setter to override its
               // default.  That is derived [DataType] provided override.
               return true;
            }
         }

         return base.ComputeHasNonDefaultEditFormat();
      }

      protected override bool ComputeHideSurroundingHtml() =>
         (this.PrototypeCache.HiddenInput != null) ?
            !this.PrototypeCache.HiddenInput.DisplayValue
            : base.ComputeHideSurroundingHtml();

      protected override bool ComputeHtmlEncode() =>
         (this.PrototypeCache.DisplayFormat != null) ?
            this.PrototypeCache.DisplayFormat.HtmlEncode
            : base.ComputeHtmlEncode();

      protected override bool ComputeIsReadOnly() {

         if (this.PrototypeCache.Editable != null) {
            return !this.PrototypeCache.Editable.AllowEdit;
         }

         if (this.PrototypeCache.ReadOnly != null) {
            return this.PrototypeCache.ReadOnly.IsReadOnly;
         }

         return base.ComputeIsReadOnly();
      }

      protected override bool ComputeIsRequired() =>
         (this.PrototypeCache.Required != null) ? true : base.ComputeIsRequired();

      protected override string ComputeNullDisplayText() =>
         (this.PrototypeCache.DisplayFormat != null) ?
            this.PrototypeCache.DisplayFormat.NullDisplayText
            : base.ComputeNullDisplayText();

      protected override int ComputeOrder() =>
         this.PrototypeCache.Display?.GetOrder()
            ?? base.ComputeOrder();

      protected override string ComputeShortDisplayName() =>
         (this.PrototypeCache.Display != null) ?
            this.PrototypeCache.Display.GetShortName()
            : base.ComputeShortDisplayName();

      protected override bool ComputeShowForDisplay() =>
         (this.PrototypeCache.ScaffoldColumn != null) ?
            this.PrototypeCache.ScaffoldColumn.Scaffold
            : base.ComputeShowForDisplay();

      protected override bool ComputeShowForEdit() =>
         (this.PrototypeCache.ScaffoldColumn != null) ?
            this.PrototypeCache.ScaffoldColumn.Scaffold
            : base.ComputeShowForEdit();

      protected override string ComputeSimpleDisplayText() {

         if (this.Model != null) {

            if (this.PrototypeCache.DisplayColumn != null
               && !String.IsNullOrEmpty(this.PrototypeCache.DisplayColumn.DisplayColumn)) {

               PropertyInfo displayColumnProperty = this.ModelType.GetProperty(this.PrototypeCache.DisplayColumn.DisplayColumn, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
               ValidateDisplayColumnAttribute(this.PrototypeCache.DisplayColumn, displayColumnProperty, this.ModelType);

               object simpleDisplayTextValue = displayColumnProperty.GetValue(this.Model, new object[0]);

               if (simpleDisplayTextValue != null) {
                  return simpleDisplayTextValue.ToString();
               }
            }
         }

         return base.ComputeSimpleDisplayText();
      }

      protected override string ComputeTemplateHint() {

         if (this.PrototypeCache.UIHint != null) {
            return this.PrototypeCache.UIHint.UIHint;
         }

         if (this.PrototypeCache.HiddenInput != null) {
            return "HiddenInput";
         }

         return base.ComputeTemplateHint();
      }

      protected override string ComputeWatermark() =>
         (this.PrototypeCache.Display != null) ?
            this.PrototypeCache.Display.GetPrompt()
            : base.ComputeWatermark();

      protected override string ComputeGroupName() =>
         (this.PrototypeCache.Display != null) ?
            this.PrototypeCache.Display.GetGroupName()
            : base.ComputeGroupName();

      static void ValidateDisplayColumnAttribute(DisplayColumnAttribute displayColumnAttribute, PropertyInfo displayColumnProperty, Type modelType) {

         if (displayColumnProperty is null) {

            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.DataAnnotationsModelMetadataProvider_UnknownProperty,
                  modelType.FullName, displayColumnAttribute.DisplayColumn));
         }

         if (displayColumnProperty.GetGetMethod() is null) {

            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.DataAnnotationsModelMetadataProvider_UnreadableProperty,
                  modelType.FullName, displayColumnAttribute.DisplayColumn));
         }
      }
   }

   class CachedDataAnnotationsMetadataAttributes {

      public DataTypeAttribute DataType { get; protected set; }

      public DisplayAttribute Display { get; protected set; }

      public DisplayColumnAttribute DisplayColumn { get; protected set; }

      public DisplayFormatAttribute DisplayFormat { get; protected set; }

      public DisplayNameAttribute DisplayName { get; protected set; }

      public EditableAttribute Editable { get; protected set; }

      public HiddenInputAttribute HiddenInput { get; protected set; }

      public ReadOnlyAttribute ReadOnly { get; protected set; }

      public RequiredAttribute Required { get; protected set; }

      public ScaffoldColumnAttribute ScaffoldColumn { get; protected set; }

      public UIHintAttribute UIHint { get; protected set; }

      public CachedDataAnnotationsMetadataAttributes(Attribute[] attributes) {

         this.DataType = attributes.OfType<DataTypeAttribute>().FirstOrDefault();
         this.Display = attributes.OfType<DisplayAttribute>().FirstOrDefault();
         this.DisplayColumn = attributes.OfType<DisplayColumnAttribute>().FirstOrDefault();
         this.DisplayFormat = attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
         this.DisplayName = attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
         this.Editable = attributes.OfType<EditableAttribute>().FirstOrDefault();
         this.HiddenInput = attributes.OfType<HiddenInputAttribute>().FirstOrDefault();
         this.ReadOnly = attributes.OfType<ReadOnlyAttribute>().FirstOrDefault();
         this.Required = attributes.OfType<RequiredAttribute>().FirstOrDefault();
         this.ScaffoldColumn = attributes.OfType<ScaffoldColumnAttribute>().FirstOrDefault();

         var uiHintAttributes = attributes.OfType<UIHintAttribute>();

         this.UIHint = uiHintAttributes.FirstOrDefault(a => String.Equals(a.PresentationLayer, "MVC", StringComparison.OrdinalIgnoreCase))
            ?? uiHintAttributes.FirstOrDefault(a => String.IsNullOrEmpty(a.PresentationLayer));

         if (this.DisplayFormat is null
            && this.DataType != null) {

            this.DisplayFormat = this.DataType.DisplayFormat;
         }
      }
   }

   class CachedDataAnnotationsModelMetadataProvider : CachedAssociatedMetadataProvider<CachedDataAnnotationsModelMetadata> {

      protected override CachedDataAnnotationsModelMetadata CreateMetadataPrototype(IEnumerable<Attribute> attributes, Type containerType, Type modelType, string propertyName) =>
         new CachedDataAnnotationsModelMetadata(this, containerType, modelType, propertyName, attributes);

      protected override CachedDataAnnotationsModelMetadata CreateMetadataFromPrototype(CachedDataAnnotationsModelMetadata prototype, Func<object> modelAccessor) =>
         new CachedDataAnnotationsModelMetadata(prototype, modelAccessor);
   }
}
