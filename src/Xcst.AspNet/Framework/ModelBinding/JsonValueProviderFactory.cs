// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Web.Mvc.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xcst.Web.Configuration;

namespace System.Web.Mvc {

   public sealed class JsonValueProviderFactory : ValueProviderFactory {

      public static JsonSerializerSettings SerializationSettings { get; } = new JsonSerializerSettings {
         // using same default as ASP.NET Core
         MaxDepth = 32,
         Converters = { new ExpandoObjectConverter() }
      };

      static void AddToBackingStore(EntryLimitedDictionary backingStore, string prefix, object? value) {

         if (value is IDictionary<string, object> d) {

            foreach (KeyValuePair<string, object> entry in d) {
               AddToBackingStore(backingStore, MakePropertyKey(prefix, entry.Key), entry.Value);
            }

            return;
         }

         if (value is IList l) {

            for (int i = 0; i < l.Count; i++) {
               AddToBackingStore(backingStore, MakeArrayKey(prefix, i), l[i]);
            }

            return;
         }

         // primitive

         backingStore.Add(prefix, value);
      }

      static object? GetDeserializedObject(ControllerContext controllerContext) {

         var request = controllerContext.HttpContext.Request;

         if (!request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase)) {

            // not JSON request

            return null;
         }

#if NETCOREAPP
         Stream inputStream = request.Body;
#else
         Stream inputStream = request.InputStream;
#endif

         var textReader = new StreamReader(inputStream);
         var jsonReader = new JsonTextReader(textReader);

         if (!jsonReader.Read()) {
            return null;
         }

         JsonSerializer serializer = JsonSerializer.Create(SerializationSettings);

         object jsonData;

         if (jsonReader.TokenType == JsonToken.StartArray) {
            jsonData = serializer.Deserialize<List<ExpandoObject>>(jsonReader);
         } else {
            jsonData = serializer.Deserialize<ExpandoObject>(jsonReader);
         }

         return jsonData;
      }

      public override IValueProvider? GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         object? jsonData = GetDeserializedObject(controllerContext);

         if (jsonData is null) {
            return null;
         }

         var backingStore = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
         var backingStoreWrapper = new EntryLimitedDictionary(backingStore);
         AddToBackingStore(backingStoreWrapper, String.Empty, jsonData);

         return new DictionaryValueProvider<object?>(backingStore, CultureInfo.CurrentCulture);
      }

      static string MakeArrayKey(string prefix, int index) =>
         prefix + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

      static string MakePropertyKey(string prefix, string propertyName) =>
         (String.IsNullOrEmpty(prefix)) ? propertyName : prefix + "." + propertyName;

      class EntryLimitedDictionary {

         static int _maximumDepth = GetMaximumDepth();
         readonly IDictionary<string, object?> _innerDictionary;
         int _itemCount = 0;

         public EntryLimitedDictionary(IDictionary<string, object?> innerDictionary) {
            _innerDictionary = innerDictionary;
         }

         public void Add(string key, object? value) {

            if (++_itemCount > _maximumDepth) {
               throw new InvalidOperationException(MvcResources.JsonValueProviderFactory_RequestTooLarge);
            }

            _innerDictionary.Add(key, value);
         }

         static int GetMaximumDepth() {

            int maxMembers = XcstWebConfiguration.Instance.ModelBinding.MaxJsonDeserializerMembers;

            if (maxMembers > -1) {
               return maxMembers;
            }

            return 1000; // Fallback default
         }
      }
   }
}
