using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.PreApplicationStartCode;

[assembly: AssemblyTitle("Xcst.AspNet.dll")]
[assembly: AssemblyDescription("Xcst.AspNet.dll")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]

namespace Xcst {

   partial class AssemblyInfo {

      public const string XcstPatch = "1";
   }
}
