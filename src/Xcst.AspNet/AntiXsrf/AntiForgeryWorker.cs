﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;
using System.Web.Mvc.Properties;
using Xcst;

namespace System.Web.Helpers.AntiXsrf {

   sealed class AntiForgeryWorker {

      readonly IAntiForgeryConfig _config;
      readonly IAntiForgeryTokenSerializer _serializer;
      readonly ITokenStore _tokenStore;
      readonly ITokenValidator _validator;

      internal AntiForgeryWorker(IAntiForgeryTokenSerializer serializer, IAntiForgeryConfig config, ITokenStore tokenStore, ITokenValidator validator) {
         _serializer = serializer;
         _config = config;
         _tokenStore = tokenStore;
         _validator = validator;
      }

      void CheckSSLConfig(HttpContextBase httpContext) {
         if (_config.RequireSSL && !httpContext.Request.IsSecureConnection) {
            throw new InvalidOperationException(WebPageResources.AntiForgeryWorker_RequireSSL);
         }
      }

      AntiForgeryToken? DeserializeToken(string? serializedToken, bool throwOnError = true) =>
         (!String.IsNullOrEmpty(serializedToken)) ?
            _serializer.Deserialize(serializedToken!, throwOnError)
            : null;

      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Caller will just regenerate token in case of failure.")]
      AntiForgeryToken? DeserializeTokenNoThrow(string? serializedToken) {

         try {
            return DeserializeToken(serializedToken);
         } catch {
            // ignore failures since we'll just generate a new token
            return null;
         }
      }

      static IIdentity? ExtractIdentity(HttpContextBase httpContext) =>
         httpContext?.User?.Identity;

      [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Caller will just regenerate token in case of failure.")]
      AntiForgeryToken? GetCookieTokenNoThrow(HttpContextBase httpContext) {

         try {
            return _tokenStore.GetCookieToken(httpContext);
         } catch {
            // ignore failures since we'll just generate a new token
            return null;
         }
      }

      // [ ENTRY POINT ]
      // Generates an anti-XSRF token pair for the current user. The return
      // value is the hidden input form element that should be rendered in
      // the <form>. This method has a side effect: it may set a response
      // cookie.

      public void GetFormInputElement(HttpContextBase httpContext, XcstWriter output) {

         CheckSSLConfig(httpContext);

         AntiForgeryToken? oldCookieToken = GetCookieTokenNoThrow(httpContext);
         AntiForgeryToken? newCookieToken;
         AntiForgeryToken formToken;
         GetTokens(httpContext, oldCookieToken, out newCookieToken, out formToken);

         if (newCookieToken != null) {
            // If a new cookie was generated, persist it.
            _tokenStore.SaveCookieToken(httpContext, newCookieToken);
         }

         if (!_config.SuppressXFrameOptionsHeader) {

            // Adding X-Frame-Options header to prevent ClickJacking. See
            // http://tools.ietf.org/html/draft-ietf-websec-x-frame-options-10
            // for more information.

            const string FrameHeaderName = "X-Frame-Options";

            HttpResponseBase response = httpContext.Response;

            if (response.Headers[FrameHeaderName] is null) {
               response.AddHeader(FrameHeaderName, "SAMEORIGIN");
            }
         }

         output.WriteStartElement("input");
         output.WriteAttributeString("type", "hidden");
         output.WriteAttributeString("name", _config.FormFieldName);
         output.WriteAttributeString("value", _serializer.Serialize(formToken));
         output.WriteEndElement();
      }

      // [ ENTRY POINT ]
      // Generates a (cookie, form) serialized token pair for the current user.
      // The caller may specify an existing cookie value if one exists. If the
      // 'new cookie value' out param is non-null, the caller *must* persist
      // the new value to cookie storage since the original value was null or
      // invalid. This method is side-effect free.

      public void GetTokens(HttpContextBase httpContext, string? serializedOldCookieToken, out string? serializedNewCookieToken, out string serializedFormToken) {

         CheckSSLConfig(httpContext);

         AntiForgeryToken? oldCookieToken = DeserializeTokenNoThrow(serializedOldCookieToken);
         AntiForgeryToken? newCookieToken;
         AntiForgeryToken formToken;

         GetTokens(httpContext, oldCookieToken, out newCookieToken, out formToken);

         serializedNewCookieToken = Serialize(newCookieToken);
         serializedFormToken = Serialize(formToken);
      }

      void GetTokens(HttpContextBase httpContext, AntiForgeryToken? oldCookieToken, out AntiForgeryToken? newCookieToken, out AntiForgeryToken formToken) {

         newCookieToken = null;

         if (!_validator.IsCookieTokenValid(oldCookieToken)) {
            // Need to make sure we're always operating with a good cookie token.
            oldCookieToken = newCookieToken = _validator.GenerateCookieToken();
         }

         Debug.Assert(_validator.IsCookieTokenValid(oldCookieToken));
         formToken = _validator.GenerateFormToken(httpContext, ExtractIdentity(httpContext), oldCookieToken!);
      }

      [return: NotNullIfNotNull("token")]
      string? Serialize(AntiForgeryToken? token) =>
         (token != null) ? _serializer.Serialize(token) : null;

      // [ ENTRY POINT ]
      // Given an HttpContext, validates that the anti-XSRF tokens contained
      // in the cookies & form are OK for this request.

      public void Validate(HttpContextBase httpContext) {

         CheckSSLConfig(httpContext);

         // Extract cookie & form tokens
         AntiForgeryToken? cookieToken = _tokenStore.GetCookieToken(httpContext);
         AntiForgeryToken? formToken = _tokenStore.GetFormToken(httpContext);

         // Validate
         _validator.ValidateTokens(httpContext, ExtractIdentity(httpContext), cookieToken, formToken);
      }

      // [ ENTRY POINT ]
      // Given the serialized string representations of a cookie & form token,
      // validates that the pair is OK for this request.

      public void Validate(HttpContextBase httpContext, string? cookieToken, string? formToken) {

         CheckSSLConfig(httpContext);

         // Extract cookie & form tokens
         AntiForgeryToken? deserializedCookieToken = DeserializeToken(cookieToken);
         AntiForgeryToken? deserializedFormToken = DeserializeToken(formToken);

         // Validate
         _validator.ValidateTokens(httpContext, ExtractIdentity(httpContext), deserializedCookieToken, deserializedFormToken);
      }

      public bool TryValidate(HttpContextBase httpContext) {

         CheckSSLConfig(httpContext);

         // Extract cookie & form tokens
         AntiForgeryToken? cookieToken = _tokenStore.GetCookieToken(httpContext, throwOnError: false);
         AntiForgeryToken? formToken = _tokenStore.GetFormToken(httpContext, throwOnError: false);

         // Validate
         return _validator.TryValidateTokens(httpContext, ExtractIdentity(httpContext), cookieToken, formToken);
      }

      public bool TryValidate(HttpContextBase httpContext, string? cookieToken, string? formToken) {

         CheckSSLConfig(httpContext);

         // Extract cookie & form tokens
         AntiForgeryToken? deserializedCookieToken = DeserializeToken(cookieToken, throwOnError: false);
         AntiForgeryToken? deserializedFormToken = DeserializeToken(formToken, throwOnError: false);

         // Validate
         return _validator.TryValidateTokens(httpContext, ExtractIdentity(httpContext), deserializedCookieToken, deserializedFormToken);
      }
   }
}
