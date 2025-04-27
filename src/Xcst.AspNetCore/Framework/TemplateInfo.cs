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

#region TemplateInfo is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xcst.Runtime;

namespace Xcst.Web.Mvc;

public class TemplateInfo {

   string?
   _htmlFieldPrefix;

   IDictionary<string, object?>?
   _htmlAttributes;

   object?
   _formattedModelValue;

   IList<string>?
   _membersNames;

   IDictionary<string, IEnumerable<SelectListItem>>?
   _membersOptions;

   IDictionary<string, object?>?
   _templateParameters;

   HashSet<object>?
   _visitedObjects;

   [AllowNull]
   public object
   FormattedModelValue {
      get => _formattedModelValue ?? String.Empty;
      set => _formattedModelValue = value;
   }

   [AllowNull]
   public string
   HtmlFieldPrefix {
      get => _htmlFieldPrefix ?? String.Empty;
      set => _htmlFieldPrefix = value;
   }

   [AllowNull]
   public IDictionary<string, object?>
   HtmlAttributes {
      get => _htmlAttributes ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
      set => _htmlAttributes = value;
   }

   [AllowNull]
   public IList<string>
   MembersNames {
      get => _membersNames ?? Array.Empty<string>();
      internal set => _membersNames = value;
   }

   [AllowNull]
   public IDictionary<string, IEnumerable<SelectListItem>>
   MembersOptions {
      get => _membersOptions ??= new Dictionary<string, IEnumerable<SelectListItem>>();
      set => _membersOptions = value;
   }

   internal Action<HtmlHelper, ISequenceWriter<object?>>?
   MemberTemplate { get; set; }

   public IDictionary<string, object?>
   TemplateParameters =>
      _templateParameters ??= new Dictionary<string, object?>();

   public int
   TemplateDepth => VisitedObjects.Count;

   public string?
   TemplateName { get; internal set; }

   // DDB #224750 - Keep a collection of visited objects to prevent infinite recursion

   internal HashSet<object>
   VisitedObjects {
      get => _visitedObjects ??= new HashSet<object>();
      set => _visitedObjects = value;
   }

   public
   TemplateInfo() { }

   public
   TemplateInfo(TemplateInfo templateInfo) {

      ArgumentNullException.ThrowIfNull(templateInfo);

      _htmlFieldPrefix = templateInfo._htmlFieldPrefix;

      if (templateInfo._htmlAttributes is { } htmlAttribs and { Count: > 0 }) {
         _htmlAttributes = new CopyOnWriteDictionary<string, object?>(htmlAttribs, StringComparer.OrdinalIgnoreCase);
      }

      if (templateInfo._membersOptions is { } memberOpts and { Count: > 0 }) {
         _membersOptions = new CopyOnWriteDictionary<string, IEnumerable<SelectListItem>>(memberOpts, EqualityComparer<string>.Default);
      }

      this.MemberTemplate = templateInfo.MemberTemplate;

      if (templateInfo._templateParameters is { } templateParams and { Count: > 0 }) {
         _templateParameters = new CopyOnWriteDictionary<string, object?>(templateParams, EqualityComparer<string>.Default);
      }
   }

   internal
   TemplateInfo(TemplateInfo templateInfo, bool inheritVisitedObjects)
      : this(templateInfo) {

      if (inheritVisitedObjects) {

         ArgumentNullException.ThrowIfNull(templateInfo);

         if (templateInfo._visitedObjects is { } visitedObjs and { Count: > 0 }) {
            _visitedObjects = new HashSet<object>(visitedObjs);
         }
      }
   }

   public string
   GetFullHtmlFieldId(string? partialFieldName) =>
      HtmlHelper.GenerateIdFromName(GetFullHtmlFieldName(partialFieldName));

   public string
   GetFullHtmlFieldName(string? partialFieldName) {

      if (String.IsNullOrEmpty(partialFieldName)) {
         return this.HtmlFieldPrefix;
      }

      if (String.IsNullOrEmpty(this.HtmlFieldPrefix)) {
         return partialFieldName;
      }

      if (partialFieldName.StartsWith('[')) {

         // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
         // with a 'dot' would be invalid.

         return this.HtmlFieldPrefix + partialFieldName;
      }

      return String.Concat(this.HtmlFieldPrefix, ".", partialFieldName);
   }

   public bool
   Visited(ModelExplorer modelExplorer) =>
      this.VisitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);

   public void
   MergeHtmlAttributes(object? htmlAttributes) {

      if (htmlAttributes is null) {
         return;
      }

      var dict = htmlAttributes as IDictionary<string, object?>
         ?? HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes);

      foreach (var kvp in dict) {

         if (StringComparer.OrdinalIgnoreCase.Equals(kvp.Key, "class")) {
            HtmlAttributeDictionary.AddClass(this.HtmlAttributes, kvp.Value);
            continue;
         }

         this.HtmlAttributes[kvp.Key] = kvp.Value;
      }
   }

   public IEnumerable<SelectListItem>?
   OptionsForModel() {

      if (_membersOptions?.TryGetValue(this.HtmlFieldPrefix, out var value) == true) {
         return value;
      }

      return null;
   }
}
