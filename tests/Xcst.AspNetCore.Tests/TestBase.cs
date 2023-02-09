using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xcst.Web.Mvc;
using TestAssert = NUnit.Framework.Assert;

namespace Xcst.Web.Tests;

static partial class TestsHelper {

   static IEnumerable<string>
   GetPackageAssemblyReferences(string assemblyPath) => new string[] {
      Path.Combine(assemblyPath, "System.Collections.Specialized.dll"),
      Path.Combine(assemblyPath, "System.Dynamic.Runtime.dll"),
      Path.Combine(assemblyPath, "System.Linq.Expressions.dll"),
      Path.Combine(assemblyPath, "System.ObjectModel.dll"),
      typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location,
      typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly.Location,
      typeof(Microsoft.AspNetCore.Http.IFormFile).Assembly.Location,
      typeof(Microsoft.AspNetCore.Http.QueryCollection).Assembly.Location,
      typeof(Microsoft.Extensions.Primitives.StringValues).Assembly.Location,
      typeof(Microsoft.AspNetCore.Mvc.HiddenInputAttribute).Assembly.Location,
      typeof(Microsoft.AspNetCore.Mvc.ModelBinding.ModelMetadata).Assembly.Location,
      typeof(Microsoft.AspNetCore.Mvc.ModelBinding.QueryStringValueProvider).Assembly.Location,
      typeof(Xcst.Web.Mvc.XcstViewPage).Assembly.Location,
      typeof(TestAssert).Assembly.Location,
      Assembly.GetExecutingAssembly().Location
   };

   static XcstViewPage
   CreatePackage(Type packageType) {

      var package = (XcstViewPage)Activator.CreateInstance(packageType)!;

      var httpContextMock = new Mock<HttpContext>();

      httpContextMock.Setup(c => c.Items)
         .Returns(() => new Dictionary<object, object?>());

      var antiforgeryMock = new Mock<IAntiforgery>();
      antiforgeryMock.Setup(p => p.GetAndStoreTokens(It.IsAny<HttpContext>()))
         .Returns(new AntiforgeryTokenSet(null, null, "__RequestVerificationToken", null));

      var services = new ServiceCollection();
      services
         .AddOptions()
         .AddLogging()
         .AddMvcCore(opts => {
            opts.ModelMetadataDetailsProviders.Add(new Xcst.Web.Mvc.ModelBinding.MetadataDetailsProvider());
         })
         .AddDataAnnotations()
         .AddViews();

      services.AddSingleton(antiforgeryMock.Object);

      var serviceProvider = services.BuildServiceProvider();

      httpContextMock.Setup(c => c.RequestServices)
         .Returns(serviceProvider);

      package.ViewContext = new ViewContext(httpContextMock.Object);

      return package;
   }
}

public abstract class TestBase : XcstViewPage {

   protected static Type
   CompileType<T>(T _) => typeof(T);

   protected static class Assert {

      public static void
      IsTrue(bool condition) =>
         TestAssert.IsTrue(condition);

      public static void
      IsFalse(bool condition) =>
         TestAssert.IsFalse(condition);

      public static void
      AreEqual<T>(T expected, T actual) =>
         TestAssert.AreEqual(expected, actual);

      public static void
      IsNull(object value) =>
         TestAssert.IsNull(value);

      public static void
      IsNotNull(object value) =>
         TestAssert.IsNotNull(value);
   }
}

public abstract class TestBase<TModel> : XcstViewPage<TModel> { }
