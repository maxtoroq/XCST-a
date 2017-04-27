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

#region DisplayInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Xcst.PackageModel;

namespace Xcst.Web.Runtime {

   /// <exclude/>

   public static class DisplayInstructions {

      public static void Display(HtmlHelper html,
                                 XcstWriter output,
                                 string expression,
                                 string templateName = null,
                                 string htmlFieldName = null,
                                 object additionalViewData = null) {

         TemplateHelpers.Template(html, output, expression, templateName, htmlFieldName, DataBoundControlMode.ReadOnly, additionalViewData);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void DisplayFor<TModel, TValue>(HtmlHelper<TModel> html,
                                                    XcstWriter output,
                                                    Expression<Func<TModel, TValue>> expression,
                                                    string templateName = null,
                                                    string htmlFieldName = null,
                                                    object additionalViewData = null) {

         TemplateHelpers.TemplateFor(html, output, expression, templateName, htmlFieldName, DataBoundControlMode.ReadOnly, additionalViewData);
      }

      public static void DisplayForModel(HtmlHelper html,
                                         XcstWriter output,
                                         string templateName = null,
                                         string htmlFieldName = null,
                                         object additionalViewData = null) {

         TemplateHelpers.TemplateHelper(html, output, html.ViewData.ModelMetadata, htmlFieldName, templateName, DataBoundControlMode.ReadOnly, additionalViewData);
      }

      public static bool ShowForDisplay(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (html == null) throw new ArgumentNullException(nameof(html));
         if (propertyMetadata == null) throw new ArgumentNullException(nameof(propertyMetadata));

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

      public static Action<TemplateContext, XcstWriter> MemberTemplate(HtmlHelper html, ModelMetadata propertyMetadata) {
         return EditorInstructions.MemberTemplate(html, propertyMetadata);
      }
   }
}
