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

#region HtmlHelper is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Xcst.Runtime;
using Xcst.Web.Mvc.ModelBinding;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   Display(string expression) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, this.ViewData);

      return new TemplateHelper(this, true, expression, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayForModel() =>
      new TemplateHelper(this, true, null, this.ViewData.ModelExplorer);

   /// <summary>
   /// Returns the properties that should be shown in a display template, based on the
   /// model's metadata.
   /// </summary>
   /// <returns>The relevant model properties.</returns>
   /// <remarks>
   /// This method uses the same logic used by the built-in <code>Object</code> display template;
   /// e.g. by default, it excludes complex-type properties.
   /// </remarks>
   public IEnumerable<ModelExplorer>
   DisplayProperties() {

      var templateInfo = this.ViewData.TemplateInfo;

      var filteredProperties = this.ViewData.ModelExplorer.Properties
         .Where(p => ShowForDisplay(p));

      var orderedProperties = (templateInfo.MembersNames.Count > 0) ?
         filteredProperties.OrderBy(p => templateInfo.MembersNames.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForDisplay(ModelExplorer propertyExplorer) {

      if (propertyExplorer is null) throw new ArgumentNullException(nameof(propertyExplorer));

      var templateInfo = this.ViewData.TemplateInfo;
      var propertyMetadata = propertyExplorer.Metadata;

      if (templateInfo.Visited(propertyExplorer)) {
         return false;
      }

      if (templateInfo.MembersNames.Count > 0) {
         return templateInfo.MembersNames.Contains(propertyMetadata.PropertyName!);
      }

      if (!propertyMetadata.ShowForDisplay) {
         return false;
      }

      if (MetadataDetailsProvider.GetShowForDisplay(propertyMetadata) is bool show) {
         return show;
      }

      return !propertyMetadata.IsComplexType;
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   Editor(string expression) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, this.ViewData);

      return new TemplateHelper(this, false, expression, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorForModel() =>
      new TemplateHelper(this, false, null, this.ViewData.ModelExplorer);

   /// <summary>
   /// Returns the properties that should be shown in an editor template, based on the
   /// model's metadata.
   /// </summary>
   /// <returns>The relevant model properties.</returns>
   /// <remarks>
   /// This method uses the same logic used by the built-in <code>Object</code> editor template;
   /// e.g. by default, it excludes complex-type properties.
   /// </remarks>
   public IEnumerable<ModelExplorer>
   EditorProperties() {

      var templateInfo = this.ViewData.TemplateInfo;

      var filteredProperties = this.ViewData.ModelExplorer.Properties
         .Where(p => ShowForEdit(p));

      var orderedProperties = (templateInfo.MembersNames.Count > 0) ?
         filteredProperties.OrderBy(p => templateInfo.MembersNames.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForEdit(ModelExplorer propertyExplorer) {

      if (propertyExplorer is null) throw new ArgumentNullException(nameof(propertyExplorer));

      var templateInfo = this.ViewData.TemplateInfo;
      var propertyMetadata = propertyExplorer.Metadata;

      if (templateInfo.Visited(propertyExplorer)) {
         return false;
      }

      if (templateInfo.MembersNames.Count > 0) {
         return templateInfo.MembersNames.Contains(propertyMetadata.PropertyName!);
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

   /// <summary>
   /// Returns the member template delegate for the provided property.
   /// </summary>
   /// <param name="propertyExplorer">The property's explorer.</param>
   /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>
   public XcstDelegate<object>?
   MemberTemplate(ModelExplorer propertyExplorer) {

      if (propertyExplorer is null) throw new ArgumentNullException(nameof(propertyExplorer));

      if (this.ViewData.TryGetValue("__xcst_member_template", out Action<HtmlHelper, ISequenceWriter<object>>? memberTemplate)
         && memberTemplate != null) {

         var helper = MakeHtmlHelperForMemberTemplate(propertyExplorer);

         return (c, o) => memberTemplate.Invoke(helper, o);
      }

      return null;
   }

   HtmlHelper
   MakeHtmlHelperForMemberTemplate(ModelExplorer memberExplorer) {

      if (memberExplorer is null) throw new ArgumentNullException(nameof(memberExplorer));

      var currentViewData = this.ViewData;

      var container = new ViewDataContainer(
         new ViewDataDictionary(currentViewData) {
            Model = memberExplorer.Model,
            ModelExplorer = memberExplorer,
            TemplateInfo = new TemplateInfo {
               HtmlFieldPrefix = currentViewData.TemplateInfo.GetFullHtmlFieldName(memberExplorer.Metadata.PropertyName)
            }
         }
      );

      return new HtmlHelper(this.ViewContext, container, this.CurrentPackage);
   }
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);

      return new TemplateHelper(this, true, ExpressionHelper.GetExpressionText(expression), modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);

      return new TemplateHelper(this, false, ExpressionHelper.GetExpressionText(expression), modelExplorer);
   }
}

public class TemplateHelper {

   readonly HtmlHelper
   _html;

   readonly bool
   _displayMode;

   readonly string?
   _expression;

   readonly ModelExplorer
   _modelExplorer;

   internal
   TemplateHelper(HtmlHelper html, bool displayMode, string? expression, ModelExplorer modelExplorer) {
      _html = html;
      _displayMode = displayMode;
      _expression = expression;
      _modelExplorer = modelExplorer;
   }

   [GeneratedCodeReference]
   public void
   Render(ISequenceWriter<object> output, string? htmlFieldName = null, string? templateName = null,
         IList<string>? membersNames = null, object? additionalViewData = null) {

      htmlFieldName ??= _expression;

      var metadata = _modelExplorer.Metadata;
      var model = _modelExplorer.Model;

      if (metadata.ConvertEmptyStringToNull
         && String.Empty.Equals(model)) {

         model = null;
      }

      // Normally this shouldn't happen, unless someone writes their own custom Object templates which
      // don't check to make sure that the object hasn't already been displayed

      var visitedObjectsKey = model ?? metadata.UnderlyingOrModelType;

      if (_html.ViewData.TemplateInfo.VisitedObjects.Contains(visitedObjectsKey)) {
         // DDB #224750
         return;
      }

      var formattedModelValue = model;

      if (model is null
         && _displayMode) {

         formattedModelValue = metadata.NullDisplayText;
      }

      var formatString = (_displayMode) ?
         metadata.DisplayFormatString
         : metadata.EditFormatString;

      if (model != null
         && !String.IsNullOrEmpty(formatString)) {

         formattedModelValue = (_displayMode) ?
            _html.CurrentPackage.Context.SimpleContent.Format(formatString, model)
            : String.Format(CultureInfo.CurrentCulture, formatString, model);
      }

      var viewData = new ViewDataDictionary(_html.ViewData) {
         Model = model,
         ModelExplorer = _modelExplorer.GetExplorerForModel(model),
         TemplateInfo = new TemplateInfo {
            FormattedModelValue = formattedModelValue,
            HtmlFieldPrefix = _html.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName),
            MembersNames = membersNames
         }
      };

      viewData.TemplateInfo.VisitedObjects = new HashSet<object>(_html.ViewData.TemplateInfo.VisitedObjects); // DDB #224750

      if (additionalViewData != null) {
         foreach (var kvp in HtmlHelper.ObjectToDictionary(additionalViewData)) {
            viewData[kvp.Key] = kvp.Value;
         }
      }

      viewData.TemplateInfo.VisitedObjects.Add(visitedObjectsKey); // DDB #224750

      new TemplateRenderer(_html.CurrentPackage, _html.ViewContext, viewData, templateName, _displayMode)
         .Render(output);
   }
}
