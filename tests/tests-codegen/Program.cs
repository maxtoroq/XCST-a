﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Xcst.Compiler;

namespace tests_codegen {

   class Program {

      readonly Uri
      _projectUri;

      readonly XcstCompilerFactory
      _compilerFactory = new();

      readonly TextWriter
      _output;

      const string
      _singleIndent = "   ";

      string
      _indent = "";

      public
      Program(Uri projectUri, TextWriter output) {
         _projectUri = projectUri;
         _output = output;
      }

      void
      PushIndent() =>
         _indent += _singleIndent;

      void
      PopIndent() =>
         _indent = _indent.Substring(_singleIndent.Length);

      void
      WriteLine(string line = "") =>
         _output.WriteLine(_indent + line);

      XElement
      TestConfig(string file) {

         var readerSettings = new XmlReaderSettings() {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Parse
         };

         using var reader = XmlReader.Create(file, readerSettings);

         while (reader.Read() && reader.NodeType != XmlNodeType.Element) {

            if (reader.NodeType == XmlNodeType.ProcessingInstruction
               && reader.LocalName == "xcst-test") {

               var piValue = reader.Value;

               if (!String.IsNullOrEmpty(piValue)) {
                  return XElement.Parse($"<xcst-test {piValue} />");
               }

               break;
            }
         }

         return new XElement("xcst-test");
      }

      void
      Run() {

         var startUri = new Uri(_projectUri, ".");
         var startDirectory = new DirectoryInfo(startUri.LocalPath);

         _output.WriteLine("//------------------------------------------------------------------------------");
         _output.WriteLine("// <auto-generated>");
         _output.WriteLine("//     This code was generated by a tool.");
         _output.WriteLine("//");
         _output.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
         _output.WriteLine("//     the code is regenerated.");
         _output.WriteLine("// </auto-generated>");
         _output.WriteLine("//------------------------------------------------------------------------------");
         _output.WriteLine("using System;");
         _output.WriteLine("using System.Linq;");
         _output.WriteLine("using TestFx = NUnit.Framework;");
         _output.WriteLine("using static Xcst.Web.Tests.TestsHelper;");
         _output.WriteLine();
         _output.WriteLine("#nullable enable");

         foreach (var subDirectory in startDirectory.GetDirectories()) {
            GenerateTestsForDirectory(subDirectory, subDirectory.Name);
         }
      }

      void
      GenerateTestsForDirectory(DirectoryInfo directory, string relativeNs) {

         var ns = $"Xcst.Web.Tests.{relativeNs}";

         var pkgDeps = directory.EnumerateFiles()
            .Where(f => f.Extension is ".xcst" or ".pxcst"
               && f.Name[0] != '_'
               // excludes *.?.xcst
               && f.Name[f.Name.Length - f.Extension.Length - 2] != '.');

         foreach (var pkgDep in pkgDeps) {

            var compiler = _compilerFactory.CreateCompiler();
            compiler.TargetClass = Path.GetFileNameWithoutExtension(pkgDep.Name);
            compiler.TargetNamespace = ns;
            compiler.TargetVisibility = CodeVisibility.Public;
            compiler.IndentChars = "   ";
            compiler.CompilationUnitHandler = href => _output;
            compiler.NullableAnnotate = true;

            //Console.WriteLine(pkgDep.FullName);
            compiler.Compile(new Uri(pkgDep.FullName));
         }

         var tests = directory.EnumerateFiles()
            .Where(f => f.Extension == ".xcst"
               // includes *.?.xcst
               && f.Name[f.Name.Length - f.Extension.Length - 2] == '.')
            .ToArray();

         if (tests.Length > 0) {

            WriteLine();
            WriteLine($"namespace {ns} {{");
            PushIndent();

            WriteLine();
            WriteLine("[TestFx.TestFixture]");
            WriteLine($"public partial class {directory.Name}Tests {{");
            PushIndent();

            foreach (var file in tests) {

               var fileName = Path.GetFileNameWithoutExtension(file.Name);
               var testName = Regex.Replace(
                  fileName.Replace('.', '_').Replace('-', '_'),
                  "([a-z])([A-Z])",
                  "$1_$2"
               );

               var error = fileName.EndsWith(".e");
               var fail = fileName.EndsWith(".f");
               var correct = error || fail || fileName.EndsWith(".c");
               var assertThrows = !correct || fail;
               var config = TestConfig(file.FullName);

               WriteLine();
               WriteLine($"#line 1 \"{file.FullName}\"");
               WriteLine($"[TestFx.Test, TestFx.Category(\"{relativeNs}\")]");

               if (config.Attribute("ignore")?.Value == "true") {
                  WriteLine("[TestFx.Ignore(\"\")]");
               }

               WriteLine($"public void {testName}() {{");
               PushIndent();

               var disableWarning = (config.Attribute("disable-warning") is XAttribute disableWarnAttr) ?
                  $"\"{disableWarnAttr.Value}\""
                  : "null";

               var warningAsError = (config.Attribute("warning-as-error") is XAttribute warnAsErrorAttr) ?
                  $"\"{warnAsErrorAttr.Value}\""
                  : "null";

               var languageVersion = (config.Attribute("language-version") is XAttribute langVerAttr) ?
                  langVerAttr.Value + "m"
                  : "-1m";

               string extension;

               if (config.Attribute("extension")?.Value is string extValue) {
                  var pair = extValue.Split(' ');
                  extension = $"(new Uri(\"{pair[0]}\", UriKind.Absolute), typeof({pair[1]}))";
               } else {
                  extension = "null";
               }

               var testCall = "RunXcstTest("
                  + $"@\"{file.FullName}\""
                  + $", \"{testName}\""
                  + $", \"{ns}\""
                  + $", correct: {correct.ToString().ToLower()}"
                  + $", error: {error.ToString().ToLower()}"
                  + $", fail: {fail.ToString().ToLower()}"
                  //+ $", languageVersion: {languageVersion}"
                  + $", disableWarning: {disableWarning}"
                  //+ $", warningAsError: {warningAsError}"
                  //+ $", extension: {extension}"
                  + ")";

               if (assertThrows) {

                  var testException = (config.Attribute("exception") is XAttribute exceptionAttr) ?
                     exceptionAttr.Value
                     : "Xcst.RuntimeException";

                  WriteLine($"TestFx.Assert.Throws<{testException}>(() => {testCall});");

               } else {
                  WriteLine(testCall + ";");
               }

               PopIndent();
               WriteLine("}");
               WriteLine("#line default");
            }

            PopIndent();
            WriteLine("}");

            PopIndent();
            WriteLine("}");
         }

         foreach (var subDirectory in directory.GetDirectories()) {
            GenerateTestsForDirectory(subDirectory, relativeNs + "." + subDirectory.Name);
         }
      }

      static void
      Main(string[] args) {

         var currentDir = Environment.CurrentDirectory;

         if (currentDir.Last() != Path.DirectorySeparatorChar) {
            currentDir += Path.DirectorySeparatorChar;
         }

         var callerBaseUri = new Uri(currentDir, UriKind.Absolute);
         var projectUri = new Uri(callerBaseUri, args[0]);
         var outputUri = new Uri(projectUri, args[1]);

         using TextWriter output = File.CreateText(outputUri.LocalPath);

         // Because XML parsers normalize CRLF to LF,
         // we want to be consistent with the additional content we create
         output.NewLine = "\n";

         new Program(projectUri, output)
            .Run();
      }
   }
}
