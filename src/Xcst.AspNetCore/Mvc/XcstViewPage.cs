﻿// Copyright 2015 Max Toro Q.
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
using System.Web.Mvc;

namespace Xcst.Web.Mvc {

   // Many of the properties of XcstViewPage can be null if ViewContext is not initialized.
   // These are however not marked as nullable since, at runtime, ViewContext is always initialized.

   public abstract class XcstViewPage : XcstPage, IViewDataContainer {

      ViewContext?
      _viewContext;

      ViewDataDictionary?
      _viewData;

      DynamicViewDataDictionary?
      _viewBag;

      UrlHelper?
      _url;

      HtmlHelper?
      _html;

      TempDataDictionary?
      _tempData;

      public virtual ViewContext
      ViewContext {
#pragma warning disable CS8603
         get => _viewContext;
#pragma warning restore CS8603
         set {
            _viewContext = value;

#pragma warning disable CS8601
            Context = value?.HttpContext;
#pragma warning restore CS8601

#pragma warning disable CS8625
            Url = null;
            Html = null;
#pragma warning restore CS8625
         }
      }

      public ViewDataDictionary
      ViewData {
         get {
            if (_viewData is null) {
               SetViewData(new ViewDataDictionary());
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

      public virtual UrlHelper
      Url {
         get {
            if (_url is null
               && Context != null) {

               _url = new UrlHelper(Context);
            }
#pragma warning disable CS8603
            return _url;
#pragma warning restore CS8603
         }
         set => _url = value;
      }

      public HtmlHelper
      Html {
         get {
            if (_html is null
               && ViewContext != null) {

               _html = new HtmlHelper(ViewContext, this);
            }
#pragma warning disable CS8603
            return _html;
#pragma warning restore CS8603
         }
         set => _html = value;
      }

      public ModelStateDictionary
      ModelState => ViewData.ModelState;

      public TempDataDictionary
      TempData {
         get {
            if (_tempData is null) {
               _tempData = new TempDataDictionary();
               _tempData.Load(ViewContext, ViewContext.TempDataProvider);
            }

            return _tempData;
         }
         set => _tempData = value;
      }

      internal bool
      HasTempData => _tempData != null;

      internal virtual void
      SetViewData(ViewDataDictionary viewData) {
         _viewData = viewData;
      }

      public void
      Redirect(string url) {

         _tempData?.Keep();

         this.Response.Redirect(this.Url.Content(url));
      }

      public void
      RedirectPermanent(string url) {

         _tempData?.Keep();

         this.Response.Redirect(this.Url.Content(url), permanent: true);
      }

      public bool
      TryBind(
            object value, Type? type = null, string? prefix = null, string[]? includeProperties = null,
            string[]? excludeProperties = null, IValueProvider? valueProvider = null) {

         if (value is null) throw new ArgumentNullException(nameof(value));

         if (type is null) {
            type = value.GetType();
         }

         if (valueProvider is null) {
            valueProvider = ValueProviderFactories.Factories.GetValueProvider(this.ViewContext);
         }

         var bindingContext = new ModelBindingContext {
            ModelMetadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, type),
            ModelName = prefix,
            ModelState = this.ModelState,
            PropertyFilter = p => IsPropertyAllowed(p, includeProperties, excludeProperties),
            ValueProvider = valueProvider
         };

         IModelBinder binder = ModelBinders.Binders.GetBinder(type);

         binder.BindModel(this.ViewContext, bindingContext);

         return this.ModelState.IsValid;
      }

      public bool
      TryValidate(object value, string? prefix = null) {

         if (value is null) throw new ArgumentNullException(nameof(value));

         ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, value.GetType());

         foreach (ModelValidationResult validationResult in ModelValidator.GetModelValidator(metadata, this.ViewContext).Validate(null)) {
            this.ModelState.AddModelError(CreateSubPropertyName(prefix, validationResult.MemberName), validationResult.Message);
         }

         return this.ModelState.IsValid;
      }

      static bool
      IsPropertyAllowed(string propertyName, string[]? includeProperties, string[]? excludeProperties) {
         // We allow a property to be bound if its both in the include list AND not in the exclude list.
         // An empty include list implies all properties are allowed.
         // An empty exclude list implies no properties are disallowed.
         bool includeProperty = (includeProperties is null) || (includeProperties.Length == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         return includeProperty && !excludeProperty;
      }

      static string
      CreateSubPropertyName(string? prefix, string propertyName) {

         if (String.IsNullOrEmpty(prefix)) {
            return propertyName;
         }

         if (String.IsNullOrEmpty(propertyName)) {
            return prefix ?? String.Empty;
         }

         return (prefix + "." + propertyName);
      }

      public override XcstViewPageHandler
      CreateHttpHandler() => new XcstViewPageHandler(this);

      protected override void
      CopyState(XcstPage page) {

         base.CopyState(page);

         if (page is XcstViewPage viewPage) {

            viewPage.ViewContext = this.ViewContext.Clone();

            if (_viewData != null) {
               viewPage.ViewData = new ViewDataDictionary(_viewData);
            }

            if (_tempData != null) {
               viewPage.TempData = _tempData;
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
               SetViewData(new ViewDataDictionary<TModel>());
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

               _html = new HtmlHelper<TModel>(ViewContext, this);
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
}
