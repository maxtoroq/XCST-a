using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Xcst.Web.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Antiforgery;
using Moq;
using TestAssert = NUnit.Framework.Assert;

namespace Xcst.Web.Tests {

   static partial class TestsHelper {

      static XcstViewPage
      CreatePackage(Type packageType) {

         var package = (XcstViewPage)Activator.CreateInstance(packageType)!;

         var httpContextMock = new Mock<HttpContext>();

         httpContextMock.Setup(c => c.Items)
            .Returns(() => new Dictionary<object, object?>());

         var antiforgeryMock = new Mock<IAntiforgery>();
         antiforgeryMock.Setup(p => p.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet(null, null, "__RequestVerificationToken", null));

         var serviceProviderMock = new Mock<IServiceProvider>();
         serviceProviderMock.Setup(p => p.GetService(It.Is<Type>(t => t == typeof(IAntiforgery))))
            .Returns(antiforgeryMock.Object);

         httpContextMock.Setup(c => c.RequestServices)
            .Returns(serviceProviderMock.Object);

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
}
