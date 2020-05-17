// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web.Mvc.Properties;
using System.Web.Routing;

namespace System.Web.Mvc {

   public class UrlHelper {

      public RequestContext RequestContext { get; private set; }

      public RouteCollection RouteCollection { get; private set; }

      // The default constructor is intended for use by unit testing only.
      public UrlHelper() { }

      public UrlHelper(RequestContext requestContext)
         : this(requestContext, RouteTable.Routes) { }

      public UrlHelper(RequestContext requestContext, RouteCollection routeCollection) {

         if (requestContext == null) throw new ArgumentNullException(nameof(requestContext));
         if (routeCollection == null) throw new ArgumentNullException(nameof(routeCollection));

         this.RequestContext = requestContext;
         this.RouteCollection = routeCollection;
      }

      public virtual string Content(string contentPath) =>
         GenerateContentUrl(contentPath, this.RequestContext.HttpContext);

      [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "As the return value will used only for rendering, string return value is more appropriate.")]
      public static string GenerateContentUrl(string contentPath, HttpContextBase httpContext) {

         if (String.IsNullOrEmpty(contentPath)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(contentPath));
         if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

         if (contentPath[0] == '~') {
            return UrlUtil.GenerateClientUrl(httpContext, contentPath);
         } else {
            return contentPath;
         }
      }

      public string Href(string path, params object[] pathParts) {

         if (String.IsNullOrEmpty(path)) {
            return path;
         }

         if (!(path[0] == '/' || path[0] == '~')) {
            throw new ArgumentException("An absolute or application-relative path is expected.", nameof(path));
         }

         string query;
         string processedPath = UrlUtil.UrlBuilder.BuildUrl(path, out query, pathParts);

         // many of the methods we call internally can't handle query strings properly, so tack it on after processing
         // the virtual app path and url rewrites

         if (String.IsNullOrEmpty(query)) {
            return UrlUtil.GenerateClientUrlInternal(this.RequestContext.HttpContext, processedPath);
         } else {
            return UrlUtil.GenerateClientUrlInternal(this.RequestContext.HttpContext, processedPath) + query;
         }
      }

      //REVIEW: Should we have an overload that takes Uri?

      [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Needs to take same parameters as HttpUtility.UrlEncode()")]
      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
      public virtual string Encode(string url) =>
         HttpUtility.UrlEncode(url);

      [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "Response.Redirect() takes its URI as a string parameter.")]
      public virtual bool IsLocalUrl(string url) =>
         // TODO this should call the System.Web.dll API once it gets added to the framework and MVC takes a dependency on it.
         HttpRequestExtensions.IsUrlLocalToHost(this.RequestContext.HttpContext.Request, url);
   }

   static class UrlUtil {

      // this method can accept an app-relative path or an absolute path for contentPath
      public static string GenerateClientUrl(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // many of the methods we call internally can't handle query strings properly, so just strip it out for
         // the time being

         string query;
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

      static string StripQuery(string path, out string query) {

         int queryIndex = path.IndexOf('?');

         if (queryIndex >= 0) {
            query = path.Substring(queryIndex);
            return path.Substring(0, queryIndex);
         } else {
            query = null;
            return path;
         }
      }

      internal static class UrlBuilder {

         internal static string BuildUrl(string path, out string query, params object[] pathParts) {

            // Performance senstive
            // 
            // This code branches on the number of path-parts to either favor string.Concat or StringBuilder
            // for performance. The most common case (for WebPages) will provide a single int value as a
            // path-part - string.Concat can be more efficient when we know the number of strings to join.

            string finalPath;

            if (pathParts == null
               || pathParts.Length == 0) {

               query = String.Empty;
               finalPath = path;

            } else if (pathParts.Length == 1) {

               object pathPart = pathParts[0];

               if (pathPart == null) {
                  query = String.Empty;
                  finalPath = path;

               } else if (IsDisplayableType(pathPart.GetType())) {

                  string displayablePath = Convert.ToString(pathPart, CultureInfo.InvariantCulture);
                  path = path + "/" + displayablePath;
                  query = String.Empty;
                  finalPath = path;

               } else {

                  var queryBuilder = new StringBuilder();
                  AppendToQueryString(queryBuilder, pathPart);

                  query = queryBuilder.ToString();
                  finalPath = path;
               }

            } else {

               var pathBuilder = new StringBuilder(path);
               var queryBuilder = new StringBuilder();

               for (int i = 0; i < pathParts.Length; i++) {

                  object pathPart = pathParts[i];

                  if (pathPart == null) {
                     continue;
                  }

                  if (IsDisplayableType(pathPart.GetType())) {

                     var displayablePath = Convert.ToString(pathPart, CultureInfo.InvariantCulture);
                     pathBuilder.Append('/');
                     pathBuilder.Append(displayablePath);

                  } else {
                     AppendToQueryString(queryBuilder, pathPart);
                  }
               }

               query = queryBuilder.ToString();
               finalPath = pathBuilder.ToString();
            }

            return HttpUtility.UrlPathEncode(finalPath);
         }

         /// <summary>
         /// Determines if a type is displayable as part of a Url path.
         /// </summary>
         /// <remarks>
         /// If a type is a displayable type, then we format values of that type as part of the Url Path. If not, then
         /// we attempt to create a RouteValueDictionary, and encode the value as key-value pairs in the query string.
         /// 
         /// We determine if a type is displayable by whether or not it implements any interfaces. The built-in simple
         /// types like Int32 implement IFormattable, which will be used to convert it to a string.
         /// 
         /// Primarily we do this check to allow anonymous types to represent key-value pairs (anonymous types don't 
         /// implement any interfaces).
         /// </remarks>
         static bool IsDisplayableType(Type t) =>
            t.GetInterfaces().Length > 0;

         static void AppendToQueryString(StringBuilder queryString, object obj) {

            // If this method is called, then obj isn't a type that we can put in the path, instead
            // we want to format it as key-value pairs for the query string. The mostly likely
            // user scenario for this is an anonymous type.

            IDictionary<string, object> dictionary = TypeHelpers.ObjectToDictionary(obj);

            foreach (var item in dictionary) {

               if (queryString.Length == 0) {
                  queryString.Append('?');
               } else {
                  queryString.Append('&');
               }

               string stringValue = Convert.ToString(item.Value, CultureInfo.InvariantCulture);

               queryString.Append(HttpUtility.UrlEncode(item.Key))
                   .Append('=')
                   .Append(HttpUtility.UrlEncode(stringValue));
            }
         }
      }

      internal static class UrlRewrite {

         static UrlRewriterHelper _urlRewriterHelper = new UrlRewriterHelper();

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

            string query;
            basePath = StripQuery(basePath, out query);

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
