// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Web.Mvc {

   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
   public sealed class BindAttribute : Attribute {

      static readonly char[] _splitParameter = new[] { ',' };
      string _exclude;
      string[] _excludeSplit = new string[0];
      string _include;
      string[] _includeSplit = new string[0];

      public string Exclude {
         get => _exclude ?? String.Empty;
         set {
            _exclude = value;
            _excludeSplit = SplitString(value);
         }
      }

      public string Include {
         get => _include ?? String.Empty;
         set {
            _include = value;
            _includeSplit = SplitString(value);
         }
      }

      public string Prefix { get; set; }

      internal static bool IsPropertyAllowed(string propertyName, ICollection<string> includeProperties, ICollection<string> excludeProperties) {

         // We allow a property to be bound if its both in the include list AND not in the exclude list.
         // An empty include list implies all properties are allowed.
         // An empty exclude list implies no properties are disallowed.

         bool includeProperty = (includeProperties is null) || (includeProperties.Count == 0) || includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
         bool excludeProperty = (excludeProperties != null) && excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);

         return includeProperty && !excludeProperty;
      }

      public bool IsPropertyAllowed(string propertyName) =>
         IsPropertyAllowed(propertyName, _includeSplit, _excludeSplit);

      static string[] SplitString(string original) {

         if (String.IsNullOrEmpty(original)) {
            return new string[0];
         }

         var split = from piece in original.Split(_splitParameter)
                     let trimmed = piece.Trim()
                     where !String.IsNullOrEmpty(trimmed)
                     select trimmed;

         return split.ToArray();
      }
   }
}
