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
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using Xcst.Compiler;
using Xcst.Web.Mvc;

namespace Xcst.Web.Configuration {

   public sealed class XcstWebConfiguration {

      public const string FileExtension = "xcst";

      public static XcstWebConfiguration Instance { get; } = new XcstWebConfiguration();

      public XcstCompilerFactory CompilerFactory { get; } = new XcstCompilerFactory();

      internal IList<Func<object, IHttpHandler>> HttpHandlerFactories { get; } = new List<Func<object, IHttpHandler>>();

      public EditorTemplatesConfiguration EditorTemplates { get; } = new EditorTemplatesConfiguration();

#if ASPNETLIB
      public DisplayTemplatesConfiguration DisplayTemplates { get; } = new DisplayTemplatesConfiguration();

      public ModelBindingConfiguration ModelBinding { get; } = new ModelBindingConfiguration();

      /// <summary>
      /// Instructs the ASP.NET runtime to perform request validation. The default is true.
      /// </summary>

      public void RegisterHandlerFactory(Func<object, IHttpHandler> handlerFactory) {
         this.HttpHandlerFactories.Insert(0, handlerFactory);
      }
#endif

      private XcstWebConfiguration() {

         this.CompilerFactory.EnableExtensions = true;
         this.CompilerFactory.RegisterApplicationExtension();
      }
   }

   public class EditorTemplatesConfiguration {

      /// <summary>
      /// Default message used by <code>a:validation-message</code> and <code>a:validation-summary</code>
      /// when model state contains an error but with a null or empty message.
      /// </summary>

      public Func<string> DefaultValidationMessage { get; set; }

#if ASPNETLIB
      /// <summary>
      /// Validation message for numeric types.
      /// </summary>

      public Func<string> NumberValidationMessage { get; set; }

      /// <summary>
      /// Validation message for date types.
      /// </summary>

      public Func<string> DateValidationMessage { get; set; }

      public Func<string, ViewContext, XcstViewPage> TemplateFactory { get; set; }
#endif

      public Func<EditorInfo, string, string> EditorCssClass { get; set; }
   }

#if ASPNETLIB

   public class DisplayTemplatesConfiguration {
      public Func<string, ViewContext, XcstViewPage> TemplateFactory { get; set; }
   }

   public class ModelBindingConfiguration {

      /// <summary>
      /// Default message used when setting a property results in an exception.
      /// </summary>

      public Func<string> DefaultInvalidPropertyValueErrorMessage { get; set; }

      /// <summary>
      /// Default message used when there's no value for a non-nullable property that does not explicitly
      /// use the <code>RequiredAttribute</code>.
      /// </summary>

      public Func<string> DefaultRequiredPropertyValueErrorMessage { get; set; }
   }
#endif
}
