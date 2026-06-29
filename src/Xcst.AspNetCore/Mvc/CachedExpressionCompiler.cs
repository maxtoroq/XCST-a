// Copyright 2026 Max Toro Q.
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
using System.Linq.Expressions;
using System.Reflection;

namespace Xcst.Web.Mvc;

static class CachedExpressionCompiler {

   static class ImplAccessor<TModel, TResult> {

      static readonly Func<Expression<Func<TModel, TResult>>, Func<TModel, object>?>
      _implDel;

      static
      ImplAccessor() {

         _implDel = _implMethod
            .MakeGenericMethod(typeof(TModel), typeof(TResult))
            .CreateDelegate<Func<Expression<Func<TModel, TResult>>, Func<TModel, object>?>>();
      }

      public static Func<TModel, object>?
      Process(Expression<Func<TModel, TResult>> expression) =>
         _implDel.Invoke(expression);
   }

   static readonly MethodInfo
   _implMethod;

   static
   CachedExpressionCompiler() {

      var type = Type.GetType("Microsoft.AspNetCore.Mvc.ViewFeatures.CachedExpressionCompiler, Microsoft.AspNetCore.Mvc.ViewFeatures", throwOnError: true)!;
      _implMethod = type.GetMethod("Process")!;
   }

   public static Func<TModel, object>?
   Process<TModel, TResult>(Expression<Func<TModel, TResult>> expression) =>
      ImplAccessor<TModel, TResult>.Process(expression);
}
