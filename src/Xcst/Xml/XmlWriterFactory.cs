﻿// Copyright 2015 Max Toro Q.
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
using System.IO;
using System.Text;
using System.Xml;

namespace Xcst.Xml {

   abstract class XmlWriterFactory : IWriterFactory {

      readonly OutputParameters overrideParameters;
      bool disposed;

      public Uri OutputUri { get; }

      protected XmlWriterFactory(Uri outputUri, OutputParameters overrideParameters = null) {

         if (outputUri == null) throw new ArgumentNullException(nameof(outputUri));

         this.OutputUri = outputUri;
         this.overrideParameters = overrideParameters;
      }

      public XmlWriter Create(OutputParameters defaultParameters) {

         if (this.disposed) {
            throw new ObjectDisposedException(GetType().FullName);
         }

         if (this.overrideParameters != null) {
            defaultParameters.Merge(this.overrideParameters);
         }

         XmlWriter writer = CreateWriter(defaultParameters);

         if (defaultParameters.Method == OutputParameters.StandardMethods.XHtml) {
            writer = new XHtmlWriter(writer);
         }

         return writer;
      }

      protected abstract XmlWriter CreateWriter(OutputParameters finalParameters);

      public void Dispose() {

         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing) {

         if (this.disposed) {
            return;
         }

         this.disposed = true;
      }
   }

   class StreamXmlWriterFactory : XmlWriterFactory {

      readonly Stream output;
      readonly bool autoClose;

      public StreamXmlWriterFactory(Stream output, Uri outputUri, OutputParameters overrideParameters = null, bool autoClose = false)
         : base(outputUri, overrideParameters) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         this.output = output;
         this.autoClose = autoClose;
      }

      protected override XmlWriter CreateWriter(OutputParameters finalParameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(finalParameters);
         writerSettings.CloseOutput = autoClose;

         return XmlWriter.Create(this.output, writerSettings);
      }

      protected override void Dispose(bool disposing) {

         if (disposing
            && this.autoClose) {

            this.output.Dispose();
         }

         base.Dispose(disposing);
      }
   }

   class TextXmlWriterFactory : XmlWriterFactory {

      readonly TextWriter output;
      readonly bool autoClose;

      public TextXmlWriterFactory(TextWriter output, Uri outputUri, OutputParameters overrideParameters = null, bool autoClose = false)
         : base(outputUri, overrideParameters) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         this.output = output;
         this.autoClose = autoClose;
      }

      protected override XmlWriter CreateWriter(OutputParameters finalParameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(finalParameters);
         writerSettings.CloseOutput = autoClose;

         return XmlWriter.Create(this.output, writerSettings);
      }

      protected override void Dispose(bool disposing) {

         if (disposing
            && this.autoClose) {

            this.output.Dispose();
         }

         base.Dispose(disposing);
      }
   }

   class StringXmlWriterFactory : XmlWriterFactory {

      readonly StringBuilder output;

      public StringXmlWriterFactory(StringBuilder output, OutputParameters overrideParameters = null)
         : base(new Uri("", UriKind.Relative), overrideParameters) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         this.output = output;
      }

      protected override XmlWriter CreateWriter(OutputParameters finalParameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(finalParameters);

         return XmlWriter.Create(this.output, writerSettings);
      }
   }

   class FileUriXmlWriterFactory : XmlWriterFactory {

      public FileUriXmlWriterFactory(Uri file, OutputParameters overrideParameters = null)
         : base(file, overrideParameters) { }

      protected override XmlWriter CreateWriter(OutputParameters finalParameters) {

         var writerSettings = XmlWriterSettingsFactory.Create(finalParameters);
         writerSettings.CloseOutput = true;

         return XmlWriter.Create(this.OutputUri.LocalPath, writerSettings);
      }
   }

   class InstanceXmlWriterFactory : XmlWriterFactory {

      readonly XmlWriter output;

      public InstanceXmlWriterFactory(XmlWriter output, Uri outputUri)
         : base(outputUri) {

         if (output == null) throw new ArgumentNullException(nameof(output));

         this.output = output;
      }

      protected override XmlWriter CreateWriter(OutputParameters finalParameters) {
         return output;
      }
   }
}