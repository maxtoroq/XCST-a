﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;

namespace System.Web.Mvc {

   static class UrlHelperImpl {

      // this method can accept an app-relative path or an absolute path for contentPath
      public static string GenerateClientUrl(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // many of the methods we call internally can't handle query strings properly, so just strip it out for
         // the time being

         string? query;
         contentPath = StripQuery(contentPath, out query);

         // many of the methods we call internally can't handle query strings properly, so tack it on after processing
         // the virtual app path and url rewrites

         if (String.IsNullOrEmpty(query)) {
            return GenerateClientUrlInternal(httpContext, contentPath);
         } else {
            return GenerateClientUrlInternal(httpContext, contentPath) + query;
         }
      }

      internal static string GenerateClientUrlInternal(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // can't call VirtualPathUtility.IsAppRelative since it throws on some inputs

         bool isAppRelative = contentPath[0] == '~';

         if (isAppRelative) {
            string absoluteContentPath = VirtualPathUtility.ToAbsolute(contentPath, httpContext.Request.ApplicationPath);
            return GenerateClientUrlInternal(httpContext, absoluteContentPath);
         }

         return UrlRewrite.GenerateClientUrlInternal(httpContext, contentPath);
      }

      static string StripQuery(string path, out string? query) {

         int queryIndex = path.IndexOf('?');

         if (queryIndex >= 0) {
            query = path.Substring(queryIndex);
            return path.Substring(0, queryIndex);
         } else {
            query = null;
            return path;
         }
      }

      internal static class UrlRewrite {

         static readonly UrlRewriterHelper _urlRewriterHelper = new UrlRewriterHelper();

         internal static string GenerateClientUrlInternal(HttpContextBase httpContext, string contentPath) {

            // we only want to manipulate the path if URL rewriting is active for this request, else we risk breaking the generated URL

            if (_urlRewriterHelper.WasRequestRewritten(httpContext, httpContext.Items)) {

               // Since the rawUrl represents what the user sees in his browser, it is what we want to use as the base
               // of our absolute paths. For example, consider mysite.example.com/foo, which is internally
               // rewritten to content.example.com/mysite/foo. When we want to generate a link to ~/bar, we want to
               // base it from / instead of /foo, otherwise the user ends up seeing mysite.example.com/foo/bar,
               // which is incorrect.

               var request = httpContext.Request;

               string relativeUrlToDestination = MakeRelative(request.Path, contentPath);
               string absoluteUrlToDestination = MakeAbsolute(request.RawUrl, relativeUrlToDestination);

               return absoluteUrlToDestination;
            }

            return contentPath;
         }

         // same method as above, but for HttpContext
         internal static string GenerateClientUrlInternal(HttpContext httpContext, string contentPath) {

            if (_urlRewriterHelper.WasRequestRewritten(httpContext, httpContext.Items)) {

               var request = httpContext.Request;

               string relativeUrlToDestination = MakeRelative(request.Path, contentPath);
               string absoluteUrlToDestination = MakeAbsolute(request.RawUrl, relativeUrlToDestination);

               return absoluteUrlToDestination;
            }

            return contentPath;
         }

         public static string MakeAbsolute(string basePath, string relativePath) {

            // The Combine() method can't handle query strings on the base path, so we trim it off.

            basePath = StripQuery(basePath, out _);

            return VirtualPathUtility.Combine(basePath, relativePath);
         }

         public static string MakeRelative(string fromPath, string toPath) {

            string relativeUrl = VirtualPathUtility.MakeRelative(fromPath, toPath);

            if (String.IsNullOrEmpty(relativeUrl) || relativeUrl[0] == '?') {

               // Sometimes VirtualPathUtility.MakeRelative() will return an empty string when it meant to return '.',
               // but links to {empty string} are browser dependent. We replace it with an explicit path to force
               // consistency across browsers.

               relativeUrl = "./" + relativeUrl;
            }

            return relativeUrl;
         }
      }
   }

   class UrlRewriterHelper {

      // internal for tests

      internal const string UrlWasRewrittenServerVar = "IIS_WasUrlRewritten";
      internal const string UrlRewriterEnabledServerVar = "IIS_UrlRewriteModule";

      internal const string UrlWasRequestRewrittenTrueValue = "true";
      internal const string UrlWasRequestRewrittenFalseValue = "false";

      object _lockObject = new object();
      bool _urlRewriterIsTurnedOnValue;
      volatile bool _urlRewriterIsTurnedOnCalculated = false;

      public virtual bool WasRequestRewritten(IServiceProvider httpContext, IDictionary httpContextItems) =>
         IsUrlRewriterTurnedOn(httpContext)
            && WasThisRequestRewritten(httpContext, httpContextItems);

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