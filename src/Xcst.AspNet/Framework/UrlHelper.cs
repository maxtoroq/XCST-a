// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Web.Mvc;
using System.Web.Mvc.Properties;
using System.Web.Routing;
using System.Web.WebPages;

namespace System.Web.Mvc {

   public class UrlHelper {

      public RequestContext RequestContext { get; private set; }

      public RouteCollection RouteCollection { get; private set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="UrlHelper"/> class.
      /// </summary>
      /// <remarks>The default constructor is intended for use by unit testing only.</remarks>

      public UrlHelper() { }

      public UrlHelper(RequestContext requestContext)
         : this(requestContext, RouteTable.Routes) { }

      public UrlHelper(RequestContext requestContext, RouteCollection routeCollection) {

         if (requestContext == null) throw new ArgumentNullException(nameof(requestContext));
         if (routeCollection == null) throw new ArgumentNullException(nameof(routeCollection));

         this.RequestContext = requestContext;
         this.RouteCollection = routeCollection;
      }

      public virtual string Content(string contentPath) {
         return GenerateContentUrl(contentPath, this.RequestContext.HttpContext);
      }

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

      //REVIEW: Should we have an overload that takes Uri?

      [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", Justification = "Needs to take same parameters as HttpUtility.UrlEncode()")]
      [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "For consistency, all helpers are instance methods.")]
      public virtual string Encode(string url) {
         return HttpUtility.UrlEncode(url);
      }

      [SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "0#", Justification = "Response.Redirect() takes its URI as a string parameter.")]
      public virtual bool IsLocalUrl(string url) {
         
         // TODO this should call the System.Web.dll API once it gets added to the framework and MVC takes a dependency on it.

         return HttpRequestExtensions.IsUrlLocalToHost(this.RequestContext.HttpContext.Request, url);
      }
   }
}

namespace System.Web.WebPages {

   static class UrlUtil {

      static UrlRewriterHelper _urlRewriterHelper = new UrlRewriterHelper();

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

      static string GenerateClientUrlInternal(HttpContextBase httpContext, string contentPath) {

         if (String.IsNullOrEmpty(contentPath)) {
            return contentPath;
         }

         // can't call VirtualPathUtility.IsAppRelative since it throws on some inputs

         bool isAppRelative = contentPath[0] == '~';

         if (isAppRelative) {
            string absoluteContentPath = VirtualPathUtility.ToAbsolute(contentPath, httpContext.Request.ApplicationPath);
            return GenerateClientUrlInternal(httpContext, absoluteContentPath);
         }

         // we only want to manipulate the path if URL rewriting is active for this request, else we risk breaking the generated URL

         bool wasRequestRewritten = _urlRewriterHelper.WasRequestRewritten(httpContext, httpContext.Items);

         if (!wasRequestRewritten) {
            return contentPath;
         }

         // Since the rawUrl represents what the user sees in his browser, it is what we want to use as the base
         // of our absolute paths. For example, consider mysite.example.com/foo, which is internally
         // rewritten to content.example.com/mysite/foo. When we want to generate a link to ~/bar, we want to
         // base it from / instead of /foo, otherwise the user ends up seeing mysite.example.com/foo/bar,
         // which is incorrect.

         string relativeUrlToDestination = MakeRelative(httpContext.Request.Path, contentPath);
         string absoluteUrlToDestination = MakeAbsolute(httpContext.Request.RawUrl, relativeUrlToDestination);

         return absoluteUrlToDestination;
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
   }
}
