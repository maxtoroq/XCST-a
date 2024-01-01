// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Xcst.Web;

public class UrlHelper {

   readonly HttpContext
   _httpContext;

   // The default constructor is intended for use by unit testing only.
   //public UrlHelper() { }

   public
   UrlHelper(HttpContext httpContext) {

      if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

      _httpContext = httpContext;
   }

   public virtual string
   Content(string contentPath) =>
      GenerateContentUrl(contentPath, _httpContext);

   [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "As the return value will used only for rendering, string return value is more appropriate.")]
   public static string
   GenerateContentUrl(string contentPath, HttpContext httpContext) {

      if (String.IsNullOrEmpty(contentPath)) {
         throw new ArgumentException(nameof(contentPath) + " cannot be null or empty.", nameof(contentPath));
      }

      if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

      if (contentPath[0] == '~') {
         return GenerateClientUrl(httpContext, contentPath);
      } else {
         return contentPath;
      }
   }

   public string
   Href(string path, params object[] pathParts) {

      if (String.IsNullOrEmpty(path)) {
         return path;
      }

      if (!(path[0] == '/' || path[0] == '~')) {
         throw new ArgumentException("An absolute or application-relative path is expected.", nameof(path));
      }

      var processedPath = UrlBuilder.BuildUrl(path, out var query, pathParts);

      // many of the methods we call internally can't handle query strings properly, so tack it on after processing
      // the virtual app path and url rewrites

      if (String.IsNullOrEmpty(query)) {
         return GenerateClientUrlInternal(_httpContext, processedPath);
      } else {
         return GenerateClientUrlInternal(_httpContext, processedPath) + query;
      }
   }

   //REVIEW: Should we have an overload that takes Uri?

   [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Needs to take same parameters as HttpUtility.UrlEncode()")]
   [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
   [return: NotNullIfNotNull(nameof(url))]
   public virtual string?
   Encode(string? url) => HttpUtility.UrlEncode(url);

   [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "Response.Redirect() takes its URI as a string parameter.")]
   public virtual bool
   IsLocalUrl(string? url) =>
      // TODO this should call the System.Web.dll API once it gets added to the framework and MVC takes a dependency on it.
      _httpContext.Request.IsUrlLocalToHost(url);

   // this method can accept an app-relative path or an absolute path for contentPath
   [return: NotNullIfNotNull(nameof(contentPath))]
   static string?
   GenerateClientUrl(HttpContext httpContext, string? contentPath) {

      if (String.IsNullOrEmpty(contentPath)) {
         return contentPath;
      }

      // many of the methods we call internally can't handle query strings properly, so just strip it out for
      // the time being

      contentPath = StripQuery(contentPath, out var query);

      // many of the methods we call internally can't handle query strings properly, so tack it on after processing
      // the virtual app path and url rewrites

      if (String.IsNullOrEmpty(query)) {
         return GenerateClientUrlInternal(httpContext, contentPath);
      } else {
         return GenerateClientUrlInternal(httpContext, contentPath) + query;
      }
   }

   [return: NotNullIfNotNull(nameof(contentPath))]
   static string?
   GenerateClientUrlInternal(HttpContext httpContext, string? contentPath) {

      if (String.IsNullOrEmpty(contentPath)) {
         return contentPath;
      }

      var isAppRelative = contentPath[0] == '~';

      if (isAppRelative) {

         // See also Microsoft.AspNetCore.Mvc.Routing.UrlHelperBase.Content
         var other = new PathString(contentPath.Substring(1));
         return httpContext.Request.PathBase.Add(other).Value!;
      }

      return contentPath;
   }

   static string
   StripQuery(string path, out string? query) {

      var queryIndex = path.IndexOf('?');

      if (queryIndex >= 0) {
         query = path.Substring(queryIndex);
         return path.Substring(0, queryIndex);
      } else {
         query = null;
         return path;
      }
   }

   internal static class UrlBuilder {

      internal static string
      BuildUrl(string path, out string query, params object?[]? pathParts) {

         // Performance senstive
         // 
         // This code branches on the number of path-parts to either favor string.Concat or StringBuilder
         // for performance. The most common case (for WebPages) will provide a single int value as a
         // path-part - string.Concat can be more efficient when we know the number of strings to join.

         string finalPath;

         if (pathParts is null
            || pathParts.Length == 0) {

            query = string.Empty;
            finalPath = path;

         } else if (pathParts.Length == 1) {

            var pathPart = pathParts[0];

            if (pathPart is null) {
               query = string.Empty;
               finalPath = path;

            } else if (IsDisplayableType(pathPart.GetType())) {

               var displayablePath = Convert.ToString(pathPart, CultureInfo.InvariantCulture);
               path = path + "/" + displayablePath;
               query = string.Empty;
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

               var pathPart = pathParts[i];

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
      internal static bool
      IsDisplayableType(Type t) =>
         t.GetInterfaces().Length > 0;

      static void
      AppendToQueryString(StringBuilder queryString, object obj) {

         // If this method is called, then obj isn't a type that we can put in the path, instead
         // we want to format it as key-value pairs for the query string. The mostly likely
         // user scenario for this is an anonymous type.

         var dictionary = TypeHelpers.ObjectToDictionary(obj);

         foreach (var item in dictionary) {

            if (queryString.Length == 0) {
               queryString.Append('?');
            } else {
               queryString.Append('&');
            }

            var stringValue = Convert.ToString(item.Value, CultureInfo.InvariantCulture);

            queryString.Append(HttpUtility.UrlEncode(item.Key))
                .Append('=')
                .Append(HttpUtility.UrlEncode(stringValue));
         }
      }
   }
}
