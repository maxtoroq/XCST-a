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
using System.Globalization;
using System.Linq.Expressions;

namespace Xcst.Web.Mvc;

partial class HtmlHelper {

   /// <exclude/>
   public IDisposable
   Radio(XcstWriter output, string name, object value, string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      var isChecked = RadioValueEquals(value, EvalString(name));

      return GenerateRadio(output, modelExplorer: null, name, value, isChecked, @class);
   }

   /// <exclude/>
   public IDisposable
   Radio(XcstWriter output, string name, object value, bool isChecked, string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      return GenerateRadio(output, modelExplorer: null, name, value, isChecked, @class);
   }

   /// <exclude/>
   public IDisposable
   RadioForModel(XcstWriter output, object value, string? @class = null) =>
      RadioForModelExplorer(output, this.ViewData.ModelExplorer, String.Empty, value, isChecked: null, @class);

   /// <exclude/>
   public IDisposable
   RadioForModel(XcstWriter output, object value, bool isChecked, string? @class = null) =>
      RadioForModelExplorer(output, this.ViewData.ModelExplorer, String.Empty, value, isChecked, @class);

   internal IDisposable
   RadioForModelExplorer(XcstWriter output, ModelExplorer? modelExplorer, string expression, object value,
         bool? isChecked, string? @class) {

      var model = modelExplorer?.Model;

      if (isChecked is null
         && model != null) {

         isChecked = RadioValueEquals(value, model.ToString());
      }

      return GenerateRadio(output, modelExplorer, expression, value, isChecked, @class);
   }

   protected IDisposable
   GenerateRadio(XcstWriter output, ModelExplorer? modelExplorer, string name, object value,
         bool? isChecked, string? @class) {

      return GenerateInput(
         output,
         InputType.Radio,
         type: null,
         modelExplorer,
         name,
         value,
         useViewData: false,
         isChecked: isChecked.GetValueOrDefault(),
         setId: true,
         isExplicitValue: true,
         format: null,
         @class);
   }

   static bool
   RadioValueEquals(object value, string viewDataValue) {

      var valueString = Convert.ToString(value, CultureInfo.CurrentCulture);

      return String.Equals(viewDataValue, valueString, StringComparison.OrdinalIgnoreCase);
   }
}

partial class HtmlHelper<TModel> {

   /// <exclude/>
   public IDisposable
   RadioFor<TResult>(XcstWriter output, Expression<Func<TModel, TResult>> expression, object value,
         string? @class = null) {

      if (value is null) throw new ArgumentNullException(nameof(value));

      var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, this.ViewData);
      var expressionString = ExpressionHelper.GetExpressionText(expression);

      return RadioForModelExplorer(output, modelExplorer, expressionString, value, isChecked: null, @class);
   }
}
