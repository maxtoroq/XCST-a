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
using Xcst.Web.Mvc;

namespace Xcst.Web.Configuration;

public sealed class XcstWebConfiguration {

   public static XcstWebConfiguration
   Instance { get; } = new();

   public EditorTemplatesConfiguration
   EditorTemplates { get; } = new();

   public DisplayTemplatesConfiguration
   DisplayTemplates { get; } = new();

   private
   XcstWebConfiguration() { }
}

public class EditorTemplatesConfiguration {

   public Func<string, ViewContext, XcstViewPage?>?
   TemplateFactory { get; set; }

   public Func<EditorInfo, string?, string?>?
   EditorCssClass { get; set; }
}

public class DisplayTemplatesConfiguration {

   public Func<string, ViewContext, XcstViewPage?>?
   TemplateFactory { get; set; }
}
