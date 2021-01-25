// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Xml;
using Xcst.Compiler;
using Xcst.Web.Mvc;

namespace Xcst.Web.Compilation {

   [BuildProviderAppliesTo(BuildProviderAppliesTo.Web | BuildProviderAppliesTo.Code)]
   public class PageBuildProvider : BaseBuildProvider {

      readonly Uri _applicationUri = new Uri(HostingEnvironment.ApplicationPhysicalPath, UriKind.Absolute);
      CompileResult? _result;
      LoggingResolver? _moduleResolver;
      bool? _ignoreFile;

      public static XcstCompilerFactory CompilerFactory { get; } = new XcstCompilerFactory {
         EnableExtensions = true
      };

      public const string FileExtension = "xcst";

      public override ICollection VirtualPathDependencies {
         get {

            if (_moduleResolver != null) {
               return
                  new[] { VirtualPath }.Concat(
                     from u in _moduleResolver.ResolvedUris
                     let rel = _applicationUri.MakeRelativeUri(u)
                     where !rel.IsAbsoluteUri
                        && !rel.OriginalString.StartsWith("..")
                     let vp = VirtualPathUtility.ToAbsolute("~/" + rel.OriginalString)
                     select vp)
                  .ToArray();
            }

            return base.VirtualPathDependencies;
         }
      }

      protected override string GeneratedTypeNamePrefix => "_Page_";

      protected override bool IgnoreFile =>
         _ignoreFile ??= VirtualPath.Split('/').Last()[0] == '_';

      protected Type PageType { get; }

      public PageBuildProvider()
         : this(typeof(XcstPage)) { }

      protected PageBuildProvider(Type pageType) {

         if (pageType is null) throw new ArgumentNullException(nameof(pageType));
         if (!pageType.IsClass) throw new ArgumentException("pageType must be a class.", nameof(pageType));

         this.PageType = pageType;
      }

      protected override string ParsePath() {

         using (Stream source = OpenStream()) {

            XcstCompiler compiler = CompilerFactory.CreateCompiler();

            ConfigureCompiler(compiler);

            _result = compiler.Compile(source, baseUri: this.PhysicalPath);

            return _result.Language;
         }
      }

      protected virtual void ConfigureCompiler(XcstCompiler compiler) {

         compiler.PackageTypeResolver = typeName => BuildManager.GetType(typeName, throwOnError: false);
         compiler.PackageFileDirectory = HostingEnvironment.MapPath("~/App_Code");
         compiler.PackageFileExtension = FileExtension;
         compiler.UseLineDirective = true;

         if (this.IsFileInCodeDir) {
            compiler.NamedPackage = true;
         } else {

            compiler.SetTargetBaseTypes(this.PageType);
            compiler.TargetNamespace = this.GeneratedTypeNamespace;
            compiler.TargetClass = this.GeneratedTypeName;

            compiler.ModuleResolver = (_moduleResolver = new LoggingResolver(new XmlUrlResolver()));
         }

         compiler.SetParameter(XmlNamespaces.XcstApplication, "application-uri", _applicationUri);
         compiler.SetParameter(XmlNamespaces.XcstApplication, "generate-href", true);
      }

      protected override IEnumerable<CodeCompileUnit> BuildCompileUnits() {

         // The 'Show Complete Compilation Source' feature of the ASP.NET server error page
         // shows the last compile unit. Returning IFileDependent partial first.

         if (!this.IsFileInCodeDir) {
            yield return FileDependentPartial();
         }

         foreach (string unit in _result!.CompilationUnits) {
            yield return new CodeSnippetCompileUnit(unit);
         }
      }

      CodeCompileUnit FileDependentPartial() {

         var fileArray = new CodeArrayCreateExpression(typeof(string));
         fileArray.Initializers.Add(new CodePrimitiveExpression(this.PhysicalPath.LocalPath));

         foreach (Uri uri in _moduleResolver!.ResolvedUris.Where(u => u.IsFile)) {
            fileArray.Initializers.Add(new CodePrimitiveExpression(uri.LocalPath));
         }

         return new CodeCompileUnit {
            Namespaces = {
               new CodeNamespace(this.GeneratedTypeNamespace) {
                  Types = {
                     new CodeTypeDeclaration(this.GeneratedTypeName) {
                        IsClass = true,
                        IsPartial = true,
                        BaseTypes = {
                           new CodeTypeReference(typeof(IFileDependent))
                        },
                        Members = {
                           new CodeMemberProperty {
                              Name = nameof(IFileDependent.FileDependencies),
                              PrivateImplementationType = new CodeTypeReference(typeof(IFileDependent)),
                              Type = new CodeTypeReference(fileArray.CreateType, 1),
                              GetStatements = {
                                 new CodeMethodReturnStatement(fileArray)
                              }
                           }
                        }
                     }
                  }
               }
            }
         };
      }

      public override void GenerateCode(AssemblyBuilder assemblyBuilder) {

         if (this.IgnoreFile) {
            return;
         }

         base.GenerateCode(assemblyBuilder);

         assemblyBuilder.AddAssemblyReference(typeof(Xcst.PackageModel.IXcstPackage).Assembly);

         if (!this.IsFileInCodeDir) {
            assemblyBuilder.AddAssemblyReference(this.PageType.Assembly);
            assemblyBuilder.GenerateTypeFactory(this.GeneratedTypeFullName);
         }
      }

      class LoggingResolver : XmlResolver {

         readonly XmlResolver wrappedResolver;

         public HashSet<Uri> ResolvedUris { get; } = new HashSet<Uri>();

         public override ICredentials Credentials {
            set => wrappedResolver.Credentials = value;
         }

         public LoggingResolver(XmlResolver wrappedResolver) {

            if (wrappedResolver is null) throw new ArgumentNullException(nameof(wrappedResolver));

            this.wrappedResolver = wrappedResolver;
         }

         public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) {

            object entity = this.wrappedResolver.GetEntity(absoluteUri, role, ofObjectToReturn);

            this.ResolvedUris.Add(absoluteUri);

            return entity;
         }

         public override Task<object> GetEntityAsync(Uri absoluteUri, string role, Type ofObjectToReturn) {

            var task = this.wrappedResolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);

            this.ResolvedUris.Add(absoluteUri);

            return task;
         }

         public override bool SupportsType(Uri absoluteUri, Type type) =>
            this.wrappedResolver.SupportsType(absoluteUri, type);

         public override Uri ResolveUri(Uri baseUri, string relativeUri) =>
            this.wrappedResolver.ResolveUri(baseUri, relativeUri);
      }
   }

   public class ViewPageBuildProvider : PageBuildProvider {

      public ViewPageBuildProvider()
         : this(typeof(XcstViewPage)) { }

      protected ViewPageBuildProvider(Type pageType)
         : base(pageType) { }

      protected override void ConfigureCompiler(XcstCompiler compiler) {

         base.ConfigureCompiler(compiler);

         if (this.IsFileInCodeDir) {
            compiler.SetParameter(XmlNamespaces.XcstApplication, "page-type", this.PageType);
         } else {
            compiler.SetParameter(XmlNamespaces.XcstApplication, "default-model-dynamic", true);
         }
      }
   }
}
