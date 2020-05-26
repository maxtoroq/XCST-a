// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Helpers.AntiXsrf {

   // Represents the security token for the Anti-XSRF system.
   // The token is a random 128-bit value that correlates the session with the request body.

   sealed class AntiForgeryToken {

      internal const int SecurityTokenBitLength = 128;
      internal const int ClaimUidBitLength = 256;

      string? _additionalData;
      BinaryBlob? _securityToken;
      string? _username;

      public string AdditionalData {
         get => _additionalData ?? String.Empty;
         set => _additionalData = value;
      }

      public BinaryBlob? ClaimUid { get; set; }

      public bool IsSessionToken { get; set; }

      public BinaryBlob SecurityToken {
         get => _securityToken ?? (_securityToken = new BinaryBlob(SecurityTokenBitLength));
         set => _securityToken = value;
      }

      public string Username {
         get => _username ?? String.Empty;
         set { _username = value; }
      }
   }
}
