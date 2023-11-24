[XCST] web pages for ASP.NET Core
=================================
This repository provides integration of XCST with ASP.NET Core for web application development. It includes a set of extension instructions known as the "application extension" based on the HTML helpers from ASP.NET MVC 5.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST-a/v2)

### Packages Built From This Repository

Package | Description | Targets
------- | ----------- | -------
[Xcst.AspNetCore] | XCST web pages for ASP.NET Core. | .NET 7
[Xcst.AspNetCore.Extension] | Extension instructions for XCST web pages. | .NET 5

### Related Repositories

- [XCST](https://github.com/maxtoroq/XCST)

Documentation
-------------
The documentation can be found at the [project home][XCST].

About v2
--------
*v2* is the main branch for major version 2. See *v1* for version 1 (no longer maintained).

Support for ASP.NET 4 (.NET Framework) was dropped in v2, focusing on ASP.NET Core going forward. As a consequence, the number of NuGet packages was reduced from six to two.

The runtime on v2 is much more integrated with ASP.NET Core. Functionality that was previously copied from ASP.NET MVC 5 such as *model metadata*, *model binding*, *model validation*, *anti-forgery*, etc. is now reused from ASP.NET Core.

The extension on v2 can generate code for runtime v1 or v2 (the default). This not only accounts for runtime API changes, but the extension instructions and attributes also. For example, if v2 removes an attribute, it will be available if you target v1. On the other hand, new instructions and attributes on v2 are not supported when targeting v1. To put is simply, the extension is backwards compatible, but you must explicitly target v1. You are therefore encouraged to use the v2 extension and XCST's v2 compiler to maintain your legacy v1 apps.

System Requirements
-------------------
The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts require **PowerShell 5.1** or **PowerShell Core**.

The [application extension schema](schemas/xcst-app.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.AspNetCore]: https://www.nuget.org/packages/Xcst.AspNetCore
[Xcst.AspNetCore.Extension]: https://www.nuget.org/packages/Xcst.AspNetCore.Extension
[Trang]: https://github.com/relaxng/jing-trang
