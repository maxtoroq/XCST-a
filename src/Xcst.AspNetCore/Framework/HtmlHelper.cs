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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

   static string?
   _idAttributeDotReplacement;

   DynamicViewDataDictionary?
   _viewBag;

   DefaultValidationHtmlAttributeProvider?
   _validationAttributeProvider;

   public static string
   IdAttributeDotReplacement {
      get {
         if (String.IsNullOrEmpty(_idAttributeDotReplacement)) {
            _idAttributeDotReplacement = "_";
         }
         return _idAttributeDotReplacement;
      }
      set => _idAttributeDotReplacement = value;
   }

   public dynamic
   ViewBag =>
      _viewBag ??= new DynamicViewDataDictionary(() => ViewData);

   public ViewContext
   ViewContext { get; }

   public ViewDataDictionary
   ViewData => ViewDataContainer.ViewData;

   public IViewDataContainer
   ViewDataContainer { get; internal set; }

   public ModelExplorer
   ModelExplorer => ViewData.ModelExplorer;

   public ModelMetadata
   ModelMetadata => ViewData.ModelMetadata;

   private DefaultValidationHtmlAttributeProvider
   ValidationAttributeProvider =>
      _validationAttributeProvider ??=
         ActivatorUtilities.CreateInstance<DefaultValidationHtmlAttributeProvider>(ViewContext.HttpContext.RequestServices);

   internal IXcstPackage
   CurrentPackage { get; }

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, IXcstPackage currentPackage) {

      this.ViewContext = viewContext ?? throw new ArgumentNullException(nameof(viewContext));
      this.ViewDataContainer = viewDataContainer ?? throw new ArgumentNullException(nameof(viewDataContainer));
      this.CurrentPackage = currentPackage ?? throw new ArgumentNullException(nameof(currentPackage));
   }

   /// <summary>
   /// Creates a dictionary of HTML attributes from the input object,
   /// translating underscores to dashes.
   /// </summary>
   /// <example>
   /// <c>new { data_name="value" }</c> will translate to the entry <c>{ "data-name" , "value" }</c>
   /// in the resulting dictionary.
   /// </example>
   /// <param name="htmlAttributes">Anonymous object describing HTML attributes.</param>
   /// <returns>A dictionary that represents HTML attributes.</returns>
   public static IDictionary<string, object?>
   AnonymousObjectToHtmlAttributes(object? htmlAttributes) {

      if (htmlAttributes is IDictionary<string, object?> dictionary) {
         return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
      }

      var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

      if (htmlAttributes != null) {
         foreach (var property in HtmlAttributePropertyHelper.GetProperties(htmlAttributes)) {
            result.Add(property.Name, property.GetValue(htmlAttributes));
         }
      }

      return result;
   }

   public static string
   GenerateIdFromName(string name) =>
      GenerateIdFromName(name, IdAttributeDotReplacement);

   public static string
   GenerateIdFromName(string name, string idAttributeDotReplacement) {

      if (name is null) throw new ArgumentNullException(nameof(name));
      if (idAttributeDotReplacement is null) throw new ArgumentNullException(nameof(idAttributeDotReplacement));

      // TagBuilder.CreateSanitizedId returns null for empty strings, return String.Empty instead to avoid breaking change

      if (name.Length == 0) {
         return String.Empty;
      }

#pragma warning disable CS8603 // there's a small chance CreateSanitizedId returns null
      return TagBuilder.CreateSanitizedId(name, idAttributeDotReplacement);
#pragma warning restore CS8603
   }

   public static string
   GetInputTypeString(InputType inputType) =>
      inputType switch {
         InputType.CheckBox => "checkbox",
         InputType.Hidden => "hidden",
         InputType.Password => "password",
         InputType.Radio => "radio",
         InputType.Text => "text",
         _ => "text",
      };

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

   internal string
   EvalString(string key) =>
      Convert.ToString(this.ViewData.Eval(key), CultureInfo.CurrentCulture);

   internal string
   EvalString(string key, string? format) =>
      Convert.ToString(this.ViewData.Eval(key, format), CultureInfo.CurrentCulture);

   [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
   public string
   FormatValue(object? value, string? format) =>
      ViewDataDictionary.FormatValueInternal(value, format);

   internal bool
   EvalBoolean(string key) =>
      Convert.ToBoolean(this.ViewData.Eval(key), CultureInfo.InvariantCulture);

   internal object?
   GetModelStateValue(string key, Type destinationType) {

      if (this.ViewData.ModelState.TryGetValue(key, out var modelState)
         && modelState.RawValue != null) {

         return ConvertTo(modelState.RawValue, destinationType, culture: null);
      }

      return null;

      static object? ConvertTo(object? value, Type type, CultureInfo? culture) {

         if (value == null) {

            if (!type.IsValueType) {
               return null;
            }

            return Activator.CreateInstance(type);
         }

         if (type.IsAssignableFrom(value.GetType())) {
            return value;
         }

         culture ??= CultureInfo.InvariantCulture;

         return ModelBinding.ValueProviderResult.UnwrapPossibleArrayType(culture, value, type);
      }
   }

   public IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer: null);

   // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
   // never rendered validation for a field with this name in this form. Also, if there's no form context,
   // then we can't render the attributes (we'd have no <form> to attach them to).

   public IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer, false);

   internal IDictionary<string, string>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength) {

      var results = new Dictionary<string, string>();

      WriteUnobtrusiveValidationAttributes(
         name, modelExplorer, excludeMinMaxLength, (key, value) => results[key] = value);

      return results;
   }

   internal void
   WriteUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength, XcstWriter output) {

      WriteUnobtrusiveValidationAttributes(
         name, modelExplorer, excludeMinMaxLength, (key, value) => output.WriteAttributeString(key, value));
   }

   internal void
   WriteUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength, Action<string, string> writeFn) {

      // The ordering of these 3 checks (and the early exits) is for performance reasons.

      if (!this.ViewContext.ClientValidationEnabled) {
         return;
      }

      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (formContext is null) {
         return;
      }

      var fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (formContext.RenderedField(fullName)) {
         return;
      }

      formContext.RenderedField(fullName, true);

      modelExplorer ??= ExpressionMetadataProvider.FromStringExpression(name, this.ViewData, this.ViewData.MetadataProvider);

      var attributes = new Dictionary<string, string>();

      this.ValidationAttributeProvider
         .AddValidationAttributes(this.ViewContext.ActionContext, modelExplorer, attributes, excludeMinMaxLength);

      foreach (var pair in attributes) {
         writeFn.Invoke(pair.Key, pair.Value);
      }
   }

   public string
   DisplayName(string name) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return DisplayNameHelper(modelExplorer, name);
   }

   public string
   DisplayNameForModel() =>
      DisplayNameHelper(this.ViewData.ModelExplorer, String.Empty);

   internal string
   DisplayNameHelper(ModelExplorer modelExplorer, string htmlFieldName) {

      var metadata = modelExplorer.Metadata;

      // We don't call ModelMetadata.GetDisplayName here because we want to fall back to the field name rather than the ModelType.
      // This is similar to how the LabelHelpers get the text of a label.

      var resolvedDisplayName = metadata.DisplayName
         ?? metadata.PropertyName
         ?? htmlFieldName.Split('.').Last();

      return resolvedDisplayName;
   }

   public void
   DisplayText(ISequenceWriter<string> output, string name) =>
      DisplayTextHelper(output, ExpressionMetadataProvider.FromStringExpression(name, this.ViewData));

   public string
   DisplayString(string name) =>
      DisplayStringHelper(ExpressionMetadataProvider.FromStringExpression(name, this.ViewData));

   internal string
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
      this.ViewData.TemplateInfo.GetFullHtmlFieldId(name);

   public string
   IdForModel() => Id(String.Empty);

   public string
   Name(string name) =>
      this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

   public string
   NameForModel() => Name(String.Empty);

   public string
   Value(string name) {

      if (name is null) throw new ArgumentNullException(nameof(name));

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(name, this.ViewData);

      return ValueHelper(name, value: null, format: modelExplorer.Metadata.EditFormatString, useViewData: true);
   }

   public string
   Value(string name, string format) {

      if (name is null) throw new ArgumentNullException(nameof(name));

      return ValueHelper(name, value: null, format: format, useViewData: true);
   }

   public string
   ValueForModel() {

      var format = this.ViewData.ModelMetadata.EditFormatString;

      return ValueHelper(String.Empty, value: null, format: format, useViewData: true);
   }

   internal string
   ValueHelper(string name, object? value, string? format, bool useViewData) {

      var fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
      var attemptedValue = (string?)GetModelStateValue(fullName, typeof(string));
      string resolvedValue;

      if (attemptedValue != null) {

         // case 1: if ModelState has a value then it's already formatted so ignore format string

         resolvedValue = attemptedValue;

      } else if (useViewData) {

         if (name.Length == 0) {

            // case 2(a): format the value from ModelMetadata for the current model

            var modelExplorer = ExpressionMetadataProvider.FromStringExpression(String.Empty, this.ViewData);
            resolvedValue = FormatValue(modelExplorer.Model, format);

         } else {

            // case 2(b): format the value from ViewData

            resolvedValue = EvalString(name, format);
         }
      } else {

         // case 3: format the explicit value from ModelMetadata

         resolvedValue = FormatValue(value, format);
      }

      return resolvedValue;
   }

   internal void
   WriteId(string name, XcstWriter output) {

      var sanitizedId = TagBuilder.CreateSanitizedId(name);

      if (!String.IsNullOrEmpty(sanitizedId)) {
         output.WriteAttributeString("id", sanitizedId);
      }
   }

   /// <exclude/>
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
      // See also HtmlAttributeDictionary.SetAttributes

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

   /// <exclude/>
   public void
   WriteAttribute(string key, object? value, XcstWriter output) =>
      output.WriteAttributeString(key, output.SimpleContent.Convert(value));
}

public partial class HtmlHelper<TModel> : HtmlHelper {

   public new ViewDataDictionary<TModel>
   ViewData => (ViewDataDictionary<TModel>)ViewDataContainer.ViewData;

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer, IXcstPackage currentPackage)
      : base(viewContext, viewDataContainer, currentPackage) {

      if (!(viewDataContainer.ViewData is ViewDataDictionary<TModel>)) {
         throw new ArgumentException(
            $"{nameof(viewDataContainer)}.ViewData should be an instance of 'ViewDataDictionary<TModel>'.",
            nameof(viewDataContainer)
         );
      }
   }

   public string
   DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = (typeof(IEnumerable<TModel>).IsAssignableFrom(typeof(TModel))) ?
          ExpressionMetadataProvider.FromLambdaExpression(expression, new ViewDataDictionary<TModel>(this.ViewData.MetadataProvider, this.ViewData.ModelState))
          : ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);

      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return DisplayNameHelper(modelExplorer, expressionString);
   }

   public void
   DisplayTextFor<TResult>(ISequenceWriter<string> output, Expression<Func<TModel, TResult>> expression) =>
      DisplayTextHelper(output, ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData));

   public string
   DisplayStringFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      DisplayStringHelper(ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData));

   public string
   IdFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      Id(ExpressionHelper.GetExpressionText(expression));

   public string
   NameFor<TResult>(Expression<Func<TModel, TResult>> expression) =>
      Name(ExpressionHelper.GetExpressionText(expression));

   public string
   ValueFor<TResult>(Expression<Func<TModel, TResult>> expression) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValueHelper(expressionString, modelExplorer.Model, format: modelExplorer.Metadata.EditFormatString, useViewData: false);
   }

   public string
   ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string format) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValueHelper(expressionString, modelExplorer.Model, format, useViewData: false);
   }
}

public interface IViewDataContainer {

   ViewDataDictionary
   ViewData { get; set; }
}

public enum InputType {
   CheckBox,
   Hidden,
   Password,
   Radio,
   Text
}

static class TagBuilder {

   public static string?
   CreateSanitizedId(string originalId) =>
      CreateSanitizedId(originalId, HtmlHelper.IdAttributeDotReplacement);

   public static string?
   CreateSanitizedId(string originalId, string invalidCharReplacement) {

      if (String.IsNullOrEmpty(originalId)) {
         return null;
      }

      if (invalidCharReplacement is null) {
         throw new ArgumentNullException(nameof(invalidCharReplacement));
      }

      var firstChar = originalId[0];

      if (!Html401IdUtil.IsLetter(firstChar)) {
         // the first character must be a letter
         return null;
      }

      var sb = new StringBuilder(originalId.Length);
      sb.Append(firstChar);

      for (int i = 1; i < originalId.Length; i++) {
         var thisChar = originalId[i];
         if (Html401IdUtil.IsValidIdCharacter(thisChar)) {
            sb.Append(thisChar);
         } else {
            sb.Append(invalidCharReplacement);
         }
      }

      return sb.ToString();
   }

   // Valid IDs are defined in http://www.w3.org/TR/html401/types.html#type-id

   static class Html401IdUtil {

      public static bool
      IsValidIdCharacter(char c) =>
         (IsLetter(c) || IsDigit(c) || IsAllowableSpecialCharacter(c));

      static bool
      IsAllowableSpecialCharacter(char c) {
         switch (c) {
            case '-':
            case '_':
            case ':':
               // note that we're specifically excluding the '.' character
               return true;

            default:
               return false;
         }
      }

      static bool
      IsDigit(char c) =>
         ('0' <= c && c <= '9');

      public static bool
      IsLetter(char c) =>
         (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z'));
   }
}

class HtmlAttributePropertyHelper : PropertyHelper {

   static ConcurrentDictionary<Type, PropertyHelper[]>
   _reflectionCache = new();

   [AllowNull]
   public override string
   Name {
      get => base.Name;
      protected set => base.Name = value?.Replace('_', '-');
   }

   public static new PropertyHelper[]
   GetProperties(object instance) =>
      GetProperties(instance, CreateInstance, _reflectionCache);

   static PropertyHelper
   CreateInstance(PropertyInfo property) =>
      new HtmlAttributePropertyHelper(property);

   public
   HtmlAttributePropertyHelper(PropertyInfo property)
      : base(property) { }
}
