// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

   TemplateInfo?
   _templateMetadata;

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

   public ModelStateDictionary
   ModelState { get; }

   public TemplateInfo
   TemplateInfo {
      get => _templateMetadata ??= new TemplateInfo();
      set => _templateMetadata = value;
   }

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
   ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState)
      : this(metadataProvider, modelState, typeof(object)) { }

   private protected
   ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState, Type declaredModelType) {

      ArgumentNullException.ThrowIfNull(metadataProvider);
      ArgumentNullException.ThrowIfNull(modelState);
      ArgumentNullException.ThrowIfNull(declaredModelType);

      this.MetadataProvider = metadataProvider;
      this.ModelState = modelState;

      _innerDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      _declaredModelType = declaredModelType;
   }

   public
   ViewDataDictionary(ViewDataDictionary dictionary)
      : this(dictionary, dictionary._declaredModelType) { }

   private protected
   ViewDataDictionary(ViewDataDictionary dictionary, Type declaredModelType) {

      ArgumentNullException.ThrowIfNull(dictionary);
      ArgumentNullException.ThrowIfNull(declaredModelType);

      _innerDictionary = new CopyOnWriteDictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);

      this.ModelState = new ModelStateDictionary(dictionary.ModelState);
      this.MetadataProvider = dictionary.MetadataProvider;
      this.TemplateInfo = dictionary.TemplateInfo;

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

   public string?
   Eval(string? expression, string? format) {

      var value = Eval(expression);

      if (value is null) {
         return null;
      }

      return FormatValueInternal(value, format);
   }

   internal static string
   FormatValueInternal(object? value, string? format) {

      if (value is null) {
         return String.Empty;
      }

      if (String.IsNullOrEmpty(format)) {
         return Convert.ToString(value, CultureInfo.CurrentCulture) ?? String.Empty;
      } else {
         return String.Format(CultureInfo.CurrentCulture, format, value);
      }
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

   internal static class ViewDataEvaluator {

      public static ViewDataInfo?
      Eval(ViewDataDictionary viewData, string? expression) {

         ArgumentNullException.ThrowIfNull(viewData);

         if (String.IsNullOrEmpty(expression)) {
            // Null or empty expression name means current model even if that model is null.
            return new ViewDataInfo {
               Container = viewData,
               Value = viewData.Model
            };
         } else {
            return EvalComplexExpression(viewData.Model, expression);
         }
      }

      static ViewDataInfo?
      EvalComplexExpression(object? indexableObject, string? expression) {

         if (indexableObject is null) {
            return null;
         }

         // In case a Dictionary indexableObject contains a "" entry, don't short-circuit the logic below.
         expression ??= String.Empty;

         return InnerEvalComplexExpression(indexableObject, expression);
      }

      static ViewDataInfo?
      InnerEvalComplexExpression(object indexableObject, string expression) {

         Debug.Assert(expression != null);

         var leftExpression = expression;

         do {
            var targetInfo = GetPropertyValue(indexableObject, leftExpression);

            if (targetInfo != null) {

               if (leftExpression.Length == expression.Length) {
                  // Nothing remaining in expression after leftExpression.
                  return targetInfo;
               }

               if (targetInfo.Value != null) {

                  var rightExpression = expression.Substring(leftExpression.Length + 1);

                  targetInfo = InnerEvalComplexExpression(targetInfo.Value, rightExpression);

                  if (targetInfo != null) {
                     return targetInfo;
                  }
               }
            }

            leftExpression = GetNextShorterExpression(leftExpression);

         } while (!String.IsNullOrEmpty(leftExpression));

         return null;
      }

      // Given "one.two.three.four" initially, calls return
      //  "one.two.three"
      //  "one.two"
      //  "one"
      //  ""
      // Recursion of InnerEvalComplexExpression() further sub-divides these cases to cover the full set of
      // combinations shown in Eval(ViewDataDictionary, string) comments.
      static string
      GetNextShorterExpression(string expression) {

         if (String.IsNullOrEmpty(expression)) {
            return String.Empty;
         }

         var lastDot = expression.LastIndexOf('.');

         if (lastDot == -1) {
            return String.Empty;
         }

         return expression.Substring(0, lastDot);
      }

      static ViewDataInfo?
      GetIndexedPropertyValue(object indexableObject, string key) {

         object? value = null;
         var success = false;

         if (indexableObject is IDictionary<string, object> dict) {
            success = dict.TryGetValue(key, out value);
         } else {

            var tgvDel = TypeHelpers.CreateTryGetValueDelegate(indexableObject.GetType());

            if (tgvDel != null) {
               success = tgvDel.Invoke(indexableObject, key, out value);
            }
         }

         if (success) {
            return new ViewDataInfo {
               Container = indexableObject,
               Value = value
            };
         }

         return null;
      }

      // This method handles one "segment" of a complex property expression
      static ViewDataInfo?
      GetPropertyValue(object container, string propertyName) {

         // First, try to evaluate the property based on its indexer.
         var value = GetIndexedPropertyValue(container, propertyName);

         if (value != null) {
            return value;
         }

         // Do not attempt to find a property with an empty name and or of a ViewDataDictionary.
         if (String.IsNullOrEmpty(propertyName) || container is ViewDataDictionary) {
            return null;
         }

         // Second, we try to use PropertyDescriptors and treat the expression as a property name
         var descriptor = TypeDescriptor.GetProperties(container)
            .Find(propertyName, ignoreCase: true);

         if (descriptor is null) {
            return null;
         }

         return new ViewDataInfo(() => descriptor.GetValue(container)) {
            Container = container,
            PropertyDescriptor = descriptor
         };
      }
   }

   IEnumerator
   IEnumerable.GetEnumerator() =>
      _innerDictionary.GetEnumerator();
}

public class ViewDataInfo {

   object?
   _value;

   Func<object?>?
   _valueAccessor;

   public object
   Container { get; set; }

   public PropertyDescriptor
   PropertyDescriptor { get; set; }

   public object?
   Value {
      get {
         if (_valueAccessor != null) {
            _value = _valueAccessor();
            _valueAccessor = null;
         }

         return _value;
      }
      set {
         _value = value;
         _valueAccessor = null;
      }
   }

#pragma warning disable CS8618
   public
   ViewDataInfo() { }

   public
   ViewDataInfo(Func<object?> valueAccessor) {
      _valueAccessor = valueAccessor;
   }
#pragma warning restore CS8618
}

public class ViewDataDictionary<TModel> : ViewDataDictionary {

   [MaybeNull]
   public new TModel
   Model {
      get => (TModel?)base.Model;
      set => SetModel(value);
   }

   public
   ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState)
      : base(metadataProvider, modelState, typeof(TModel)) { }

   public
   ViewDataDictionary(ViewDataDictionary viewDataDictionary)
      : base(viewDataDictionary, typeof(TModel)) { }

   protected override void
   SetModel(object? value) {

      var castWillSucceed = TypeHelpers.IsCompatibleObject<TModel>(value);

      if (castWillSucceed) {
         base.SetModel((TModel?)value);
      } else {

         var errorMessage = (value != null) ?
            $"The model item passed into the dictionary is of type '{value.GetType()}', but this dictionary requires a model item of type '{typeof(TModel)}'."
            : $"The model item passed into the dictionary is null, but this dictionary requires a non-null model item of type '{typeof(TModel)}'.";

         throw new InvalidOperationException(errorMessage);
      }
   }
}
