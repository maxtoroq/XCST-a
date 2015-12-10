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

#region EditorExtensions is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Web.UI.WebControls;
using System.Web.Mvc;
using Xcst.Runtime;

namespace Xcst.Web.Mvc.Html {

   /// <exclude/>
   public static class EditorExtensions {

      public static Func<EditorInfo, string, string> EditorCssClassFunction { get; set; }

      public static bool OmitPasswordValue { get; set; }

      public static void Editor(this HtmlHelper html,
                                DynamicContext context,
                                string expression,
                                string templateName = null,
                                string htmlFieldName = null,
                                object additionalViewData = null) {

         TemplateHelpers.Template(html, context, expression, templateName, htmlFieldName, DataBoundControlMode.Edit, additionalViewData);
      }

      [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "This is an appropriate nesting of generic types")]
      public static void EditorFor<TModel, TValue>(this HtmlHelper<TModel> html,
                                                   DynamicContext context,
                                                   Expression<Func<TModel, TValue>> expression,
                                                   string templateName = null,
                                                   string htmlFieldName = null,
                                                   object additionalViewData = null) {

         TemplateHelpers.TemplateFor(html, context, expression, templateName, htmlFieldName, DataBoundControlMode.Edit, additionalViewData);
      }

      public static void EditorForModel(this HtmlHelper html,
                                        DynamicContext context,
                                        string templateName = null,
                                        string htmlFieldName = null,
                                        object additionalViewData = null) {

         TemplateHelpers.TemplateHelper(html, context, html.ViewData.ModelMetadata, htmlFieldName, templateName, DataBoundControlMode.Edit, additionalViewData);
      }
   }
}
