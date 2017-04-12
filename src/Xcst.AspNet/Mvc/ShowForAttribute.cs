// Copyright 2017 Max Toro Q.
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
using System.ComponentModel;
using System.Web.Mvc;

namespace Xcst.Web.Mvc {

   /// <exclude/>

   [EditorBrowsable(EditorBrowsableState.Never)]
   [AttributeUsage(AttributeTargets.Property)]
   public class ShowForAttribute : Attribute, IMetadataAware {

      bool displaySet;
      bool display;

      bool editSet;
      bool edit;

      public bool Display {
         get { return display; }
         set {
            display = value;
            displaySet = true;
         }
      }

      public bool Edit {
         get { return edit; }
         set {
            edit = value;
            editSet = true;
         }
      }

      public void OnMetadataCreated(ModelMetadata metadata) {

         if (metadata == null) throw new ArgumentNullException(nameof(metadata));

         // because the framework uses true as default, we need a way to 
         // tell if a value is explicitly specified, hence the use of AdditionalValues

         if (this.displaySet) {
            metadata.ShowForDisplay = this.display;
            metadata.AdditionalValues[nameof(metadata.ShowForDisplay)] = this.display;
         }

         if (this.editSet) {
            metadata.ShowForEdit = this.edit;
            metadata.AdditionalValues[nameof(metadata.ShowForEdit)] = this.edit;
         }
      }
   }
}
