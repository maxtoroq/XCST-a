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
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

using ModelBinding;

partial class HtmlHelper {

   // *ForModel() helpers should call GetModelExplorerFromString(String.Empty)
   // and not use this.ModelExplorer

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   Display(string expression) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return new TemplateHelper(this, true, expression, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayForModel() =>
      Display(String.Empty);

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

      var filteredProperties = this.ModelExplorer.Properties
         .Where(ShowForDisplay);

      var orderedProperties = (this.ViewContext.MembersNames.Count > 0) ?
         filteredProperties.OrderBy(p => this.ViewContext.MembersNames.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForDisplay(ModelExplorer propertyExplorer) {

      ArgumentNullException.ThrowIfNull(propertyExplorer);

      var propertyMetadata = propertyExplorer.Metadata;

      if (this.ViewContext.Visited(propertyExplorer)) {
         return false;
      }

      if (this.ViewContext.MembersNames is { Count: > 0 } names) {
         return names.Contains(propertyMetadata.PropertyName!);
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

      var modelExplorer = GetModelExplorerFromString(expression);

      return new TemplateHelper(this, false, expression, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorForModel() =>
      Editor(String.Empty);

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

      var filteredProperties = this.ModelExplorer.Properties
         .Where(ShowForEdit);

      var orderedProperties = (this.ViewContext.MembersNames.Count > 0) ?
         filteredProperties.OrderBy(p => this.ViewContext.MembersNames.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForEdit(ModelExplorer propertyExplorer) {

      ArgumentNullException.ThrowIfNull(propertyExplorer);

      var propertyMetadata = propertyExplorer.Metadata;

      if (this.ViewContext.Visited(propertyExplorer)) {
         return false;
      }

      if (this.ViewContext.MembersNames is { Count: > 0 } names) {
         return names.Contains(propertyMetadata.PropertyName!);
      }

      if (!propertyMetadata.ShowForEdit) {
         return false;
      }

      if (MetadataDetailsProvider.GetShowForEdit(propertyMetadata) is bool show) {
         return show;
      }

      var formFileType = typeof(IFormFile);

      if (formFileType.IsAssignableFrom(propertyMetadata.ModelType)
         || (propertyMetadata.IsEnumerableType && formFileType.IsAssignableFrom(propertyMetadata.ElementType))) {

         return true;
      }

      return !propertyMetadata.IsComplexType;
   }

   /// <summary>
   /// Returns the member template delegate for the provided property.
   /// </summary>
   /// <param name="propertyExplorer">The property's explorer.</param>
   /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>
   public XcstDelegate<object?>?
   MemberTemplate(ModelExplorer propertyExplorer) {

      ArgumentNullException.ThrowIfNull(propertyExplorer);

      if (this.ViewContext.MemberTemplate is { } memberTemplate) {

         var helper = MakeHtmlHelperForMemberTemplate(propertyExplorer);

         return (c, o) => memberTemplate.Invoke(helper, o);
      }

      return null;
   }

   HtmlHelper
   MakeHtmlHelperForMemberTemplate(ModelExplorer memberExplorer) {

      ArgumentNullException.ThrowIfNull(memberExplorer);

      var container = new ViewDataContainer(memberExplorer);

      var viewContext = new ViewContext(this.ViewContext) {
         HtmlFieldPrefix = GenerateName(memberExplorer.Metadata.PropertyName!),
         VisitedObjects = null,
      };

      return new HtmlHelper(viewContext, container, this.MetadataProvider);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public OptionList
   NewOptionList(int staticOptionsCount) =>
      new OptionList(staticOptionsCount, this);
}

partial class HtmlHelper<TModel> {

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return new TemplateHelper(this, true, expressionString, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return new TemplateHelper(this, false, expressionString, modelExplorer);
   }
}

public class TemplateHelper {

   [GeneratedCodeReference]
   public struct RenderArgs {

      [GeneratedCodeReference]
      public string?
      htmlFieldName { get; set; }

      [GeneratedCodeReference]
      public string?
      templateName { get; set; }

      [GeneratedCodeReference]
      public IList<string>?
      membersNames { get; set; }

      [GeneratedCodeReference]
      public IDictionary<string, IEnumerable<SelectListItem>?>?
      membersOptions { get; set; }

      [GeneratedCodeReference]
      public Action<HtmlHelper, ISequenceWriter<object?>>?
      memberTemplate { get; set; }

      [GeneratedCodeReference]
      public object?
      withParams { get; set; }
   }

   readonly HtmlHelper
   _html;

   readonly bool
   _displayMode;

   readonly string
   _expression;

   readonly ModelExplorer
   _modelExplorer;

   internal
   TemplateHelper(HtmlHelper html, bool displayMode, string expression, ModelExplorer modelExplorer) {
      _html = html;
      _displayMode = displayMode;
      _expression = expression;
      _modelExplorer = modelExplorer;
   }

   [GeneratedCodeReference]
   public void
   Render(ISequenceWriter<object> output, RenderArgs args = default) {

      var htmlFieldName = args.htmlFieldName;
      var templateName = args.templateName;
      var membersNames = args.membersNames;
      var membersOptions = args.membersOptions;
      var memberTemplate = args.memberTemplate;
      var withParams = args.withParams;

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

      if (_html.ViewContext.VisitedObjects.Contains(visitedObjectsKey)) {
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
            _html.SimpleContent.Format(formatString, model)
            : _html.FormatValue(model, formatString);
      }

      var viewContext = new ViewContext(_html.ViewContext) {
         HtmlFieldPrefix = _html.GenerateName(htmlFieldName),
         FormattedModelValue = formattedModelValue,
         MembersNames = membersNames,
         VisitedObjects = { visitedObjectsKey },
      };

      if (membersOptions != null) {
         foreach (var kvp in membersOptions) {
            viewContext.MembersOptions[kvp.Key] = kvp.Value;
         }
      }

      if (memberTemplate != null) {
         viewContext.MemberTemplate = memberTemplate;
      }

      if (withParams != null) {
         foreach (var kvp in TypeHelpers.ObjectToDictionary(withParams)) {
            viewContext.ViewParameters[kvp.Key] = kvp.Value;
         }
      }

      var container = new ViewDataContainer(_modelExplorer.GetExplorerForModel(model));

      new TemplateRenderer(viewContext, container, _html.MetadataProvider, templateName, _displayMode)
         .Render(output);
   }
}
