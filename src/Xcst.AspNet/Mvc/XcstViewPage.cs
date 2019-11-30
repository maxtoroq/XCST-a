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
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Xcst.Web.Mvc {

   public abstract class XcstViewPage : XcstPage, IViewDataContainer {

      ViewContext _ViewContext;
      ViewDataDictionary _ViewData;
      UrlHelper _Url;
      HtmlHelper _Html;
      TempDataDictionary _TempData;

      public virtual ViewContext ViewContext {
         get { return _ViewContext; }
         set {
            _ViewContext = value;

            Context = value?.HttpContext;

            if (value?.ViewData is ViewDataDictionary vd) {
               ViewData = vd;
            }

            Url = null;
            Html = null;
         }
      }

      public ViewDataDictionary ViewData {
         get {
            if (_ViewData == null) {
               SetViewData(new ViewDataDictionary());
            }
            return _ViewData;
         }
         set { SetViewData(value); }
      }

      public dynamic ViewBag => ViewContext?.ViewBag;

      public object Model => ViewData.Model;

      public virtual UrlHelper Url {
         get {
            if (_Url == null
               && ViewContext != null) {
               _Url =
#if !ASPNETLIB
                  (ViewContext.Controller as Controller)?.Url ??
#endif
                  new UrlHelper(ViewContext.RequestContext);
            }
            return _Url;
         }
         set { _Url = value; }
      }

      public HtmlHelper Html {
         get {
            if (_Html == null
               && ViewContext != null) {
               _Html = new HtmlHelper(ViewContext, this);
            }
            return _Html;
         }
         set { _Html = value; }
      }

      public ModelStateDictionary ModelState => ViewData.ModelState;

      public virtual TempDataDictionary TempData {
         get {
            return _TempData
               ?? (_TempData = ViewContext?.TempData)
               ?? (_TempData = new TempDataDictionary());
         }
         set { _TempData = value; }
      }

      internal virtual void SetViewData(ViewDataDictionary viewData) {
         _ViewData = viewData;
      }

#if ASPNETLIB
      public override IHttpHandler CreateHttpHandler() {
         return new XcstViewPageHandler(this);
      }

      public void Redirect(string url) {

         this.TempData?.Keep();

         this.Response.Redirect(this.Url.Content(url), endResponse: false);
      }

      public void RedirectPermanent(string url) {

         this.TempData?.Keep();

         this.Response.RedirectPermanent(this.Url.Content(url), endResponse: false);
      }

      public bool TryBind(object value, Type type = null, string prefix = null, string[] includeProperties = null, string[] excludeProperties = null, IValueProvider valueProvider = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         if (type == null) {
            type = value.GetType();
         }

         if (valueProvider == null) {
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

      public bool TryValidate(object value, string prefix = null) {

         if (value == null) throw new ArgumentNullException(nameof(value));

         ModelMetadata metadata = ModelMetadataProviders.Current.GetMetadataForType(() => value, value.GetType());

         foreach (ModelValidationResult validationResult in ModelValidator.GetModelValidator(metadata, this.ViewContext).Validate(null)) {
            this.ModelState.AddModelError(CreateSubPropertyName(prefix, validationResult.MemberName), validationResult.Message);
         }

         return this.ModelState.IsValid;
      }

      static bool IsPropertyAllowed(string propertyName, string[] includeProperties, string[] excludeProperties) {
         // We allow a property to be bound if its both in the include list AND not in the exclude list.
         // An empty include list implies all properties are allowed.
         // An empty exclude list implies no properties are disallowed.
         bool includeProperty = (includeProperties == null) || (includeProperties.Length == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         return includeProperty && !excludeProperty;
      }

      static string CreateSubPropertyName(string prefix, string propertyName) {

         if (String.IsNullOrEmpty(prefix)) {
            return propertyName;
         }

         if (String.IsNullOrEmpty(propertyName)) {
            return prefix;
         }

         return (prefix + "." + propertyName);
      }
#endif

      protected override void CopyState(XcstPage page) {

         base.CopyState(page);

         if (page is XcstViewPage viewPage) {

            viewPage.ViewContext = this.ViewContext.Clone(
#if !ASPNETLIB
               view: new XcstView(this.ViewContext, viewPage.VirtualPath),
#endif
               viewData: (_ViewData != null) ? new ViewDataDictionary(_ViewData)
                  // Never use this.ViewContext.ViewData
                  : (this.ViewContext.ViewData != null) ? new ViewDataDictionary()
                  : null,
               tempData: _TempData
            );
         }
      }
   }

   public abstract class XcstViewPage<TModel> : XcstViewPage {

      ViewDataDictionary<TModel> _ViewData;
      HtmlHelper<TModel> _Html;

      public new ViewDataDictionary<TModel> ViewData {
         get {
            if (_ViewData == null) {
               SetViewData(new ViewDataDictionary<TModel>());
            }
            return _ViewData;
         }
         set { SetViewData(value); }
      }

      public new TModel Model => ViewData.Model;

      public new HtmlHelper<TModel> Html {
         get {
            if (_Html == null
               && ViewContext != null) {
               _Html = new HtmlHelper<TModel>(ViewContext, this);
            }
            return _Html;
         }
         set { _Html = value; }
      }

      internal override void SetViewData(ViewDataDictionary viewData) {

         _ViewData = viewData as ViewDataDictionary<TModel>
            ?? new ViewDataDictionary<TModel>(viewData);

         base.SetViewData(_ViewData);
      }
   }
}
