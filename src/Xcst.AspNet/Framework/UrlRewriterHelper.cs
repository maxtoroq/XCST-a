// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;

namespace System.Web.WebPages {

   class UrlRewriterHelper {

      // internal for tests

      internal const string UrlWasRewrittenServerVar = "IIS_WasUrlRewritten";
      internal const string UrlRewriterEnabledServerVar = "IIS_UrlRewriteModule";

      internal const string UrlWasRequestRewrittenTrueValue = "true";
      internal const string UrlWasRequestRewrittenFalseValue = "false";

      object _lockObject = new object();
      bool _urlRewriterIsTurnedOnValue;
      volatile bool _urlRewriterIsTurnedOnCalculated = false;

      public virtual bool WasRequestRewritten(IServiceProvider httpContext, IDictionary httpContextItems) {

         return IsUrlRewriterTurnedOn(httpContext)
            && WasThisRequestRewritten(httpContext, httpContextItems);
      }

      bool IsUrlRewriterTurnedOn(IServiceProvider httpContext) {

         // Need to do double-check locking because a single instance of this class is shared in the entire app domain (see PathHelpers)

         if (!_urlRewriterIsTurnedOnCalculated) {
            lock (_lockObject) {
               if (!_urlRewriterIsTurnedOnCalculated) {

                  var httpWorkerRequest = (HttpWorkerRequest)httpContext.GetService(typeof(HttpWorkerRequest));

                  bool urlRewriterIsEnabled =
                     httpWorkerRequest?.GetServerVariable(UrlRewriterEnabledServerVar) != null;

                  _urlRewriterIsTurnedOnValue = urlRewriterIsEnabled;
                  _urlRewriterIsTurnedOnCalculated = true;
               }
            }
         }

         return _urlRewriterIsTurnedOnValue;
      }

      static bool WasThisRequestRewritten(IServiceProvider httpContext, IDictionary httpContextItems) {

         if (httpContextItems.Contains(UrlWasRewrittenServerVar)) {
            return Object.Equals(httpContextItems[UrlWasRewrittenServerVar], UrlWasRequestRewrittenTrueValue);
         } else {

            var httpWorkerRequest = (HttpWorkerRequest)httpContext.GetService(typeof(HttpWorkerRequest));

            bool requestWasRewritten =
               httpWorkerRequest?.GetServerVariable(UrlWasRewrittenServerVar) != null;

            if (requestWasRewritten) {
               httpContextItems.Add(UrlWasRewrittenServerVar, UrlWasRequestRewrittenTrueValue);
            } else {
               httpContextItems.Add(UrlWasRewrittenServerVar, UrlWasRequestRewrittenFalseValue);
            }

            return requestWasRewritten;
         }
      }
   }
}
