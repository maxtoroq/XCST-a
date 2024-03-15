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

   ViewContext?
   _viewContext;

   ViewDataDictionary?
   _viewData;

   IModelMetadataProvider?
   _modelMetadataProvider;

   DynamicViewDataDictionary?
   _viewBag;

   HtmlHelper?
   _html;

   public override HttpContext
   HttpContext {
      get => base.HttpContext;
      set {
         base.HttpContext = value;

         if (value != null
            && ViewContext is null) {

            ViewContext = new ViewContext(value);
         }
      }
   }

   public virtual ViewContext
   ViewContext {
#pragma warning disable CS8603
      get => _viewContext;
#pragma warning restore CS8603
      set {
         _viewContext = value;

#pragma warning disable CS8601
         HttpContext = value?.HttpContext;
#pragma warning restore CS8601

#pragma warning disable CS8625
         _html = null;
#pragma warning restore CS8625
      }
   }

   public ViewDataDictionary
   ViewData {
      get {
         if (_viewData is null) {
            SetViewData(new ViewDataDictionary(MetadataProvider, ModelState));
         }
         return _viewData!;
      }
      set => SetViewData(value);
   }

   public dynamic
   ViewBag =>
      _viewBag ??= new DynamicViewDataDictionary(() => ViewData);

   public object?
   Model => ViewData.Model;

   private protected IModelMetadataProvider
   MetadataProvider {
      get => _modelMetadataProvider
         ??= HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();
   }

   public HtmlHelper
   Html {
      get {
         if (_html is null
            && ViewContext != null) {

            _html = new HtmlHelper(ViewContext, this, (IXcstPackage)this);
         }
#pragma warning disable CS8603
         return _html;
#pragma warning restore CS8603
      }
      set => _html = value;
   }

   public ModelStateDictionary
   ModelState => ViewContext.ActionContext.ModelState;

   internal virtual void
   SetViewData(ViewDataDictionary viewData) {
      _viewData = viewData;
   }

   public async Task<bool>
   TryUpdateModelAsync(
         object model, Type? modelType = null, string? prefix = null, IValueProvider? valueProvider = null) {

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
      //modelBindingContext.PropertyFilter = propertyFilter;

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

   protected override void
   CopyState(XcstPage page) {

      base.CopyState(page);

      if (page is XcstViewPage viewPage) {

         viewPage.ViewContext = new ViewContext(this.ViewContext);

         if (_viewData != null) {
            viewPage.ViewData = new ViewDataDictionary(_viewData);
         }
      }
   }
}

public abstract class XcstViewPage<TModel> : XcstViewPage {

   ViewDataDictionary<TModel>?
   _viewData;

   HtmlHelper<TModel>?
   _html;

   public new ViewDataDictionary<TModel>
   ViewData {
      get {
         if (_viewData is null) {
            SetViewData(new ViewDataDictionary<TModel>(MetadataProvider, ModelState));
         }
         return _viewData!;
      }
      set => SetViewData(value);
   }

   [MaybeNull]
   public new TModel
   Model => ViewData.Model;

   public new HtmlHelper<TModel>
   Html {
      get {
         if (_html is null
            && ViewContext != null) {

            _html = new HtmlHelper<TModel>(ViewContext, this, (IXcstPackage)this);
         }
#pragma warning disable CS8603
         return _html;
#pragma warning restore CS8603
      }
      set => _html = value;
   }

   internal override void
   SetViewData(ViewDataDictionary viewData) {

      _viewData = viewData as ViewDataDictionary<TModel>
         ?? new ViewDataDictionary<TModel>(viewData);

      base.SetViewData(_viewData);
   }
}
