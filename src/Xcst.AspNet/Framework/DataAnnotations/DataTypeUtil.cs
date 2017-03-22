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

      internal static string ToDataTypeName(this DataTypeAttribute attribute, Func<DataTypeAttribute, Boolean> isDataType = null) {

         if (isDataType == null) {
            isDataType = t => t.GetType().Equals(typeof(DataTypeAttribute));
         }

         // GetDataTypeName is virtual, so this is only safe if they haven't derived from DataTypeAttribute.
         // However, if they derive from DataTypeAttribute, they can help their own perf by overriding GetDataTypeName
         // and returning an appropriate string without invoking the ToString() on the enum.

         if (isDataType(attribute)) {

            // Statically known dataTypes are handled separately for performance

            string name = KnownDataTypeToString(attribute.DataType);

            if (name == null) {
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

      static string KnownDataTypeToString(DataType dataType) {

         switch (dataType) {
            case DataType.CreditCard:
               return CreditCardTypeName;
            case DataType.Currency:
               return CurrencyTypeName;
            case DataType.Date:
               return DateTypeName;
            case DataType.DateTime:
               return DateTimeTypeName;
            case DataType.Duration:
               return DurationTypeName;
            case DataType.EmailAddress:
               return EmailAddressTypeName;
            case DataType.Html:
               return HtmlTypeName;
            case DataType.ImageUrl:
               return ImageUrlTypeName;
            case DataType.MultilineText:
               return MultiLineTextTypeName;
            case DataType.Password:
               return PasswordTypeName;
            case DataType.PhoneNumber:
               return PhoneNumberTypeName;
            case DataType.PostalCode:
               return PostalCodeTypeName;
            case DataType.Text:
               return TextTypeName;
            case DataType.Time:
               return TimeTypeName;
            case DataType.Upload:
               return UploadTypeName;
            case DataType.Url:
               return UrlTypeName;
         }

         return null;
      }

      static Dictionary<object, string> CreateDataTypeToName() {

         var dataTypeToName = new Dictionary<object, string>();

         foreach (DataType dataTypeValue in Enum.GetValues(typeof(DataType))) {

            // Don't add to the dictionary any of the statically known types.
            // This is a workingset size optimization.

            if (dataTypeValue != DataType.Custom
               && KnownDataTypeToString(dataTypeValue) == null) {

               string name = Enum.GetName(typeof(DataType), dataTypeValue);
               dataTypeToName[dataTypeValue] = name;
            }
         }

         return dataTypeToName;
      }
   }
}
