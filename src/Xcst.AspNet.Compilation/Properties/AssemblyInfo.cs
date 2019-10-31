using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.Compilation.PreApplicationStartCode;

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]

namespace Xcst.Web {

   partial class AssemblyInfo {

      public const string XcstPatch = "0";
   }
}
