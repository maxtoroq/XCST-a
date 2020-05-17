// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Web.Mvc {

   [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "It is not anticipated that users will need to serialize this type.")]
   [SuppressMessage("Microsoft.Design", "CA1035:ICollectionImplementationsHaveStronglyTypedMembers", Justification = "It is not anticipated that users will call FormCollection.CopyTo().")]
   [FormCollectionBinder]
   public sealed class FormCollection : NameValueCollection, IValueProvider {

      public FormCollection() { }

      public FormCollection(NameValueCollection collection) {

         if (collection == null) throw new ArgumentNullException(nameof(collection));

         Add(collection);
      }

      public ValueProviderResult GetValue(string name) {

         if (name == null) throw new ArgumentNullException(nameof(name));

         string[] rawValue = GetValues(name);

         if (rawValue == null) {
            return null;
         }

         string attemptedValue = this[name];

         return new ValueProviderResult(rawValue, attemptedValue, CultureInfo.CurrentCulture);
      }

      public IValueProvider ToValueProvider() => this;

      #region IValueProvider Members

      bool IValueProvider.ContainsPrefix(string prefix) =>
         CollectionContainsPrefix(this.AllKeys, prefix);

      ValueProviderResult IValueProvider.GetValue(string key) =>
         GetValue(key);

      static bool CollectionContainsPrefix(IEnumerable<string> collection, string prefix) {

         foreach (string key in collection) {

            if (key != null) {

               if (prefix.Length == 0) {
                  return true; // shortcut - non-null key matches empty prefix
               }

               if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {

                  if (key.Length == prefix.Length) {
                     return true; // exact match
                  } else {
                     switch (key[prefix.Length]) {
                        case '.': // known separator characters
                        case '[':
                           return true;
                     }
                  }
               }
            }
         }

         return false; // nothing found
      }

      #endregion

      sealed class FormCollectionBinderAttribute : CustomModelBinderAttribute {

         // since the FormCollectionModelBinder.BindModel() method is thread-safe, we only need to keep
         // a single instance of the binder around

         static readonly FormCollectionModelBinder _binder = new FormCollectionModelBinder();

         public override IModelBinder GetBinder() => _binder;

         // this class is used for generating a FormCollection object

         sealed class FormCollectionModelBinder : IModelBinder {

            public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {

               if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));

               return new FormCollection(controllerContext.HttpContext.Request.Form);
            }
         }
      }
   }
}
