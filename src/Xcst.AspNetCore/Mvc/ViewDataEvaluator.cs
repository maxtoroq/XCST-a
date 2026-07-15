// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Xcst.Web.Mvc;

static class ViewDataEvaluator {

   public static ViewDataInfo?
   Eval(ModelExplorer modelExplorer, string? expression) {

      ArgumentNullException.ThrowIfNull(modelExplorer);

      if (String.IsNullOrEmpty(expression)) {
         // Null or empty expression name means current model even if that model is null.
         return new ViewDataInfo(modelExplorer, modelExplorer.Model);
      }

      return EvalComplexExpression(modelExplorer.Model, expression);
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
         return new ViewDataInfo(indexableObject, value);
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
      if (String.IsNullOrEmpty(propertyName)/* || container is ViewDataDictionary*/) {
         return null;
      }

      // Second, we try to use PropertyDescriptors and treat the expression as a property name
      var descriptor = TypeDescriptor.GetProperties(container)
         .Find(propertyName, ignoreCase: true);

      if (descriptor is null) {
         return null;
      }

      return new ViewDataInfo(container, descriptor, () => descriptor.GetValue(container));
   }
}

class ViewDataInfo {

   object?
   _value;

   Func<object?>?
   _valueAccessor;

   public object
   Container { get; }

   public PropertyDescriptor?
   PropertyDescriptor { get; }

   public object?
   Value {
      get {
         if (_valueAccessor != null) {
            _value = _valueAccessor.Invoke();
            _valueAccessor = null;
         }

         return _value;
      }
      set {
         _value = value;
         _valueAccessor = null;
      }
   }

   public
   ViewDataInfo(object container, object? value) {
      this.Container = container;
      _value = value;
   }

   public
   ViewDataInfo(object container, PropertyDescriptor propertyDescriptor) {
      this.Container = container;
      this.PropertyDescriptor = propertyDescriptor;
   }

   public
   ViewDataInfo(object container, PropertyDescriptor propertyDescriptor, Func<object?> valueAccessor)
      : this(container, propertyDescriptor) {

      _valueAccessor = valueAccessor;
   }
}
