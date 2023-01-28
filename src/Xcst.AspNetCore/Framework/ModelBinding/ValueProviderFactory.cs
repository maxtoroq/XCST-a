// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Xcst.Web.Mvc.ModelBinding;

public abstract class ValueProviderFactory {

   public abstract IValueProvider?
   GetValueProvider(ControllerContext controllerContext);
}

public static class ValueProviderFactories {

   public static ValueProviderFactoryCollection
   Factories { get; } = new() {
      new FormValueProviderFactory(),
      new JsonValueProviderFactory(),
      new RouteDataValueProviderFactory(),
      new QueryStringValueProviderFactory(),
      new HttpFileCollectionValueProviderFactory(),
      new JQueryFormValueProviderFactory()
   };
}

public class ValueProviderFactoryCollection : Collection<ValueProviderFactory> {

   ValueProviderFactory[]?
   _combinedItems;

   IDependencyResolver?
   _dependencyResolver;

   internal ValueProviderFactory[]
   CombinedItems {
      get {
         var combinedItems = _combinedItems;
         if (combinedItems is null) {
            combinedItems = MultiServiceResolver.GetCombined(Items, _dependencyResolver);
            _combinedItems = combinedItems;
         }
         return combinedItems;
      }
   }

   public
   ValueProviderFactoryCollection() { }

   public
   ValueProviderFactoryCollection(IList<ValueProviderFactory> list)
      : base(list) { }

   internal
   ValueProviderFactoryCollection(IList<ValueProviderFactory> list, IDependencyResolver dependencyResolver)
      : base(list) {

      _dependencyResolver = dependencyResolver;
   }

   public IValueProvider
   GetValueProvider(ControllerContext controllerContext) {

      var current = this.CombinedItems;
      var providers = new List<IValueProvider>(current.Length);

      for (int i = 0; i < current.Length; i++) {

         var factory = current[i];
         var provider = factory.GetValueProvider(controllerContext);

         if (provider != null) {
            providers.Add(provider);
         }
      }

      return new ValueProviderCollection(providers);
   }

   protected override void
   ClearItems() {
      _combinedItems = null;
      base.ClearItems();
   }

   protected override void
   InsertItem(int index, ValueProviderFactory item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      _combinedItems = null;
      base.InsertItem(index, item);
   }

   protected override void
   RemoveItem(int index) {
      _combinedItems = null;
      base.RemoveItem(index);
   }

   protected override void
   SetItem(int index, ValueProviderFactory item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      _combinedItems = null;
      base.SetItem(index, item);
   }
}

public class ValueProviderCollection : Collection<IValueProvider>, IValueProvider, IEnumerableValueProvider {

   public
   ValueProviderCollection() { }

   public
   ValueProviderCollection(IList<IValueProvider> list)
      : base(list) { }

   public virtual bool
   ContainsPrefix(string prefix) {

      // Performance sensitive, so avoid Linq and delegates
      // Saving Count is faster for looping over Collection<T>

      var itemCount = this.Count;

      for (int i = 0; i < itemCount; i++) {
         if (this[i].ContainsPrefix(prefix)) {
            return true;
         }
      }

      return false;
   }

   public virtual ValueProviderResult?
   GetValue(string key) {

      // Performance sensitive.
      // Caching the count is faster for Collection<T>

      var providerCount = this.Count;

      for (int i = 0; i < providerCount; i++) {

         var result = this[i].GetValue(key);

         if (result != null) {
            return result;
         }
      }

      return null;
   }

   public virtual IDictionary<string, string>
   GetKeysFromPrefix(string prefix) =>
      (from provider in this
       let result = GetKeysFromPrefixFromProvider(provider, prefix)
       where result != null && result.Any()
       select result).FirstOrDefault()
         ?? new Dictionary<string, string>();

   internal static IDictionary<string, string>?
   GetKeysFromPrefixFromProvider(IValueProvider provider, string prefix) {

      var enumeratedProvider = provider as IEnumerableValueProvider;

      return enumeratedProvider?.GetKeysFromPrefix(prefix);
   }

   protected override void
   InsertItem(int index, IValueProvider item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      base.InsertItem(index, item);
   }

   protected override void
   SetItem(int index, IValueProvider item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      base.SetItem(index, item);
   }
}
