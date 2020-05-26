// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Helpers.AntiXsrf {

   // Provides an abstraction around how tokens are persisted and retrieved for a request

   interface ITokenStore {
      AntiForgeryToken? GetCookieToken(HttpContextBase httpContext);
      AntiForgeryToken? GetCookieToken(HttpContextBase httpContext, bool throwOnError);
      AntiForgeryToken? GetFormToken(HttpContextBase httpContext);
      AntiForgeryToken? GetFormToken(HttpContextBase httpContext, bool throwOnError);
      void SaveCookieToken(HttpContextBase httpContext, AntiForgeryToken token);
   }
}
