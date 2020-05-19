﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Web.Mvc.Properties;
using System.Web.Routing;

namespace System.Web.Mvc {

   public class ViewContext : ControllerContext {

      // Some values have to be stored in HttpContext.Items in order to be propagated between calls
      // to RenderPartial(), RenderAction(), etc.

      static readonly object _formContextKey = new object();

      DynamicViewDataDictionary _ViewBag;

      // We need a default FormContext if the user uses html <form> instead of an MvcForm

      FormContext _defaultFormContext = new FormContext();

      public virtual bool ClientValidationEnabled { get; set; } = true;

      public virtual bool UnobtrusiveJavaScriptEnabled { get; set; } = true;

      /// <summary>
      /// Element name used to wrap a top-level message generated by
      /// <code>a:validation-summary</code>.
      /// </summary>
      public virtual string ValidationSummaryMessageElement { get; set; } = "span";

      /// <summary>
      /// Element name used to wrap a top-level message generated by
      /// <code>a:validation-message</code>.
      /// </summary>
      public virtual string ValidationMessageElement { get; set; } = "span";

      public virtual FormContext FormContext {
         get => HttpContext.Items[_formContextKey] as FormContext
            // Never return a null form context, this is important for validation purposes
            ?? _defaultFormContext;
         set => HttpContext.Items[_formContextKey] = value;
      }

      [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "The property setter is only here to support mocking this type and should not be called at runtime.")]
      public virtual ViewDataDictionary/*?*/ ViewData { get; set; }

      [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "The property setter is only here to support mocking this type and should not be called at runtime.")]
      public virtual TempDataDictionary/*?*/ TempData { get; set; }

      // parameterless constructor used for mocking
      public ViewContext() { }

      public ViewContext(HttpContextBase httpContext)
         : base((httpContext != null) ?
            httpContext.Request?.RequestContext ?? new RequestContext(httpContext, new RouteData())
            : throw new ArgumentNullException(nameof(httpContext))) { }

      [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "The virtual property setters are only to support mocking frameworks, in which case this constructor shouldn't be called anyway.")]
      public ViewContext(ControllerContext controllerContext, ViewDataDictionary/*?*/ viewData, TempDataDictionary/*?*/ tempData)
         : base(controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));
         // When cloning, ViewData/TempData can be null if ViewContext was initialized with HttpContextBase only
         //if (viewData is null) throw new ArgumentNullException(nameof(viewData));
         //if (tempData is null) throw new ArgumentNullException(nameof(tempData));

         this.ViewData = viewData;
         this.TempData = tempData;
      }

      internal FormContext GetFormContextForClientValidation() =>
         (this.ClientValidationEnabled) ? this.FormContext : null;
   }

   public class FormContext {

      readonly Dictionary<string, FieldValidationMetadata> _fieldValidators = new Dictionary<string, FieldValidationMetadata>();
      readonly Dictionary<string, bool> _renderedFields = new Dictionary<string, bool>();

      public IDictionary<string, FieldValidationMetadata> FieldValidators => _fieldValidators;

      public FieldValidationMetadata GetValidationMetadataForField(string fieldName) =>
         GetValidationMetadataForField(fieldName, createIfNotFound: false);

      public FieldValidationMetadata GetValidationMetadataForField(string fieldName, bool createIfNotFound) {

         if (String.IsNullOrEmpty(fieldName)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(fieldName));

         FieldValidationMetadata metadata;

         if (!this.FieldValidators.TryGetValue(fieldName, out metadata)) {

            if (createIfNotFound) {

               metadata = new FieldValidationMetadata {
                  FieldName = fieldName
               };

               this.FieldValidators[fieldName] = metadata;
            }
         }

         return metadata;
      }

      public bool RenderedField(string fieldName) {
         bool result;
         _renderedFields.TryGetValue(fieldName, out result);
         return result;
      }

      public void RenderedField(string fieldName, bool value) =>
         _renderedFields[fieldName] = value;
   }

   public class FieldValidationMetadata {

      readonly Collection<ModelClientValidationRule> _validationRules = new Collection<ModelClientValidationRule>();
      string _fieldName;

      public string FieldName {
         get => _fieldName ?? String.Empty;
         set => _fieldName = value;
      }

      public bool ReplaceValidationMessageContents { get; set; }

      public string ValidationMessageId { get; set; }

      public ICollection<ModelClientValidationRule> ValidationRules => _validationRules;
   }
}
