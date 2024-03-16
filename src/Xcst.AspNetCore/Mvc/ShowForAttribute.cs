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

namespace Xcst.Web.Mvc;

[GeneratedCodeReference]
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Property)]
public class ShowForAttribute : Attribute {

   internal bool
   _displaySet;

   bool
   _display;

   internal bool
   _editSet;

   bool
   _edit;

   public bool
   Display {
      get => _display;
      set {
         _display = value;
         _displaySet = true;
      }
   }

   public bool
   Edit {
      get => _edit;
      set {
         _edit = value;
         _editSet = true;
      }
   }
}
