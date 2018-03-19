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

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace Xcst.Web.Mvc {

   /// <exclude/>

   [EditorBrowsable(EditorBrowsableState.Never)]
   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
   public class FileMaxLengthAttribute : ValidationAttribute {

      public int MaxLength { get; }

      public FileMaxLengthAttribute(int length) {

         this.MaxLength = length;

         base.GetType().GetProperty("DefaultErrorMessage", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(this, "The {0} file cannot exceed {1} bytes.");
      }

      public override string FormatErrorMessage(string name) {
         return String.Format(CultureInfo.CurrentCulture, this.ErrorMessageString, name, this.MaxLength);
      }

      public override bool IsValid(object value) {

         if (value == null) {
            return true;
         }

         var valueAsFile = value as HttpPostedFileBase;

         if (valueAsFile != null) {
            return ValidateLength(valueAsFile.ContentLength);
         }

         return false;
      }

      bool ValidateLength(int length) {
         return length <= this.MaxLength;
      }
   }
}
