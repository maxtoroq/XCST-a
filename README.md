[XCST] web pages for ASP.NET Core
=================================
This repository provides integration of XCST with ASP.NET Core for web application development. It includes a set of extension instructions known as the "application extension" based on a [trimmed down fork](src/Xcst.AspNetCore/Framework) of ASP.NET MVC 5.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST-a/v2)

### Packages Built From This Repository

Package | Description | Targets
------- | ----------- | -------
[Xcst.AspNetCore] | XCST web pages for ASP.NET Core. | .NET 7
[Xcst.AspNetCore.Extension] | Extension instructions for XCST web pages. | .NET 5

### Related Repositories

- [XCST](https://github.com/maxtoroq/XCST)

System Requirements
-------------------
The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts are written in PowerShell 5.1.

The [application extension schema](schemas/xcst-app.rng) is written in Relax NG and converted to XSD using [Trang], which requires Java.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.AspNetCore]: https://www.nuget.org/packages/Xcst.AspNetCore
[Xcst.AspNetCore.Extension]: https://www.nuget.org/packages/Xcst.AspNetCore.Extension
[Trang]: https://github.com/relaxng/jing-trang
