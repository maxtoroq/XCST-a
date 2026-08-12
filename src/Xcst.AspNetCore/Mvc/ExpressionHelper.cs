// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Xcst.Web.Mvc;

public static class ExpressionHelper {

   public static string
   GetExpressionText(LambdaExpression expression) {

      ArgumentNullException.ThrowIfNull(expression);

      var lastSegment = default(string);
      var lastIsIndex = false;
      var builder = default(StringBuilder);

      var part = expression.Body;

      while (part != null) {

         string segment;
         bool isIndex;

         if (part.NodeType == ExpressionType.Call) {

            var methodExpression = (MethodCallExpression)part;

            if (!IsSingleArgumentIndexer(methodExpression)) {
               break;
            }

            segment = GetIndexerInvocation(
               methodExpression.Arguments.Single(),
               expression);

            isIndex = true;
            part = methodExpression.Object;

         } else if (part.NodeType == ExpressionType.ArrayIndex) {

            var binaryExpression = (BinaryExpression)part;

            segment = GetIndexerInvocation(
               binaryExpression.Right,
               expression);

            isIndex = true;
            part = binaryExpression.Left;

         } else if (part.NodeType == ExpressionType.MemberAccess) {

            var memberExpressionPart = (MemberExpression)part;

            segment = memberExpressionPart.Member.Name;
            isIndex = false;
            part = memberExpressionPart.Expression;

         } else {
            break;
         }

         if (lastSegment != null) {

            if (builder is null) {
               builder = new StringBuilder(
                  lastSegment.Length
                  + (lastIsIndex ? 0 : 1)
                  + segment.Length);
               builder.Append(lastSegment);
            }

            if (!lastIsIndex) {
               builder.Insert(0, '.');
            }

            builder.Insert(0, segment);
         }

         lastSegment = segment;
         lastIsIndex = isIndex;
      }

      if (lastSegment != null) {

         if (builder != null) {
            return builder.ToString();
         }

         return lastSegment;
      }

      return String.Empty;
   }

   static string
   GetIndexerInvocation(Expression expression, LambdaExpression parentExpression) {

      var converted = Expression.Convert(expression, typeof(object));
      var fakeParameter = Expression.Parameter(typeof(object), null);
      var lambda = Expression.Lambda<Func<object, object>>(converted, fakeParameter);
      Func<object, object> func;

      try {
         func = CachedExpressionCompiler.Process(lambda)
            ?? lambda.Compile();

      } catch (InvalidOperationException ex) {

         var p0 = parentExpression.Parameters[0];

         throw new InvalidOperationException(
            $"The expression compiler was unable to evaluate the indexer expression '{expression}' because it references the model parameter '{p0.Name}' which is unavailable.",
            ex);
      }

      return String.Create(CultureInfo.InvariantCulture, $"[{func.Invoke(null!)}]");
   }

   internal static bool
   IsSingleArgumentIndexer(Expression expression) {

      if (expression is MethodCallExpression { Arguments.Count: 1 } methodExpression) {

         return methodExpression.Method
            .DeclaringType!
            .GetDefaultMembers()
            .OfType<PropertyInfo>()
            .Any(p => p.GetGetMethod() == methodExpression.Method);
      }

      return false;
   }
}
