// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Xcst.Web.Mvc;

public class ViewDataDictionary : IDictionary<string, object?> {

   readonly IDictionary<string, object?>
   _innerDictionary;

   readonly Type
   _declaredModelType;

   object?
   _model;

   ModelExplorer?
   _modelExplorer;

   public int
   Count => _innerDictionary.Count;

   public bool
   IsReadOnly => _innerDictionary.IsReadOnly;

   public ICollection<string>
   Keys => _innerDictionary.Keys;

   public object?
   Model {
      get => _model;
      set {
         SetModel(value);
      }
   }

   internal IModelMetadataProvider
   MetadataProvider { get; }

   public ModelExplorer
   ModelExplorer {
      get {
         _modelExplorer ??= MetadataProvider
            .GetModelExplorerForType(_model?.GetType() ?? _declaredModelType, _model);

         return _modelExplorer;
      }
      set {
         Model = value?.Model;
         _modelExplorer = value;
      }
   }

   public ModelMetadata
   ModelMetadata => ModelExplorer.Metadata;

   public ICollection<object?>
   Values => _innerDictionary.Values;

   public object?
   this[string key] {
      get {
         _innerDictionary.TryGetValue(key, out var value);
         return value;
      }
      set => _innerDictionary[key] = value;
   }

   // For unit testing

   internal IDictionary<string, object?>
   InnerDictionary => _innerDictionary;

   public
   ViewDataDictionary(IModelMetadataProvider metadataProvider)
      : this(metadataProvider, typeof(object)) { }

   private protected
   ViewDataDictionary(IModelMetadataProvider metadataProvider, Type declaredModelType) {

      ArgumentNullException.ThrowIfNull(metadataProvider);
      ArgumentNullException.ThrowIfNull(declaredModelType);

      this.MetadataProvider = metadataProvider;

      _innerDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      _declaredModelType = declaredModelType;
   }

   public
   ViewDataDictionary(ViewDataDictionary dictionary)
      : this(dictionary, dictionary?._declaredModelType!) { }

   private protected
   ViewDataDictionary(ViewDataDictionary dictionary, Type declaredModelType) {

      ArgumentNullException.ThrowIfNull(dictionary);
      ArgumentNullException.ThrowIfNull(declaredModelType);

      _innerDictionary = new CopyOnWriteDictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);

      this.MetadataProvider = dictionary.MetadataProvider;

      _declaredModelType = declaredModelType;
      _model = dictionary._model;
      _modelExplorer = dictionary._modelExplorer;
   }

   public void
   Add(KeyValuePair<string, object?> item) =>
      _innerDictionary.Add(item);

   public void
   Add(string key, object? value) =>
      _innerDictionary.Add(key, value);

   public void
   Clear() => _innerDictionary.Clear();

   public bool
   Contains(KeyValuePair<string, object?> item) =>
      _innerDictionary.Contains(item);

   public bool
   ContainsKey(string key) =>
      _innerDictionary.ContainsKey(key);

   public void
   CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
      _innerDictionary.CopyTo(array, arrayIndex);

   public object?
   Eval(string? expression) {
      var info = GetViewDataInfo(expression);
      return info?.Value;
   }

   public IEnumerator<KeyValuePair<string, object?>>
   GetEnumerator() => _innerDictionary.GetEnumerator();

   public ViewDataInfo?
   GetViewDataInfo(string? expression) =>
      ViewDataEvaluator.Eval(this, expression);

   public bool
   Remove(KeyValuePair<string, object?> item) =>
      _innerDictionary.Remove(item);

   public bool
   Remove(string key) =>
      _innerDictionary.Remove(key);

   // This method will execute before the derived type's instance constructor executes. Derived types must
   // be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
   // enough so as not to depend on the "this" pointer referencing a fully constructed object.

   protected virtual void
   SetModel(object? value) {
      _model = value;
      _modelExplorer = null;
   }

   public bool
   TryGetValue(string key, out object? value) =>
      _innerDictionary.TryGetValue(key, out value);

   IEnumerator
   IEnumerable.GetEnumerator() =>
      _innerDictionary.GetEnumerator();
}

public class ViewDataDictionary<TModel> : ViewDataDictionary {

   [MaybeNull]
   public new TModel
   Model {
      get => (TModel?)base.Model;
      set => SetModel(value);
   }

   public
   ViewDataDictionary(IModelMetadataProvider metadataProvider)
      : base(metadataProvider, typeof(TModel)) { }

   public
   ViewDataDictionary(ViewDataDictionary viewDataDictionary)
      : base(viewDataDictionary, typeof(TModel)) { }

   protected override void
   SetModel(object? value) {

      var castWillSucceed = TypeHelpers.IsCompatibleObject<TModel>(value);

      if (!castWillSucceed) {

         var errorMessage = (value != null) ?
            $"The model item passed into the dictionary is of type '{value.GetType()}', but this dictionary requires a model item of type '{typeof(TModel)}'."
            : $"The model item passed into the dictionary is null, but this dictionary requires a non-null model item of type '{typeof(TModel)}'.";

         throw new InvalidOperationException(errorMessage);
      }

      base.SetModel((TModel?)value);
   }
}
