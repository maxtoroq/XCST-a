// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Helpers.AntiXsrf {

   // Abstracts out the serialization process for an anti-forgery token

   interface IAntiForgeryTokenSerializer {
      AntiForgeryToken Deserialize(string serializedToken);
      AntiForgeryToken Deserialize(string serializedToken, bool throwOnError);
      string Serialize(AntiForgeryToken token);
   }
}
