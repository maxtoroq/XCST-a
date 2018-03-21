// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.WebPages {

   public class BrowserOverrideStores {

      static BrowserOverrideStores _instance = new BrowserOverrideStores();
      BrowserOverrideStore _currentOverrideStore = new CookieBrowserOverrideStore();

      /// <summary>
      /// The current BrowserOverrideStore
      /// </summary>

      public static BrowserOverrideStore Current {
         get { return _instance.CurrentInternal; }
         set { _instance.CurrentInternal = value; }
      }

      internal BrowserOverrideStore CurrentInternal {
         get { return _currentOverrideStore; }
         set { _currentOverrideStore = value ?? new RequestBrowserOverrideStore(); }
      }
   }

   /// <summary>
   /// The current BrowserOverrideStore is used to get and set the user agent of a request.
   /// For an example see CookieBasedBrowserOverrideStore.
   /// </summary>

   public abstract class BrowserOverrideStore {
      public abstract string GetOverriddenUserAgent(HttpContextBase httpContext);
      public abstract void SetOverriddenUserAgent(HttpContextBase httpContext, string userAgent);
   }

   /// <summary>
   /// The default BrowserOverrideStore. Gets overridden user agent for a request from a cookie.
   /// Creates a cookie to set the overridden user agent.
   /// </summary>

   public class CookieBrowserOverrideStore : BrowserOverrideStore {

      internal static readonly string BrowserOverrideCookieName = ".ASPXBrowserOverride";
      readonly int _daysToExpire;

      /// <summary>
      /// Creates the BrowserOverrideStore setting any browser override cookie to expire in 7 days.
      /// </summary>

      public CookieBrowserOverrideStore()
         : this(daysToExpire: 7) { }

      /// <summary>
      /// Constructor to control the expiration of the browser override cookie.
      /// </summary>

      public CookieBrowserOverrideStore(int daysToExpire) {
         _daysToExpire = daysToExpire;
      }

      /// <summary>
      /// Looks for a user agent by searching for the browser override cookie. If no cookie is found
      /// returns null.
      /// </summary>

      public override string GetOverriddenUserAgent(HttpContextBase httpContext) {

         // Check the response to see if the cookie has been set somewhere in the current request.

         HttpCookieCollection responseCookies = httpContext.Response.Cookies;

         // NOTE: Only look for the key (via AllKeys) so a new cookie is not automatically created.

         string[] cookieNames = responseCookies.AllKeys;

         // NOTE: use a simple for loop since it performs an order of magnitude faster than .Contains()
         // and this is a hot path that gets executed for every request.

         for (int i = 0; i < cookieNames.Length; i++) {

            // HttpCookieCollection uses OrdinalIgnoreCase comparison for its keys

            if (String.Equals(cookieNames[i], BrowserOverrideCookieName, StringComparison.OrdinalIgnoreCase)) {

               HttpCookie currentOverriddenBrowserCookie = responseCookies[BrowserOverrideCookieName];

               return currentOverriddenBrowserCookie.Value;
            }
         }

         // If there was no cookie found in the response check the request.
         var requestOverrideCookie = httpContext.Request.Cookies[BrowserOverrideCookieName];

         return requestOverrideCookie?.Value;
      }

      /// <summary>
      /// Adds a browser override cookie with the set user agent to the response of the current request.
      /// If the user agent is null the browser override cookie is set to expire, otherwise its expiration is set
      /// to daysToExpire, specified when CookieBasedOverrideStore is created.
      /// </summary>

      public override void SetOverriddenUserAgent(HttpContextBase httpContext, string userAgent) {

         HttpCookie browserOverrideCookie = new HttpCookie(BrowserOverrideCookieName, HttpUtility.UrlEncode(userAgent));

         if (userAgent == null) {
            browserOverrideCookie.Expires = DateTime.Now.AddDays(-1);
         } else {

            // Only set expiration if the cookie should live longer than the current session

            if (_daysToExpire > 0) {
               browserOverrideCookie.Expires = DateTime.Now.AddDays(_daysToExpire);
            }
         }

         httpContext.Response.Cookies.Remove(BrowserOverrideCookieName);
         httpContext.Response.Cookies.Add(browserOverrideCookie);
      }
   }

   /// <summary>
   /// RequestBrowserOverrideStore simply returns the user agent of the current request.
   /// </summary>

   sealed class RequestBrowserOverrideStore : BrowserOverrideStore {

      public override string GetOverriddenUserAgent(HttpContextBase httpContext) {
         return httpContext.Request.UserAgent;
      }

      public override void SetOverriddenUserAgent(HttpContextBase httpContext, string userAgent) {
         return;
      }
   }
}
