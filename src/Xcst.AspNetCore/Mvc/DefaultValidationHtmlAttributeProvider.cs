// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Options;

namespace Xcst.Web.Mvc;

class DefaultValidationHtmlAttributeProvider {

   readonly IModelMetadataProvider
   _metadataProvider;

   readonly ClientValidatorCache
   _clientValidatorCache;

   readonly IClientModelValidatorProvider
   _clientModelValidatorProvider;

   public
   DefaultValidationHtmlAttributeProvider(
         IOptions<MvcViewOptions> optionsAccessor,
         IModelMetadataProvider metadataProvider,
         ClientValidatorCache clientValidatorCache) {

      ArgumentNullException.ThrowIfNull(optionsAccessor);
      ArgumentNullException.ThrowIfNull(metadataProvider);
      ArgumentNullException.ThrowIfNull(clientValidatorCache);

      _clientValidatorCache = clientValidatorCache;
      _metadataProvider = metadataProvider;

      var clientValidatorProviders = optionsAccessor.Value.ClientModelValidatorProviders;
      _clientModelValidatorProvider = new CompositeClientModelValidatorProvider(clientValidatorProviders);
   }

   public void
   AddValidationAttributes(
         ActionContext actionContext,
         ModelExplorer modelExplorer,
         IDictionary<string, string> attributes,
         bool excludeMinMaxLength) {

      ArgumentNullException.ThrowIfNull(actionContext);
      ArgumentNullException.ThrowIfNull(modelExplorer);
      ArgumentNullException.ThrowIfNull(attributes);

      var validators = _clientValidatorCache
         .GetValidators(modelExplorer.Metadata, _clientModelValidatorProvider);

      if (excludeMinMaxLength) {

         validators = validators
            .Where(p => !(p is ValidationAttributeAdapter<MinLengthAttribute>
               || p is ValidationAttributeAdapter<MaxLengthAttribute>))
            .ToArray();
      }

      if (validators.Count > 0) {

         var validationContext = new ClientModelValidationContext(
            actionContext,
            modelExplorer.Metadata,
            _metadataProvider,
            attributes);

         for (var i = 0; i < validators.Count; i++) {
            var validator = validators[i];
            validator.AddValidation(validationContext);
         }
      }
   }
}
