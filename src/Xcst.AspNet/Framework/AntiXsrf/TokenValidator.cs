// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics.Contracts;
using System.Globalization;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.WebPages.Resources;

namespace System.Web.Helpers.AntiXsrf {

   sealed class TokenValidator : ITokenValidator {

      readonly IClaimUidExtractor _claimUidExtractor;
      readonly IAntiForgeryConfig _config;

      internal TokenValidator(IAntiForgeryConfig config, IClaimUidExtractor claimUidExtractor) {
         _config = config;
         _claimUidExtractor = claimUidExtractor;
      }

      public AntiForgeryToken GenerateCookieToken() {

         return new AntiForgeryToken {

            // SecurityToken will be populated automatically.

            IsSessionToken = true
         };
      }

      public AntiForgeryToken GenerateFormToken(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken cookieToken) {

         Contract.Assert(IsCookieTokenValid(cookieToken));

         var formToken = new AntiForgeryToken {
            SecurityToken = cookieToken.SecurityToken,
            IsSessionToken = false
         };

         bool requireAuthenticatedUserHeuristicChecks = false;

         // populate Username and ClaimUid

         if (identity?.IsAuthenticated == true) {

            if (!_config.SuppressIdentityHeuristicChecks) {

               // If the user is authenticated and heuristic checks are not suppressed,
               // then Username, ClaimUid, or AdditionalData must be set.

               requireAuthenticatedUserHeuristicChecks = true;
            }

            formToken.ClaimUid = _claimUidExtractor.ExtractClaimUid(identity);

            if (formToken.ClaimUid == null) {
               formToken.Username = identity.Name;
            }
         }

         // populate AdditionalData

         if (_config.AdditionalDataProvider != null) {
            formToken.AdditionalData = _config.AdditionalDataProvider.GetAdditionalData(httpContext);
         }

         if (requireAuthenticatedUserHeuristicChecks
            && String.IsNullOrEmpty(formToken.Username)
            && formToken.ClaimUid == null
            && String.IsNullOrEmpty(formToken.AdditionalData)) {

            // Application says user is authenticated, but we have no identifier for the user.

            throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, WebPageResources.TokenValidator_AuthenticatedUserWithoutUsername, identity.GetType()));
         }

         return formToken;
      }

      public bool IsCookieTokenValid(AntiForgeryToken cookieToken) {
         return cookieToken?.IsSessionToken == true;
      }

      public void ValidateTokens(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken sessionToken, AntiForgeryToken fieldToken) {

         Exception ex = ValidateTokensImpl(httpContext, identity, sessionToken, fieldToken);

         if (ex != null) {
            throw ex;
         }
      }

      public bool TryValidateTokens(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken sessionToken, AntiForgeryToken fieldToken) {

         Exception ex = ValidateTokensImpl(httpContext, identity, sessionToken, fieldToken);

         return ex == null;
      }

      Exception ValidateTokensImpl(HttpContextBase httpContext, IIdentity identity, AntiForgeryToken sessionToken, AntiForgeryToken fieldToken) {

         // Were the tokens even present at all?

         if (sessionToken == null) return HttpAntiForgeryException.CreateCookieMissingException(_config.CookieName);
         if (fieldToken == null) return HttpAntiForgeryException.CreateFormFieldMissingException(_config.FormFieldName);

         // Do the tokens have the correct format?

         if (!sessionToken.IsSessionToken || fieldToken.IsSessionToken) {
            return HttpAntiForgeryException.CreateTokensSwappedException(_config.CookieName, _config.FormFieldName);
         }

         // Are the security tokens embedded in each incoming token identical?

         if (!Equals(sessionToken.SecurityToken, fieldToken.SecurityToken)) {
            return HttpAntiForgeryException.CreateSecurityTokenMismatchException();
         }

         // Is the incoming token meant for the current user?

         string currentUsername = String.Empty;
         BinaryBlob currentClaimUid = null;

         if (identity?.IsAuthenticated == true) {

            currentClaimUid = _claimUidExtractor.ExtractClaimUid(identity);

            if (currentClaimUid == null) {
               currentUsername = identity.Name ?? String.Empty;
            }
         }

         // OpenID and other similar authentication schemes use URIs for the username.
         // These should be treated as case-sensitive.

         bool useCaseSensitiveUsernameComparison = currentUsername.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || currentUsername.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

         if (!String.Equals(fieldToken.Username, currentUsername, (useCaseSensitiveUsernameComparison) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)) {
            return HttpAntiForgeryException.CreateUsernameMismatchException(fieldToken.Username, currentUsername);
         }

         if (!Equals(fieldToken.ClaimUid, currentClaimUid)) {
            return HttpAntiForgeryException.CreateClaimUidMismatchException();
         }

         // Is the AdditionalData valid?
         if (_config.AdditionalDataProvider != null && !_config.AdditionalDataProvider.ValidateAdditionalData(httpContext, fieldToken.AdditionalData)) {
            return HttpAntiForgeryException.CreateAdditionalDataCheckFailedException();
         }

         return null;
      }
   }
}
