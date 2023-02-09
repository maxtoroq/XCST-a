// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xcst.Web.Mvc.ExpressionUtil;

namespace Xcst.Web.Mvc;

public static class ExpressionHelper {

   public static string
   GetExpressionText(LambdaExpression expression) {

      // Split apart the expression string for property/field accessors to create its name

      var nameParts = new Stack<string>();
      var part = expression.Body;

      while (part != null) {

         if (part.NodeType == ExpressionType.Call) {

            var methodExpression = (MethodCallExpression)part;

            if (!IsSingleArgumentIndexer(methodExpression)) {
               break;
            }

            nameParts.Push(
               GetIndexerInvocation(
                  methodExpression.Arguments.Single(),
                  expression.Parameters.ToArray()));

            part = methodExpression.Object;

         } else if (part.NodeType == ExpressionType.ArrayIndex) {

            var binaryExpression = (BinaryExpression)part;

            nameParts.Push(
               GetIndexerInvocation(
                  binaryExpression.Right,
                  expression.Parameters.ToArray()));

            part = binaryExpression.Left;

         } else if (part.NodeType == ExpressionType.MemberAccess) {

            var memberExpressionPart = (MemberExpression)part;
            nameParts.Push("." + memberExpressionPart.Member.Name);
            part = memberExpressionPart.Expression;

         } else if (part.NodeType == ExpressionType.Parameter) {

            // Dev10 Bug #907611
            // When the expression is parameter based (m => m.Something...), we'll push an empty
            // string onto the stack and stop evaluating. The extra empty string makes sure that
            // we don't accidentally cut off too much of m => m.Model.

            nameParts.Push(String.Empty);
            part = null;

         } else {
            break;
         }
      }

      if (nameParts.Count > 0) {
         return nameParts.Aggregate((left, right) => left + right).TrimStart('.');
      }

      return String.Empty;
   }

   static string
   GetIndexerInvocation(Expression expression, ParameterExpression[] parameters) {

      var converted = Expression.Convert(expression, typeof(object));
      var fakeParameter = Expression.Parameter(typeof(object), null);
      var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
      Func<object?, object> func;

      try {
         func = CachedExpressionCompiler.Process(lambda);

      } catch (InvalidOperationException ex) {

         throw new InvalidOperationException(
            $"The expression compiler was unable to evaluate the indexer expression '{expression}' because it references the model parameter '{parameters[0].Name}' which is unavailable.",
            ex);
      }

      return "[" + Convert.ToString(func(null), CultureInfo.InvariantCulture) + "]";
   }

   internal static bool
   IsSingleArgumentIndexer(Expression expression) {

      if (expression is MethodCallExpression methodExpression
         && methodExpression.Arguments.Count == 1) {

         return methodExpression.Method
            .DeclaringType
            .GetDefaultMembers()
            .OfType<PropertyInfo>()
            .Any(p => p.GetGetMethod() == methodExpression.Method);
      }

      return false;
   }
}
