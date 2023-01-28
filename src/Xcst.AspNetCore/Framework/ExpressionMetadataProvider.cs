// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Xcst.Web.Mvc.ExpressionUtil;
using Xcst.Web.Mvc.Properties;

namespace Xcst.Web.Mvc;

static class ExpressionMetadataProvider {

   [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
   public static ModelExplorer
   FromLambdaExpression<TParameter, TValue>(Expression<Func<TParameter, TValue>> expression, ViewDataDictionary<TParameter> viewData) =>
      FromLambdaExpression(expression, viewData, metadataProvider: null);

   internal static ModelExplorer
   FromLambdaExpression<TParameter, TValue>(
         Expression<Func<TParameter, TValue>> expression,
         ViewDataDictionary<TParameter> viewData,
         IModelMetadataProvider? metadataProvider) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));
      if (viewData is null) throw new ArgumentNullException(nameof(viewData));

      metadataProvider ??= viewData.MetadataProvider;

      string? propertyName = null;
      Type? containerType = null;
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
            propertyName = memberExpression.Member is PropertyInfo ? memberExpression.Member.Name : null;
            containerType = memberExpression.Expression.Type;
            legalExpression = true;
            break;

         case ExpressionType.Parameter:
            // Parameter expression means "model => model", so we delegate to FromModel
            return FromModel(viewData, metadataProvider);
      }

      if (!legalExpression) {
         throw new InvalidOperationException(MvcResources.TemplateHelpers_TemplateLimitations);
      }

      object? modelAccessor(object container) {
         try {
            return CachedExpressionCompiler
               .Process(expression)
               .Invoke((TParameter)container);
         } catch (NullReferenceException) {
            return null;
         }
      }

      ModelMetadata metadata = null;

      if (containerType != null && propertyName != null) {
         // Ex:
         //    m => m.Color (simple property access)
         //    m => m.Color.Red (nested property access)
         //    m => m.Widgets[0].Size (expression ending with property-access)
         metadata = metadataProvider.GetMetadataForType(containerType).Properties[propertyName];
      }

      if (metadata == null) {
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
         metadata = metadataProvider.GetMetadataForType(typeof(TValue));
         Debug.Assert(metadata != null);
      }

      return viewData.ModelExplorer.GetExplorerForExpression(metadata, modelAccessor);
   }

   public static ModelExplorer
   FromStringExpression(string expression, ViewDataDictionary viewData) =>
      FromStringExpression(expression, viewData, metadataProvider: null);

   internal static ModelExplorer
   FromStringExpression(string expression, ViewDataDictionary viewData, IModelMetadataProvider? metadataProvider) {

      if (expression is null) throw new ArgumentNullException(nameof(expression));
      if (viewData is null) throw new ArgumentNullException(nameof(viewData));

      metadataProvider ??= viewData.MetadataProvider;

      if (expression.Length == 0) {
         // Empty string really means "model metadata for the current model"
         return FromModel(viewData, metadataProvider);
      }

      var vdi = viewData.GetViewDataInfo(expression);

      if (vdi != null) {

         var containerExplorer = viewData.ModelExplorer;
         var containerType = vdi.Container?.GetType();

         if (vdi.Container != null) {
            containerExplorer = metadataProvider.GetModelExplorerForType(containerType, vdi.Container);
         }

         if (vdi.PropertyDescriptor != null) {

            // We've identified a property access, which provides us with accurate metadata.
            var containerMetadata = metadataProvider.GetMetadataForType(containerType!);
            var propertyMetadata = containerMetadata.Properties[vdi.PropertyDescriptor.Name];

            Func<object, object> modelAccessor = (_) => vdi.Value;

            return containerExplorer.GetExplorerForExpression(propertyMetadata, modelAccessor);
         }

         if (vdi.Value != null) {

            // We have a value, even though we may not know where it came from.

            var valueMetadata = metadataProvider.GetMetadataForType(vdi.Value.GetType());
            return containerExplorer.GetExplorerForExpression(valueMetadata, vdi.Value);
         }

      } else if (viewData.ModelExplorer != null) {

         //  Try getting a property from ModelMetadata if we couldn't find an answer in ViewData

         var propertyExplorer = viewData.ModelExplorer.GetExplorerForProperty(expression);

         if (propertyExplorer != null) {
            return propertyExplorer;
         }
      }

      // Treat the expression as string if we don't find anything better.

      //var stringMetadata = metadataProvider.GetMetadataForType(typeof(string));

      //return viewData.ModelExplorer.GetExplorerForExpression(stringMetadata, modelAccessor: null);
      return metadataProvider.GetModelExplorerForType(typeof(string), null);
   }

   static ModelExplorer
   FromModel(ViewDataDictionary viewData, IModelMetadataProvider metadataProvider) {

      ArgumentNullException.ThrowIfNull(viewData);

      return viewData.ModelExplorer
         ?? metadataProvider.GetModelExplorerForType(typeof(string), null);
   }
}
