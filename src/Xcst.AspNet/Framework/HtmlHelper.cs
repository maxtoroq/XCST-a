// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web.Routing;
using System.Web.Mvc.Properties;
using Xcst;
using Xcst.Web.Runtime;

namespace System.Web.Mvc {

   public class HtmlHelper {

      public static readonly string ValidationInputCssClassName = "input-validation-error";
      public static readonly string ValidationInputValidCssClassName = "input-validation-valid";
      public static readonly string ValidationMessageCssClassName = "field-validation-error";
      public static readonly string ValidationMessageValidCssClassName = "field-validation-valid";
      public static readonly string ValidationSummaryCssClassName = "validation-summary-errors";
      public static readonly string ValidationSummaryValidCssClassName = "validation-summary-valid";

      static string? _idAttributeDotReplacement;

      DynamicViewDataDictionary? _ViewBag;

      public static string IdAttributeDotReplacement {
         get {
            if (String.IsNullOrEmpty(_idAttributeDotReplacement)) {
               _idAttributeDotReplacement = "_";
            }
            return _idAttributeDotReplacement!;
         }
         set => _idAttributeDotReplacement = value;
      }

      public dynamic ViewBag =>
         _ViewBag ??= new DynamicViewDataDictionary(() => ViewData);

      public ViewContext ViewContext { get; private set; }

      public ViewDataDictionary ViewData => ViewDataContainer.ViewData;

      public IViewDataContainer ViewDataContainer { get; internal set; }

      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "The usage of the property is as an instance property of the helper.")]
      public Html5DateRenderingMode Html5DateRenderingMode { get; set; }

      public ModelMetadata ModelMetadata => ViewData.ModelMetadata;

      internal Func<string, ModelMetadata?, IEnumerable<ModelClientValidationRule>> ClientValidationRuleFactory { get; set; }

      public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer) {

         if (viewContext is null) throw new ArgumentNullException(nameof(viewContext));
         if (viewDataContainer is null) throw new ArgumentNullException(nameof(viewDataContainer));

         this.ViewContext = viewContext;
         this.ViewDataContainer = viewDataContainer;
         this.ClientValidationRuleFactory = (name, metadata) =>
            ModelValidatorProviders.Providers
               .GetValidators(metadata ?? ModelMetadata.FromStringExpression(name, this.ViewData), this.ViewContext)
               .SelectMany(v => v.GetClientValidationRules());
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
      public static RouteValueDictionary AnonymousObjectToHtmlAttributes(object? htmlAttributes) {

         var result = new RouteValueDictionary();

         if (htmlAttributes != null) {
            foreach (PropertyHelper property in HtmlAttributePropertyHelper.GetProperties(htmlAttributes)) {
               result.Add(property.Name, property.GetValue(htmlAttributes));
            }
         }

         return result;
      }

      public static string GenerateIdFromName(string name) =>
         GenerateIdFromName(name, IdAttributeDotReplacement);

      public static string GenerateIdFromName(string name, string idAttributeDotReplacement) {

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

      public static string GetInputTypeString(InputType inputType) =>
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
      public static IDictionary<string, object?> ObjectToDictionary(object value) =>
         TypeHelpers.ObjectToDictionary(value);

      internal string EvalString(string key) =>
         Convert.ToString(this.ViewData.Eval(key), CultureInfo.CurrentCulture);

      internal string EvalString(string key, string? format) =>
         Convert.ToString(this.ViewData.Eval(key, format), CultureInfo.CurrentCulture);

      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
      public string FormatValue(object? value, string? format) =>
         ViewDataDictionary.FormatValueInternal(value, format);

      internal bool EvalBoolean(string key) =>
         Convert.ToBoolean(this.ViewData.Eval(key), CultureInfo.InvariantCulture);

      internal object? GetModelStateValue(string key, Type destinationType) {

         if (this.ViewData.ModelState.TryGetValue(key, out ModelState modelState)
            && modelState.Value != null) {

            return modelState.Value.ConvertTo(destinationType, culture: null);
         }

         return null;
      }

      public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name) =>
         GetUnobtrusiveValidationAttributes(name, metadata: null);

      // Only render attributes if unobtrusive client-side validation is enabled, and then only if we've
      // never rendered validation for a field with this name in this form. Also, if there's no form context,
      // then we can't render the attributes (we'd have no <form> to attach them to).

      public IDictionary<string, object> GetUnobtrusiveValidationAttributes(string name, ModelMetadata? metadata) {

         var results = new Dictionary<string, object>();

         // The ordering of these 3 checks (and the early exits) is for performance reasons.

         if (!this.ViewContext.UnobtrusiveJavaScriptEnabled) {
            return results;
         }

         FormContext? formContext = this.ViewContext.GetFormContextForClientValidation();

         if (formContext is null) {
            return results;
         }

         string fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);

         if (formContext.RenderedField(fullName)) {
            return results;
         }

         formContext.RenderedField(fullName, true);

         IEnumerable<ModelClientValidationRule> clientRules = ClientValidationRuleFactory(name, metadata);

         UnobtrusiveValidationAttributesGenerator.GetValidationAttributes(clientRules, results);

         return results;
      }

      public string DisplayNameForModel() =>
         MetadataInstructions.DisplayNameForModel(this);

      public string DisplayName(string name) =>
         MetadataInstructions.DisplayName(this, name);

      public string IdForModel() =>
         Id(String.Empty);

      public string Id(string name) =>
         this.ViewData.TemplateInfo.GetFullHtmlFieldId(name);

      public string NameForModel() =>
         InputInstructions.NameForModel(this);

      public string Name(string name) =>
         InputInstructions.Name(this, name);

      public string ValueForModel() {

         string? format = this.ViewData.ModelMetadata.EditFormatString;

         return ValueHelper(String.Empty, value: null, format: format, useViewData: true);
      }

      public string Value(string name) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         ModelMetadata metadata = ModelMetadata.FromStringExpression(name, this.ViewData);

         return ValueHelper(name, value: null, format: metadata.EditFormatString, useViewData: true);
      }

      public string Value(string name, string format) {

         if (name is null) throw new ArgumentNullException(nameof(name));

         return ValueHelper(name, value: null, format: format, useViewData: true);
      }

      internal string ValueHelper(string name, object? value, string? format, bool useViewData) {

         string fullName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
         string? attemptedValue = (string?)GetModelStateValue(fullName, typeof(string));
         string resolvedValue;

         if (attemptedValue != null) {

            // case 1: if ModelState has a value then it's already formatted so ignore format string

            resolvedValue = attemptedValue;

         } else if (useViewData) {

            if (name.Length == 0) {

               // case 2(a): format the value from ModelMetadata for the current model

               ModelMetadata metadata = ModelMetadata.FromStringExpression(String.Empty, this.ViewData);
               resolvedValue = FormatValue(metadata.Model, format);

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
      /// Determines whether a property should be shown in a display template, based on its metadata.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> display template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>
      public bool ShowForDisplay(ModelMetadata propertyMetadata) =>
         DisplayInstructions.ShowForDisplay(this, propertyMetadata);

      /// <summary>
      /// Determines whether a property should be shown in an editor template, based on its metadata.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>true if the property should be shown; otherwise false.</returns>
      /// <remarks>
      /// This method uses the same logic used by the built-in <code>Object</code> editor template;
      /// e.g. by default, it returns false for complex types.
      /// </remarks>
      public bool ShowForEdit(ModelMetadata propertyMetadata) =>
         EditorInstructions.ShowForEdit(this, propertyMetadata);

      /// <summary>
      /// Returns the member template delegate for the provided property.
      /// </summary>
      /// <param name="propertyMetadata">The property's metadata.</param>
      /// <returns>The member template delegate for the provided property; or null if a member template is not available.</returns>
      public XcstDelegate<object>? MemberTemplate(ModelMetadata propertyMetadata) =>
         EditorInstructions.MemberTemplate(this, propertyMetadata);
   }

   public class HtmlHelper<TModel> : HtmlHelper {

      public new ViewDataDictionary<TModel> ViewData => (ViewDataDictionary<TModel>)ViewDataContainer.ViewData;

      public HtmlHelper(ViewContext viewContext, IViewDataContainer viewDataContainer)
         : base(viewContext, viewDataContainer) {

         if (!(viewDataContainer.ViewData is ViewDataDictionary<TModel>)) {
            throw new ArgumentException(
               $"{nameof(viewDataContainer)}.ViewData should be an instance of 'ViewDataDictionary<TModel>'.",
               nameof(viewDataContainer)
            );
         }
      }

      public string DisplayNameFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
         MetadataInstructions.DisplayNameFor(this, expression);

      public string IdFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
         Id(ExpressionHelper.GetExpressionText(expression));

      public string NameFor<TProperty>(Expression<Func<TModel, TProperty>> expression) =>
         InputInstructions.NameFor(this, expression);

      public string ValueFor<TProperty>(Expression<Func<TModel, TProperty>> expression) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, this.ViewData);

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return ValueHelper(expressionString, metadata.Model, format: metadata.EditFormatString, useViewData: false);
      }

      public string ValueFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string format) {

         ModelMetadata metadata = ModelMetadata.FromLambdaExpression(expression, this.ViewData);

         string expressionString = ExpressionHelper.GetExpressionText(expression);

         return ValueHelper(expressionString, metadata.Model, format, useViewData: false);
      }
   }

   /// <summary>
   /// Controls the value-rendering method For HTML5 input elements of types such as date, time, datetime and datetime-local.
   /// </summary>
   public enum Html5DateRenderingMode {

      /// <summary>
      /// Render date and time values as Rfc3339 compliant strings to support HTML5 date and time types of input elements.
      /// </summary>
      Rfc3339 = 0,

      /// <summary>
      /// Render date and time values according to the current culture's ToString behavior.
      /// </summary>
      CurrentCulture
   }

   public enum InputType {
      CheckBox,
      Hidden,
      Password,
      Radio,
      Text
   }

   static class UnobtrusiveValidationAttributesGenerator {

      public static void GetValidationAttributes(IEnumerable<ModelClientValidationRule> clientRules, IDictionary<string, object> results) {

         if (clientRules is null) throw new ArgumentNullException(nameof(clientRules));
         if (results is null) throw new ArgumentNullException(nameof(results));

         bool renderedRules = false;

         foreach (ModelClientValidationRule rule in clientRules) {

            renderedRules = true;
            string ruleName = "data-val-" + rule.ValidationType;

            ValidateUnobtrusiveValidationRule(rule, results, ruleName);

            results.Add(ruleName, rule.ErrorMessage ?? String.Empty);
            ruleName += "-";

            foreach (var kvp in rule.ValidationParameters) {
               results.Add(ruleName + kvp.Key, kvp.Value ?? String.Empty);
            }
         }

         if (renderedRules) {
            results.Add("data-val", "true");
         }
      }

      static void ValidateUnobtrusiveValidationRule(ModelClientValidationRule rule, IDictionary<string, object> resultsDictionary, string dictionaryKey) {

         if (String.IsNullOrWhiteSpace(rule.ValidationType)) {
            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  WebPageResources.UnobtrusiveJavascript_ValidationTypeCannotBeEmpty,
                  rule.GetType().FullName));
         }

         if (resultsDictionary.ContainsKey(dictionaryKey)) {
            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  WebPageResources.UnobtrusiveJavascript_ValidationTypeMustBeUnique,
                  rule.ValidationType));
         }

         if (rule.ValidationType.Any(c => !Char.IsLower(c))) {
            throw new InvalidOperationException(
               String.Format(CultureInfo.CurrentCulture, WebPageResources.UnobtrusiveJavascript_ValidationTypeMustBeLegal,
                  rule.ValidationType,
                  rule.GetType().FullName));
         }

         foreach (var key in rule.ValidationParameters.Keys) {

            if (String.IsNullOrWhiteSpace(key)) {
               throw new InvalidOperationException(
                  String.Format(
                     CultureInfo.CurrentCulture,
                     WebPageResources.UnobtrusiveJavascript_ValidationParameterCannotBeEmpty,
                     rule.GetType().FullName));
            }

            if (!Char.IsLower(key.First()) || key.Any(c => !Char.IsLower(c) && !Char.IsDigit(c))) {
               throw new InvalidOperationException(
                  String.Format(
                     CultureInfo.CurrentCulture,
                     WebPageResources.UnobtrusiveJavascript_ValidationParameterMustBeLegal,
                     key,
                     rule.GetType().FullName));
            }
         }
      }
   }

   static class TagBuilder {

      public static string? CreateSanitizedId(string originalId) =>
         CreateSanitizedId(originalId, HtmlHelper.IdAttributeDotReplacement);

      public static string? CreateSanitizedId(string originalId, string invalidCharReplacement) {

         if (String.IsNullOrEmpty(originalId)) {
            return null;
         }

         if (invalidCharReplacement is null) {
            throw new ArgumentNullException(nameof(invalidCharReplacement));
         }

         char firstChar = originalId[0];

         if (!Html401IdUtil.IsLetter(firstChar)) {
            // the first character must be a letter
            return null;
         }

         var sb = new StringBuilder(originalId.Length);
         sb.Append(firstChar);

         for (int i = 1; i < originalId.Length; i++) {
            char thisChar = originalId[i];
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

         public static bool IsValidIdCharacter(char c) =>
            (IsLetter(c) || IsDigit(c) || IsAllowableSpecialCharacter(c));

         static bool IsAllowableSpecialCharacter(char c) {
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

         static bool IsDigit(char c) =>
            ('0' <= c && c <= '9');

         public static bool IsLetter(char c) =>
            (('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z'));
      }
   }

   class HtmlAttributePropertyHelper : PropertyHelper {

      static ConcurrentDictionary<Type, PropertyHelper[]> _reflectionCache = new ConcurrentDictionary<Type, PropertyHelper[]>();

      [AllowNull]
      public override string Name {
         get => base.Name;
         protected set => base.Name = value?.Replace('_', '-');
      }

      public static new PropertyHelper[] GetProperties(object instance) =>
         GetProperties(instance, CreateInstance, _reflectionCache);

      static PropertyHelper CreateInstance(PropertyInfo property) =>
         new HtmlAttributePropertyHelper(property);

      public HtmlAttributePropertyHelper(PropertyInfo property)
         : base(property) { }
   }
}
