﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   // TODO: Unit test ModelState interaction with VDD

   public class ViewDataDictionary : IDictionary<string, object> {

      readonly IDictionary<string, object> _innerDictionary;
      readonly ModelStateDictionary _modelState;
      object _model;
      ModelMetadata _modelMetadata;
      TemplateInfo _templateMetadata;

      public int Count => _innerDictionary.Count;

      public bool IsReadOnly => _innerDictionary.IsReadOnly;

      public ICollection<string> Keys => _innerDictionary.Keys;

      public object Model {
         get { return _model; }
         set {
            _modelMetadata = null;
            SetModel(value);
         }
      }

      public virtual ModelMetadata ModelMetadata {
         get {
            if (_modelMetadata == null && _model != null) {
               _modelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => _model, _model.GetType());
            }
            return _modelMetadata;
         }
         set { _modelMetadata = value; }
      }

      public ModelStateDictionary ModelState {
         get { return _modelState; }
      }

      public TemplateInfo TemplateInfo {
         get {
            if (_templateMetadata == null) {
               _templateMetadata = new TemplateInfo();
            }
            return _templateMetadata;
         }
         set { _templateMetadata = value; }
      }

      public ICollection<object> Values => _innerDictionary.Values;

      public object this[string key] {
         get {
            object value;
            _innerDictionary.TryGetValue(key, out value);
            return value;
         }
         set { _innerDictionary[key] = value; }
      }

      // For unit testing

      internal IDictionary<string, object> InnerDictionary => _innerDictionary;

      public ViewDataDictionary()
         : this((object)null) { }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
      public ViewDataDictionary(object model) {

         this.Model = model;
         _innerDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
         _modelState = new ModelStateDictionary();
      }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
      public ViewDataDictionary(ViewDataDictionary dictionary) {

         if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

         _innerDictionary = new CopyOnWriteDictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase);
         _modelState = new ModelStateDictionary(dictionary.ModelState);

         this.Model = dictionary.Model;
         TemplateInfo = dictionary.TemplateInfo;

         // PERF: Don't unnecessarily instantiate the model metadata
         _modelMetadata = dictionary._modelMetadata;
      }

      public void Add(KeyValuePair<string, object> item) {
         _innerDictionary.Add(item);
      }

      public void Add(string key, object value) {
         _innerDictionary.Add(key, value);
      }

      public void Clear() {
         _innerDictionary.Clear();
      }

      public bool Contains(KeyValuePair<string, object> item) {
         return _innerDictionary.Contains(item);
      }

      public bool ContainsKey(string key) {
         return _innerDictionary.ContainsKey(key);
      }

      public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
         _innerDictionary.CopyTo(array, arrayIndex);
      }

      public object Eval(string expression) {
         ViewDataInfo info = GetViewDataInfo(expression);
         return (info != null) ? info.Value : null;
      }

      public string Eval(string expression, string format) {
         object value = Eval(expression);
         return FormatValueInternal(value, format);
      }

      internal static string FormatValueInternal(object value, string format) {

         if (value == null) {
            return String.Empty;
         }

         if (String.IsNullOrEmpty(format)) {
            return Convert.ToString(value, CultureInfo.CurrentCulture);
         } else {
            return String.Format(CultureInfo.CurrentCulture, format, value);
         }
      }

      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
         return _innerDictionary.GetEnumerator();
      }

      public ViewDataInfo GetViewDataInfo(string expression) {

         if (String.IsNullOrEmpty(expression)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(expression));

         return ViewDataEvaluator.Eval(this, expression);
      }

      public bool Remove(KeyValuePair<string, object> item) {
         return _innerDictionary.Remove(item);
      }

      public bool Remove(string key) {
         return _innerDictionary.Remove(key);
      }

      // This method will execute before the derived type's instance constructor executes. Derived types must
      // be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
      // enough so as not to depend on the "this" pointer referencing a fully constructed object.

      protected virtual void SetModel(object value) {
         _model = value;
      }

      public bool TryGetValue(string key, out object value) {
         return _innerDictionary.TryGetValue(key, out value);
      }

      internal static class ViewDataEvaluator {

         public static ViewDataInfo Eval(ViewDataDictionary vdd, string expression) {

            //Given an expression "foo.bar.baz" we look up the following (pseudocode):
            //  this["foo.bar.baz.quux"]
            //  this["foo.bar.baz"]["quux"]
            //  this["foo.bar"]["baz.quux]
            //  this["foo.bar"]["baz"]["quux"]
            //  this["foo"]["bar.baz.quux"]
            //  this["foo"]["bar.baz"]["quux"]
            //  this["foo"]["bar"]["baz.quux"]
            //  this["foo"]["bar"]["baz"]["quux"]

            ViewDataInfo evaluated = EvalComplexExpression(vdd, expression);
            return evaluated;
         }

         static ViewDataInfo EvalComplexExpression(object indexableObject, string expression) {

            foreach (ExpressionPair expressionPair in GetRightToLeftExpressions(expression)) {

               string subExpression = expressionPair.Left;
               string postExpression = expressionPair.Right;

               ViewDataInfo subTargetInfo = GetPropertyValue(indexableObject, subExpression);

               if (subTargetInfo != null) {

                  if (String.IsNullOrEmpty(postExpression)) {
                     return subTargetInfo;
                  }

                  if (subTargetInfo.Value != null) {

                     ViewDataInfo potential = EvalComplexExpression(subTargetInfo.Value, postExpression);

                     if (potential != null) {
                        return potential;
                     }
                  }
               }
            }

            return null;
         }

         static IEnumerable<ExpressionPair> GetRightToLeftExpressions(string expression) {

            // Produces an enumeration of all the combinations of complex property names
            // given a complex expression. See the list above for an example of the result
            // of the enumeration.

            yield return new ExpressionPair(expression, String.Empty);

            int lastDot = expression.LastIndexOf('.');

            string subExpression = expression;
            string postExpression = String.Empty;

            while (lastDot > -1) {
               subExpression = expression.Substring(0, lastDot);
               postExpression = expression.Substring(lastDot + 1);
               yield return new ExpressionPair(subExpression, postExpression);

               lastDot = subExpression.LastIndexOf('.');
            }
         }

         static ViewDataInfo GetIndexedPropertyValue(object indexableObject, string key) {

            IDictionary<string, object> dict = indexableObject as IDictionary<string, object>;
            object value = null;
            bool success = false;

            if (dict != null) {
               success = dict.TryGetValue(key, out value);
            } else {

               TryGetValueDelegate tgvDel = TypeHelpers.CreateTryGetValueDelegate(indexableObject.GetType());

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

         static ViewDataInfo GetPropertyValue(object container, string propertyName) {

            // This method handles one "segment" of a complex property expression

            // First, we try to evaluate the property based on its indexer
            ViewDataInfo value = GetIndexedPropertyValue(container, propertyName);

            if (value != null) {
               return value;
            }

            // If the indexer didn't return anything useful, continue...

            // If the container is a ViewDataDictionary then treat its Model property
            // as the container instead of the ViewDataDictionary itself.

            var vdd = container as ViewDataDictionary;

            if (vdd != null) {
               container = vdd.Model;
            }

            // If the container is null, we're out of options
            if (container == null) {
               return null;
            }

            // Second, we try to use PropertyDescriptors and treat the expression as a property name
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(container).Find(propertyName, true);

            if (descriptor == null) {
               return null;
            }

            return new ViewDataInfo(() => descriptor.GetValue(container)) {
               Container = container,
               PropertyDescriptor = descriptor
            };
         }

         struct ExpressionPair {

            public readonly string Left;
            public readonly string Right;

            public ExpressionPair(string left, string right) {
               Left = left;
               Right = right;
            }
         }
      }

      #region IEnumerable Members

      IEnumerator IEnumerable.GetEnumerator() {
         return _innerDictionary.GetEnumerator();
      }

      #endregion
   }

   public class TemplateInfo {

      string _htmlFieldPrefix;
      object _formattedModelValue;
      HashSet<object> _visitedObjects;

      public object FormattedModelValue {
         get { return _formattedModelValue ?? String.Empty; }
         set { _formattedModelValue = value; }
      }

      public string HtmlFieldPrefix {
         get { return _htmlFieldPrefix ?? String.Empty; }
         set { _htmlFieldPrefix = value; }
      }

      public int TemplateDepth => VisitedObjects.Count;

      // DDB #224750 - Keep a collection of visited objects to prevent infinite recursion

      internal HashSet<object> VisitedObjects {
         get {
            if (_visitedObjects == null) {
               _visitedObjects = new HashSet<object>();
            }
            return _visitedObjects;
         }
         set { _visitedObjects = value; }
      }

      public string GetFullHtmlFieldId(string partialFieldName) {
         return HtmlHelper.GenerateIdFromName(GetFullHtmlFieldName(partialFieldName));
      }

      public string GetFullHtmlFieldName(string partialFieldName) {

         if (partialFieldName != null
            && partialFieldName.StartsWith("[", StringComparison.Ordinal)) {

            // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
            // with a 'dot' would be invalid.

            return this.HtmlFieldPrefix + partialFieldName;

         } else {

            // This uses "combine and trim" because either or both of these values might be empty
            return (this.HtmlFieldPrefix + "." + (partialFieldName ?? String.Empty)).Trim('.');
         }
      }

      public bool Visited(ModelMetadata metadata) {
         return this.VisitedObjects.Contains(metadata.Model ?? metadata.ModelType);
      }
   }

   public class ViewDataInfo {

      object _value;
      Func<object> _valueAccessor;

      public object Container { get; set; }

      public PropertyDescriptor PropertyDescriptor { get; set; }

      public object Value {
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

      public ViewDataInfo() { }

      public ViewDataInfo(Func<object> valueAccessor) {
         _valueAccessor = valueAccessor;
      }
   }

   public class ViewDataDictionary<TModel> : ViewDataDictionary {

      public new TModel Model {
         get { return (TModel)base.Model; }
         set { SetModel(value); }
      }

      public override ModelMetadata ModelMetadata {
         get {
            ModelMetadata result = base.ModelMetadata;

            if (result == null) {
               result = base.ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(TModel));
            }

            return result;
         }
         set { base.ModelMetadata = value; }
      }

      public ViewDataDictionary()
         : base(default(TModel)) { }

      public ViewDataDictionary(TModel model)
         : base(model) { }

      public ViewDataDictionary(ViewDataDictionary viewDataDictionary)
         : base(viewDataDictionary) { }

      protected override void SetModel(object value) {

         bool castWillSucceed = TypeHelpers.IsCompatibleObject<TModel>(value);

         if (castWillSucceed) {
            base.SetModel((TModel)value);
         } else {

            InvalidOperationException exception = (value != null) ?
               Error.ViewDataDictionary_WrongTModelType(value.GetType(), typeof(TModel))
               : Error.ViewDataDictionary_ModelCannotBeNull(typeof(TModel));

            throw exception;
         }
      }
   }

   public interface IViewDataContainer {

      [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is the mechanism by which the ViewPage / ViewUserControl get their ViewDataDictionary objects.")]
      ViewDataDictionary ViewData { get; set; }
   }

   sealed class DynamicViewDataDictionary : DynamicObject {

      readonly Func<ViewDataDictionary> _viewDataThunk;

      private ViewDataDictionary ViewData {
         get {
            ViewDataDictionary viewData = _viewDataThunk();
            Debug.Assert(viewData != null);
            return viewData;
         }
      }

      public DynamicViewDataDictionary(Func<ViewDataDictionary> viewDataThunk) {
         _viewDataThunk = viewDataThunk;
      }

      // Implementing this function improves the debugging experience as it provides the debugger with the list of all
      // the properties currently defined on the object

      public override IEnumerable<string> GetDynamicMemberNames() {
         return this.ViewData.Keys;
      }

      public override bool TryGetMember(GetMemberBinder binder, out object result) {

         result = this.ViewData[binder.Name];

         // since ViewDataDictionary always returns a result even if the key does not exist, always return true

         return true;
      }

      public override bool TrySetMember(SetMemberBinder binder, object value) {

         this.ViewData[binder.Name] = value;

         // you can always set a key in the dictionary so return true

         return true;
      }
   }
}