using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics {

   static class Assert {

      [Conditional("DEBUG")]
      public static void IsNotNull(object? value) =>
         Debug.Assert(value != null);

      [Conditional("DEBUG")]
      public static void That([DoesNotReturnIf(false)] bool condition, string message) =>
         Debug.Assert(condition, message);
   }
}
