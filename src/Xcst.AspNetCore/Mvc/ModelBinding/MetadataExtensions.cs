// Copyright 2023 Max Toro Q.
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

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Xcst.Web.Mvc.ModelBinding;

public static class MetadataExtensions {

   public static string?
   GetGroupName(this ModelMetadata metadata) =>
      MetadataDetailsProvider.GetGroupName(metadata);

   public static string?
   GetShortDisplayName(this ModelMetadata metadata) =>
      MetadataDetailsProvider.GetShortName(metadata);
}
