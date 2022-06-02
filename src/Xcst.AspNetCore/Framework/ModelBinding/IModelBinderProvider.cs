// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Web.Mvc;

public interface IModelBinderProvider {

   IModelBinder
   GetBinder(Type modelType);
}

public static class ModelBinderProviders {

   public static ModelBinderProviderCollection
   BinderProviders { get; } = new();
}

public class ModelBinderProviderCollection : Collection<IModelBinderProvider> {

   IModelBinderProvider[]?
   _combinedItems;

   IDependencyResolver?
   _dependencyResolver;

   internal IModelBinderProvider[]
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
   ModelBinderProviderCollection() { }

   public
   ModelBinderProviderCollection(IList<IModelBinderProvider> list)
      : base(list) { }

   internal
   ModelBinderProviderCollection(IList<IModelBinderProvider> list, IDependencyResolver dependencyResolver)
      : base(list) {

      _dependencyResolver = dependencyResolver;
   }

   protected override void
   ClearItems() {
      _combinedItems = null;
      base.ClearItems();
   }

   protected override void
   InsertItem(int index, IModelBinderProvider item) {

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
   SetItem(int index, IModelBinderProvider item) {

      if (item is null) throw new ArgumentNullException(nameof(item));

      _combinedItems = null;
      base.SetItem(index, item);
   }

   public IModelBinder?
   GetBinder(Type modelType) {

      if (modelType is null) throw new ArgumentNullException(nameof(modelType));

      // Performance sensitive.

      var providers = this.CombinedItems;

      for (int i = 0; i < providers.Length; i++) {

         var binder = providers[i].GetBinder(modelType);

         if (binder != null) {
            return binder;
         }
      }

      return null;
   }
}
