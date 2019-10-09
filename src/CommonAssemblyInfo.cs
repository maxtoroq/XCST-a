using System;
using System.Reflection;
using static Xcst.Web.AssemblyInfo;

[assembly: AssemblyProduct("XCST")]
[assembly: AssemblyCompany("http://maxtoroq.github.io/XCST/")]
[assembly: AssemblyCopyright("2015-2019 Max Toro Q.")]
[assembly: AssemblyVersion(XcstAssemblyVersion)]
[assembly: AssemblyFileVersion(XcstAssemblyFileVersion)]
[assembly: AssemblyInformationalVersion(XcstAssemblyInformationalVersion)]

namespace Xcst.Web {

   partial class AssemblyInfo {

      public const string XcstMajorMinor = "0.65";
      public const string XcstAssemblyVersion = "1.0.0";
      public const string XcstAssemblyFileVersion = XcstMajorMinor + "." + XcstPatch;
      public const string XcstAssemblyInformationalVersion = XcstAssemblyFileVersion;
   }
}
