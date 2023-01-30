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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Xcst.Runtime;
using Xcst.Web.Mvc;
using Xcst.Web.Mvc.ModelBinding;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Runtime;

/// <exclude/>
public static class EditorInstructions {

   public static void
   Editor(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output, string expression,
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
   public static void
   EditorFor<TModel, TValue>(HtmlHelper<TModel> html, IXcstPackage package, ISequenceWriter<object> output, Expression<Func<TModel, TValue>> expression,
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

   public static void
   EditorForModel(HtmlHelper html, IXcstPackage package, ISequenceWriter<object> output,
         string? htmlFieldName = null, string? templateName = null, IList<string>? membersNames = null, object? additionalViewData = null) =>
      TemplateHelpers.TemplateHelper(
         html,
         package,
         output,
         html.ViewData.ModelExplorer,
         htmlFieldName: htmlFieldName,
         templateName: templateName,
         membersNames,
         DataBoundControlMode.Edit,
         additionalViewData
      );

   internal static IEnumerable<ModelExplorer>
   EditorProperties(HtmlHelper html) {

      if (html is null) throw new ArgumentNullException(nameof(html));

      var templateInfo = html.ViewData.TemplateInfo;

      var filteredProperties = html.ViewData.ModelExplorer.Properties
         .Where(p => ShowForEdit(html, p));

      var orderedProperties = (templateInfo.MembersNames.Count > 0) ?
         filteredProperties.OrderBy(p => templateInfo.MembersNames.IndexOf(p.Metadata.PropertyName))
         : filteredProperties;

      return orderedProperties;
   }

   static bool
   ShowForEdit(HtmlHelper html, ModelExplorer propertyExplorer) {

      if (html is null) throw new ArgumentNullException(nameof(html));
      if (propertyExplorer is null) throw new ArgumentNullException(nameof(propertyExplorer));

      var templateInfo = html.ViewData.TemplateInfo;
      var propertyMetadata = propertyExplorer.Metadata;

      if (templateInfo.Visited(propertyExplorer)) {
         return false;
      }

      if (templateInfo.MembersNames.Count > 0) {
         return templateInfo.MembersNames.Contains(propertyMetadata.PropertyName);
      }

      if (!propertyMetadata.ShowForEdit) {
         return false;
      }

      if (MetadataDetailsProvider.GetShowForEdit(propertyMetadata) is bool show) {
         return show;
      }

      if (propertyMetadata.ModelType == typeof(IFormFile)) {
         return true;
      }

      return !propertyMetadata.IsComplexType;
   }

   public static XcstDelegate<object>?
   MemberTemplate(HtmlHelper html, ModelExplorer propertyExplorer) {

      if (html is null) throw new ArgumentNullException(nameof(html));
      if (propertyExplorer is null) throw new ArgumentNullException(nameof(propertyExplorer));

      if (html.ViewData.TryGetValue("__xcst_member_template", out Action<HtmlHelper, ISequenceWriter<object>>? memberTemplate)
         && memberTemplate != null) {

         var helper = HtmlHelperFactory.ForMemberTemplate(html, propertyExplorer);

         return (c, o) => memberTemplate(helper, o);
      }

      return null;
   }
}
