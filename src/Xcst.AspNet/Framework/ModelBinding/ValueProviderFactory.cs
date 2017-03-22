// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Web.Mvc {

   public abstract class ValueProviderFactory {
      public abstract IValueProvider GetValueProvider(ControllerContext controllerContext);
   }

   public static class ValueProviderFactories {

      static readonly ValueProviderFactoryCollection _factories = new ValueProviderFactoryCollection {
         new FormValueProviderFactory(),
         new JsonValueProviderFactory(),
         new RouteDataValueProviderFactory(),
         new QueryStringValueProviderFactory(),
         new HttpFileCollectionValueProviderFactory(),
         new JQueryFormValueProviderFactory()
      };

      public static ValueProviderFactoryCollection Factories => _factories;
   }

   public class ValueProviderFactoryCollection : Collection<ValueProviderFactory> {

      ValueProviderFactory[] _combinedItems;
      IDependencyResolver _dependencyResolver;

      internal ValueProviderFactory[] CombinedItems {
         get {
            ValueProviderFactory[] combinedItems = _combinedItems;
            if (combinedItems == null) {
               combinedItems = MultiServiceResolver.GetCombined<ValueProviderFactory>(Items, _dependencyResolver);
               _combinedItems = combinedItems;
            }
            return combinedItems;
         }
      }

      public ValueProviderFactoryCollection() { }

      public ValueProviderFactoryCollection(IList<ValueProviderFactory> list)
         : base(list) { }

      internal ValueProviderFactoryCollection(IList<ValueProviderFactory> list, IDependencyResolver dependencyResolver)
         : base(list) {

         _dependencyResolver = dependencyResolver;
      }

      public IValueProvider GetValueProvider(ControllerContext controllerContext) {

         ValueProviderFactory[] current = this.CombinedItems;
         var providers = new List<IValueProvider>(current.Length);

         for (int i = 0; i < current.Length; i++) {

            ValueProviderFactory factory = current[i];
            IValueProvider provider = factory.GetValueProvider(controllerContext);

            if (provider != null) {
               providers.Add(provider);
            }
         }

         return new ValueProviderCollection(providers);
      }

      protected override void ClearItems() {
         _combinedItems = null;
         base.ClearItems();
      }

      protected override void InsertItem(int index, ValueProviderFactory item) {

         if (item == null) throw new ArgumentNullException(nameof(item));

         _combinedItems = null;
         base.InsertItem(index, item);
      }

      protected override void RemoveItem(int index) {
         _combinedItems = null;
         base.RemoveItem(index);
      }

      protected override void SetItem(int index, ValueProviderFactory item) {

         if (item == null) throw new ArgumentNullException(nameof(item));

         _combinedItems = null;
         base.SetItem(index, item);
      }
   }

   public class ValueProviderCollection : Collection<IValueProvider>, IValueProvider, IUnvalidatedValueProvider, IEnumerableValueProvider {

      public ValueProviderCollection() { }

      public ValueProviderCollection(IList<IValueProvider> list)
         : base(list) { }

      public virtual bool ContainsPrefix(string prefix) {

         // Performance sensitive, so avoid Linq and delegates
         // Saving Count is faster for looping over Collection<T>

         int itemCount = this.Count;

         for (int i = 0; i < itemCount; i++) {
            if (this[i].ContainsPrefix(prefix)) {
               return true;
            }
         }

         return false;
      }

      public virtual ValueProviderResult GetValue(string key) {
         return GetValue(key, skipValidation: false);
      }

      public virtual ValueProviderResult GetValue(string key, bool skipValidation) {

         // Performance sensitive.
         // Caching the count is faster for Collection<T>

         int providerCount = this.Count;

         for (int i = 0; i < providerCount; i++) {

            ValueProviderResult result = GetValueFromProvider(this[i], key, skipValidation);

            if (result != null) {
               return result;
            }
         }

         return null;
      }

      public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix) {
         return
            (from provider in this
             let result = GetKeysFromPrefixFromProvider(provider, prefix)
             where result != null && result.Any()
             select result).FirstOrDefault()
            ?? new Dictionary<string, string>();
      }

      internal static ValueProviderResult GetValueFromProvider(IValueProvider provider, string key, bool skipValidation) {

         // Since IUnvalidatedValueProvider is a superset of IValueProvider, it's always OK to use the
         // IUnvalidatedValueProvider-supplied members if they're present. Otherwise just call the
         // normal IValueProvider members.

         var unvalidatedProvider = provider as IUnvalidatedValueProvider;

         return (unvalidatedProvider != null) ?
            unvalidatedProvider.GetValue(key, skipValidation)
            : provider.GetValue(key);
      }

      internal static IDictionary<string, string> GetKeysFromPrefixFromProvider(IValueProvider provider, string prefix) {

         var enumeratedProvider = provider as IEnumerableValueProvider;

         return (enumeratedProvider != null) ?
            enumeratedProvider.GetKeysFromPrefix(prefix)
            : null;
      }

      protected override void InsertItem(int index, IValueProvider item) {

         if (item == null) throw new ArgumentNullException(nameof(item));

         base.InsertItem(index, item);
      }

      protected override void SetItem(int index, IValueProvider item) {

         if (item == null) throw new ArgumentNullException(nameof(item));

         base.SetItem(index, item);
      }
   }
}
