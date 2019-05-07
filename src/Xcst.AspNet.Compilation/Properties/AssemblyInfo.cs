using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.Compilation.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.AspNet.Compilation.dll")]
[assembly: AssemblyDescription("Xcst.AspNet.Compilation.dll")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]

namespace Xcst {

   partial class AssemblyInfo {

      public const string XcstPatch = "1";
   }
}
