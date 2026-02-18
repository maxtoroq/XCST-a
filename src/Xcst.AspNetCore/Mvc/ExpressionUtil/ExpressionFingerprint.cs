// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Xcst.Web.Mvc.ExpressionUtil;

// Serves as the base class for all expression fingerprints. Provides a default implementation
// of GetHashCode().

abstract class ExpressionFingerprint {

   // the type of expression node, e.g. OP_ADD, MEMBER_ACCESS, etc.

   public ExpressionType
   NodeType { get; private set; }

   // the CLR type resulting from this expression, e.g. int, string, etc.

   public Type
   Type { get; private set; }

   protected
   ExpressionFingerprint(ExpressionType nodeType, Type type) {

      this.NodeType = nodeType;
      this.Type = type;
   }

   internal virtual void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddInt32((int)this.NodeType);
      combiner.AddObject(this.Type);
   }

   protected bool
   Equals(ExpressionFingerprint? other) =>
      (other != null)
         && (this.NodeType == other.NodeType)
         && Equals(this.Type, other.Type);

   public override bool
   Equals(object? obj) =>
      Equals(obj as ExpressionFingerprint);

   public override int
   GetHashCode() {

      var combiner = new HashCodeCombiner();
      AddToHashCodeCombiner(combiner);

      return combiner.CombinedHash;
   }
}

#pragma warning disable 659 // overrides AddToHashCodeCombiner instead

// BinaryExpression fingerprint class
// Useful for things like array[index]

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class BinaryExpressionFingerprint : ExpressionFingerprint {

   public
   BinaryExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
      : base(nodeType, type) {

      // Other properties on BinaryExpression (like IsLifted / IsLiftedToNull) are simply derived
      // from Type and NodeType, so they're not necessary for inclusion in the fingerprint.

      this.Method = method;
   }

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.binaryexpression.method.aspx

   public MethodInfo
   Method { get; private set; }

   public override bool
   Equals(object? obj) =>
      obj is BinaryExpressionFingerprint other
         && Equals(this.Method, other.Method)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {

      combiner.AddObject(this.Method);

      base.AddToHashCodeCombiner(combiner);
   }
}

// ConditionalExpression fingerprint class
// Expression of form (test) ? ifTrue : ifFalse

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class ConditionalExpressionFingerprint : ExpressionFingerprint {

   public
   ConditionalExpressionFingerprint(ExpressionType nodeType, Type type)
      : base(nodeType, type) {

      // There are no properties on ConditionalExpression that are worth including in
      // the fingerprint.
   }

   public override bool
   Equals(object? obj) =>
      obj is ConditionalExpressionFingerprint other
         && this.Equals(other);
}

// ConstantExpression fingerprint class
//
// A ConstantExpression might represent a captured local variable, so we can't compile
// the value directly into the cached function. Instead, a placeholder is generated
// and the value is hoisted into a local variables array. This placeholder can then
// be compiled and cached, and the array lookup happens at runtime.

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class ConstantExpressionFingerprint : ExpressionFingerprint {

   public
   ConstantExpressionFingerprint(ExpressionType nodeType, Type type)
      : base(nodeType, type) {

      // There are no properties on ConstantExpression that are worth including in
      // the fingerprint.
   }

   public override bool
   Equals(object? obj) =>
      obj is ConstantExpressionFingerprint other
         && this.Equals(other);
}

// DefaultExpression fingerprint class
// Expression of form default(T)

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class DefaultExpressionFingerprint : ExpressionFingerprint {

   public
   DefaultExpressionFingerprint(ExpressionType nodeType, Type type)
      : base(nodeType, type) {

      // There are no properties on DefaultExpression that are worth including in
      // the fingerprint.
   }

   public override bool
   Equals(object? obj) =>
      obj is DefaultExpressionFingerprint other
         && this.Equals(other);
}

// IndexExpression fingerprint class
// Represents certain forms of array access or indexer property access

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class IndexExpressionFingerprint : ExpressionFingerprint {

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.indexexpression.indexer.aspx

   public PropertyInfo
   Indexer { get; private set; }

   public
   IndexExpressionFingerprint(ExpressionType nodeType, Type type, PropertyInfo indexer)
      : base(nodeType, type) {

      // Other properties on IndexExpression (like the argument count) are simply derived
      // from Type and Indexer, so they're not necessary for inclusion in the fingerprint.

      this.Indexer = indexer;
   }

   public override bool
   Equals(object? obj) =>
      obj is IndexExpressionFingerprint other
         && Equals(this.Indexer, other.Indexer)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddObject(Indexer);
      base.AddToHashCodeCombiner(combiner);
   }
}

// LambdaExpression fingerprint class
// Represents a lambda expression (root element in Expression<T>)

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class LambdaExpressionFingerprint : ExpressionFingerprint {

   public
   LambdaExpressionFingerprint(ExpressionType nodeType, Type type)
      : base(nodeType, type) {

      // There are no properties on LambdaExpression that are worth including in
      // the fingerprint.
   }

   public override bool
   Equals(object? obj) =>
      obj is LambdaExpressionFingerprint other
         && this.Equals(other);
}

// MemberExpression fingerprint class
// Expression of form xxx.FieldOrProperty

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class MemberExpressionFingerprint : ExpressionFingerprint {

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.memberexpression.member.aspx

   public MemberInfo
   Member { get; private set; }

   public
   MemberExpressionFingerprint(ExpressionType nodeType, Type type, MemberInfo member)
      : base(nodeType, type) {

      this.Member = member;
   }

   public override bool
   Equals(object? obj) =>
      obj is MemberExpressionFingerprint other
         && Equals(this.Member, other.Member)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddObject(this.Member);
      base.AddToHashCodeCombiner(combiner);
   }
}

// MethodCallExpression fingerprint class
// Expression of form xxx.Foo(...), xxx[...] (get_Item()), etc.

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class MethodCallExpressionFingerprint : ExpressionFingerprint {

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.methodcallexpression.method.aspx

   public MethodInfo
   Method { get; private set; }

   public
   MethodCallExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
      : base(nodeType, type) {

      // Other properties on MethodCallExpression (like the argument count) are simply derived
      // from Type and Indexer, so they're not necessary for inclusion in the fingerprint.

      this.Method = method;
   }

   public override bool
   Equals(object? obj) =>
      obj is MethodCallExpressionFingerprint other
         && Equals(this.Method, other.Method)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddObject(this.Method);
      base.AddToHashCodeCombiner(combiner);
   }
}

// ParameterExpression fingerprint class
// Can represent the model parameter or an inner parameter in an open lambda expression

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class ParameterExpressionFingerprint : ExpressionFingerprint {

   // Parameter position within the overall expression, used to maintain alpha equivalence.

   public int
   ParameterIndex { get; private set; }

   public
   ParameterExpressionFingerprint(ExpressionType nodeType, Type type, int parameterIndex)
      : base(nodeType, type) {

      this.ParameterIndex = parameterIndex;
   }

   public override bool
   Equals(object? obj) =>
      obj is ParameterExpressionFingerprint other
         && (this.ParameterIndex == other.ParameterIndex)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddInt32(this.ParameterIndex);
      base.AddToHashCodeCombiner(combiner);
   }
}

// TypeBinary fingerprint class
// Expression of form "obj is T"

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class TypeBinaryExpressionFingerprint : ExpressionFingerprint {

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.typebinaryexpression.typeoperand.aspx

   public Type
   TypeOperand { get; private set; }

   public
   TypeBinaryExpressionFingerprint(ExpressionType nodeType, Type type, Type typeOperand)
      : base(nodeType, type) {

      this.TypeOperand = typeOperand;
   }

   public override bool
   Equals(object? obj) =>
      obj is TypeBinaryExpressionFingerprint other
         && Equals(this.TypeOperand, other.TypeOperand)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddObject(this.TypeOperand);
      base.AddToHashCodeCombiner(combiner);
   }
}

// UnaryExpression fingerprint class
// The most common appearance of a UnaryExpression is a cast or other conversion operator

[SuppressMessage("Microsoft.Usage", "CA2218:OverrideGetHashCodeOnOverridingEquals", Justification = "Overrides AddToHashCodeCombiner() instead.")]
sealed class UnaryExpressionFingerprint : ExpressionFingerprint {

   // http://msdn.microsoft.com/en-us/library/system.linq.expressions.unaryexpression.method.aspx

   public MethodInfo
   Method { get; private set; }

   public
   UnaryExpressionFingerprint(ExpressionType nodeType, Type type, MethodInfo method)
      : base(nodeType, type) {

      // Other properties on UnaryExpression (like IsLifted / IsLiftedToNull) are simply derived
      // from Type and NodeType, so they're not necessary for inclusion in the fingerprint.

      this.Method = method;
   }

   public override bool
   Equals(object? obj) =>
      obj is UnaryExpressionFingerprint other
         && Equals(this.Method, other.Method)
         && this.Equals(other);

   internal override void
   AddToHashCodeCombiner(HashCodeCombiner combiner) {
      combiner.AddObject(this.Method);
      base.AddToHashCodeCombiner(combiner);
   }
}
