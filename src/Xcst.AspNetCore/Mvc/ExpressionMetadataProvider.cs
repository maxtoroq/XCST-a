// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

static class ExpressionMetadataProvider {

   public static ModelExplorer
   FromLambdaExpression<TParameter, TValue>(
         Expression<Func<TParameter, TValue>> expression,
         ModelExplorer modelExplorer,
         IModelMetadataProvider metadataProvider) {

      ArgumentNullException.ThrowIfNull(expression);
      ArgumentNullException.ThrowIfNull(modelExplorer);
      ArgumentNullException.ThrowIfNull(metadataProvider);

      var propertyName = default(string);
      var containerType = default(Type);
      var legalExpression = false;

      // Need to verify the expression is valid; it needs to at least end in something
      // that we can convert to a meaningful string for model binding purposes

      switch (expression.Body.NodeType) {
         case ExpressionType.ArrayIndex:
            // ArrayIndex always means a single-dimensional indexer; multi-dimensional indexer is a method call to Get()
            legalExpression = true;
            break;

         case ExpressionType.Call:
            // Only legal method call is a single argument indexer/DefaultMember call
            legalExpression = ExpressionHelper.IsSingleArgumentIndexer(expression.Body);
            break;

         case ExpressionType.MemberAccess:
            // Property/field access is always legal
            var memberExpression = (MemberExpression)expression.Body;

            propertyName = (memberExpression.Member is PropertyInfo) ?
               memberExpression.Member.Name : null;

            containerType = memberExpression.Expression?.Type;
            legalExpression = true;
            break;

         case ExpressionType.Parameter:
            // Parameter expression means "model => model", so we delegate to FromModel
            return FromModel(modelExplorer, metadataProvider);
      }

      if (!legalExpression) {
         throw new InvalidOperationException(
            "Templates can be used only with field access, property access, single-dimension array index, or single-parameter custom indexer expressions.");
      }

      object? modelAccessor(object container) {

         var model = (TParameter)container;

         var cachedFn = CachedExpressionCompiler.Process(expression);

         if (cachedFn != null) {
            return cachedFn.Invoke(model);
         }

         var fn = expression.Compile();

         try {
            return fn.Invoke((TParameter)container);

         } catch (NullReferenceException) {
            return null;
         }
      }

      var metadata = default(ModelMetadata);

      if (containerType != null
         && propertyName != null) {

         // Ex:
         //    m => m.Color (simple property access)
         //    m => m.Color.Red (nested property access)
         //    m => m.Widgets[0].Size (expression ending with property-access)
         metadata = metadataProvider.GetMetadataForType(containerType)
            .Properties[propertyName];
      }

      // Ex:
      //    m => 5 (arbitrary expression)
      //    m => foo (arbitrary expression)
      //    m => m.Widgets[0] (expression ending with non-property-access)
      //
      // This can also happen for any case where we cannot retrieve a model metadata.
      // This will happen for:
      // - fields
      // - statics
      // - non-visibility (internal/private)
      metadata ??= metadataProvider.GetMetadataForType(typeof(TValue));

      return modelExplorer.GetExplorerForExpression(metadata, modelAccessor);
   }

   public static ModelExplorer
   FromStringExpression(string expression, ModelExplorer modelExplorer, IModelMetadataProvider metadataProvider) {

      ArgumentNullException.ThrowIfNull(modelExplorer);
      ArgumentNullException.ThrowIfNull(metadataProvider);

      var viewDataInfo = ViewDataEvaluator.Eval(modelExplorer, expression);

      if (viewDataInfo is null) {

         //  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData

         var propertyExplorer = modelExplorer.GetExplorerForProperty(expression);

         if (propertyExplorer != null) {
            return propertyExplorer;
         }
      }

      if (viewDataInfo != null) {

         if (viewDataInfo.Container == modelExplorer
            && viewDataInfo.Value == modelExplorer.Model
            && String.IsNullOrEmpty(expression)) {

            // Nothing for empty expression in ViewData and ViewDataEvaluator just returned the model. Handle
            // using FromModel() for its object special case.
            return FromModel(modelExplorer, metadataProvider);
         }

         var containerExplorer = modelExplorer;
         var containerType = viewDataInfo.Container?.GetType();

         if (viewDataInfo.Container != null) {
            containerExplorer = metadataProvider.GetModelExplorerForType(containerType, viewDataInfo.Container);
         }

         if (viewDataInfo.PropertyDescriptor != null) {

            // We've identified a property access, which provides us with accurate metadata.
            var containerMetadata = metadataProvider.GetMetadataForType(containerType!);
            var propertyMetadata = containerMetadata.Properties[viewDataInfo.PropertyDescriptor.Name];

            object? modelAccessor(object _) => viewDataInfo.Value;

            return containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor);
         }

         if (viewDataInfo.Value != null) {

            // We have a value, even though we may not know where it came from.

            var valueMetadata = metadataProvider.GetMetadataForType(viewDataInfo.Value.GetType());
            return containerExplorer.GetExplorerForExpression(valueMetadata, viewDataInfo.Value);
         }
      }

      // Treat the expression as string if we don't find anything better.

      var stringMetadata = metadataProvider.GetMetadataForType(typeof(string));

      return modelExplorer.GetExplorerForExpression(stringMetadata, modelAccessor: null);
   }

   static ModelExplorer
   FromModel(ModelExplorer modelExplorer, IModelMetadataProvider metadataProvider) {

      ArgumentNullException.ThrowIfNull(modelExplorer);

      if (modelExplorer.Metadata.ModelType == typeof(object)) {

         // Use common simple type rather than object so e.g. Editor() at least generates a TextBox.
         var model = (modelExplorer.Model is null) ? null
            : Convert.ToString(modelExplorer.Model, CultureInfo.CurrentCulture);

         return metadataProvider.GetModelExplorerForType(typeof(string), model);
      }

      return modelExplorer;
   }
}
