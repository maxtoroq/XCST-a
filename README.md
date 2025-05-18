[XCST] web pages for ASP.NET Core
=================================
This repository provides integration of XCST with ASP.NET Core for web application development. It includes a set of extension instructions known as the "application extension" based on the HTML helpers from ASP.NET MVC.

See the [project home][XCST] for more information.

[![Build status](https://ci.appveyor.com/api/projects/status/4chhbklsb4b6h09c/branch/v2?svg=true)](https://ci.appveyor.com/project/maxtoroq/xcst-a/branch/v2) ![Tests](https://img.shields.io/appveyor/tests/maxtoroq/XCST-a/v2)

### Packages Built From This Repository

Package | Description | Targets
------- | ----------- | -------
[Xcst.AspNetCore] | XCST web pages for ASP.NET Core. | .NET 7
[Xcst.AspNetCore.Extension] | Extension instructions for XCST web pages. | .NET 7

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

The extension on v2 can generate code for runtime v1 or v2 (the default). This not only accounts for runtime API changes, but the extension instructions and attributes also. For example, if v2 removes an attribute, it will be available if you target v1. On the other hand, new instructions and attributes on v2 are not supported when targeting v1. To put it simply, the extension is backwards compatible, but you must explicitly target v1. You are therefore encouraged to use the v2 extension and XCST's v2 compiler to maintain your legacy v1 apps.

Breaking Changes
----------------

### Entension

- [Removed 'attributes' attribute from all html instructions (can now use sequence constructor to add attributes)](https://github.com/maxtoroq/XCST-a/commit/9fdf3de9175ce00153e83bce1949a3bd63af65fe)
- [Not using ViewDataDictionary value on form instructions](https://github.com/maxtoroq/XCST-a/commit/c23401630f2dbcef8bf6ff041bc281296b1665a1)
- [Not adding blank option on a:select (a:editor still does)](https://github.com/maxtoroq/XCST-a/commit/9fdf3de9175ce00153e83bce1949a3bd63af65fe)
- [Not looking for options in ViewData on a:select](https://github.com/maxtoroq/XCST-a/commit/9fdf3de9175ce00153e83bce1949a3bd63af65fe)
- [Not auto-creating IEnumerable&lt;SelectListItem> from various types](https://github.com/maxtoroq/XCST-a/commit/e995a3ffa4589c1c4d30c6622c7be9ddb3744622)
- [Not prepending new line on a:textarea when value is empty](https://github.com/maxtoroq/XCST-a/commit/7df19560060f7ac8a1ed09aab1ff833b961a09b5)
- [Adding 'readonly' and 'placeholder' on a:input and a:textarea based on metadata](https://github.com/maxtoroq/XCST-a/commit/12789aff82b056158b4e7f1c7b7d0469dc0a13c1)
- [Using more specific type on a:input based on metadata](https://github.com/maxtoroq/XCST-a/commit/b46332d98ec618dbfef6147c7747c09ff44ab478)
- [Default format on a:input](https://github.com/maxtoroq/XCST-a/commit/11b0df99f7773ddfdbe24be0beb1085a67999190)
- [Keeping track of form method to use appropriate culture (invariant for GET requests) and including `__Invariant` hidden field for invariant input types](https://github.com/maxtoroq/XCST-a/commit/8d3b75c0ec877bb0ab6ec6cf2fde0b68e78b1a63)
- [Ignoring Html5DateRenderingMode](https://github.com/maxtoroq/XCST-a/commit/11b0df99f7773ddfdbe24be0beb1085a67999190) ([and removed](https://github.com/maxtoroq/XCST-a/commit/9ba3d8de281f1b263b2654964476d89c88126459))
- [Text-only sequence constructor for a:with-options/a:option](https://github.com/maxtoroq/XCST-a/commit/a168d65dcaec34680f0ad15e2bf3b7821e0bf00a)
- [Renamed a:model's 'as' attribute to 'type'](https://github.com/maxtoroq/XCST-a/commit/3ac2492e377e7be181f0aa49e981e20c67010608)
- [Using single Number template for integral types](https://github.com/maxtoroq/XCST-a/commit/6787fb48ee29127f536dcb52c267082be7791cee)

### Runtime
The runtime has lots of changes, including renamed namespaces and moved types. These are the most notable changes:

- [Merged TemplateInfo into ViewContext](https://github.com/maxtoroq/XCST-a/commit/996c47dd75e79f76c856616bb4c4dfab4ead69f4)
- [Removed RouteValueDictionary dependency from ObjectToDictionary() and AnonymousObjectToHtmlAttributes()](https://github.com/maxtoroq/XCST-a/commit/25e0171c73cf199280dbaa77445c3532074f23d0)
- [Removed TempData](https://github.com/maxtoroq/XCST-a/commit/1d2fdacbe13cde8383af34245d2170379a529289)
- [Removed ViewBag](https://github.com/maxtoroq/XCST-a/commit/83f147cd0cad4b1c9ff0638f895be3ea730bde8c)
- [Removed InputType enum](https://github.com/maxtoroq/XCST-a/commit/2cd8abcc6d21a55014ac8a43151e89a6fe149ed5)
- [Renamed XcstPage.Context to HttpContext](https://github.com/maxtoroq/XCST-a/commit/a576f482c5ce36c2463c1af9328a6058e4eabcec)
- [Removed XcstPageHandler, moved RenderPage() to XcstPage](https://github.com/maxtoroq/XCST-a/commit/8d32a32fb74180a85d28675ab0d24845fe0a974c)

System Requirements
-------------------
The [application extension schema](schemas/xcst-app.rng) is written in **Relax NG** and converted to XSD using [Trang], which requires **Java**.


[XCST]: https://maxtoroq.github.io/XCST/
[Xcst.AspNetCore]: https://www.nuget.org/packages/Xcst.AspNetCore
[Xcst.AspNetCore.Extension]: https://www.nuget.org/packages/Xcst.AspNetCore.Extension
[Trang]: https://github.com/relaxng/jing-trang
