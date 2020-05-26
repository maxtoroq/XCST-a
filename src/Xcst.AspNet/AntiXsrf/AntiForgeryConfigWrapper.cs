// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Helpers.AntiXsrf {

   sealed class AntiForgeryConfigWrapper : IAntiForgeryConfig {

      public IAntiForgeryAdditionalDataProvider? AdditionalDataProvider => AntiForgeryConfig.AdditionalDataProvider;

      public string CookieName => AntiForgeryConfig.CookieName;

      public string FormFieldName => AntiForgeryConfig.AntiForgeryTokenFieldName;

      public bool RequireSSL => AntiForgeryConfig.RequireSsl;

      public bool SuppressIdentityHeuristicChecks => AntiForgeryConfig.SuppressIdentityHeuristicChecks;

      public string UniqueClaimTypeIdentifier => AntiForgeryConfig.UniqueClaimTypeIdentifier;

      public bool SuppressXFrameOptionsHeader => AntiForgeryConfig.SuppressXFrameOptionsHeader;
   }
}
