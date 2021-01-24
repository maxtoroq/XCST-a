// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web.Mvc.Properties;
#if NETCOREAPP
using HttpContextBase = Microsoft.AspNetCore.Http.HttpContext;
#endif

namespace System.Web.Mvc {

   public class UrlHelper {

      readonly HttpContextBase _httpContext;

      // The default constructor is intended for use by unit testing only.
      //public UrlHelper() { }

      public UrlHelper(HttpContextBase httpContext) {

         if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

         _httpContext = httpContext;
      }

      public virtual string Content(string contentPath) =>
         GenerateContentUrl(contentPath, _httpContext);

      [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "As the return value will used only for rendering, string return value is more appropriate.")]
      public static string GenerateContentUrl(string contentPath, HttpContextBase httpContext) {

         if (String.IsNullOrEmpty(contentPath)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(contentPath));
         if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

         if (contentPath[0] == '~') {
            return UrlHelperImpl.GenerateClientUrl(httpContext, contentPath);
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
         string processedPath = UrlBuilder.BuildUrl(path, out query, pathParts);

         // many of the methods we call internally can't handle query strings properly, so tack it on after processing
         // the virtual app path and url rewrites

         if (String.IsNullOrEmpty(query)) {
            return UrlHelperImpl.GenerateClientUrlInternal(_httpContext, processedPath);
         } else {
            return UrlHelperImpl.GenerateClientUrlInternal(_httpContext, processedPath) + query;
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
         HttpRequestExtensions.IsUrlLocalToHost(_httpContext.Request, url);

      internal static class UrlBuilder {

         internal static string BuildUrl(string path, out string query, params object?[]? pathParts) {

            // Performance senstive
            // 
            // This code branches on the number of path-parts to either favor string.Concat or StringBuilder
            // for performance. The most common case (for WebPages) will provide a single int value as a
            // path-part - string.Concat can be more efficient when we know the number of strings to join.

            string finalPath;

            if (pathParts is null
               || pathParts.Length == 0) {

               query = String.Empty;
               finalPath = path;

            } else if (pathParts.Length == 1) {

               object? pathPart = pathParts[0];

               if (pathPart is null) {
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

                  object? pathPart = pathParts[i];

                  if (pathPart is null) {
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
         internal static bool IsDisplayableType(Type t) =>
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
   }
}
