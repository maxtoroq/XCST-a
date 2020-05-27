// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace System.Web.Mvc {

   static class DataTypeUtil {

      static readonly string CreditCardTypeName = DataType.CreditCard.ToString();
      static readonly string CurrencyTypeName = DataType.Currency.ToString();
      static readonly string DateTypeName = DataType.Date.ToString();
      static readonly string DateTimeTypeName = DataType.DateTime.ToString();
      static readonly string DurationTypeName = DataType.Duration.ToString();
      static readonly string EmailAddressTypeName = DataType.EmailAddress.ToString();
      internal static readonly string HtmlTypeName = DataType.Html.ToString();
      static readonly string ImageUrlTypeName = DataType.ImageUrl.ToString();
      static readonly string MultiLineTextTypeName = DataType.MultilineText.ToString();
      static readonly string PasswordTypeName = DataType.Password.ToString();
      static readonly string PhoneNumberTypeName = DataType.PhoneNumber.ToString();
      static readonly string PostalCodeTypeName = DataType.PostalCode.ToString();
      static readonly string TextTypeName = DataType.Text.ToString();
      static readonly string TimeTypeName = DataType.Time.ToString();
      static readonly string UploadTypeName = DataType.Upload.ToString();
      static readonly string UrlTypeName = DataType.Url.ToString();

      static readonly Lazy<Dictionary<object, string>> _dataTypeToName = new Lazy<Dictionary<object, string>>(CreateDataTypeToName, isThreadSafe: true);

      // This is a faster version of GetDataTypeName(). It internally calls ToString() on the enum
      // value, which can be quite slow because of value verification.

      internal static string ToDataTypeName(this DataTypeAttribute attribute, Func<DataTypeAttribute, Boolean>? isDataType = null) {

         if (isDataType is null) {
            isDataType = t => t.GetType().Equals(typeof(DataTypeAttribute));
         }

         // GetDataTypeName is virtual, so this is only safe if they haven't derived from DataTypeAttribute.
         // However, if they derive from DataTypeAttribute, they can help their own perf by overriding GetDataTypeName
         // and returning an appropriate string without invoking the ToString() on the enum.

         if (isDataType(attribute)) {

            // Statically known dataTypes are handled separately for performance

            string? name = KnownDataTypeToString(attribute.DataType);

            if (name is null) {
               // Unknown types fallback to a dictionary lookup.
               // Code running on .NET 4.5 will not enter this code for statically known data types.
               // Versions of .NET greater than 4.5 will enter this code for any new data types added to those frameworks
               _dataTypeToName.Value.TryGetValue(attribute.DataType, out name);
            }

            if (name != null) {
               return name;
            }
         }

         return attribute.GetDataTypeName();
      }

      static string? KnownDataTypeToString(DataType dataType) =>
         dataType switch {
            DataType.CreditCard => CreditCardTypeName,
            DataType.Currency => CurrencyTypeName,
            DataType.Date => DateTypeName,
            DataType.DateTime => DateTimeTypeName,
            DataType.Duration => DurationTypeName,
            DataType.EmailAddress => EmailAddressTypeName,
            DataType.Html => HtmlTypeName,
            DataType.ImageUrl => ImageUrlTypeName,
            DataType.MultilineText => MultiLineTextTypeName,
            DataType.Password => PasswordTypeName,
            DataType.PhoneNumber => PhoneNumberTypeName,
            DataType.PostalCode => PostalCodeTypeName,
            DataType.Text => TextTypeName,
            DataType.Time => TimeTypeName,
            DataType.Upload => UploadTypeName,
            DataType.Url => UrlTypeName,
            _ => null,
         };

      static Dictionary<object, string> CreateDataTypeToName() {

         var dataTypeToName = new Dictionary<object, string>();

         foreach (DataType dataTypeValue in Enum.GetValues(typeof(DataType))) {

            // Don't add to the dictionary any of the statically known types.
            // This is a workingset size optimization.

            if (dataTypeValue != DataType.Custom
               && KnownDataTypeToString(dataTypeValue) is null) {

               string name = Enum.GetName(typeof(DataType), dataTypeValue);
               dataTypeToName[dataTypeValue] = name;
            }
         }

         return dataTypeToName;
      }
   }
}
