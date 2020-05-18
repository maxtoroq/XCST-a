using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using Xcst.Web;
using Xcst.Web.Runtime;

namespace AspNetPrecompiled.Infrastructure {

   [AttributeUsage(AttributeTargets.Assembly)]
   public sealed class PrecompiledModuleAttribute : Attribute { }

   [AttributeUsage(AttributeTargets.Class)]
   public sealed class VirtualPathAttribute : Attribute {

      public string VirtualPath { get; }

      public VirtualPathAttribute(string virtualPath) {
         this.VirtualPath = virtualPath;
      }
   }

   public class ExtensionlessUrlModule : IHttpModule {

      static readonly object _hasBeenRegisteredKey = new object();
      static readonly Dictionary<string, Type> _pageMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

      public static void InitializePageMap() {

         var pairs = FindPages();

         foreach (KeyValuePair<string, Type> item in pairs) {

            if (_pageMap.ContainsKey(item.Key)) {
               throw new InvalidOperationException($"Ambiguous path '{item.Key}', is used by '{_pageMap[item.Key].AssemblyQualifiedName}' and '{item.Value.AssemblyQualifiedName}'.");
            }

            _pageMap.Add(item.Key, item.Value);
         }
      }

      static IEnumerable<KeyValuePair<string, Type>> FindPages() =>
         from a in BuildManager.GetReferencedAssemblies().Cast<Assembly>()
         where a.IsDefined(typeof(PrecompiledModuleAttribute))
         from t in a.GetTypes()
         where t.IsDefined(typeof(VirtualPathAttribute))
         select new KeyValuePair<string, Type>(
            t.GetCustomAttribute<VirtualPathAttribute>().VirtualPath,
            t);

      static bool PageExists(string pagePath) =>
         _pageMap.ContainsKey(pagePath);

      static Type PageType(string pagePath) =>
         _pageMap[pagePath];

      public void Dispose() { }

      public void Init(HttpApplication application) {

         if (application.Context.Items[_hasBeenRegisteredKey] is null) {
            application.Context.Items[_hasBeenRegisteredKey] = true;
            application.PostResolveRequestCache += OnApplicationPostResolveRequestCache;
         }
      }

      static void OnApplicationPostResolveRequestCache(object sender, EventArgs e) {

         HttpContext context = ((HttpApplication)sender).Context;
         HttpRequest request = context.Request;

         // Parse incoming URL (we trim off the first two chars since they're always "~/")

         string requestPath = request.AppRelativeCurrentExecutionFilePath.Substring(2) + request.PathInfo;

         if (MatchRequest(requestPath, out string pagePath, out string pathInfo)) {

            var page = (XcstPage)Activator.CreateInstance(PageType(pagePath));
            page.VirtualPath = "~/" + pagePath;
            page.PathInfo = pathInfo;

            if (page is ISessionStateAware sess) {
               context.SetSessionStateBehavior(sess.SessionStateBehavior);
            }

            IHttpHandler handler = page.CreateHttpHandler();

            context.RemapHandler(handler);

         } else {
            // let the next module try to find it
         }
      }

      static bool MatchRequest(string requestPath, out string pagePath, out string pathInfo) {

         Debug.Assert(requestPath != null);
         Debug.Assert(!requestPath.StartsWith("~/"));

         // We can skip the file exists check and normal lookup for empty paths, but we still need to look for default pages

         if (!String.IsNullOrEmpty(requestPath)) {

            // For each trimmed part of the path try to add a known extension and
            // check if it matches a file in the application.

            string currentLevel = requestPath;
            string currentPathInfo = String.Empty;

            while (true) {

               // Does the current route level patch any supported extension?

               if (PageExists(currentLevel)) {

                  pagePath = currentLevel;
                  pathInfo = currentPathInfo;
                  return true;
               }

               // Try to remove the last path segment (e.g. go from /foo/bar to /foo)

               int indexOfLastSlash = currentLevel.LastIndexOf('/');

               if (indexOfLastSlash == -1) {

                  // If there are no more slashes, we're done

                  break;

               } else {

                  // Chop off the last path segment to get to the next one

                  currentLevel = currentLevel.Substring(0, indexOfLastSlash);

                  // And save the path info in case there is a match

                  currentPathInfo = requestPath.Substring(indexOfLastSlash + 1);
               }
            }
         }

         // If we haven't found anything yet, now try looking for index.* at the current url

         if (MatchDefaultFile(requestPath, out pagePath)) {

            pathInfo = String.Empty;
            return true;
         }

         pagePath = null;
         pathInfo = null;
         return false;
      }

      static bool MatchDefaultFile(string requestPath, out string pagePath) {

         const string defaultDocument = "index";

         string currentLevel = requestPath;
         string currentLevelIndex;

         if (String.IsNullOrEmpty(currentLevel)) {
            currentLevelIndex = defaultDocument;
         } else {

            if (currentLevel[currentLevel.Length - 1] != '/') {
               currentLevel += "/";
            }

            currentLevelIndex = currentLevel + defaultDocument;
         }

         if (PageExists(currentLevelIndex)) {

            pagePath = currentLevelIndex;
            return true;
         }

         pagePath = null;
         return false;
      }
   }

   public static class LinkToHelper {

      public static string LinkTo(string path, params object[] pathParts) =>
         UrlUtil.GenerateClientUrl(null, path, pathParts);

      public static string LinkToDefault(string path, string defaultPath, params object[] pathParts) {

         if (pathParts is null
            || pathParts.Length == 0
            || pathParts.All(p => p is null || !IsDisplayableType(p.GetType()))) {

            return LinkTo(defaultPath, pathParts);
         }

         return LinkTo(path, pathParts);
      }

      // see System.Web.Mvc.UrlUtil
      static bool IsDisplayableType(Type t) =>
         t.GetInterfaces().Length != 0;
   }
}
