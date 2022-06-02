// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc;

[Serializable]
public class ValueProviderResult {

   static readonly CultureInfo
   _staticCulture = CultureInfo.InvariantCulture;

   CultureInfo?
   _instanceCulture;

   public string?
   AttemptedValue { get; protected set; }

   public CultureInfo
   Culture {
      get => _instanceCulture ??= _staticCulture;
      protected set => _instanceCulture = value;
   }

   public object?
   RawValue { get; protected set; }

   // default constructor so that subclassed types can set the properties themselves

   protected
   ValueProviderResult() { }

   public
   ValueProviderResult(object? rawValue, string? attemptedValue, CultureInfo culture) {

      this.RawValue = rawValue;
      this.AttemptedValue = attemptedValue;
      this.Culture = culture;
   }

   [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Conversion failure is not fatal")]
   static object?
   ConvertSimpleType(CultureInfo culture, object? value, Type destinationType) {

      if (value is null || destinationType.IsInstanceOfType(value)) {
         return value;
      }

      // if this is a user-input value but the user didn't type anything, return no value

      var valueAsString = value as string;

      if (valueAsString != null && String.IsNullOrWhiteSpace(valueAsString)) {
         return null;
      }

      // In case of a Nullable object, we extract the underlying type and try to convert it.

      if (Nullable.GetUnderlyingType(destinationType) is Type underlyingType) {
         destinationType = underlyingType;
      }

      // String doesn't provide convertibles to interesting types, and thus it will typically throw rather than succeed.

      if (valueAsString is null) {

         // If the source type implements IConvertible, try that first

         if (value is IConvertible convertible) {
            try {
               return convertible.ToType(destinationType, culture);
            } catch {
            }
         }
      }

      // Last resort, look for a type converter

      var converter = TypeDescriptor.GetConverter(destinationType);
      var canConvertFrom = converter.CanConvertFrom(value.GetType());

      if (!canConvertFrom) {
         converter = TypeDescriptor.GetConverter(value.GetType());
      }

      if (!(canConvertFrom || converter.CanConvertTo(destinationType))) {

         // EnumConverter cannot convert integer, so we verify manually

         if (destinationType.IsEnum && value is int i) {
            return Enum.ToObject(destinationType, i);
         }

         var message = String.Format(CultureInfo.CurrentCulture, MvcResources.ValueProviderResult_NoConverterExists, value.GetType().FullName, destinationType.FullName);

         throw new InvalidOperationException(message);
      }

      try {

         var convertedValue = (canConvertFrom) ?
            converter.ConvertFrom(null /* context */, culture, value)
            : converter.ConvertTo(null /* context */, culture, value, destinationType);

         return convertedValue;

      } catch (Exception ex) {

         var message = String.Format(CultureInfo.CurrentCulture, MvcResources.ValueProviderResult_ConversionThrew, value.GetType().FullName, destinationType.FullName);

         throw new InvalidOperationException(message, ex);
      }
   }

   public object?
   ConvertTo(Type type) =>
      ConvertTo(type, culture: null);

   public virtual object?
   ConvertTo(Type type, CultureInfo? culture) {

      if (type is null) throw new ArgumentNullException(nameof(type));

      var cultureToUse = culture ?? this.Culture;

      return UnwrapPossibleArrayType(cultureToUse, this.RawValue, type);
   }

   static object?
   UnwrapPossibleArrayType(CultureInfo culture, object? value, Type destinationType) {

      if (value is null
         || destinationType.IsInstanceOfType(value)) {

         return value;
      }

      // array conversion results in four cases, as below

      var valueAsArray = value as Array;

      if (destinationType.IsArray) {

         var destinationElementType = destinationType.GetElementType()!;

         if (valueAsArray != null) {

            // case 1: both destination + source type are arrays, so convert each element

            var converted = (IList)Array.CreateInstance(destinationElementType, valueAsArray.Length);

            for (int i = 0; i < valueAsArray.Length; i++) {
               converted[i] = ConvertSimpleType(culture, valueAsArray.GetValue(i), destinationElementType);
            }

            return converted;

         } else {

            // case 2: destination type is array but source is single element, so wrap element in array + convert

            var element = ConvertSimpleType(culture, value, destinationElementType);
            var converted = (IList)Array.CreateInstance(destinationElementType, 1);
            converted[0] = element;

            return converted;
         }

      } else if (valueAsArray != null) {

         // case 3: destination type is single element but source is array, so extract first element + convert

         if (valueAsArray.Length > 0) {
            value = valueAsArray.GetValue(0);
            return ConvertSimpleType(culture, value, destinationType);
         } else {

            // case 3(a): source is empty array, so can't perform conversion

            return null;
         }
      }

      // case 4: both destination + source type are single elements, so convert

      return ConvertSimpleType(culture, value, destinationType);
   }
}
