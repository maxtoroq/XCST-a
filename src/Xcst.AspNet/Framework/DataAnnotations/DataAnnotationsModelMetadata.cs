// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   class DataAnnotationsModelMetadata : ModelMetadata {

      DisplayColumnAttribute _displayColumnAttribute;

      internal DataAnnotationsModelMetadata(DataAnnotationsModelMetadataProvider provider, Type containerType,
            Func<object> modelAccessor, Type modelType, string propertyName,
            DisplayColumnAttribute displayColumnAttribute)
         : base(provider, containerType, modelAccessor, modelType, propertyName) {

         _displayColumnAttribute = displayColumnAttribute;
      }

      protected override string GetSimpleDisplayText() {

         if (this.Model != null) {

            if (_displayColumnAttribute != null
               && !String.IsNullOrEmpty(_displayColumnAttribute.DisplayColumn)) {

               PropertyInfo displayColumnProperty = this.ModelType.GetProperty(_displayColumnAttribute.DisplayColumn, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance);
               ValidateDisplayColumnAttribute(_displayColumnAttribute, displayColumnProperty, this.ModelType);

               object simpleDisplayTextValue = displayColumnProperty.GetValue(this.Model, new object[0]);

               if (simpleDisplayTextValue != null) {
                  return simpleDisplayTextValue.ToString();
               }
            }
         }

         return base.GetSimpleDisplayText();
      }

      static void ValidateDisplayColumnAttribute(DisplayColumnAttribute displayColumnAttribute, PropertyInfo displayColumnProperty, Type modelType) {

         if (displayColumnProperty == null) {

            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.DataAnnotationsModelMetadataProvider_UnknownProperty,
                  modelType.FullName, displayColumnAttribute.DisplayColumn));
         }

         if (displayColumnProperty.GetGetMethod() == null) {

            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.DataAnnotationsModelMetadataProvider_UnreadableProperty,
                  modelType.FullName, displayColumnAttribute.DisplayColumn));
         }
      }
   }

   class DataAnnotationsModelMetadataProvider : AssociatedMetadataProvider {

      protected override ModelMetadata CreateMetadata(IEnumerable<Attribute> attributes, Type containerType, Func<object> modelAccessor, Type modelType, string propertyName) {

         var attributeList = new List<Attribute>(attributes);
         DisplayColumnAttribute displayColumnAttribute = attributeList.OfType<DisplayColumnAttribute>().FirstOrDefault();
         var result = new DataAnnotationsModelMetadata(this, containerType, modelAccessor, modelType, propertyName, displayColumnAttribute);

         // Do [HiddenInput] before [UIHint], so you can override the template hint

         HiddenInputAttribute hiddenInputAttribute = attributeList.OfType<HiddenInputAttribute>().FirstOrDefault();

         if (hiddenInputAttribute != null) {
            result.TemplateHint = "HiddenInput";
            result.HideSurroundingHtml = !hiddenInputAttribute.DisplayValue;
         }

         // We prefer [UIHint("...", PresentationLayer = "MVC")] but will fall back to [UIHint("...")]

         IEnumerable<UIHintAttribute> uiHintAttributes = attributeList.OfType<UIHintAttribute>();
         UIHintAttribute uiHintAttribute = uiHintAttributes.FirstOrDefault(a => String.Equals(a.PresentationLayer, "MVC", StringComparison.OrdinalIgnoreCase))
            ?? uiHintAttributes.FirstOrDefault(a => String.IsNullOrEmpty(a.PresentationLayer));

         if (uiHintAttribute != null) {
            result.TemplateHint = uiHintAttribute.UIHint;
         }

         EditableAttribute editable = attributes.OfType<EditableAttribute>().FirstOrDefault();

         if (editable != null) {
            result.IsReadOnly = !editable.AllowEdit;
         } else {

            ReadOnlyAttribute readOnlyAttribute = attributeList.OfType<ReadOnlyAttribute>().FirstOrDefault();

            if (readOnlyAttribute != null) {
               result.IsReadOnly = readOnlyAttribute.IsReadOnly;
            }
         }

         DataTypeAttribute dataTypeAttribute = attributeList.OfType<DataTypeAttribute>().FirstOrDefault();
         DisplayFormatAttribute displayFormatAttribute = attributeList.OfType<DisplayFormatAttribute>().FirstOrDefault();
         SetFromDataTypeAndDisplayAttributes(result, dataTypeAttribute, displayFormatAttribute);

         ScaffoldColumnAttribute scaffoldColumnAttribute = attributeList.OfType<ScaffoldColumnAttribute>().FirstOrDefault();

         if (scaffoldColumnAttribute != null) {
            result.ShowForDisplay = result.ShowForEdit = scaffoldColumnAttribute.Scaffold;
         }

         DisplayAttribute display = attributes.OfType<DisplayAttribute>().FirstOrDefault();

         string name = null;

         if (display != null) {

            result.Description = display.GetDescription();
            result.ShortDisplayName = display.GetShortName();
            result.Watermark = display.GetPrompt();
            result.Order = display.GetOrder() ?? ModelMetadata.DefaultOrder;
            result.GroupName = display.GetGroupName();

            name = display.GetName();
         }

         if (name != null) {
            result.DisplayName = name;
         } else {

            DisplayNameAttribute displayNameAttribute = attributeList.OfType<DisplayNameAttribute>().FirstOrDefault();

            if (displayNameAttribute != null) {
               result.DisplayName = displayNameAttribute.DisplayName;
            }
         }

         RequiredAttribute requiredAttribute = attributeList.OfType<RequiredAttribute>().FirstOrDefault();

         if (requiredAttribute != null) {
            result.IsRequired = true;
         }

         return result;
      }

      static void SetFromDataTypeAndDisplayAttributes(DataAnnotationsModelMetadata result, DataTypeAttribute dataTypeAttribute, DisplayFormatAttribute displayFormatAttribute) {

         if (dataTypeAttribute != null) {
            result.DataTypeName = dataTypeAttribute.ToDataTypeName();
         }

         if (displayFormatAttribute == null
            && dataTypeAttribute != null) {

            displayFormatAttribute = dataTypeAttribute.DisplayFormat;

            // If DisplayFormat value was non-null and this [DataType] is of a subclass, assume the [DataType]
            // constructor used the protected DisplayFormat setter to override its default. Note deriving from
            // [DataType] but preserving DataFormatString and ApplyFormatInEditMode results in
            // HasNonDefaultEditFormat==true.

            if (displayFormatAttribute != null && dataTypeAttribute.GetType() != typeof(DataTypeAttribute)) {
               result.HasNonDefaultEditFormat = true;
            }

         } else if (displayFormatAttribute != null) {
            result.HasNonDefaultEditFormat = true;
         }

         if (displayFormatAttribute != null) {

            result.NullDisplayText = displayFormatAttribute.NullDisplayText;
            result.DisplayFormatString = displayFormatAttribute.DataFormatString;
            result.ConvertEmptyStringToNull = displayFormatAttribute.ConvertEmptyStringToNull;
            result.HtmlEncode = displayFormatAttribute.HtmlEncode;

            if (displayFormatAttribute.ApplyFormatInEditMode) {
               result.EditFormatString = displayFormatAttribute.DataFormatString;
            }

            if (!displayFormatAttribute.HtmlEncode
               && String.IsNullOrWhiteSpace(result.DataTypeName)) {

               result.DataTypeName = DataTypeUtil.HtmlTypeName;
            }

            // Regardless of HasNonDefaultEditFormat calculation above, treat missing EditFormatString as the
            // default.  Note the corner case of a [DataType] subclass overriding a non-empty default to apply a
            // [DisplayFormat] lacking DataFormatString or with ApplyFormatInEditMode==false results in
            // HasNonDefaultEditFormat==false.

            if (String.IsNullOrEmpty(result.EditFormatString)) {
               result.HasNonDefaultEditFormat = false;
            }
         }
      }
   }
}
