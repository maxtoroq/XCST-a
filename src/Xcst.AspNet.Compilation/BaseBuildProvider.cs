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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using Xcst.Compiler;

namespace Xcst.Web.Compilation {

   public abstract class BaseBuildProvider : BuildProvider {

      string? _generatedTypeName, _generatedTypeNamespace, _generatedTypeFullName;
      Uri? _physicalPath;
      string? _appRelativeVirtualPath;
      bool? _isFileInCodeDir;

      bool _parsed;
      CompilerType? _codeCompilerType;

      protected string AppRelativeVirtualPath =>
         _appRelativeVirtualPath ??= VirtualPathUtility.ToAppRelative(VirtualPath);

      protected Uri PhysicalPath =>
         _physicalPath ??= new Uri(HostingEnvironment.MapPath(VirtualPath), UriKind.Absolute);

      protected bool IsFileInCodeDir =>
         _isFileInCodeDir ??= AppRelativeVirtualPath
            .Remove(0, 2)
            .Split('/')[0]
            .Equals("App_Code", StringComparison.OrdinalIgnoreCase);

      protected string GeneratedTypeName {
         get {
            if (_generatedTypeName is null) {

               string typeName;

               _generatedTypeNamespace = GetNamespaceAndTypeNameFromVirtualPath((IsFileInCodeDir) ? 1 : 0, out typeName);
               _generatedTypeName = GeneratedTypeNamePrefix + typeName;
            }
            return _generatedTypeName;
         }
      }

      protected virtual string? GeneratedTypeNamePrefix => null;

      [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "s")]
      protected string GeneratedTypeNamespace {
         get {
            if (_generatedTypeNamespace is null) {
               // getting GeneratedTypeName will initialize _GeneratedTypeNamespace
               string s = GeneratedTypeName;
            }
            return _generatedTypeNamespace!;
         }
      }

      protected string GeneratedTypeFullName {
         get => _generatedTypeFullName ??= (GeneratedTypeNamespace.Length == 0) ?
            GeneratedTypeName
            : String.Concat(GeneratedTypeNamespace, ".", GeneratedTypeName);
         set {
            if (String.IsNullOrEmpty(value)) {
               throw new ArgumentException("value cannot be null or empty", nameof(value));
            }

            _generatedTypeName = _generatedTypeNamespace = _generatedTypeFullName = null;

            if (value.Contains(".")) {
               string[] segments = value.Split('.');
               _generatedTypeName = segments[segments.Length - 1];
               _generatedTypeNamespace = String.Join(".", segments, 0, segments.Length - 1);
            } else {
               _generatedTypeName = value;
               _generatedTypeNamespace = String.Empty;
            }
         }
      }

      protected virtual bool IgnoreFile => false;

      public override CompilerType? CodeCompilerType {
         get {
            if (IgnoreFile) {
               return null;
            }

            if (!_parsed) {

               string language = Parse();

               _codeCompilerType = (!String.IsNullOrEmpty(language)) ?
                  GetDefaultCompilerTypeForLanguage(language)
                  : null;

               _parsed = true;
            }

            return _codeCompilerType;
         }
      }

      string Parse() {

         try {
            return ParsePath();

         } catch (CompileException ex) {

            string? moduleUri = ex.ModuleUri;

            if (moduleUri != null) {

               if (Uri.TryCreate(moduleUri, UriKind.Absolute, out Uri uri)
                  && uri.IsFile) {

                  moduleUri = uri.LocalPath;
               }
            }

            throw new HttpParseException(ex.Message, ex, moduleUri ?? this.VirtualPath, null, ex.LineNumber);
         }
      }

      protected abstract string ParsePath();

      protected abstract IEnumerable<CodeCompileUnit> BuildCompileUnits();

      public override void GenerateCode(AssemblyBuilder assemblyBuilder) {

         if (this.IgnoreFile) {
            return;
         }

         foreach (CodeCompileUnit compileUnit in BuildCompileUnits()) {
            assemblyBuilder.AddCodeCompileUnit(this, compileUnit);
         }
      }

      public override Type? GetGeneratedType(CompilerResults results) {

         if (this.IgnoreFile) {
            return null;
         }

         return results.CompiledAssembly.GetType(this.GeneratedTypeFullName);
      }

      protected Exception CreateParseException(string message, int line, string? virtualPath = null, Exception? innerException = null) =>
         new HttpParseException(message, innerException, virtualPath ?? this.VirtualPath, null, line);

      string GetNamespaceAndTypeNameFromVirtualPath(int chunksToIgnore, out string typeName) {

         string fileName = (this.IsFileInCodeDir) ?
            VirtualPathUtility.GetFileName(this.VirtualPath) :
            this.AppRelativeVirtualPath.Remove(0, 2);

         string[] strArray = fileName.Split(new char[] { '.', '/', '\\' });
         int num = strArray.Length - chunksToIgnore;

         if (strArray[num - 1].Trim().Length == 0) {
            throw new HttpException($"The file name '{fileName}' is not supported.");
         }

         typeName = MakeValidTypeNameFromString(
            (this.IsFileInCodeDir) ? strArray[num - 1]
               : String.Join("_", strArray, 0, num).ToLowerInvariant()
         );

         if (!this.IsFileInCodeDir) {
            return "ASP";
         }

         for (int i = 0; i < (num - 1); i++) {

            if (strArray[i].Trim().Length == 0) {
               throw new HttpException($"The file name '{fileName}' is not supported.");
            }

            strArray[i] = MakeValidTypeNameFromString(strArray[i]);
         }

         return String.Join(".", strArray, 0, num - 1);
      }

      string MakeValidTypeNameFromString(string s) {

         var builder = new StringBuilder();

         for (int i = 0; i < s.Length; i++) {

            if ((i == 0) && char.IsDigit(s[0])) {
               builder.Append('_');
            }

            builder.Append(char.IsLetterOrDigit(s[i]) ? s[i] : '_');
         }
         return builder.ToString();
      }
   }
}
