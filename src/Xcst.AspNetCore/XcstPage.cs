﻿// Copyright 2015 Max Toro Q.
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
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Xcst.Web;

// Many of the properties of XcstPage can be null if Context is not initialized.
// These are however not marked as nullable since, at runtime, Context is always initialized.

public abstract class XcstPage {

   HttpContext?
   _httpContext;

   IList<string>?
   _urlData;

   UrlHelper?
   _url;

   AntiforgeryHelper?
   _antiforgery;

#pragma warning disable CS8618
   public virtual string
   VirtualPath { get; set; }
#pragma warning restore CS8618

   public virtual string?
   PathInfo { get; set; }

   public virtual IList<string>
   UrlData {
      get => _urlData ??= new UrlDataList(PathInfo);
      set => _urlData = value;
   }

   public virtual HttpContext
   HttpContext {
#pragma warning disable CS8603
      get => _httpContext;
#pragma warning restore CS8603
      set {
         _httpContext = value;
         _urlData = null;
         _url = null;
      }
   }

#pragma warning disable CS8603
   public HttpRequest
   Request => HttpContext?.Request;

   public HttpResponse
   Response => HttpContext?.Response;

   public ISession
   Session => HttpContext?.Session;

   public IPrincipal
   User => HttpContext?.User;
#pragma warning restore CS8603

   public virtual bool
   IsPost => Request?.Method == "POST";

   public virtual bool
   IsAjax => Request?.IsAjaxRequest() ?? false;

   public virtual UrlHelper
   Url {
      get {
         if (_url is null
            && HttpContext != null) {

            _url = new UrlHelper(HttpContext);
         }
#pragma warning disable CS8603
         return _url;
#pragma warning restore CS8603
      }
      set => _url = value;
   }

   public AntiforgeryHelper
   Antiforgery =>
      _antiforgery ??= new AntiforgeryHelper(() => HttpContext);

   public virtual void
   Redirect(string url) {
      this.Response.Redirect(this.Url.Content(url));
   }

   public virtual void
   RedirectPermanent(string url) {
      this.Response.Redirect(this.Url.Content(url), permanent: true);
   }

   public async Task<bool>
   TryAuthorizeAsync(string? policy = null, string[]? roles = null) {

      var authorizeResult = await TryAuthorizeCoreAsync(policy, roles);

      if (authorizeResult.Challenged) {
         await this.HttpContext.ChallengeAsync();
         return false;
      }

      if (authorizeResult.Forbidden) {
         await this.HttpContext.ForbidAsync();
         return false;
      }

      return true;
   }

   public virtual async Task<PolicyAuthorizationResult>
   TryAuthorizeCoreAsync(string? policy = null, string[]? roles = null) {

      var httpContext = this.HttpContext;
      var user = this.User;

      if (user.Identity is null
         || !user.Identity.IsAuthenticated) {

         return PolicyAuthorizationResult.Challenge();
      }

      if (policy is null
         && (roles is null || roles.Length == 0)) {

         return PolicyAuthorizationResult.Success();
      }

      var policyProvider = httpContext.RequestServices.GetRequiredService<IAuthorizationPolicyProvider>();
      var policyEval = httpContext.RequestServices.GetRequiredService<IPolicyEvaluator>();

      var authorizeData = new AuthorizeData {
         Policy = policy,
         Roles = (roles != null) ? String.Join(',', roles) : null
      };

      var authorizationPolicy = await AuthorizationPolicy.CombineAsync(policyProvider, new[] { authorizeData });
      var authenticateResult = await policyEval.AuthenticateAsync(authorizationPolicy!, httpContext);
      var authorizeResult = await policyEval.AuthorizeAsync(authorizationPolicy!, authenticateResult, httpContext, this);

      return authorizeResult;
   }

   public virtual async Task
   RenderPageAsync() {

      if (this is IPageInit pInit) {

         await XcstEvaluator.Using(pInit)
            .CallFunction(async p => await p.Init())
            .Evaluate();

         return;
      }

      XcstEvaluator.Using((object)this)
         .CallInitialTemplate()
         .OutputTo(this.Response.BodyWriter.AsStream())
         .Run();
   }

   protected virtual void
   CopyState(XcstPage page) {
      page.HttpContext = this.HttpContext;
   }

   class AuthorizeData : IAuthorizeData {

      public string?
      Policy { get; set; }

      public string?
      Roles { get; set; }

      public string?
      AuthenticationSchemes { get; set; }
   }
}

public interface IPageInit : IXcstPackage {

   Task
   Init();
}
