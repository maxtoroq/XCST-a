﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xcst;
using Xcst.Compiler;

namespace XcstCodeGen;

class Program {

   const string
   _fileExt = "xcst";

#pragma warning disable CS8618
   public Uri
   ProjectUri { get; init; }

   public string
   Configuration { get; init; }
#pragma warning restore CS8618

   public decimal
   TargetRuntime { get; set; }

   public bool
   PageEnable { get; set; }

   public string?
   PageBaseType { get; set; }

   private string
   Language => ProjectLang(ProjectUri);

   static string
   ProjectLang(Uri projectUri) {
      var projExt = Path.GetExtension(projectUri.LocalPath).TrimStart('.');
      return projExt.Substring(0, projExt.Length - "proj".Length);
   }

   static string
   AssemblyName(XDocument projectDoc, string projectPath) =>
      projectDoc.Root!
         .Element("PropertyGroup")?
         .Element("AssemblyName")?.Value
         ?? Path.GetFileNameWithoutExtension(projectPath);

   static string
   RootNamespace(XDocument projectDoc, string projectPath) =>
      projectDoc.Root!
         .Element("PropertyGroup")?
         .Element("RootNamespace")?.Value
         ?? Path.GetFileNameWithoutExtension(projectPath);

   static string?
   Nullable(XDocument projectDoc) =>
      projectDoc.Root!
         .Element("PropertyGroup")?
         .Element("Nullable")?.Value;

   string
   ReferenceAssemblyPath(string refPath) {

      var refDoc = XDocument.Load(new Uri(ProjectUri, refPath).LocalPath);
      var refName = AssemblyName(refDoc, refPath);
      var refDir = Path.GetDirectoryName(refPath)!;
      var refDll = Path.Combine(refDir, "bin", Configuration);

      var propGroup = refDoc.Root!
         .Element("PropertyGroup")!;

      var targetFx = propGroup.Element("TargetFramework")?.Value
         ?? propGroup.Element("TargetFrameworks")?.Value.Split(';')[1];

      refDll = Path.Combine(refDll, targetFx!);
      refDll = Path.Combine(refDll, refName + ".dll");

      return new Uri(ProjectUri, refDll).LocalPath;
   }

   // Adding project dependencies as package libraries enables referencing packages from other projects
   void
   AddProjectDependencies(XDocument projectDoc, XcstCompiler compiler) {

      foreach (var projRef in projectDoc.Root!.Elements("ItemGroup").Elements("ProjectReference")) {

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

      var prefix = (Language == "vb") ? "'" : "//";

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

      var startUri = new Uri(ProjectUri, ".");

      var compiler = new XcstCompiler {
         PackageFileDirectory = startUri.LocalPath,
         PackageFileExtension = _fileExt,
         IndentChars = "   "
      };

      if (TargetRuntime != default) {
         compiler.TargetRuntime = TargetRuntime;
      }

      // Enable "application" extension
      compiler.RegisterExtension(() => new Xcst.Web.Extension.ExtensionPackage {
         TargetRuntime = 2m,
         ApplicationUri = startUri,
         GenerateLinkTo = true,
         AnnotateVirtualPath = true
      });

      var projectDoc = XDocument.Load(ProjectUri.LocalPath);
      var rootNamespace = RootNamespace(projectDoc, ProjectUri.LocalPath);
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
         var isPage = PageEnable
            && !fileBaseName.EndsWith("Package");

         compiler.TargetNamespace = FileNamespace(fileUri, startUri, rootNamespace);

         if (isPage) {

            compiler.TargetClass = "_Page_" + CleanIdentifier(fileBaseName);

            if (PageBaseType != null) {
               compiler.TargetBaseTypes = new[] { PageBaseType };
            }

         } else {

            compiler.TargetClass = CleanIdentifier(fileBaseName);
            compiler.TargetBaseTypes = null;
         }

         Xcst.Web.Extension.ExtensionPackage.IsPage(compiler.SetTunnelParam, isPage);

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

      if (currentDir[^1] != Path.DirectorySeparatorChar) {
         currentDir += Path.DirectorySeparatorChar;
      }

      var callerBaseUri = new Uri(currentDir, UriKind.Absolute);

      var program = new Program {
         ProjectUri = new Uri(callerBaseUri, args[0]),
         Configuration = args[1]
      };

      bool nextArgIsValue(int i) =>
         i + 1 < args.Length && args[i + 1][0] != '-';

      bool switchArg(ref int i) =>
         nextArgIsValue(i) ? Boolean.Parse(args[++i]) : true;

      decimal decimalArg(ref int i) =>
         nextArgIsValue(i) ? Decimal.Parse(args[++i], CultureInfo.InvariantCulture) : default;

      string? stringArg(ref int i) =>
         nextArgIsValue(i) ? args[++i] : null;

      for (int i = 2; i < args.Length; i++) {

         var name = args[i].Substring(1);

         switch (name) {
            case nameof(TargetRuntime):
               program.TargetRuntime = decimalArg(ref i);
               break;

            case nameof(PageEnable):
               program.PageEnable = switchArg(ref i);
               break;

            case nameof(PageBaseType):
               program.PageBaseType = stringArg(ref i);
               break;

            default:
               throw new ArgumentException($"Unknown parameter '{name}'.", nameof(args));
         }
      }

      var outputUri = new Uri(program.ProjectUri, $"xcst.generated.{ProjectLang(program.ProjectUri)}");

      using var output = File.CreateText(outputUri.LocalPath);

      // Because XML parsers normalize CRLF to LF,
      // we want to be consistent with the additional content we create
      output.NewLine = "\n";

      program.Run(output);
   }
}
