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

using System.Web.Mvc;

namespace Xcst.Web.Configuration;

public class EditorInfo {

   public string
   TemplateName { get; }

   public string
   TagName { get; }

   public InputType
   InputType { get; }

   internal
   EditorInfo(string templateName, string tagName)
      : this(templateName, tagName, (System.Web.Mvc.InputType)(-1)) { }

   internal
   EditorInfo(string templateName, string tagName, InputType inputType) {

      this.TemplateName = templateName;
      this.TagName = tagName;
      this.InputType = inputType;
   }
}
