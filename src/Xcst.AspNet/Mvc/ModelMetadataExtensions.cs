// Copyright 2016 Max Toro Q.
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
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using Xcst.Runtime;

namespace Xcst.Web.Mvc
#if !ASPNETLIB
   .Html
#endif
   {

#if !ASPNETLIB
   public
#endif

   static class ModelMetadataExtensions {

      const string MemberTemplateKey = "__xcst_member_template";

      /// <summary>
      /// Determines whether a property should be shown in a display template, based on its metadata.
      /// </summary>
      /// <param name="html">The current <see cref="HtmlHelper"/>.</param>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> display template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>

      public static bool ShowForDisplay(this HtmlHelper html, ModelMetadata propertyMetadata) {

         if (!propertyMetadata.ShowForDisplay
            || html.ViewData.TemplateInfo.Visited(propertyMetadata)) {

            return false;
         }

         bool show;

         if (propertyMetadata.AdditionalValues.TryGetValue(nameof(propertyMetadata.ShowForDisplay), out show)) {
            return show;
         }

#if !ASPNETLIB
         if (propertyMetadata.ModelType == typeof(EntityState)) {
            return false;
         }
#endif
         return !propertyMetadata.IsComplexType;
      }

      /// <summary>
      /// Determines whether a property should be shown in an editor template, based on its metadata.
      /// </summary>
      /// <param name="html">The current <see cref="HtmlHelper"/>.</param>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> editor template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>

      public static bool ShowForEdit(this HtmlHelper html, ModelMetadata propertyMetadata) {

         if (!propertyMetadata.ShowForEdit
            || html.ViewData.TemplateInfo.Visited(propertyMetadata)) {

            return false;
         }

         bool show;

         if (propertyMetadata.AdditionalValues.TryGetValue(nameof(propertyMetadata.ShowForEdit), out show)) {
            return show;
         }

#if !ASPNETLIB
         if (propertyMetadata.ModelType == typeof(EntityState)) {
            return false;
         } 
#endif
         return !propertyMetadata.IsComplexType;
      }

      /// <summary>
      /// Returns the member template delegate for the provided property.
      /// </summary>
      /// <param name="html">The current <see cref="HtmlHelper"/>.</param>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>

      public static Action<TemplateContext, XcstWriter> MemberTemplate(this HtmlHelper html, ModelMetadata propertyMetadata) {

         if (html == null) throw new ArgumentNullException(nameof(html));
         if (propertyMetadata == null) throw new ArgumentNullException(nameof(propertyMetadata));

         Action<ModelHelper, XcstWriter> memberTemplate;

         if (!html.ViewData.TryGetValue(MemberTemplateKey, out memberTemplate)) {
            return null;
         }

         ModelHelper modelHelper = ModelHelper.ForMemberTemplate(html, propertyMetadata);

         return (c, o) => memberTemplate(modelHelper, o);
      }
   }
}
