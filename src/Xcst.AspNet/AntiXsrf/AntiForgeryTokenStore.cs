// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Helpers.AntiXsrf {

   // Saves anti-XSRF tokens split between HttpRequest.Cookies and HttpRequest.Form

   sealed class AntiForgeryTokenStore : ITokenStore {

      readonly IAntiForgeryConfig _config;
      readonly IAntiForgeryTokenSerializer _serializer;

      internal AntiForgeryTokenStore(IAntiForgeryConfig config, IAntiForgeryTokenSerializer serializer) {
         _config = config;
         _serializer = serializer;
      }

      public AntiForgeryToken GetCookieToken(HttpContextBase httpContext) {
         return GetCookieToken(httpContext, throwOnError: true);
      }

      public AntiForgeryToken GetCookieToken(HttpContextBase httpContext, bool throwOnError) {

         HttpCookie cookie = httpContext.Request.Cookies[_config.CookieName];

         if (cookie == null || String.IsNullOrEmpty(cookie.Value)) {
            // did not exist
            return null;
         }

         return _serializer.Deserialize(cookie.Value, throwOnError);
      }

      public AntiForgeryToken GetFormToken(HttpContextBase httpContext) {
         return GetFormToken(httpContext, throwOnError: true);
      }

      public AntiForgeryToken GetFormToken(HttpContextBase httpContext, bool throwOnError) {

         string value = httpContext.Request.Form[_config.FormFieldName];

         if (String.IsNullOrEmpty(value)) {
            // did not exist
            return null;
         }

         return _serializer.Deserialize(value, throwOnError);
      }

      public void SaveCookieToken(HttpContextBase httpContext, AntiForgeryToken token) {

         string serializedToken = _serializer.Serialize(token);

         var newCookie = new HttpCookie(_config.CookieName, serializedToken) {
            HttpOnly = true
         };

         // Note: don't use "newCookie.Secure = _config.RequireSSL;" since the default
         // value of newCookie.Secure is automatically populated from the <httpCookies>
         // config element.

         if (_config.RequireSSL) {
            newCookie.Secure = true;
         }

         httpContext.Response.Cookies.Set(newCookie);
      }
   }
}
