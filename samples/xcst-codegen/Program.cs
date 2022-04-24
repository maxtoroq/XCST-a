﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xcst;
using Xcst.Compiler;

namespace XcstCodeGen {

   class Program {

      const string
      _fileExt = "xcst";

      readonly Uri
      _projectUri;

      readonly string
      _configuration;

      readonly bool
      _pageEnable;

      readonly string?
      _pageBaseType;

      public string
      Language { get; }

      public
      Program(Uri projectUri, string configuration, bool pageEnable, string? pageBaseType) {

         _projectUri = projectUri;
         _configuration = configuration;
         _pageEnable = pageEnable;
         _pageBaseType = pageBaseType;

         this.Language = ProjectLang(_projectUri);
      }

      static string
      ProjectLang(Uri projectUri) {
         var projExt = Path.GetExtension(projectUri.LocalPath).TrimStart('.');
         return projExt.Substring(0, projExt.Length - "proj".Length);
      }

      static bool
      IsSdkStyle(XDocument projectDoc) =>
         projectDoc.Root!.Name.NamespaceName.Length == 0;

      static string
      AssemblyName(XDocument projectDoc, string projectPath) {

         XNamespace xmlns = projectDoc.Root!.Name.Namespace;

         return projectDoc.Root!
            .Element(xmlns + "PropertyGroup")?
            .Element(xmlns + "AssemblyName")?.Value
            ?? Path.GetFileNameWithoutExtension(projectPath);
      }

      static string
      RootNamespace(XDocument projectDoc, string projectPath) {

         XNamespace xmlns = projectDoc.Root!.Name.Namespace;

         return projectDoc.Root!
            .Element(xmlns + "PropertyGroup")?
            .Element(xmlns + "RootNamespace")?.Value
            ?? Path.GetFileNameWithoutExtension(projectPath);
      }

      static string?
      Nullable(XDocument projectDoc) {

         XNamespace xmlns = projectDoc.Root!.Name.Namespace;

         return projectDoc.Root!
            .Element(xmlns + "PropertyGroup")?
            .Element(xmlns + "Nullable")?.Value;
      }

      string
      ReferenceAssemblyPath(string refPath) {

         var refDoc = XDocument.Load(new Uri(_projectUri, refPath).LocalPath);
         XNamespace xmlns = refDoc.Root!.Name.Namespace;

         var refName = AssemblyName(refDoc, refPath);
         var refDir = Path.GetDirectoryName(refPath)!;
         var refDll = Path.Combine(refDir, "bin", _configuration);

         if (IsSdkStyle(refDoc)) {

            var propGroup = refDoc.Root!
               .Element(xmlns + "PropertyGroup")!;

            var targetFx = propGroup.Element(xmlns + "TargetFramework")?.Value
               ?? propGroup.Element(xmlns + "TargetFrameworks")?.Value.Split(';')[1];

            refDll = Path.Combine(refDll, targetFx!);
         }

         refDll = Path.Combine(refDll, refName + ".dll");

         return new Uri(_projectUri, refDll).LocalPath;
      }

      // Adding project dependencies as package libraries enables referencing packages from other projects
      void
      AddProjectDependencies(XDocument projectDoc, XcstCompiler compiler) {

         XNamespace xmlns = projectDoc.Root!.Name.Namespace;

         foreach (var projRef in projectDoc.Root.Elements(xmlns + "ItemGroup").Elements(xmlns + "ProjectReference")) {

            var refPath = projRef.Attribute("Include")!.Value;
            var refDll = ReferenceAssemblyPath(refPath);

            if (File.Exists(refDll)) {
               compiler.AddPackageLibrary(refDll);
            }
         }
      }

      static string
      FileNamespace(Uri fileUri, Uri startUri, string rootNamespace) {

         var ns = rootNamespace;
         var relativePath = startUri.MakeRelativeUri(fileUri).OriginalString;

         if (relativePath.Contains("/")) {

            var relativeDir = startUri.MakeRelativeUri(new Uri(Path.GetDirectoryName(fileUri.LocalPath)!, UriKind.Absolute))
               .OriginalString;

            ns = String.Join(".", new[] { ns }.Concat(
               relativeDir
                  .Split('/')
                  .Select(n => CleanIdentifier(n))));
         }

         return ns;
      }

      // Transforms invalid identifier (class, namespace, variable) characters
      static string
      CleanIdentifier(string identifier) =>
         Regex.Replace(identifier, "[^a-z0-9_]", "_", RegexOptions.IgnoreCase);

      // Show compilation errors on Visual Studio's Error List
      // Also makes the error on the Output window clickable
      static void
      VisualStudioErrorLog(RuntimeException ex) {

         dynamic? errorData = ex.ErrorData;

         if (errorData != null) {

            var uriString = errorData.ModuleUri;
            var path = (Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri) && uri.IsFile) ?
               uri.LocalPath
               : uriString;

            Console.WriteLine($"{path}({errorData.LineNumber}): XCST error {ex.ErrorCode}: {ex.Message}");
         }
      }

      void
      WriteAutogeneratedComment(TextWriter output) {

         var prefix = (this.Language == "vb") ? "'" : "//";

         output.WriteLine(prefix + "------------------------------------------------------------------------------");
         output.WriteLine(prefix + " <auto-generated>");
         output.WriteLine(prefix + $"     This code was generated by {typeof(XcstCompiler).Namespace}.");
         output.WriteLine(prefix + "");
         output.WriteLine(prefix + "     Changes to this file may cause incorrect behavior and will be lost if");
         output.WriteLine(prefix + "     the code is regenerated.");
         output.WriteLine(prefix + " </auto-generated>");
         output.WriteLine(prefix + "------------------------------------------------------------------------------");
      }

      void
      Run(TextWriter output) {

         var startUri = new Uri(_projectUri, ".");

         var compilerFact = new XcstCompilerFactory {
            EnableExtensions = true,
            //ProcessXInclude = true
         };

         // Enable "application" extension
         var appExt = new Xcst.Web.Extension.ExtensionPackage {
            ApplicationUri = startUri,
            GenerateLinkTo = true,
            //GenerateHref = true,
            AnnotateVirtualPath = true
         };

         compilerFact.RegisterExtension(appExt);

         var compiler = compilerFact.CreateCompiler();
         compiler.PackageFileDirectory = startUri.LocalPath;
         compiler.PackageFileExtension = _fileExt;
         compiler.IndentChars = "   ";

         var projectDoc = XDocument.Load(_projectUri.LocalPath);

         var rootNamespace = RootNamespace(projectDoc, _projectUri.LocalPath);
         var nullable = Nullable(projectDoc);

         if (nullable != null) {
            compiler.NullableAnnotate = true;
            compiler.NullableContext = nullable;
         }

         AddProjectDependencies(projectDoc, compiler);
         WriteAutogeneratedComment(output);

         compiler.CompilationUnitHandler = href => output;

         foreach (var file in Directory.EnumerateFiles(startUri.LocalPath, "*." + _fileExt, SearchOption.AllDirectories)) {

            var fileUri = new Uri(file, UriKind.Absolute);
            var fileName = Path.GetFileName(file);
            var fileBaseName = Path.GetFileNameWithoutExtension(file);

            // Ignore files starting with underscore
            if (fileName[0] == '_') {
               continue;
            }

            // Treat files ending with 'Package' as library packages; other files as pages
            // An alternative would be to use different file extensions for library packages and pages
            var isPage = _pageEnable
               && !fileBaseName.EndsWith("Package");

            compiler.TargetNamespace = FileNamespace(fileUri, startUri, rootNamespace);

            if (isPage) {

               compiler.TargetClass = "_Page_" + CleanIdentifier(fileBaseName);

               if (_pageBaseType != null) {
                  compiler.TargetBaseTypes = new[] { _pageBaseType };
               }

            } else {

               compiler.TargetClass = CleanIdentifier(fileBaseName);
               compiler.TargetBaseTypes = null;
            }

            appExt.IsPage = isPage;

            try {
               compiler.Compile(fileUri);

            } catch (RuntimeException ex) {
               VisualStudioErrorLog(ex);
               throw;
            }
         }
      }

      public static void
      Main(string[] args) {

         var currentDir = Environment.CurrentDirectory;

         if (currentDir.Last() != Path.DirectorySeparatorChar) {
            currentDir += Path.DirectorySeparatorChar;
         }

         var callerBaseUri = new Uri(currentDir, UriKind.Absolute);
         var projectUri = new Uri(callerBaseUri, args[0]);
         var config = args[1];

         var pageEnable = false;
         string? pageBaseType = null;

         bool nextArgIsValue(int i) =>
            i + 1 < args.Length
               && args[i + 1][0] != '-';

         for (int i = 2; i < args.Length; i++) {

            var name = args[i].Substring(1);

            switch (name) {
               case "PageEnable":
                  pageEnable = true;

                  if (nextArgIsValue(i)) {
                     i++;
                     pageEnable = Boolean.Parse(args[i]);
                  }
                  break;

               case "PageBaseType":

                  if (nextArgIsValue(i)) {
                     i++;
                     pageBaseType = args[i];
                  }
                  break;

               default:
                  throw new ArgumentException($"Unknown parameter '{name}'.", nameof(args));
            }
         }

         var outputUri = new Uri(projectUri, $"xcst.generated.{ProjectLang(projectUri)}");

         using var output = File.CreateText(outputUri.LocalPath);

         // Because XML parsers normalize CRLF to LF,
         // we want to be consistent with the additional content we create
         output.NewLine = "\n";

         new Program(projectUri, config, pageEnable, pageBaseType)
            .Run(output);
      }
   }
}
