// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   // TODO: Unit test ModelState interaction with VDD

   public class ViewDataDictionary : IDictionary<string, object?> {

      readonly IDictionary<string, object?> _innerDictionary;
      readonly ModelStateDictionary _modelState;
      object? _model;
      ModelMetadata? _modelMetadata;
      TemplateInfo? _templateMetadata;

      public int Count => _innerDictionary.Count;

      public bool IsReadOnly => _innerDictionary.IsReadOnly;

      public ICollection<string> Keys => _innerDictionary.Keys;

      public object? Model {
         get => _model;
         set {
            _modelMetadata = null;
            SetModel(value);
         }
      }

      public virtual ModelMetadata ModelMetadata {
         get {
            if (_modelMetadata is null && _model != null) {
               _modelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => _model, _model.GetType());
            }
#pragma warning disable CS8603 // can be null, but most times when requested it's not
            return _modelMetadata;
#pragma warning restore CS8603
         }
         set => _modelMetadata = value;
      }

      public ModelStateDictionary ModelState => _modelState;

      public TemplateInfo TemplateInfo {
         get => _templateMetadata ??= new TemplateInfo();
         set => _templateMetadata = value;
      }

      public ICollection<object?> Values => _innerDictionary.Values;

      public object? this[string key] {
         get {
            object? value;
            _innerDictionary.TryGetValue(key, out value);
            return value;
         }
         set => _innerDictionary[key] = value;
      }

      // For unit testing

      internal IDictionary<string, object?> InnerDictionary => _innerDictionary;

      public ViewDataDictionary()
         : this(default(object)) { }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
      public ViewDataDictionary(object? model) {

         this.Model = model;
         _innerDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
         _modelState = new ModelStateDictionary();
      }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "See note on SetModel() method.")]
      public ViewDataDictionary(ViewDataDictionary dictionary) {

         if (dictionary is null) throw new ArgumentNullException(nameof(dictionary));

         _innerDictionary = new CopyOnWriteDictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
         _modelState = new ModelStateDictionary(dictionary.ModelState);

         this.Model = dictionary.Model;
         this.TemplateInfo = dictionary.TemplateInfo;

         // PERF: Don't unnecessarily instantiate the model metadata
         _modelMetadata = dictionary._modelMetadata;
      }

      public void Add(KeyValuePair<string, object?> item) =>
         _innerDictionary.Add(item);

      public void Add(string key, object? value) =>
         _innerDictionary.Add(key, value);

      public void Clear() =>
         _innerDictionary.Clear();

      public bool Contains(KeyValuePair<string, object?> item) =>
         _innerDictionary.Contains(item);

      public bool ContainsKey(string key) =>
         _innerDictionary.ContainsKey(key);

      public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
         _innerDictionary.CopyTo(array, arrayIndex);

      public object? Eval(string expression) {
         ViewDataInfo? info = GetViewDataInfo(expression);
         return info?.Value;
      }

      public string Eval(string expression, string? format) {
         object? value = Eval(expression);
         return FormatValueInternal(value, format);
      }

      internal static string FormatValueInternal(object? value, string? format) {

         if (value is null) {
            return String.Empty;
         }

         if (String.IsNullOrEmpty(format)) {
            return Convert.ToString(value, CultureInfo.CurrentCulture);
         } else {
            return String.Format(CultureInfo.CurrentCulture, format, value);
         }
      }

      public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() =>
         _innerDictionary.GetEnumerator();

      public ViewDataInfo? GetViewDataInfo(string expression) {

         if (String.IsNullOrEmpty(expression)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(expression));

         return ViewDataEvaluator.Eval(this, expression);
      }

      public bool Remove(KeyValuePair<string, object?> item) =>
         _innerDictionary.Remove(item);

      public bool Remove(string key) =>
         _innerDictionary.Remove(key);

      // This method will execute before the derived type's instance constructor executes. Derived types must
      // be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
      // enough so as not to depend on the "this" pointer referencing a fully constructed object.

      protected virtual void SetModel(object? value) =>
         _model = value;

      public bool TryGetValue(string key, out object? value) =>
         _innerDictionary.TryGetValue(key, out value);

      internal static class ViewDataEvaluator {

         public static ViewDataInfo? Eval(ViewDataDictionary vdd, string expression) {

            //Given an expression "foo.bar.baz" we look up the following (pseudocode):
            //  this["foo.bar.baz.quux"]
            //  this["foo.bar.baz"]["quux"]
            //  this["foo.bar"]["baz.quux]
            //  this["foo.bar"]["baz"]["quux"]
            //  this["foo"]["bar.baz.quux"]
            //  this["foo"]["bar.baz"]["quux"]
            //  this["foo"]["bar"]["baz.quux"]
            //  this["foo"]["bar"]["baz"]["quux"]

            ViewDataInfo? evaluated = EvalComplexExpression(vdd, expression);
            return evaluated;
         }

         static ViewDataInfo? EvalComplexExpression(object indexableObject, string expression) {

            foreach (ExpressionPair expressionPair in GetRightToLeftExpressions(expression)) {

               string subExpression = expressionPair.Left;
               string postExpression = expressionPair.Right;

               ViewDataInfo? subTargetInfo = GetPropertyValue(indexableObject, subExpression);

               if (subTargetInfo != null) {

                  if (String.IsNullOrEmpty(postExpression)) {
                     return subTargetInfo;
                  }

                  if (subTargetInfo.Value != null) {

                     ViewDataInfo? potential = EvalComplexExpression(subTargetInfo.Value, postExpression);

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

         static ViewDataInfo? GetIndexedPropertyValue(object indexableObject, string key) {

            object? value = null;
            bool success = false;

            if (indexableObject is IDictionary<string, object> dict) {
               success = dict.TryGetValue(key, out value);
            } else {

               TryGetValueDelegate? tgvDel = TypeHelpers.CreateTryGetValueDelegate(indexableObject.GetType());

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

         static ViewDataInfo? GetPropertyValue(object container, string propertyName) {

            // This method handles one "segment" of a complex property expression

            // First, we try to evaluate the property based on its indexer
            ViewDataInfo? value = GetIndexedPropertyValue(container, propertyName);

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
            PropertyDescriptor descriptor = TypeDescriptor.GetProperties(container)
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

            public readonly string Left;
            public readonly string Right;

            public ExpressionPair(string left, string right) {
               Left = left;
               Right = right;
            }
         }
      }

      IEnumerator IEnumerable.GetEnumerator() =>
         _innerDictionary.GetEnumerator();
   }

   public class TemplateInfo {

      string? _htmlFieldPrefix;
      object? _formattedModelValue;
      IList<string>? _membersNames;
      HashSet<object>? _visitedObjects;

      [AllowNull]
      public object FormattedModelValue {
         get => _formattedModelValue ?? String.Empty;
         set => _formattedModelValue = value;
      }

      [AllowNull]
      public string HtmlFieldPrefix {
         get => _htmlFieldPrefix ?? String.Empty;
         set => _htmlFieldPrefix = value;
      }

      [AllowNull]
      internal IList<string> MembersNames {
         get => _membersNames ?? Array.Empty<string>();
         set => _membersNames = value;
      }

      public int TemplateDepth => VisitedObjects.Count;

      // DDB #224750 - Keep a collection of visited objects to prevent infinite recursion

      internal HashSet<object> VisitedObjects {
         get => _visitedObjects ??= new HashSet<object>();
         set => _visitedObjects = value;
      }

      public string GetFullHtmlFieldId(string? partialFieldName) =>
         HtmlHelper.GenerateIdFromName(GetFullHtmlFieldName(partialFieldName));

      public string GetFullHtmlFieldName(string? partialFieldName) {

         if (partialFieldName?.StartsWith("[", StringComparison.Ordinal) == true) {

            // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
            // with a 'dot' would be invalid.

            return this.HtmlFieldPrefix + partialFieldName;

         } else {

            // This uses "combine and trim" because either or both of these values might be empty
            return (this.HtmlFieldPrefix + "." + (partialFieldName ?? String.Empty)).Trim('.');
         }
      }

      public bool Visited(ModelMetadata metadata) =>
         this.VisitedObjects.Contains(metadata.Model ?? metadata.ModelType);
   }

   public class ViewDataInfo {

      object? _value;
      Func<object?>? _valueAccessor;

      public object Container { get; set; }

      public PropertyDescriptor PropertyDescriptor { get; set; }

      public object? Value {
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
      public ViewDataInfo() { }

      public ViewDataInfo(Func<object?> valueAccessor) {
         _valueAccessor = valueAccessor;
      }
#pragma warning restore CS8618
   }

   public class ViewDataDictionary<TModel> : ViewDataDictionary {

      [MaybeNull]
      public new TModel Model {
         get => (TModel)base.Model;
         set => SetModel(value);
      }

      public override ModelMetadata ModelMetadata {
         get {
            ModelMetadata result = base.ModelMetadata;

            if (result is null) {
               result = base.ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, typeof(TModel));
            }

            return result;
         }
         set => base.ModelMetadata = value;
      }

      public ViewDataDictionary()
         : base(default(TModel)) { }

      public ViewDataDictionary(TModel model)
         : base(model) { }

      public ViewDataDictionary(ViewDataDictionary viewDataDictionary)
         : base(viewDataDictionary) { }

      protected override void SetModel(object? value) {

         bool castWillSucceed = TypeHelpers.IsCompatibleObject<TModel>(value);

         if (castWillSucceed) {
            base.SetModel((TModel)value);
         } else {

            string errorMessage = (value != null) ?
               String.Format(CultureInfo.CurrentCulture, MvcResources.ViewDataDictionary_WrongTModelType, value.GetType(), typeof(TModel))
               : String.Format(CultureInfo.CurrentCulture, MvcResources.ViewDataDictionary_ModelCannotBeNull, typeof(TModel));

            throw new InvalidOperationException(errorMessage);
         }
      }
   }

   public interface IViewDataContainer {

      [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is the mechanism by which the ViewPage / ViewUserControl get their ViewDataDictionary objects.")]
      ViewDataDictionary ViewData { get; set; }
   }
}
