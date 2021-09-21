[XCST] web pages for ASP.NET
============================
This repository provides integration of XCST with ASP.NET and ASP.NET Core for web application development. It includes a set of extension instructions known as the "application extension" based on a [trimmed down fork](src/Xcst.AspNet/Framework) of ASP.NET MVC 5.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c/branch/master?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a/branch/master) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST-a/master)

### Packages Built From This Repository

Package | Description
------- | -----------
[Xcst.AspNet] | XCST web pages core components for ASP.NET.
[Xcst.AspNet.Extension] | Extension instructions for XCST web pages.
[Xcst.AspNet.Compilation] | ASP.NET build providers for run-time compilation.
[Xcst.AspNet.Precompilation] | HTTP module that maps requests to precompiled XCST pages.
[Xcst.Web.Mvc] | View engine for ASP.NET MVC 5.
[Xcst.AspNetCore] | XCST web pages for ASP.NET Core.

### Related Repositories

- [XCST](https://github.com/maxtoroq/XCST)

System Requirements
-------------------
The codebase is written in C# 8 and requires .NET 4.6 or higher.

The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts are written in PowerShell 5.1.

The [application extension schema](schemas/xcst-app.rng) is written in Relax NG and converted to XSD using [Trang], which requires Java.

Building
--------
Run the following commands in PowerShell to build everything (source, samples and tests).

```powershell
# clone
git clone https://github.com/maxtoroq/XCST-a.git
cd XCST-a

# restore packages
.\build\restore-packages.ps1

# build solution
MSBuild
```

[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.AspNet]: https://www.nuget.org/packages/Xcst.AspNet
[Xcst.AspNet.Extension]: https://www.nuget.org/packages/Xcst.AspNet.Extension
[Xcst.AspNet.Compilation]: https://www.nuget.org/packages/Xcst.AspNet.Compilation
[Xcst.AspNet.Precompilation]: https://www.nuget.org/packages/Xcst.AspNet.Precompilation
[Xcst.Web.Mvc]: https://www.nuget.org/packages/Xcst.Web.Mvc
[Xcst.AspNetCore]: https://www.nuget.org/packages/Xcst.AspNetCore
[Trang]: https://github.com/relaxng/jing-trang
