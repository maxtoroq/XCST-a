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

   object?
   _formattedModelValue;

   IList<string>?
   _membersNames;

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

   public IDictionary<string, object?>?
   HtmlAttributes { get; set; }

   [AllowNull]
   public IList<string>
   MembersNames {
      get => _membersNames ?? Array.Empty<string>();
      set => _membersNames = value;
   }

   public IDictionary<string, IEnumerable<SelectListItem>>?
   MembersOptions { get; set; }

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

      if (partialFieldName.StartsWith("[", StringComparison.Ordinal)) {

         // See Codeplex #544 - the partialFieldName might represent an indexer access, in which case combining
         // with a 'dot' would be invalid.

         return this.HtmlFieldPrefix + partialFieldName;
      }

      return this.HtmlFieldPrefix + "." + partialFieldName;
   }

   public bool
   Visited(ModelExplorer modelExplorer) =>
      this.VisitedObjects.Contains(modelExplorer.Model ?? modelExplorer.Metadata.ModelType);

   public void
   InheritParentState(TemplateInfo parentInfo) {
      InheritHtmlAttributes(parentInfo);
      InheritMembersOptions(parentInfo);
      InheritMemberTemplate(parentInfo);
      InheritTemplateParameters(parentInfo);
   }

   internal void
   InheritVisitedObjects(TemplateInfo parentInfo) {

      var parentObjects = parentInfo._visitedObjects;

      if (parentObjects is null or { Count: 0 }) {
         return;
      }

      if (_visitedObjects is null) {
         _visitedObjects = new HashSet<object>(parentObjects);
         return;
      }

      foreach (var item in parentObjects) {
         _visitedObjects.Add(item);
      }
   }

   void
   InheritHtmlAttributes(TemplateInfo parentInfo) {

      var parentAttribs = parentInfo.HtmlAttributes;

      if (parentAttribs is null) {
         return;
      }

      if (this.HtmlAttributes is null) {
         this.HtmlAttributes = HtmlHelper.AnonymousObjectToHtmlAttributes(parentAttribs);
         return;
      }

      var dict = this.HtmlAttributes as HtmlAttributeDictionary
         ?? new HtmlAttributeDictionary(this.HtmlAttributes);

      foreach (var pair in parentAttribs) {

         if (dict.Comparer.Equals(pair.Key, "class")) {

            dict.AddClass(pair.Value);
            continue;
         }

         if (!dict.ContainsKey(pair.Key)) {
            dict[pair.Key] = pair.Value;
         }
      }

      this.HtmlAttributes = dict;
   }

   void
   InheritMembersOptions(TemplateInfo parentInfo) {

      var parentOptions = parentInfo.MembersOptions;

      if (parentOptions is null) {
         return;
      }

      if (this.MembersOptions is null) {
         this.MembersOptions = new Dictionary<string, IEnumerable<SelectListItem>>(parentOptions);
         return;
      }

      foreach (var pair in parentOptions) {
         if (!this.MembersOptions.ContainsKey(pair.Key)) {
            this.MembersOptions[pair.Key] = pair.Value;
         }
      }
   }

   void
   InheritMemberTemplate(TemplateInfo parentInfo) {
      this.MemberTemplate ??= parentInfo.MemberTemplate;
   }

   void
   InheritTemplateParameters(TemplateInfo parentInfo) {

      var parentParams = parentInfo._templateParameters;

      if (parentParams is null) {
         return;
      }

      foreach (var pair in parentParams) {
         if (!this.TemplateParameters.ContainsKey(pair.Key)) {
            this.TemplateParameters[pair.Key] = pair.Value;
         }
      }
   }

   public IEnumerable<SelectListItem>?
   OptionsForModel() {

      if (this.MembersOptions?.TryGetValue(this.HtmlFieldPrefix, out var value) == true) {
         return value;
      }

      return null;
   }
}
