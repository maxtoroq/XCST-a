
namespace System.Diagnostics.CodeAnalysis {

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
   sealed class AllowNullAttribute : Attribute { }

   [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
   sealed class DoesNotReturnIfAttribute : Attribute {

      public bool ParameterValue { get; }

      public DoesNotReturnIfAttribute(bool parameterValue) {
         this.ParameterValue = parameterValue;
      }
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
   sealed class MaybeNullAttribute : Attribute { }

   [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
   sealed class MaybeNullWhenAttribute : Attribute {

      public bool ReturnValue { get; }

      public MaybeNullWhenAttribute(bool returnValue) {
         this.ReturnValue = returnValue;
      }
   }

   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
   sealed class NotNullIfNotNullAttribute : Attribute {

      public string ParameterName { get; }

      public NotNullIfNotNullAttribute(string parameterName) {
         this.ParameterName = parameterName;
      }
   }

   [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
   sealed class NotNullWhenAttribute : Attribute {

      public bool ReturnValue { get; }

      public NotNullWhenAttribute(bool returnValue) {
         this.ReturnValue = returnValue;
      }
   }
}
