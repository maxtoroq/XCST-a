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

   // "ForModel" helpers should call GetModelExplorerFromString(String.Empty)
   // and not use this.ModelExplorer

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayTemplate() =>
      DisplayTemplate(String.Empty);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   DisplayTemplate(string expression) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return new TemplateHelper(this, true, expression, modelExplorer);
   }

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
   DisplayProperties() =>
      DisplayProperties(this.ViewContext.MembersNames);

   internal IEnumerable<ModelExplorer>
   DisplayProperties(IList<string>? names) {

      var filteredProperties = this.ModelExplorer.Properties
         .Where(p => ShowForDisplay(p, names));

      var orderedProperties = (names is { Count: > 0 }) ?
         filteredProperties.OrderBy(p => names.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForDisplay(ModelExplorer propertyExplorer, IList<string>? names) {

      ArgumentNullException.ThrowIfNull(propertyExplorer);

      var propertyMetadata = propertyExplorer.Metadata;

      if (this.ViewContext.Visited(propertyExplorer)) {
         return false;
      }

      if (names is { Count: > 0 }) {
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
   EditorTemplate() =>
      EditorTemplate(String.Empty);

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorTemplate(string expression) {

      var modelExplorer = GetModelExplorerFromString(expression);

      return new TemplateHelper(this, false, expression, modelExplorer);
   }

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
   EditorProperties() =>
      EditorProperties(this.ViewContext.MembersNames);

   internal IEnumerable<ModelExplorer>
   EditorProperties(IList<string>? names) {

      var filteredProperties = this.ModelExplorer.Properties
         .Where(p => ShowForEdit(p, names));

      var orderedProperties = (names is { Count: > 0 }) ?
         filteredProperties.OrderBy(p => names.IndexOf(p.Metadata.PropertyName!))
         : filteredProperties;

      return orderedProperties;
   }

   bool
   ShowForEdit(ModelExplorer propertyExplorer, IList<string>? names) {

      ArgumentNullException.ThrowIfNull(propertyExplorer);

      var propertyMetadata = propertyExplorer.Metadata;

      if (this.ViewContext.Visited(propertyExplorer)) {
         return false;
      }

      if (names is { Count: > 0 }) {
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

      if (propertyExplorer.Metadata.ContainerType is null) {
         throw new ArgumentException(
            "propertyExplorer must represent a property.", nameof(propertyExplorer));
      }

      var memberTemplate = this.ViewContext.MemberTemplate;

      if (memberTemplate is null) {
         return null;
      }

      var memberContext = new ViewContext(this.ViewContext) {
         HtmlFieldPrefix = GenerateName(propertyExplorer.Metadata.PropertyName!),
      };

      TemplateHelper.SetUpMemberContext(memberContext);

      var memberHtml = new HtmlHelper(memberContext, () => propertyExplorer, this.MetadataProvider);

      return (c, o) => memberTemplate.Invoke(memberHtml, o);
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
   DisplayTemplateFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return new TemplateHelper(this, true, expressionString, modelExplorer);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public TemplateHelper
   EditorTemplateFor<TResult>(Expression<Func<TModel, TResult>> expression) {

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

      if (args.memberTemplate != null) {
         RenderMemberTemplate(output, args);
         return;
      }

      var explorer = _modelExplorer;
      var metadata = explorer.Metadata;
      var model = explorer.Model;

      if (metadata.ConvertEmptyStringToNull
         && String.Empty.Equals(model)) {

         model = null;
         explorer = explorer.GetExplorerForModel(model);
      }

      // Normally this shouldn't happen, unless someone writes their own custom Object templates which
      // don't check to make sure that the object hasn't already been displayed

      if (_html.ViewContext.Visited(explorer)) {
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

      var templateContext = MakeTemplateContext(args);
      templateContext.FormattedModelValue = formattedModelValue;
      templateContext.MembersNames = args.membersNames;

      templateContext.AddVisited(explorer);

      new TemplateRenderer(templateContext, explorer, _html.MetadataProvider, args.templateName, _displayMode)
         .Render(output);
   }

   void
   RenderMemberTemplate(ISequenceWriter<object> output, in RenderArgs args) {

      var memberTemplate = args.memberTemplate!;

      var baseContext = MakeTemplateContext(args);
      SetUpMemberContext(baseContext);

      foreach (var propertyExplorer in Members(args.membersNames)) {

         var propertyMeta = propertyExplorer.Metadata;
         var propertyName = propertyMeta.PropertyName!;

         var memberContext = new ViewContext(baseContext);

         var memberHtml = new HtmlHelper(
            memberContext, () => propertyExplorer, _html.MetadataProvider);
         memberHtml.ViewContext.HtmlFieldPrefix = memberHtml.GenerateName(propertyName);

         if (propertyMeta.HideSurroundingHtml) {

            new TemplateHelper(memberHtml, _displayMode, String.Empty, propertyExplorer)
               .Render(output);

            continue;
         }

         memberTemplate.Invoke(memberHtml, output!);
      }
   }

   internal static void
   SetUpMemberContext(ViewContext templateContext) {
      templateContext.VisitedObjects = null;
   }

   IEnumerable<ModelExplorer>
   Members(IList<string>? membersNames) =>
      (_displayMode) ?
         _html.DisplayProperties(membersNames)
         : _html.EditorProperties(membersNames);

   ViewContext
   MakeTemplateContext(in RenderArgs args) {

      var templateContext = new ViewContext(_html.ViewContext) {
         HtmlFieldPrefix = _html.GenerateName(args.htmlFieldName ?? _expression),
      };

      if (args.membersOptions is { } membersOptions) {
         foreach (var kvp in membersOptions) {
            templateContext.MembersOptions[kvp.Key] = kvp.Value;
         }
      }

      if (args.memberTemplate is { } memberTemplate) {
         templateContext.MemberTemplate = memberTemplate;
      }

      if (args.withParams is { } withParams) {
         foreach (var kvp in TypeHelpers.ObjectToDictionary(withParams)) {
            templateContext.ViewParameters[kvp.Key] = kvp.Value;
         }
      }

      return templateContext;
   }
}
