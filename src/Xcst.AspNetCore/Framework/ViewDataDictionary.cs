// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Xcst.Web.Mvc;

// TODO: Unit test ModelState interaction with VDD

public class ViewDataDictionary : IDictionary<string, object?> {

   readonly IDictionary<string, object?>
   _innerDictionary;

   readonly ModelStateDictionary
   _modelState;

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
         _modelExplorer = null;
         SetModel(value);
      }
   }

   internal IModelMetadataProvider
   MetadataProvider { get; }

   public virtual ModelExplorer
   ModelExplorer {
      get {
         if (_modelExplorer is null && _model != null) {
            _modelExplorer = MetadataProvider.GetModelExplorerForType(_model.GetType(), _model);
         }
#pragma warning disable CS8603 // can be null, but most times when requested it's not
         return _modelExplorer;
#pragma warning restore CS8603
      }
      set => _modelExplorer = value;
   }

   public ModelMetadata
#pragma warning disable CS8603 // can be null, but most times when requested it's not
   ModelMetadata => ModelExplorer?.Metadata;
#pragma warning restore CS8603

   public ModelStateDictionary
   ModelState => _modelState;

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

   [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
   public
   ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState) {

      this.MetadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
      _innerDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      _modelState = modelState;
   }

   [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
   public
   ViewDataDictionary(ViewDataDictionary dictionary) {

      if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

      _innerDictionary = new CopyOnWriteDictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
      _modelState = new ModelStateDictionary(dictionary.ModelState);

      this.Model = dictionary.Model;
      this.MetadataProvider = dictionary.MetadataProvider;
      this.TemplateInfo = dictionary.TemplateInfo;

      // PERF: Don't unnecessarily instantiate the model metadata
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
   Eval(string expression) {
      var info = GetViewDataInfo(expression);
      return info?.Value;
   }

   public string
   Eval(string expression, string? format) {
      var value = Eval(expression);
      return FormatValueInternal(value, format);
   }

   internal static string
   FormatValueInternal(object? value, string? format) {

      if (value is null) {
         return String.Empty;
      }

      if (String.IsNullOrEmpty(format)) {
         return Convert.ToString(value, CultureInfo.CurrentCulture);
      } else {
         return String.Format(CultureInfo.CurrentCulture, format, value);
      }
   }

   public IEnumerator<KeyValuePair<string, object?>>
   GetEnumerator() => _innerDictionary.GetEnumerator();

   public ViewDataInfo?
   GetViewDataInfo(string expression) {

      ArgumentNullException.ThrowIfNullOrEmpty(expression);

      return ViewDataEvaluator.Eval(this, expression);
   }

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
   SetModel(object? value) =>
      _model = value;

   public bool
   TryGetValue(string key, out object? value) =>
      _innerDictionary.TryGetValue(key, out value);

   internal static class ViewDataEvaluator {

      public static ViewDataInfo?
      Eval(ViewDataDictionary vdd, string expression) {

         //Given an expression "foo.bar.baz" we look up the following (pseudocode):
         //  this["foo.bar.baz.quux"]
         //  this["foo.bar.baz"]["quux"]
         //  this["foo.bar"]["baz.quux]
         //  this["foo.bar"]["baz"]["quux"]
         //  this["foo"]["bar.baz.quux"]
         //  this["foo"]["bar.baz"]["quux"]
         //  this["foo"]["bar"]["baz.quux"]
         //  this["foo"]["bar"]["baz"]["quux"]

         var evaluated = EvalComplexExpression(vdd, expression);
         return evaluated;
      }

      static ViewDataInfo?
      EvalComplexExpression(object indexableObject, string expression) {

         foreach (ExpressionPair expressionPair in GetRightToLeftExpressions(expression)) {

            var subExpression = expressionPair.Left;
            var postExpression = expressionPair.Right;

            var subTargetInfo = GetPropertyValue(indexableObject, subExpression);

            if (subTargetInfo != null) {

               if (String.IsNullOrEmpty(postExpression)) {
                  return subTargetInfo;
               }

               if (subTargetInfo.Value != null) {

                  var potential = EvalComplexExpression(subTargetInfo.Value, postExpression);

                  if (potential != null) {
                     return potential;
                  }
               }
            }
         }

         return null;
      }

      static IEnumerable<ExpressionPair>
      GetRightToLeftExpressions(string expression) {

         // Produces an enumeration of all the combinations of complex property names
         // given a complex expression. See the list above for an example of the result
         // of the enumeration.

         yield return new ExpressionPair(expression, String.Empty);

         var lastDot = expression.LastIndexOf('.');

         while (lastDot > -1) {
            var subExpression = expression.Substring(0, lastDot);
            var postExpression = expression.Substring(lastDot + 1);
            yield return new ExpressionPair(subExpression, postExpression);

            lastDot = subExpression.LastIndexOf('.');
         }
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
               success = tgvDel(indexableObject, key, out value);
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

      static ViewDataInfo?
      GetPropertyValue(object container, string propertyName) {

         // This method handles one "segment" of a complex property expression

         // First, we try to evaluate the property based on its indexer
         var value = GetIndexedPropertyValue(container, propertyName);

         if (value != null) {
            return value;
         }

         // If the indexer didn't return anything useful, continue...

         // If the container is a ViewDataDictionary then treat its Model property
         // as the container instead of the ViewDataDictionary itself.

         if (container is ViewDataDictionary vdd) {
#pragma warning disable CS8600 // variable reuse
            container = vdd.Model;
#pragma warning restore CS8600
         }

         // If the container is null, we're out of options
         if (container is null) {
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

      struct ExpressionPair {

         public readonly string
         Left;

         public readonly string
         Right;

         public
         ExpressionPair(string left, string right) {
            Left = left;
            Right = right;
         }
      }
   }

   IEnumerator
   IEnumerable.GetEnumerator() =>
      _innerDictionary.GetEnumerator();
}

public class TemplateInfo {

   string?
   _htmlFieldPrefix;

   object?
   _formattedModelValue;

   IList<string>?
   _membersNames;

   HashSet<object>?
   _visitedObjects;

   [AllowNull]
   public object
   FormattedModelValue {
      get => _formattedModelValue ?? String.Empty;
      set => _formattedModelValue = value;
   }

   [AllowNull]
   public string
   HtmlFieldPrefix {
      get => _htmlFieldPrefix ?? String.Empty;
      set => _htmlFieldPrefix = value;
   }

   [AllowNull]
   internal IList<string>
   MembersNames {
      get => _membersNames ?? Array.Empty<string>();
      set => _membersNames = value;
   }

   public int
   TemplateDepth => VisitedObjects.Count;

   // DDB #224750 - Keep a collection of visited objects to prevent infinite recursion

   internal HashSet<object>
   VisitedObjects {
      get => _visitedObjects ??= new HashSet<object>();
      set => _visitedObjects = value;
   }

   public string
   GetFullHtmlFieldId(string? partialFieldName) =>
      HtmlHelper.GenerateIdFromName(GetFullHtmlFieldName(partialFieldName));

   public string
   GetFullHtmlFieldName(string? partialFieldName) {

      if (String.IsNullOrEmpty(partialFieldName)) {
         return this.HtmlFieldPrefix;
      }

      if (String.IsNullOrEmpty(this.HtmlFieldPrefix)) {
         return partialFieldName;
      }

      if (partialFieldName.StartsWith("[", StringComparison.Ordinal)) {

         // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
         // with a 'dot' would be invalid.

         return this.HtmlFieldPrefix + partialFieldName;
      }

      return this.HtmlFieldPrefix + "." + partialFieldName;
   }

   public bool
   Visited(ModelExplorer modelExplorer) =>
      this.VisitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);
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

   public override ModelExplorer
   ModelExplorer {
      get {
         var result = base.ModelExplorer;

         if (result is null) {
            result = MetadataProvider.GetModelExplorerForType(typeof(TModel), null);
            base.ModelExplorer = result;
         }

         return result;
      }
      set => base.ModelExplorer = value;
   }

   public
   ViewDataDictionary(IModelMetadataProvider metadataProvider, ModelStateDictionary modelState)
      : base(metadataProvider, modelState) { }

   public
   ViewDataDictionary(ViewDataDictionary viewDataDictionary)
      : base(viewDataDictionary) { }

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
