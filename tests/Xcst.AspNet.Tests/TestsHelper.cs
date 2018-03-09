﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Moq;
using Xcst.Compiler;
using Xcst.Web.Mvc;
using TestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Xcst.Web.Tests {

   static class TestsHelper {

      static readonly XcstCompilerFactory CompilerFactory = new XcstCompilerFactory();

      static readonly QualifiedName InitialName = new QualifiedName("initial-template", "http://maxtoroq.github.io/XCST");
      static readonly QualifiedName ExpectedName = new QualifiedName("expected");

      static TestsHelper() {
         CompilerFactory.EnableExtensions = true;
         CompilerFactory.PackageTypeResolver = typeName => Assembly.GetExecutingAssembly().GetType(typeName);
         CompilerFactory.RegisterApplicationExtension();
      }

      public static void RunXcstTest(string packageFile, bool correct, bool fail) {

         var packageUri = new Uri(packageFile, UriKind.Absolute);
         string usePackageBase = new StackFrame(1, true).GetMethod().DeclaringType.Namespace;

         CompileResult xcstResult;
         string packageName;

         try {
            var codegenResult = GenerateCode(packageUri, usePackageBase);
            xcstResult = codegenResult.Item1;
            packageName = codegenResult.Item2;

         } catch (CompileException ex) {

            Console.WriteLine($"// {ex.Message}");
            Console.WriteLine($"// Module URI: {ex.ModuleUri}");
            Console.WriteLine($"// Line number: {ex.LineNumber}");

            throw;
         }

         try {

            Type packageType = CompileCode(xcstResult, packageName, packageUri);

            if (!correct) {
               return;
            }

            try {

               if (fail) {

                  if (!xcstResult.Templates.Contains(InitialName)) {
                     TestAssert.Fail("A failing package should define an initial template.");
                  } else if (xcstResult.Templates.Contains(ExpectedName)) {
                     TestAssert.Fail("A failing package should not define an 'expected' template.");
                  }

                  SimplyRun(packageType, packageUri);

               } else {

                  if (xcstResult.Templates.Contains(InitialName)) {

                     if (xcstResult.Templates.Contains(ExpectedName)) {
                        TestAssert.IsTrue(OutputEqualsToExpected(packageType));
                     } else {
                        SimplyRun(packageType, packageUri);
                     }

                  } else if (xcstResult.Templates.Contains(ExpectedName)) {
                     TestAssert.Fail("A package that defines an 'expected' template without an initial template makes no sense.");
                  }
               }

            } catch (RuntimeException ex) {

               Console.WriteLine($"// {ex.Message}");
               throw;
            }

         } finally {

            foreach (string unit in xcstResult.CompilationUnits) {
               Console.WriteLine(unit);
            }
         }
      }

      static Tuple<CompileResult, string> GenerateCode(Uri packageUri, string usePackageBase) {

         XcstCompiler compiler = CompilerFactory.CreateCompiler();
         compiler.TargetNamespace = typeof(TestsHelper).Namespace + ".Runtime";
         compiler.TargetClass = "TestModule";
         compiler.UseLineDirective = true;
         compiler.UsePackageBase = usePackageBase;
         compiler.SetTargetBaseTypes(typeof(TestBase));

         compiler.SetParameter(
            new QualifiedName("application-uri", XmlNamespaces.XcstApplication),
            new Uri(Directory.GetCurrentDirectory())
         );

         CompileResult result = compiler.Compile(packageUri);

         return Tuple.Create(result, compiler.TargetNamespace + "." + compiler.TargetClass);
      }

      static Type CompileCode(CompileResult result, string packageName, Uri packageUri) {

         var parseOptions = new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG", "TRACE" });

         SyntaxTree[] syntaxTrees = result.CompilationUnits
            .Select(c => CSharpSyntaxTree.ParseText(c, parseOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8))
            .ToArray();

         MetadataReference[] references = {
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
            MetadataReference.CreateFromFile(typeof(System.Drawing.Color).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Xcst.Web.Mvc.XcstViewPage).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.VisualStudio.TestTools.UnitTesting.Assert).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)
         };

         CSharpCompilation compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

         using (var assemblyStream = new MemoryStream()) {
            using (var pdbStream = new MemoryStream()) {

               EmitResult csharpResult = compilation.Emit(assemblyStream, pdbStream);

               if (!csharpResult.Success) {

                  Diagnostic error = csharpResult.Diagnostics
                     .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                     .FirstOrDefault();

                  if (error != null) {
                     Console.WriteLine($"// {error.Id}: {error.GetMessage()}");
                     Console.WriteLine($"// Line number: {error.Location.GetLineSpan().StartLinePosition.Line}");
                  }

                  throw new CompileException("C# compilation failed.");
               }

               assemblyStream.Position = 0;
               pdbStream.Position = 0;

               Assembly assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
               Type type = assembly.GetType(packageName);

               return type;
            }
         }
      }

      static XcstViewPage CreatePackage(Type packageType) {

         XcstViewPage package = (XcstViewPage)Activator.CreateInstance(packageType);

         var httpContextMock = new Mock<HttpContextBase>();
         httpContextMock.Setup(c => c.Items).Returns(() => new System.Collections.Hashtable());

         package.ViewContext = new ViewContext(
            new ControllerContext(
               new RequestContext(httpContextMock.Object, new RouteData())
            ),
            new ViewDataDictionary(),
            new TempDataDictionary(),
            TextWriter.Null
         );

         return package;
      }

      static bool OutputEqualsToExpected(Type packageType) {

         XcstViewPage package = CreatePackage(packageType);

         var expectedDoc = new XDocument();
         var actualDoc = new XDocument();

         XcstEvaluator evaluator = XcstEvaluator.Using(package);

         using (XmlWriter actualWriter = actualDoc.CreateWriter()) {

            evaluator.CallInitialTemplate()
               .OutputTo(actualWriter)
               .Run();
         }

         using (XmlWriter expectedWriter = expectedDoc.CreateWriter()) {

            evaluator.CallTemplate("expected")
               .OutputTo(expectedWriter)
               .Run();
         }

         return XDocumentNormalizer.DeepEqualsWithNormalization(expectedDoc, actualDoc);
      }

      static void SimplyRun(Type packageType, Uri packageUri) {

         XcstViewPage package = CreatePackage(packageType);

         XcstEvaluator.Using(package)
            .CallInitialTemplate()
            .OutputTo(TextWriter.Null)
            .WithBaseUri(packageUri)
            .WithBaseOutputUri(packageUri)
            .Run();
      }
   }

   public abstract class TestBase : XcstViewPage { }

   public abstract class TestBase<TModel> : XcstViewPage<TModel> {

      protected Type CompileType<T>(T obj) {
         return typeof(T);
      }

      public static class Assert {

         public static void IsTrue(bool condition) {
            TestAssert.IsTrue(condition);
         }

         public static void IsFalse(bool condition) {
            TestAssert.IsFalse(condition);
         }

         public static void AreEqual<T>(T expected, T actual) {
            TestAssert.AreEqual(expected, actual);
         }

         public static void IsNull(object value) {
            TestAssert.IsNull(value);
         }
      }
   }
}