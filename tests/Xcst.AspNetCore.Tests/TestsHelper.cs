using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xcst.Compiler;
using CSharpVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion;
using TestAssert = NUnit.Framework.Assert;
using TestAssertException = NUnit.Framework.AssertionException;

namespace Xcst.Web.Tests;

static partial class TestsHelper {

   const bool
   _printCode = false;

   static readonly XName
   _initialName = XName.Get("initial-template", XmlNamespaces.Xcst);

   static readonly XName
   _expectedName = "expected";

   public static void
   RunXcstTest(
         string packageFile, string testName, string testNamespace, bool correct, bool error, bool fail,
         string? disableWarning = null) {

      var printCode = _printCode || Debugger.IsAttached;
      var packageUri = new Uri(packageFile, UriKind.Absolute);

      CompileResult xcstResult;
      string packageName;

      try {
         var codegenResult = GenerateCode(packageUri, testName, testNamespace);

         if (!correct) {
            // did not fail, caller Assert.Throws will
            PrintCode(codegenResult.result);
            return;
         }

         xcstResult = codegenResult.result;
         packageName = codegenResult.packageName;

      } catch (RuntimeException ex) {

         dynamic? errorData = ex.ErrorData;

         Console.WriteLine($"// {ex.Message}");
         Console.WriteLine($"// Module URI: {errorData?.ModuleUri}");
         Console.WriteLine($"// Line number: {errorData?.LineNumber}");

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

            packageType = CompileCode(
               packageName,
               packageUri,
               xcstResult.CompilationUnits,
               xcstResult.Language,
               error,
               disableWarning,
               printCode
            );

            if (error) {
               printCode = true;
               TestAssert.Fail($"{xcstResult.Language} compilation error expected.");
            }

         } catch (ApplicationException) when (correct) {

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

            Console.WriteLine($"// {ex.Message}");

            if (!fail) {
               printCode = true;
            }

            throw;

         } catch (TestAssertException) {

            printCode = true;
            throw;
         }

      } finally {

         if (printCode) {
            PrintCode(xcstResult);
         }
      }
   }

   static void
   PrintCode(CompileResult result) {
      foreach (var unit in result.CompilationUnits) {
         Console.WriteLine(unit);
      }
   }

   public static XcstCompiler
   CreateCompiler() {

      var compiler = new XcstCompiler {
         UseLineDirective = true
      };

      compiler.RegisterExtension(() => new Xcst.Web.Extension.ExtensionPackage());
      compiler.AddPackageLibrary(Assembly.GetExecutingAssembly().Location);

      return compiler;
   }

   static (CompileResult result, string packageName)
   GenerateCode(Uri packageUri, string testName, string testNamespace) {

      var compiler = CreateCompiler();
      compiler.TargetNamespace = testNamespace;
      compiler.TargetClass = testName;
      compiler.UsePackageBase = testNamespace;
      compiler.SetTargetBaseTypes(typeof(TestBase));

      compiler.NullableAnnotate = true;
      compiler.NullableContext = "enable";

      var result = compiler.Compile(packageUri);

      return (result, compiler.TargetNamespace + "." + compiler.TargetClass);
   }

   public static Type
   CompileCode(
         string packageName, Uri packageUri, IEnumerable<string> compilationUnits, string language,
         bool error = false, string? disableWarning = null, bool printCode = false) {

      var csOptions = new CSharpParseOptions(CSharpVersion.CSharp9, preprocessorSymbols: new[] { "DEBUG", "TRACE" });

      var syntaxTrees = compilationUnits
         .Select(c => CSharpSyntaxTree.ParseText(c, csOptions, path: packageUri.LocalPath, encoding: Encoding.UTF8))
         .ToArray();

      // See <https://stackoverflow.com/a/47196516/39923>
      // The location of the .NET assemblies
      var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

      var references = new[] {
         // XCST dependencies
         Path.Combine(assemblyPath, "mscorlib.dll"),
         Path.Combine(assemblyPath, "System.dll"),
         Path.Combine(assemblyPath, "System.Collections.dll"),
         Path.Combine(assemblyPath, "System.Core.dll"),
         Path.Combine(assemblyPath, "System.Runtime.dll"),
         Path.Combine(assemblyPath, "System.Runtime.Extensions.dll"),
         Path.Combine(assemblyPath, "System.Xml.ReaderWriter.dll"),
         Path.Combine(assemblyPath, "System.Xml.XDocument.dll"),
         Path.Combine(assemblyPath, "netstandard.dll"),
         typeof(System.Object).Assembly.Location,
         typeof(System.Uri).Assembly.Location,
         typeof(System.Collections.Generic.List<>).Assembly.Location,
         typeof(System.Linq.Enumerable).Assembly.Location,
         typeof(System.Xml.XmlWriter).Assembly.Location,
         typeof(System.Xml.Linq.XDocument).Assembly.Location,
         typeof(System.Diagnostics.Trace).Assembly.Location,
         typeof(System.IServiceProvider).Assembly.Location,
         typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location,
         typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location,
         typeof(Newtonsoft.Json.JsonWriter).Assembly.Location,
         typeof(Xcst.IXcstPackage).Assembly.Location
      }.Concat(GetPackageAssemblyReferences(assemblyPath))
         .Select(p => MetadataReference.CreateFromFile(p));

      var specificDiagnosticOptions = (disableWarning != null) ?
         disableWarning.Split(' ').Select(p => new KeyValuePair<string, ReportDiagnostic>(p, ReportDiagnostic.Suppress)).ToArray()
         : Array.Empty<KeyValuePair<string, ReportDiagnostic>>();

      var compilation = CSharpCompilation.Create(
         Path.GetRandomFileName(),
         syntaxTrees: syntaxTrees,
         references: references,
         options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            specificDiagnosticOptions: specificDiagnosticOptions));

      using var assemblyStream = new MemoryStream();
      using var pdbStream = new MemoryStream();

      var codeResult = compilation.Emit(assemblyStream, pdbStream);

      var failed = codeResult.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error
         || (d.Severity == DiagnosticSeverity.Warning && d.WarningLevel > 1));

      if (printCode || failed) {

         foreach (var item in codeResult.Diagnostics.Where(d => d.Severity != DiagnosticSeverity.Hidden)) {
            var lineSpan = item.Location.GetLineSpan();
            Console.WriteLine($"// ({lineSpan.StartLinePosition.Line},{lineSpan.StartLinePosition.Character}) {item.Severity} {item.Id}: {item.GetMessage()}");
         }
      }

      if (failed) {

         if (error) {
            TestAssert.Pass($"{language} compilation failed.");
         }

         throw new ApplicationException($"{language} compilation failed.");
      }

      assemblyStream.Position = 0;
      pdbStream.Position = 0;

      var assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream.ToArray());
      var type = assembly.GetType(packageName)!;

      return type;
   }

   static bool
   OutputEqualsToExpected(Type packageType, Uri packageUri, bool printCode) {

      var package = CreatePackage(packageType);

      var expectedDoc = new XDocument();
      var actualDoc = new XDocument();

      var evaluator = XcstEvaluator.Using((object)package);

      using (var actualWriter = actualDoc.CreateWriter()) {

         evaluator.CallInitialTemplate()
            .OutputTo(actualWriter)
            .WithBaseUri(packageUri)
            .WithBaseOutputUri(packageUri)
            .Run();
      }

      using (var expectedWriter = expectedDoc.CreateWriter()) {

         evaluator.CallTemplate(_expectedName)
            .OutputTo(expectedWriter)
            .Run();
      }

      var normalizedExpected = XDocumentNormalizer.Normalize(expectedDoc);
      var normalizedActual = XDocumentNormalizer.Normalize(actualDoc);
      var equals = XNode.DeepEquals(normalizedExpected, normalizedActual);

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

   static void
   SimplyRun(Type packageType, Uri packageUri) {

      var package = CreatePackage(packageType);

      XcstEvaluator.Using((object)package)
         .CallInitialTemplate()
         .OutputTo(TextWriter.Null)
         .WithBaseUri(packageUri)
         .WithBaseOutputUri(packageUri)
         .Run();
   }
}
