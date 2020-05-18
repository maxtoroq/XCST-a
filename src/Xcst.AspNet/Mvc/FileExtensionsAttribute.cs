// Copyright 2018 Max Toro Q.
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

#region FileExtensionsAttribute is based on code from .NET Framework
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.Reflection;

namespace Xcst.Web.Mvc {

   /// <exclude/>
   [EditorBrowsable(EditorBrowsableState.Never)]
   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
   public class FileExtensionsAttribute : ValidationAttribute, IClientValidatable {

      public string Extensions { get; }

      private string ExtensionsFormatted =>
         ExtensionsParsed.Aggregate((left, right) => left + ", " + right);

      [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings are normalized to lowercase because they are presented to the user in lowercase format")]
      private string ExtensionsNormalized =>
         Extensions.Replace(" ", "").Replace(".", "").ToLowerInvariant();

      private IEnumerable<string> ExtensionsParsed =>
         ExtensionsNormalized.Split(',').Select(e => "." + e);

      public FileExtensionsAttribute(string extensions) {

         if (String.IsNullOrWhiteSpace(extensions)) throw new ArgumentNullException(nameof(extensions));

         this.Extensions = extensions;

         base.GetType().GetProperty("DefaultErrorMessage", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(this, "The {0} field only accepts files with the following extensions: {1}");
      }

      public override string FormatErrorMessage(string name) =>
         String.Format(CultureInfo.CurrentCulture, this.ErrorMessageString, name, this.ExtensionsFormatted);

      public override bool IsValid(object value) {

         if (value is null) {
            return true;
         }

         if (value is string valueAsString) {
            return ValidateExtension(valueAsString);
         }

         if (value is Uri valueAsUri) {
            return ValidateExtension(valueAsUri.OriginalString);
         }

         if (value is HttpPostedFileBase valueAsFile) {
            return ValidateExtension(valueAsFile.FileName);
         }

         return false;
      }

      [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings are normalized to lowercase because they are presented to the user in lowercase format")]
      bool ValidateExtension(string fileName) {

         try {
            return ExtensionsParsed.Contains(Path.GetExtension(fileName).ToLowerInvariant());
         } catch (ArgumentException) {
            return false;
         }
      }

      public IEnumerable<ModelClientValidationRule> GetClientValidationRules(ModelMetadata metadata, ControllerContext context) {

         var rule = new ModelClientValidationRule {
            ValidationType = "extension",
            ErrorMessage = FormatErrorMessage(metadata.GetDisplayName())
         };

         rule.ValidationParameters["extension"] = this.ExtensionsNormalized;

         yield return rule;
      }
   }
}
