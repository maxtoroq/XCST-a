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

#region EditorInstructions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
#if ASPNETMVC
using System.Data;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Web.Mvc;
using Xcst.PackageModel;
using Xcst.Runtime;
#if NETCOREAPP
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;
#else
using IFormFile = System.Web.HttpPostedFileBase;
#endif

namespace Xcst.Web.Runtime {

   /// <exclude/>
   public static class EditorInstructions {

      public static void Editor(
            HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, string expression,
            string? htmlFieldName = null, string? templateName = null, IList<string>? membersNames = null, object? additionalViewData = null) =>
         TemplateHelpers.Template(
            html,
            package,
            output,
            expression: expression,
            htmlFieldName: htmlFieldName,
            templateName: templateName,
            membersNames,
            DataBoundControlMode.Edit,
            additionalViewData
         );

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void EditorFor<TModel, TValue>(
            HtmlHelper<TModel> html, IXcstPackage package, ISequenceWriter<object> output, Expression<Func<TModel, TValue>> expression,
            string? htmlFieldName = null, string? templateName = null, IList<string>? membersNames = null, object? additionalViewData = null) =>
         TemplateHelpers.TemplateFor(
            html,
            package,
            output,
            expression: expression,
            htmlFieldName: htmlFieldName,
            templateName: templateName,
            membersNames,
            DataBoundControlMode.Edit,
            additionalViewData
         );

      public static void EditorForModel(
            HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output,
            string? htmlFieldName = null, string? templateName = null, IList<string>? membersNames = null, object? additionalViewData = null) =>
         TemplateHelpers.TemplateHelper(
            html,
            package,
            output,
            html.ViewData.ModelMetadata,
            htmlFieldName: htmlFieldName,
            templateName: templateName,
            membersNames,
            DataBoundControlMode.Edit,
            additionalViewData
         );

      public static bool ShowForEdit(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (html is null) throw new ArgumentNullException(nameof(html));
         if (propertyMetadata is null) throw new ArgumentNullException(nameof(propertyMetadata));

         TemplateInfo templateInfo = html.ViewData.TemplateInfo;

         if (templateInfo.Visited(propertyMetadata)) {
            return false;
         }

#if !ASPNETMVC
         if (templateInfo.MembersNames.Count > 0) {
            return templateInfo.MembersNames.Contains(propertyMetadata.PropertyName);
         }
#endif

         if (!propertyMetadata.ShowForEdit) {
            return false;
         }

         if (propertyMetadata.AdditionalValues.TryGetValue(nameof(ModelMetadata.ShowForEdit), out bool show)) {
            return show;
         }

#if ASPNETMVC
         if (propertyMetadata.ModelType == typeof(EntityState)) {
            return false;
         }
#endif

         if (propertyMetadata.ModelType == typeof(IFormFile)) {
            return true;
         }

         return !propertyMetadata.IsComplexType;
      }

      public static XcstDelegate<object>? MemberTemplate(HtmlHelper html, ModelMetadata propertyMetadata) {

         if (html is null) throw new ArgumentNullException(nameof(html));
         if (propertyMetadata is null) throw new ArgumentNullException(nameof(propertyMetadata));

         if (html.ViewData.TryGetValue("__xcst_member_template", out Action<HtmlHelper, ISequenceWriter<object>>? memberTemplate)
               && memberTemplate != null) {

            HtmlHelper helper = HtmlHelperFactory.ForMemberTemplate(html, propertyMetadata);

            return (c, o) => memberTemplate(helper, o);
         }

         return null;
      }
   }
}
