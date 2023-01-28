// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Xcst.Web.Mvc;

public class ModelClientValidationRule {

   readonly Dictionary<string, object>
   _validationParameters = new();

   string?
   _validationType;

   public string?
   ErrorMessage { get; set; }

   public IDictionary<string, object>
   ValidationParameters => _validationParameters;

   public string
   ValidationType {
      get => _validationType ?? String.Empty;
      set => _validationType = value;
   }
}

public class ModelClientValidationEqualToRule : ModelClientValidationRule {

   public
   ModelClientValidationEqualToRule(string errorMessage, object other) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "equalto";
      this.ValidationParameters["other"] = other;
   }
}

public class ModelClientValidationMaxLengthRule : ModelClientValidationRule {

   public
   ModelClientValidationMaxLengthRule(string errorMessage, int maximumLength) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "maxlength";
      this.ValidationParameters["max"] = maximumLength;
   }
}

public class ModelClientValidationMinLengthRule : ModelClientValidationRule {

   public
   ModelClientValidationMinLengthRule(string errorMessage, int minimumLength) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "minlength";
      this.ValidationParameters["min"] = minimumLength;
   }
}

public class ModelClientValidationRangeRule : ModelClientValidationRule {

   public
   ModelClientValidationRangeRule(string errorMessage, object minValue, object maxValue) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "range";
      this.ValidationParameters["min"] = minValue;
      this.ValidationParameters["max"] = maxValue;
   }
}

public class ModelClientValidationRegexRule : ModelClientValidationRule {

   public
   ModelClientValidationRegexRule(string errorMessage, string pattern) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "regex";
      this.ValidationParameters.Add("pattern", pattern);
   }
}

public class ModelClientValidationRequiredRule : ModelClientValidationRule {

   public
   ModelClientValidationRequiredRule(string errorMessage) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "required";
   }
}

public class ModelClientValidationStringLengthRule : ModelClientValidationRule {

   public
   ModelClientValidationStringLengthRule(string errorMessage, int minimumLength, int maximumLength) {

      this.ErrorMessage = errorMessage;
      this.ValidationType = "length";

      if (minimumLength != 0) {
         this.ValidationParameters["min"] = minimumLength;
      }

      if (maximumLength != Int32.MaxValue) {
         this.ValidationParameters["max"] = maximumLength;
      }
   }
}
