// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MvcOptions = Microsoft.AspNetCore.Mvc.MvcOptions;

namespace Xcst.Web.Mvc;

// Many of the properties of XcstViewPage can be null if ViewContext is not initialized.
// These are however not marked as nullable since, at runtime, ViewContext is always initialized.

public abstract class XcstViewPage : XcstPage, IViewDataContainer {

   ModelExplorer?
   _modelExplorer;

   IModelMetadataProvider?
   _modelMetadataProvider;

   HtmlHelper?
   _html;

#pragma warning disable CS8618
   public ViewContext
   ViewContext { get; private set; }
#pragma warning restore CS8618

   protected internal virtual Type
   DeclaredModelType => typeof(Object);

   public ModelExplorer
   ModelExplorer {
      get => _modelExplorer
         ??= MetadataProvider.GetModelExplorerForType(DeclaredModelType, null);
      internal set => _modelExplorer = value;
   }

   public object?
   Model {
      get => ModelExplorer.Model;
      set => SetModel(value);
   }

   private IModelMetadataProvider
   MetadataProvider => _modelMetadataProvider
      ??= HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();

   public HtmlHelper
   Html => _html
      ??= CreateHtmlHelper(ViewContext, this, MetadataProvider);

   public ModelStateDictionary
   ModelState => ViewContext.ActionContext.ModelState;

   void
   SetModel(object? value) {

      var declaredType = this.DeclaredModelType;
      var isCompatibleType = (value is null) ?
         TypeHelpers.TypeAllowsNullValue(declaredType)
         : declaredType.IsInstanceOfType(value);

      if (!isCompatibleType) {
         throw new ArgumentException(
            $"The provided value is not compatible with the declared model type '{declaredType}'.", nameof(value));
      }

      _modelExplorer = this.MetadataProvider
         .GetModelExplorerForType(value?.GetType() ?? declaredType, value);
   }

   public sealed override void
   Contextualize(HttpContext httpContext, string virtualPath, string pathInfo) {

      ArgumentNullException.ThrowIfNull(httpContext);
      ArgumentNullException.ThrowIfNull(virtualPath);
      ArgumentNullException.ThrowIfNull(pathInfo);

      base.Contextualize(httpContext, virtualPath, pathInfo);

      Contextualize(new ViewContext(httpContext, this as IXcstPackage));
   }

   public virtual void
   Contextualize(ViewContext viewContext) {

      ArgumentNullException.ThrowIfNull(viewContext);

      if (this.HttpContext is null) {
         base.Contextualize(viewContext.HttpContext, String.Empty, String.Empty);
      }

      this.ViewContext = viewContext;

      _html = null;
   }

   protected virtual HtmlHelper
   CreateHtmlHelper(ViewContext viewContext, IViewDataContainer container, IModelMetadataProvider metadataProvider) =>
      new HtmlHelper(viewContext, container, metadataProvider);

   public async Task<bool>
   TryUpdateModelAsync(
         object model, Type? modelType = null, string? prefix = null,
         IValueProvider? valueProvider = null, Func<ModelMetadata, bool>? propertyFilter = null) {

      var modelBinderFactory = this.HttpContext.RequestServices
         .GetRequiredService<IModelBinderFactory>();

      var objectValidator = this.HttpContext.RequestServices
         .GetRequiredService<IObjectModelValidator>();

      var mvcOptions = this.HttpContext.RequestServices
         .GetRequiredService<IOptions<MvcOptions>>();

      ArgumentNullException.ThrowIfNull(model);

      var actionContext = this.ViewContext.ActionContext;

      modelType ??= model.GetType();

      valueProvider ??= await CompositeValueProvider
         .CreateAsync(actionContext, mvcOptions.Value.ValueProviderFactories.ToArray());

      var metadataForType = this.MetadataProvider.GetMetadataForType(modelType);

      if (metadataForType.BoundConstructor != null) {
         throw new NotSupportedException(
            $"{nameof(TryUpdateModelAsync)} cannot update a record type model."
            + $" If a '{modelType}' must be updated, include it in an object type.");
      }

      var modelBindingContext = DefaultModelBindingContext
         .CreateBindingContext(actionContext, valueProvider, metadataForType, null, prefix ?? String.Empty);
      modelBindingContext.Model = model;
      modelBindingContext.PropertyFilter = propertyFilter;

      var context = new ModelBinderFactoryContext {
         Metadata = metadataForType,
         BindingInfo = new BindingInfo {
            BinderModelName = metadataForType.BinderModelName,
            BinderType = metadataForType.BinderType,
            BindingSource = metadataForType.BindingSource,
            PropertyFilterProvider = metadataForType.PropertyFilterProvider
         },
         CacheToken = metadataForType
      };

      await modelBinderFactory.CreateBinder(context)
         .BindModelAsync(modelBindingContext);

      var result = modelBindingContext.Result;

      if (result.IsModelSet) {
         objectValidator.Validate(actionContext, modelBindingContext.ValidationState, modelBindingContext.ModelName, result.Model);
         return this.ModelState.IsValid;
      }

      return false;
   }

   public bool
   TryValidateModel(object model, string? prefix = null) {

      ArgumentNullException.ThrowIfNull(model);

      var objectValidator = this.HttpContext.RequestServices
         .GetRequiredService<IObjectModelValidator>();

      objectValidator.Validate(this.ViewContext.ActionContext, null, prefix ?? String.Empty, model);

      return this.ModelState.IsValid;
   }

   public override Task
   RenderPageAsync() => RenderViewPageAsync();

   protected virtual Task
   RenderViewPageAsync() => base.RenderPageAsync();
}

public abstract class XcstViewPage<TModel> : XcstViewPage {

   protected internal override Type
   DeclaredModelType => typeof(TModel);

   [MaybeNull]
   public new TModel
   Model {
      get => (TModel)base.Model!;
      set => base.Model = value;
   }

   public new HtmlHelper<TModel>
   Html => (HtmlHelper<TModel>)base.Html;

   protected override HtmlHelper
   CreateHtmlHelper(ViewContext viewContext, IViewDataContainer container, IModelMetadataProvider metadataProvider) =>
      new HtmlHelper<TModel>(viewContext, container, metadataProvider);
}
