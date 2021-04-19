using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Moq;
using Xcst.Compiler;
using Xcst.Web.Mvc;
using CSharpVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using TestAssert = NUnit.Framework.Assert;
using TestAssertException = NUnit.Framework.AssertionException;

namespace Xcst.Web.Tests {

   static class TestsHelper {

      const bool _printCode = false;

      static readonly XcstCompilerFactory _compilerFactory = new XcstCompilerFactory();

      static readonly string _initialName = $"Q{{{XmlNamespaces.Xcst}}}initial-template";
      static readonly string _expectedName = "expected";

      static TestsHelper() {
         _compilerFactory.EnableExtensions = true;
         _compilerFactory.RegisterExtension(new Xcst.Web.Extension.ExtensionLoader {
            DefaultModelDynamic = true
         });
      }

      public static void RunXcstTest(string packageFile, string testName, string testNamespace, bool correct, bool fail) {

         bool printCode = _printCode;
         var packageUri = new Uri(packageFile, UriKind.Absolute);

         CompileResult xcstResult;
         string packageName;

         try {
            var codegenResult = GenerateCode(packageUri, testName, testNamespace);
            xcstResult = codegenResult.result;
            packageName = codegenResult.packageName;

         } catch (CompileException ex) when (printCode) {

            Console.WriteLine($"// {ex.Message}");
            Console.WriteLine($"// Module URI: {ex.ModuleUri}");
            Console.WriteLine($"// Line number: {ex.LineNumber}");

            throw;
         }

         if (fail) {

            if (!xcstResult.Templates.Contains(_initialName)) {
               TestAssert.Fail("A failing package should define an initial template.");
            } else if (xcstResult.Templates.Contains(_expectedName)) {
               TestAssert.Fail("A failing package should not define an 'expected' template.");
            }

         } else {

            if (xcstResult.Templates.Contains(_expectedName)
               && !xcstResult.Templates.Contains(_initialName)) {

               TestAssert.Fail("A package that defines an 'expected' template without an initial template makes no sense.");
            }
         }

         try {

            Type packageType;

            try {

               packageType = CompileCode(packageName, packageUri, xcstResult.CompilationUnits, xcstResult.Language, correct);

               if (!correct) {
                  // did not fail, caller Assert.Throws will
                  return;
               }

            } catch (CompileException) when (correct) {

               printCode = true;
               throw;
            }

            try {

               if (fail) {

                  SimplyRun(packageType, packageUri);

                  // did not fail, print code
                  printCode = true;

               } else if (xcstResult.Templates.Contains(_initialName)) {

                  if (xcstResult.Templates.Contains(_expectedName)) {
                     TestAssert.IsTrue(OutputEqualsToExpected(packageType, packageUri, printCode));
                  } else {
                     SimplyRun(packageType, packageUri);
                  }
               }

            } catch (RuntimeException ex) {

               if (printCode) {
                  Console.WriteLine($"// {ex.Message}");
               } else if (!fail) {
                  printCode = true;
               }

               throw;

            } catch (TestAssertException) {

               printCode = true;
               throw;
            }

         } finally {

            if (printCode) {
               foreach (string unit in xcstResult.CompilationUnits) {
                  Console.WriteLine(unit);
               }
            }
         }
      }

      public static XcstCompiler CreateCompiler() {

         XcstCompiler compiler = _compilerFactory.CreateCompiler();
         compiler.UseLineDirective = true;
         compiler.PackageTypeResolver = n => Assembly.GetExecutingAssembly().GetType(n);

         return compiler;
      }

      static (CompileResult result, string packageName) GenerateCode(Uri packageUri, string testName, string testNamespace) {

         XcstCompiler compiler = CreateCompiler();
         compiler.TargetNamespace = testNamespace;
         compiler.TargetClass = testName;
         compiler.UsePackageBase = testNamespace;
         compiler.SetTargetBaseTypes(typeof(TestBase));

         CompileResult result = compiler.Compile(packageUri);

         return (result, compiler.TargetNamespace + "." + compiler.TargetClass);
      }

      public static Type CompileCode(string packageName, Uri packageUri, IEnumerable<string> compilationUnits, string language, bool correct) {

         var csOptions = new CSharpParseOptions(CSharpVersion.CSharp6, preprocessorSymbols: new[] { "DEBUG", "TRACE" });

         SyntaxTree[] syntaxTrees = compilationUnits
            .Select(c => CSharpSyntaxTree.ParseText(c, csOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8))
            .ToArray();

         MetadataReference[] references = {
            // XCST dependencies
            MetadataReference.CreateFromFile(typeof(System.Object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Uri).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.XmlWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XDocument).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonWriter).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Xcst.PackageModel.IXcstPackage).Assembly.Location),
            // Tests dependencies
            MetadataReference.CreateFromFile(typeof(System.Web.HttpContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Web.Mvc.ViewContext).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Xcst.Web.Mvc.XcstViewPage).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(TestAssert).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
         };

         var compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

         using (var assemblyStream = new MemoryStream()) {
            using (var pdbStream = new MemoryStream()) {

               EmitResult codeResult = compilation.Emit(assemblyStream, pdbStream);

               if (!codeResult.Success) {

                  Diagnostic? error = codeResult.Diagnostics
                     .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                     .FirstOrDefault();

                  if (error != null
                     && correct) {

                     Console.WriteLine($"// {error.Id}: {error.GetMessage()}");
                     Console.WriteLine($"// Line number: {error.Location.GetLineSpan().StartLinePosition.Line}");
                  }

                  throw new CompileException($"{language} compilation failed.");
               }

               assemblyStream.Position = 0;
               pdbStream.Position = 0;

               Assembly assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
               Type type = assembly.GetType(packageName)!;

               return type;
            }
         }
      }

      static XcstViewPage CreatePackage(Type packageType) {

         XcstViewPage package = (XcstViewPage)Activator.CreateInstance(packageType);

         var httpContextMock = new Mock<HttpContextBase>();

         httpContextMock.Setup(c => c.Items)
            .Returns(() => new System.Collections.Hashtable());

         // Cookies and Headers used by a:antiforgery
         httpContextMock.Setup(c => c.Response.Cookies)
            .Returns(() => new HttpCookieCollection());

         httpContextMock.Setup(c => c.Response.Headers)
            .Returns(() => new NameValueCollection());

         package.ViewContext = new ViewContext(httpContextMock.Object);

         return package;
      }

      static bool OutputEqualsToExpected(Type packageType, Uri packageUri, bool printCode) {

         XcstViewPage package = CreatePackage(packageType);

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using((object)package);

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .WithBaseUri(packageUri)
               .WithBaseOutputUri(packageUri)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate(_expectedName)
               .OutputTo(expectedWriter)
               .Run();
         }

         XDocument normalizedExpected = XDocumentNormalizer.Normalize(expectedDoc);
         XDocument normalizedActual = XDocumentNormalizer.Normalize(actualDoc);
         bool equals = XNode.DeepEquals(normalizedExpected, normalizedActual);

         if (printCode || !equals) {
            Console.WriteLine("/*");
            Console.WriteLine("<!-- expected -->");
            Console.WriteLine(normalizedExpected.ToString());
            Console.WriteLine();
            Console.WriteLine("<!-- actual -->");
            Console.WriteLine(normalizedActual.ToString());
            Console.WriteLine("*/");
         }

         return equals;
      }

      static void SimplyRun(Type packageType, Uri packageUri) {

         XcstViewPage package = CreatePackage(packageType);

         XcstEvaluator.Using((object)package)
            .CallInitialTemplate()
            .OutputTo(TextWriter.Null)
            .WithBaseUri(packageUri)
            .WithBaseOutputUri(packageUri)
            .Run();
      }
   }

   public abstract class TestBase : XcstViewPage { }

   public abstract class TestBase<TModel> : XcstViewPage<TModel> {

      protected Type CompileType<T>(T obj) =>
         typeof(T);

      public static class Assert {

         public static void IsTrue(bool condition) =>
            TestAssert.IsTrue(condition);

         public static void IsFalse(bool condition) =>
            TestAssert.IsFalse(condition);

         public static void AreEqual<T>(T expected, T actual) =>
            TestAssert.AreEqual(expected, actual);

         public static void IsNull(object value) =>
            TestAssert.IsNull(value);

         public static void IsNotNull(object value) =>
            TestAssert.IsNotNull(value);
      }
   }
}
