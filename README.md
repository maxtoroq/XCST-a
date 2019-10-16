[XCST][1] web pages for ASP.NET
===============================
This repository provides integration of XCST with ASP.NET for web application development. It includes a set of extension instructions known as the "application extension" based on a [trimmed down fork](src/Xcst.AspNet/Framework) of ASP.NET MVC 5.

See the [project home][1] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a)
[![NuGet](https://img.shields.io/nuget/v/Xcst.AspNet.svg?label=Xcst.AspNet)](https://www.nuget.org/packages/Xcst.AspNet)
[![NuGet](https://img.shields.io/nuget/v/Xcst.AspNet.Compilation.svg?label=Xcst.AspNet.Compilation)](https://www.nuget.org/packages/Xcst.AspNet.Compilation)
[![NuGet](https://img.shields.io/nuget/v/Xcst.Web.Mvc.svg?label=Xcst.Web.Mvc)](https://www.nuget.org/packages/Xcst.Web.Mvc)

### Related Repositories

- [XCST][2]

System Requirements
-------------------
This project is written in C# 7 and requires .NET 4.6 or higher.

The [release script](build/release.ps1) (which creates the NuGet packages) and other utility scripts are written in PowerShell 3.

The [application extension schema](schemas/xcst-app.rng) is written in Relax NG and converted to XSD using [Trang][3], which requires Java.

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

[1]: http://maxtoroq.github.io/XCST/
[2]: https://github.com/maxtoroq/XCST
[3]: https://github.com/relaxng/jing-trang
