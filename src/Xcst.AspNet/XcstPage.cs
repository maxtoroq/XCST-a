// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.SessionState;

namespace Xcst.Web {

   // Many of the properties of XcstPage can be null if Context is not initialized.
   // These are however not marked as nullable since, at runtime, Context is always initialized.

   public abstract class XcstPage {

      HttpContextBase? _Context;
      HttpRequestBase? _Request;
      HttpResponseBase? _Response;
      HttpSessionStateBase? _Session;

#if !ASPNETMVC
      IList<string>? _UrlData;
#endif
      IPrincipal? _User;

#pragma warning disable CS8618
      public virtual string VirtualPath { get; set; }
#pragma warning restore CS8618

#if !ASPNETMVC
      public virtual string? PathInfo { get; set; }
#endif

      // HttpContextWrapper Request/Response/Session return a new instance every time
      // need to cache result

      public virtual HttpContextBase Context {
#pragma warning disable CS8603
         get => _Context;
#pragma warning restore CS8603
         set {
            _Context = value;
            _Request = null;
            _Response = null;
            _Session = null;
#if !ASPNETMVC
            _UrlData = null;
#endif
         }
      }

#pragma warning disable CS8603
      public HttpRequestBase Request =>
         _Request ?? (_Request = Context?.Request);

      public HttpResponseBase Response =>
         _Response ?? (_Response = Context?.Response);

      public HttpSessionStateBase Session =>
         _Session ?? (_Session = Context?.Session);
#pragma warning restore CS8603

#if !ASPNETMVC
      public virtual IList<string> UrlData {
         get => _UrlData
            ?? (_UrlData = new UrlDataList(PathInfo ?? Request?.PathInfo?.TrimStart('/')));
         set => _UrlData = value;
      }
#endif

      public virtual IPrincipal User {
#pragma warning disable CS8603
         get => _User ?? (_User = Context?.User);
#pragma warning restore CS8603
         set => _User = value;
      }

      public virtual bool IsPost => Request?.HttpMethod == "POST";

      public virtual bool IsAjax => Request?.IsAjaxRequest() ?? false;

#if !ASPNETMVC
      public virtual IHttpHandler CreateHttpHandler() =>
         new XcstPageHandler(this);

      public virtual bool TryAuthorize(string[]? users = null, string[]? roles = null) {

         if (IsAuthorized(this.User, users, roles)) {

            // see System.Web.Mvc.AuthorizeAttribute

            HttpCachePolicyBase cachePolicy = this.Response.Cache;
            cachePolicy.SetProxyMaxAge(new TimeSpan(0));
            cachePolicy.AddValidationCallback(CacheValidateHandler, new object?[2] { users, roles });

            return true;

         } else {

            this.Response.StatusCode = 401;
            return false;
         }
      }

      void CacheValidateHandler(HttpContext context, object data, ref HttpValidationStatus validationStatus) {

         object?[]? dataArr = data as object?[];

         bool isAuthorized = IsAuthorized(context.User, dataArr?[0] as string[], dataArr?[1] as string[]);

         validationStatus = (isAuthorized) ?
            HttpValidationStatus.Valid
            : HttpValidationStatus.IgnoreThisRequest;
      }

      static bool IsAuthorized(IPrincipal user, string[]? users, string[]? roles) {

         if (user is null
            || !user.Identity.IsAuthenticated) {

            return false;
         }

         if (users != null
            && users.Length > 0
            && !users.Contains(user.Identity.Name, StringComparer.OrdinalIgnoreCase)) {

            return false;
         }

         if (roles != null
            && roles.Length > 0
            && !roles.Any(user.IsInRole)) {

            return false;
         }

         return true;
      }
#endif

      protected XcstPage LoadPage(string path) {

         if (path is null) throw new ArgumentNullException(nameof(path));

         string absolutePath = VirtualPathUtility.Combine(this.VirtualPath, path);

         Type pageType = BuildManager.GetCompiledType(absolutePath)
            ?? throw new ArgumentException($"A page at '{absolutePath}' was not found.", nameof(path));

         XcstPage page = Activator.CreateInstance(pageType) as XcstPage
            ?? throw new ArgumentException($"The page at '{absolutePath}' must derive from {nameof(XcstPage)}.", nameof(path));

         page.VirtualPath = absolutePath;

         CopyState(page);

         return page;
      }

      protected virtual void CopyState(XcstPage page) {
         page.Context = this.Context;
      }
   }

   public interface IFileDependent {
      string[] FileDependencies { get; }
   }

#if !ASPNETMVC
   public interface ISessionStateAware {
      SessionStateBehavior SessionStateBehavior { get; }
   }
#endif
}
