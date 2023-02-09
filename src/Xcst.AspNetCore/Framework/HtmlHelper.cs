// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Xcst.Web.Runtime;
using RouteValueDictionary = Microsoft.AspNetCore.Routing.RouteValueDictionary;

namespace Xcst.Web.Mvc;

public class HtmlHelper {

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

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer) {

      this.ViewContext = viewContext ?? throw new ArgumentNullException(nameof(viewContext));
      this.ViewDataContainer = viewDataContainer ?? throw new ArgumentNullException(nameof(viewDataContainer));
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
   public static RouteValueDictionary
   AnonymousObjectToHtmlAttributes(object? htmlAttributes) {

      var result = new RouteValueDictionary();

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

   public IDictionary<string, object>
   GetUnobtrusiveValidationAttributes(string name) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer: null);

   // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
   // never rendered validation for a field with this name in this form. Also, if there's no form context,
   // then we can't render the attributes (we'd have no <form> to attach them to).

   public IDictionary<string, object>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer) =>
      GetUnobtrusiveValidationAttributes(name, modelExplorer, false);

   internal IDictionary<string, object>
   GetUnobtrusiveValidationAttributes(string name, ModelExplorer? modelExplorer, bool excludeMinMaxLength) {

      var results = new Dictionary<string, object>();

      // The ordering of these 3 checks (and the early exits) is for performance reasons.

      if (!this.ViewContext.ClientValidationEnabled) {
         return results;
      }

      var formContext = this.ViewContext.GetFormContextForClientValidation();

      if (formContext is null) {
         return results;
      }

      var fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

      if (formContext.RenderedField(fullName)) {
         return results;
      }

      formContext.RenderedField(fullName, true);

      modelExplorer ??= ExpressionMetadataProvider.FromStringExpression(name, this.ViewData, this.ViewData.MetadataProvider);

      var attributes = new Dictionary<string, string>();

      this.ValidationAttributeProvider
         .AddValidationAttributes(this.ViewContext.ActionContext, modelExplorer, attributes, excludeMinMaxLength);

      foreach (var pair in attributes) {
         results[pair.Key] = pair.Value;
      }

      return results;
   }

   public string
   DisplayNameForModel() =>
      MetadataInstructions.DisplayNameForModel(this);

   public string
   DisplayName(string name) =>
      MetadataInstructions.DisplayName(this, name);

   public string
   IdForModel() => Id(String.Empty);

   public string
   Id(string name) =>
      this.ViewData.TemplateInfo.GetFullHtmlFieldId(name);

   public string
   NameForModel() => InputInstructions.NameForModel(this);

   public string
   Name(string name) => InputInstructions.Name(this, name);

   public string
   ValueForModel() {

      var format = this.ViewData.ModelMetadata.EditFormatString;

      return ValueHelper(String.Empty, value: null, format: format, useViewData: true);
   }

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
      DisplayInstructions.DisplayProperties(this);

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
      EditorInstructions.EditorProperties(this);

   /// <summary>
   /// Returns the member template delegate for the provided property.
   /// </summary>
   /// <param name="propertyExplorer">The property's explorer.</param>
   /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>
   public XcstDelegate<object>?
   MemberTemplate(ModelExplorer propertyExplorer) =>
      EditorInstructions.MemberTemplate(this, propertyExplorer);
}

public class HtmlHelper<TModel> : HtmlHelper {

   public new ViewDataDictionary<TModel>
   ViewData => (ViewDataDictionary<TModel>)ViewDataContainer.ViewData;

   public
   HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer)
      : base(viewContext, viewDataContainer) {

      if (!(viewDataContainer.ViewData is ViewDataDictionary<TModel>)) {
         throw new ArgumentException(
            $"{nameof(viewDataContainer)}.ViewData should be an instance of 'ViewDataDictionary<TModel>'.",
            nameof(viewDataContainer)
         );
      }
   }

   public string
   DisplayNameFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
      MetadataInstructions.DisplayNameFor(this, expression);

   public string
   IdFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
      Id(ExpressionHelper.GetExpressionText(expression));

   public string
   NameFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
      InputInstructions.NameFor(this, expression);

   public string
   ValueFor<TProperty>(Expression<Func<TModel, TProperty>> expression) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValueHelper(expressionString, modelExplorer.Model, format: modelExplorer.Metadata.EditFormatString, useViewData: false);
   }

   public string
   ValueFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string format) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return ValueHelper(expressionString, modelExplorer.Model, format, useViewData: false);
   }
}

public interface IViewDataContainer {

   [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is the mechanism by which the ViewPage / ViewUserControl get their ViewDataDictionary objects.")]
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
