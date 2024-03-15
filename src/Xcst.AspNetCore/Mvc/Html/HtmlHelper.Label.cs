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

#region HtmlHelper is based on code from ASP.NET Web Stack
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.
#endregion

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   /// <exclude/>
   public IDisposable
   Label(XcstWriter output, string expression, bool hasDefaultText = false, string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromStringExpression(expression, this.ViewData);

      return GenerateLabel(output, modelExplorer, expression, hasDefaultText, @class);
   }

   /// <exclude/>
   public IDisposable
   LabelForModel(XcstWriter output, bool hasDefaultText = false, string? @class = null) =>
      GenerateLabel(output, this.ViewData.ModelExplorer, String.Empty, hasDefaultText, @class);

   protected internal IDisposable
   GenerateLabel(XcstWriter output, ModelExplorer modelExplorer, string expression, bool hasDefaultText, string? @class) {

      var htmlFieldName = expression;
      var fullFieldName = this.ViewData.TemplateInfo.GetFullHtmlFieldName(htmlFieldName);
      var id = TagBuilder.CreateSanitizedId(fullFieldName);

      output.WriteStartElement("label");
      output.WriteAttributeString("for", id);
      WriteCssClass(@class, null, output);

      if (!hasDefaultText) {

         var metadata = modelExplorer.Metadata;

         var resolvedLabelText = metadata.DisplayName
            ?? metadata.PropertyName
            ?? htmlFieldName.Split('.').Last();

         output.WriteString(resolvedLabelText);
      }

      return new ElementEndingDisposable(output);
   }
}

partial class HtmlHelper<TModel> {

   /// <exclude/>
   public IDisposable
   LabelFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, bool hasDefaultText = false,
         string? @class = null) {

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return GenerateLabel(output, modelExplorer, expressionString, hasDefaultText, @class);
   }
}