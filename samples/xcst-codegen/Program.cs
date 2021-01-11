using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xcst.Compiler;

namespace XcstCodeGen {

   class Program {

      const string _fileExt = "xcst";
      const string _pageBaseType = "global::AspNetPrecompiled.AppPage";

      readonly Uri _projectUri;
      readonly string _configuration;
      readonly bool _libsAndPages;

      public Program(Uri projectUri, string configuration, bool libsAndPages) {

         _projectUri = projectUri;
         _configuration = configuration;
         _libsAndPages = libsAndPages;
      }

      static bool IsSdkStyle(XDocument projectDoc) =>
         projectDoc.Root.Name.NamespaceName.Length == 0;

      static string AssemblyName(XDocument projectDoc, string projectPath) {

         XNamespace xmlns = projectDoc.Root.Name.Namespace;

         return projectDoc.Root
            .Element(xmlns + "PropertyGroup")
            .Element(xmlns + "AssemblyName")?.Value
            ?? Path.GetFileNameWithoutExtension(projectPath);
      }

      static string RootNamespace(XDocument projectDoc, string projectPath) {

         XNamespace xmlns = projectDoc.Root.Name.Namespace;

         return projectDoc.Root
            .Element(xmlns + "PropertyGroup")
            .Element(xmlns + "RootNamespace")?.Value
            ?? Path.GetFileNameWithoutExtension(projectPath);
      }

      static string Nullable(XDocument projectDoc) {

         XNamespace xmlns = projectDoc.Root.Name.Namespace;

         return projectDoc.Root
            .Element(xmlns + "PropertyGroup")
            .Element(xmlns + "Nullable")?.Value;
      }

      string ReferenceAssemblyPath(string refPath) {

         XDocument refDoc = XDocument.Load(new Uri(_projectUri, refPath).LocalPath);
         XNamespace xmlns = refDoc.Root.Name.Namespace;

         string refName = AssemblyName(refDoc, refPath);
         string refDir = Path.GetDirectoryName(refPath);
         string refDll = Path.Combine(refDir, "bin", _configuration);

         if (IsSdkStyle(refDoc)) {

            XElement propGroup = refDoc.Root
               .Element(xmlns + "PropertyGroup");

            string targetFx = propGroup.Element(xmlns + "TargetFramework")?.Value
               ?? propGroup.Element(xmlns + "TargetFrameworks")?.Value.Split(';')[1];

            refDll = Path.Combine(refDll, targetFx);
         }

         refDll = Path.Combine(refDll, refName + ".dll");

         return new Uri(_projectUri, refDll).LocalPath;
      }

      // Adding project dependencies as package libraries enables referencing packages from other projects
      void AddProjectDependencies(XDocument projectDoc, XcstCompiler compiler) {

         XNamespace xmlns = projectDoc.Root.Name.Namespace;

         foreach (XElement projRef in projectDoc.Root.Elements(xmlns + "ItemGroup").Elements(xmlns + "ProjectReference")) {

            string refPath = projRef.Attribute("Include").Value;
            string refDll = ReferenceAssemblyPath(refPath);

            if (File.Exists(refDll)) {
               compiler.AddPackageLibrary(refDll);
            }
         }
      }

      // Transforms invalid identifier (class, namespace, variable) characters
      string CleanIdentifier(string identifier) =>
         Regex.Replace(identifier, "[^a-z0-9_]", "_", RegexOptions.IgnoreCase);

      // Show compilation errors on Visual Studio's Error List
      // Also makes the error on the Output window clickable
      void VisualStudioErrorLog(CompileException ex) {

         string uriString = ex.ModuleUri;
         var uri = new Uri(uriString);
         string path = (uri.IsFile) ? uri.LocalPath : uriString;

         Console.WriteLine($"{path}({ex.LineNumber}): XCST error {ex.ErrorCode}: {ex.Message}");
      }

      void Run(TextWriter output) {

         var startUri = new Uri(_projectUri, ".");

         var compilerFact = new XcstCompilerFactory {
            EnableExtensions = true
         };

         // Enable "application" extension
         compilerFact.RegisterExtension(new Xcst.Web.Extension.ExtensionLoader {
            ApplicationUri = startUri,
            GenerateLinkTo = true,
            AnnotateVirtualPath = true
         });

         XcstCompiler compiler = compilerFact.CreateCompiler();

         // No need to look for library packages on loaded assemblies (perf. tweak)
         compiler.PackageTypeResolver = n => null;

         compiler.PackageFileDirectory = startUri.LocalPath;
         compiler.PackageFileExtension = _fileExt;
         compiler.IndentChars = "   ";
         compiler.CompilationUnitHandler = href => output;

         XDocument projectDoc = XDocument.Load(_projectUri.LocalPath);

         string rootNamespace = RootNamespace(projectDoc, _projectUri.LocalPath);
         string nullable = Nullable(projectDoc);

         if (nullable != null) {
            compiler.NullableAnnotate = true;
            compiler.NullableContext = nullable;
         }

         AddProjectDependencies(projectDoc, compiler);

         output.WriteLine("//------------------------------------------------------------------------------");
         output.WriteLine("// <auto-generated>");
         output.WriteLine("//     This code was generated by a tool.");
         output.WriteLine("//");
         output.WriteLine("//     Changes to this file may cause incorrect behavior and will be lost if");
         output.WriteLine("//     the code is regenerated.");
         output.WriteLine("// </auto-generated>");
         output.WriteLine("//------------------------------------------------------------------------------");

         if (_libsAndPages) {
            output.WriteLine();
            output.WriteLine("[assembly: global::Xcst.Web.Precompilation.PrecompiledModule]");
         }

         foreach (string file in Directory.EnumerateFiles(startUri.LocalPath, "*." + _fileExt, SearchOption.AllDirectories)) {

            var fileUri = new Uri(file, UriKind.Absolute);
            string fileName = Path.GetFileName(file);
            string fileBaseName = Path.GetFileNameWithoutExtension(file);

            // Ignore files starting with underscore
            if (fileName[0] == '_') {
               continue;
            }

            string relativePath = startUri.MakeRelativeUri(fileUri).OriginalString;

            // Treat files ending with 'Package' as library packages; other files as pages
            // Library packages must be rooted at <c:package> and have a name
            // Pages must NOT be named
            // An alternative would be to use different file extensions for library packages and pages
            bool isPage = _libsAndPages
               && !fileBaseName.EndsWith("Package");

            compiler.NamedPackage = !isPage;

            if (isPage) {

               string ns = rootNamespace;

               if (relativePath.Contains("/")) {

                  string relativeDir = startUri.MakeRelativeUri(new Uri(Path.GetDirectoryName(file), UriKind.Absolute))
                     .OriginalString;

                  ns = String.Join(".", new[] { ns }.Concat(
                     relativeDir
                        .Split('/')
                        .Select(n => CleanIdentifier(n))));
               }

               compiler.TargetClass = "_Page_" + CleanIdentifier(fileBaseName);
               compiler.TargetNamespace = ns;
               compiler.TargetBaseTypes = new[] { _pageBaseType };
               compiler.TargetVisibility = CodeVisibility.Internal;

            } else {

               compiler.TargetClass = null;
               compiler.TargetNamespace = null;
               compiler.TargetBaseTypes = null;
               compiler.TargetVisibility = CodeVisibility.Public;
            }

            CompileResult xcstResult;

            try {
               xcstResult = compiler.Compile(fileUri);

            } catch (CompileException ex) {
               VisualStudioErrorLog(ex);
               throw;
            }
         }
      }

      public static void Main(string[] args) {

         string currentDir = Environment.CurrentDirectory;

         if (currentDir.Last() != Path.DirectorySeparatorChar) {
            currentDir += Path.DirectorySeparatorChar;
         }

         var callerBaseUri = new Uri(currentDir, UriKind.Absolute);
         var projectUri = new Uri(callerBaseUri, args[0]);
         string config = args[1];

         bool libsAndPages = (args.Length > 2) ?
            args[2] == "-LibsAndPages"
            : false;

         var outputUri = new Uri(projectUri, "xcst.generated.cs");

         using (var output = File.CreateText(outputUri.LocalPath)) {

            // Because XML parsers normalize CRLF to LF,
            // we want to be consistent with the additional content we create
            output.NewLine = "\n";

            new Program(projectUri, config, libsAndPages)
               .Run(output);
         }
      }
   }
}
