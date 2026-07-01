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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

public partial class HtmlHelper {

   public static readonly string
   ValidationInputCssClassName = "input-validation-error";

   public static readonly string
   ValidationInputValidCssClassName = "input-validation-valid";

   public static readonly string
   ValidationMessageCssClassName = "field-validation-error";

   public static readonly string
   ValidationMessageValidCssClassName = "field-validation-valid";

   public static readonly string
   ValidationSummaryCssClassName = "validation-summary-errors";

   public static readonly string
   ValidationSummaryValidCssClassName = "validation-summary-valid";

   DefaultValidationHtmlAttributeProvider?
   _validationAttributeProvider;

   public ViewContext
   ViewContext { get; }

   public IViewDataContainer
   ViewDataContainer { get; }

   public IModelMetadataProvider
   MetadataProvider { get; }

   public object?
   Model => ViewDataContainer.ModelExplorer.Model;

   public ModelExplorer
   ModelExplorer => ViewDataContainer.ModelExplorer;

   public ModelMetadata
   ModelMetadata => ViewDataContainer.ModelExplorer.Metadata;

   public ModelStateDictionary
   ModelState => ViewContext.ActionContext.ModelState;

   private DefaultValidationHtmlAttributeProvider
   ValidationAttributeProvider =>
      _validationAttributeProvider ??=
         ActivatorUtilities.CreateInstance<DefaultValidationHtmlAttributeProvider>(ViewContext.HttpContext.RequestServices);

   internal IXcstPackage
   CurrentPackage => ViewContext.CurrentPackage
      ?? throw new InvalidOperationException("CurrentPackage is not initialized.");

   internal SimpleContent
   SimpleContent => CurrentPackage.Context.SimpleContent;

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, IModelMetadataProvider metadataProvider) {

      ArgumentNullException.ThrowIfNull(viewContext);
      ArgumentNullException.ThrowIfNull(viewDataContainer);
      ArgumentNullException.ThrowIfNull(metadataProvider);

      this.ViewContext = viewContext;
      this.ViewDataContainer = viewDataContainer;
      this.MetadataProvider = metadataProvider;
   }

   public string
   GenerateIdFromName(string name) {

      ArgumentNullException.ThrowIfNull(name);

      if (name.Length == 0) {
         return String.Empty;
      }

      var invalidCharReplacement = this.ViewContext.Options.IdAttributeDotReplacement;

      var firstChar = name[0];

      if (!Html401IdUtil.IsLetter(firstChar)) {
         // the first character must be a letter
         return String.Empty;
      }

      var sb = new StringBuilder(name.Length);
      sb.Append(firstChar);

      for (int i = 1; i < name.Length; i++) {
         var thisChar = name[i];
         if (Html401IdUtil.IsValidIdCharacter(thisChar)) {
            sb.Append(thisChar);
         } else {
            sb.Append(invalidCharReplacement);
         }
      }

      return sb.ToString();
   }

   /// <summary>
   /// Creates a dictionary from an object, by adding each public instance property as a key with its associated
   /// value to the dictionary. It will expose public properties from derived types as well. This is typically used
   /// with objects of an anonymous type.
   /// </summary>
   /// <example>
   /// <c>new { property_name = "value" }</c> will translate to the entry <c>{ "property_name" , "value" }</c>
   /// in the resulting dictionary.
   /// </example>
   /// <param name="value">The object to be converted.</param>
   /// <returns>The created dictionary of property names and property values.</returns>
   public static IDictionary<string, object?>
   ObjectToDictionary(object value) =>
      TypeHelpers.ObjectToDictionary(value);

   public string
   GetFullHtmlFieldId(string? partialFieldName) =>
      GenerateIdFromName(GetFullHtmlFieldName(partialFieldName));

   public string
   GetFullHtmlFieldName(string? partialFieldName) {

      var htmlFieldPrefix = this.ViewContext.HtmlFieldPrefix;

      if (String.IsNullOrEmpty(partialFieldName)) {
         return htmlFieldPrefix;
      }

      if (String.IsNullOrEmpty(htmlFieldPrefix)) {
         return partialFieldName;
      }

      if (partialFieldName.StartsWith('[')) {

         // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
         // with a 'dot' would be invalid.

         return htmlFieldPrefix + partialFieldName;
      }

      return String.Concat(htmlFieldPrefix, ".", partialFieldName);
   }

   public object?
   Eval(string? expression) {
      var info = ViewDataEvaluator.Eval(this.ViewDataContainer, expression);
      return info?.Value;
   }

   public string
   FormatValue(object? value, string? format) =>
      FormatValue(value, format, null);

   public string
   FormatValue(object? value, string? format, CultureInfo? culture) {

      if (value is null) {
         return String.Empty;
      }

      culture ??= this.ViewContext.FormCulture;

      if (String.IsNullOrEmpty(format)) {
         return Convert.ToString(value, culture) ?? String.Empty;
      }

      return String.Format(culture, format, value);
   }

   object?
   GetModelStateValue(ModelStateEntry? modelState, Type destinationType) {

      if (modelState is { RawValue: not null }) {
         return ConvertTo(modelState.RawValue, destinationType, culture: null);
      }

      return null;

      static object? ConvertTo(object value, Type type, CultureInfo? culture) {

         if (type.IsAssignableFrom(value.GetType())) {
            return value;
         }

         culture ??= CultureInfo.InvariantCulture;

         return ModelBinding.ValueProviderResult.UnwrapPossibleArrayType(culture, value, type);
      }
   }

   ModelExplorer
   GetModelExplorerFromString(string expression) =>
      ExpressionMetadataProvider.FromStringExpression(expression, this.ViewDataContainer, this.MetadataProvider);

   public IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer: null);

   // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
   // never rendered validation for a field with this name in this form. Also, if there's no form context,
   // then we can't render the attributes (we'd have no <form> to attach them to).

   public IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer, false);

   IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength) {

      return GetUnobtrusiveValidationAttributesImpl(name, modelExplorer, excludeMinMaxLength)
         ?? new Dictionary<string, string>();
   }

   void
   WriteUnobtrusiveValidationAttributes(
         string name, ModelExplorer modelExplorer, bool excludeMinMaxLength, XcstWriter output) {

      var attributes = GetUnobtrusiveValidationAttributesImpl(name, modelExplorer, excludeMinMaxLength);

      if (attributes != null) {

         foreach (var kvp in attributes) {
            output.WriteAttributeString(kvp.Key, kvp.Value);
         }
      }
   }

   Dictionary<string, string>?
   GetUnobtrusiveValidationAttributesImpl(
         string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength) {

      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (formContext is null) {
         return default;
      }

      var fullName = GetFullHtmlFieldName(name);

      if (formContext.RenderedField(fullName)) {
         return default;
      }

      formContext.RenderedField(fullName, true);

      modelExplorer ??= GetModelExplorerFromString(name);

      var attributes = new Dictionary<string, string>();

      this.ValidationAttributeProvider
         .AddValidationAttributes(this.ViewContext.ActionContext, modelExplorer, attributes, excludeMinMaxLength);

      return attributes;
   }

   [GeneratedCodeReference]
   public string
   DisplayName(string name) {

      var modelExplorer = GetModelExplorerFromString(name);

      return DisplayNameHelper(modelExplorer, name);
   }

   [GeneratedCodeReference]
   public string
   DisplayNameForModel() =>
      DisplayNameHelper(this.ModelExplorer, String.Empty);

   private protected string
   DisplayNameHelper(ModelExplorer modelExplorer, string htmlFieldName) {

      var metadata = modelExplorer.Metadata;

      // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
      // This is similar to how the LabelHelpers get the text of a label.

      var resolvedDisplayName = metadata.DisplayName
         ?? metadata.PropertyName
         ?? htmlFieldName.Split('.').Last();

      return resolvedDisplayName;
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public void
   DisplayText(ISequenceWriter<string> output, string name) =>
      DisplayTextHelper(output, GetModelExplorerFromString(name));

   [GeneratedCodeReference]
   public string
   DisplayString(string name) =>
      DisplayStringHelper(GetModelExplorerFromString(name));

   private protected string
   DisplayStringHelper(ModelExplorer modelExplorer) =>
      modelExplorer.GetSimpleDisplayText();

   internal void
   DisplayTextHelper(ISequenceWriter<string> output, ModelExplorer modelExplorer) {

      var text = modelExplorer.GetSimpleDisplayText();

      if (modelExplorer.Metadata.HtmlEncode) {
         output.WriteString(text);
      } else {
         output.WriteRaw(text);
      }
   }

   public string
   Id(string name) =>
      GetFullHtmlFieldId(name);

   public string
   IdForModel() => Id(String.Empty);

   public string
   Name(string name) =>
      GetFullHtmlFieldName(name);

   public string
   NameForModel() => Name(String.Empty);

   public string
   Value(string name) =>
      Value(name, null);

   public string
   Value(string name, string? format) {

      ArgumentNullException.ThrowIfNull(name);

      var modelExplorer = GetModelExplorerFromString(name);

      return ValueHelper(name, modelExplorer, format);
   }

   public string
   ValueForModel() =>
      ValueHelper(String.Empty, this.ModelExplorer, format: null);

   private protected string
   ValueHelper(string name, ModelExplorer modelExplorer, string? format) {

      format ??= modelExplorer.Metadata.EditFormatString;

      var fullName = GetFullHtmlFieldName(name);
      var modelState = this.ModelState[fullName];

      var resolvedValue = (string?)GetModelStateValue(modelState, typeof(string))
         ?? FormatValue(modelExplorer.Model, format);

      return resolvedValue;
   }

   // extension helpers

   string
   FullNameNonEmpty(string name) {

      var fullName = GetFullHtmlFieldName(name);

      if (String.IsNullOrEmpty(fullName)) {
         throw new ArgumentException("The name of an HTML field cannot be null or empty.", nameof(name));
      }

      return fullName;
   }

   void
   WriteId(string name, XcstWriter output) {

      var sanitizedId = GenerateIdFromName(name);

      if (!String.IsNullOrEmpty(sanitizedId)) {
         output.WriteAttributeString("id", sanitizedId);
      }
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public void
   WriteBoolean(string key, bool value, XcstWriter output) {

      if (value) {
         output.WriteAttributeString(key, key);
      }
   }

   internal void
   WriteCssClass(string? userClass, string? libClass, XcstWriter output) {

      // NOTE: For backcompat, userClass must be a non-null string to be joined
      // with libClass, otherwise it's ignored. If there's no libClass,
      // userClass can be null, resulting in an empty attribute.
      // 
      // libClass must not be null or empty, which allows you to call this method
      // without having to make that check.
      // 
      // See also HtmlAttributeDictionary.AddClass()

      var libClassHasValue = !String.IsNullOrEmpty(libClass);
      var userClassHasValue = userClass != null;

      if (libClassHasValue
         || userClassHasValue) {

         var joinedClass =
            (libClassHasValue && userClassHasValue) ? userClass + " " + libClass
            : (libClassHasValue) ? libClass
            : userClass;

         output.WriteAttributeString("class", joinedClass);
      }
   }
}

public partial class HtmlHelper<TModel> : HtmlHelper {

   [MaybeNull]
   public new TModel
   Model => (TModel)ViewDataContainer.ModelExplorer.Model;

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, IModelMetadataProvider metadataProvider)
      : base(viewContext, viewDataContainer, metadataProvider) { }

   ModelExplorer
   GetModelExplorerFromLambda<TResult>(Expression<Func<TModel, TResult>> expression) =>
      ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewDataContainer, this.MetadataProvider);

   [GeneratedCodeReference]
   public string
   DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = (typeof(IEnumerable<TModel>).IsAssignableFrom(typeof(TModel))) ?
          ExpressionMetadataProvider.FromLambdaExpression(
             expression,
             new ViewDataContainer(this.MetadataProvider.GetModelExplorerForType(typeof(TModel), default)),
             this.MetadataProvider)
          : GetModelExplorerFromLambda(expression);

      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return DisplayNameHelper(modelExplorer, expressionString);
   }

   [GeneratedCodeReference]
   [EditorBrowsable(EditorBrowsableState.Never)]
   public void
   DisplayTextFor<TResult>(ISequenceWriter<string> output, Expression<Func<TModel, TResult>> expression) =>
      DisplayTextHelper(output, GetModelExplorerFromLambda(expression));

   [GeneratedCodeReference]
   public string
   DisplayStringFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      DisplayStringHelper(GetModelExplorerFromLambda(expression));

   public string
   IdFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      Id(ExpressionHelper.GetExpressionText(expression));

   public string
   NameFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      Name(ExpressionHelper.GetExpressionText(expression));

   public string
   ValueFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      ValueFor(expression, null);

   public string
   ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string? format) {

      var modelExplorer = GetModelExplorerFromLambda(expression);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValueHelper(expressionString, modelExplorer, format);
   }
}

public interface IViewDataContainer {

   ModelExplorer
   ModelExplorer { get; }
}

partial class HtmlHelper {

   // Valid IDs are defined in http://www.w3.org/TR/html401/types.html#type-id

   static class Html401IdUtil {

      public static bool
      IsValidIdCharacter(char c) =>
         (IsLetter(c) || IsDigit(c) || IsAllowableSpecialCharacter(c));

      static bool
      IsAllowableSpecialCharacter(char c) =>
         c is '-' or '_' or ':';

      static bool
      IsDigit(char c) =>
         ('0' <= c && c <= '9');

      public static bool
      IsLetter(char c) =>
         (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z'));
   }
}
