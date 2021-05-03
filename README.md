[XCST] web pages for ASP.NET
============================
This repository provides integration of XCST with ASP.NET and ASP.NET Core for web application development. It includes a set of extension instructions known as the "application extension" based on a [trimmed down fork](src/Xcst.AspNet/Framework) of ASP.NET MVC 5.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST-a)

### Packages Built From This Repository

Version | Package | Description
------- | ------- | -----------
[![NuGet][Xcst.AspNet-badge]][Xcst.AspNet] | [Xcst.AspNet] | XCST web pages core components for ASP.NET.
[![NuGet][Xcst.AspNet.Extension-badge]][Xcst.AspNet.Extension] | [Xcst.AspNet.Extension] | Extension instructions for XCST web pages.
[![NuGet][Xcst.AspNet.Compilation-badge]][Xcst.AspNet.Compilation] | [Xcst.AspNet.Compilation] | ASP.NET build providers for run-time compilation.
[![NuGet][Xcst.AspNet.Precompilation-badge]][Xcst.AspNet.Precompilation] | [Xcst.AspNet.Precompilation] | HTTP module that maps requests to precompiled XCST pages.
[![NuGet][Xcst.Web.Mvc-badge]][Xcst.Web.Mvc] | [Xcst.Web.Mvc] | View engine for ASP.NET MVC 5.
[![NuGet][Xcst.AspNetCore-badge]][Xcst.AspNetCore] | [Xcst.AspNetCore] | XCST web pages for ASP.NET Core.

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
[Xcst.AspNet-badge]: https://img.shields.io/nuget/v/Xcst.AspNet.svg
[Xcst.AspNet.Extension-badge]: https://img.shields.io/nuget/v/Xcst.AspNet.Extension.svg
[Xcst.AspNet.Compilation-badge]: https://img.shields.io/nuget/v/Xcst.AspNet.Compilation.svg
[Xcst.AspNet.Precompilation-badge]: https://img.shields.io/nuget/v/Xcst.AspNet.Precompilation.svg
[Xcst.Web.Mvc-badge]: https://img.shields.io/nuget/v/Xcst.Web.Mvc.svg
[Xcst.AspNetCore-badge]: https://img.shields.io/nuget/v/Xcst.AspNetCore.svg
[Trang]: https://github.com/relaxng/jing-trang
