// Copyright 2023 Max Toro Q.
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Xcst.Web.Mvc.ModelBinding;

public class MetadataDetailsProvider : IDisplayMetadataProvider {

   static readonly object
   _showForDisplayKey = new();

   static readonly object
   _showForEditKey = new();

   static readonly object
   _groupName = new();

   static readonly object
   _shortName = new();

   public void
   CreateDisplayMetadata(DisplayMetadataProviderContext context) {

      var metadata = context.DisplayMetadata;

      var showForAttr = context.Attributes.OfType<ShowForAttribute>()
         .FirstOrDefault();

      if (showForAttr != null) {

         // because the framework uses true as default, we need a way to
         // tell if a value is explicitly specified, hence the use of AdditionalValues

         if (showForAttr._displaySet) {
            metadata.ShowForDisplay = showForAttr.Display;
            metadata.AdditionalValues[_showForDisplayKey] = showForAttr.Display;
         }

         if (showForAttr._editSet) {
            metadata.ShowForEdit = showForAttr.Edit;
            metadata.AdditionalValues[_showForEditKey] = showForAttr.Edit;
         }
      }

      var displayAtrr = context.Attributes.OfType<DisplayAttribute>()
         .FirstOrDefault();

      if (displayAtrr != null) {

         if (displayAtrr.GetGroupName() is string groupName) {
            metadata.AdditionalValues[_groupName] = groupName;
         }

         if (displayAtrr.GetShortName() is string shortName) {
            metadata.AdditionalValues[_shortName] = shortName;
         }
      }
   }

   internal static bool?
   GetShowForDisplay(ModelMetadata metadata) {

      if (metadata == null) throw new ArgumentNullException(nameof(metadata));

      if (metadata.AdditionalValues.TryGetValue(_showForDisplayKey, out var obj)
         && obj is bool b) {

         return b;
      }

      return null;
   }

   internal static bool?
   GetShowForEdit(ModelMetadata metadata) {

      if (metadata == null) throw new ArgumentNullException(nameof(metadata));

      if (metadata.AdditionalValues.TryGetValue(_showForEditKey, out var obj)
         && obj is bool b) {

         return b;
      }

      return null;
   }

   internal static string?
   GetGroupName(ModelMetadata metadata) {

      if (metadata == null) throw new ArgumentNullException(nameof(metadata));

      if (metadata.AdditionalValues.TryGetValue(_groupName, out var obj)
         && obj is string str) {

         return str;
      }

      return null;
   }

   internal static string?
   GetShortName(ModelMetadata metadata) {

      if (metadata == null) throw new ArgumentNullException(nameof(metadata));

      if (metadata.AdditionalValues.TryGetValue(_shortName, out var obj)
         && obj is string str) {

         return str;
      }

      return null;
   }
}
