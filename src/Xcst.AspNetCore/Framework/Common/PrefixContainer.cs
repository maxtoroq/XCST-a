﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Xcst.Web.Mvc;

/// <summary>
/// This is a container for prefix values. It normalizes all the values into dotted-form and then stores
/// them in a sorted array. All queries for prefixes are also normalized to dotted-form, and searches
/// for ContainsPrefix are done with a binary search.
/// </summary>
class PrefixContainer {

   readonly ICollection<string>
   _originalValues;

   readonly string[]
   _sortedValues;

   internal
   PrefixContainer(ICollection<string> values) {

      _originalValues = values ?? throw new ArgumentNullException(nameof(values));
      _sortedValues = _originalValues.ToArrayWithoutNulls();
      Array.Sort(_sortedValues, StringComparer.OrdinalIgnoreCase);
   }

   internal bool
   ContainsPrefix(string prefix) {

      if (prefix is null) throw new ArgumentNullException(nameof(prefix));

      if (prefix.Length == 0) {
         return _sortedValues.Length > 0; // only match empty string when we have some value
      }

      var prefixComparer = new PrefixComparer(prefix);
      var containsPrefix = Array.BinarySearch(_sortedValues, prefix, prefixComparer) > -1;

      if (!containsPrefix) {
         // If there's something in the search boundary that starts with the same name
         // as the collection prefix that we're trying to find, the binary search would actually fail.
         // For example, let's say we have foo.a, foo.bE and foo.b[0]. Calling Array.BinarySearch
         // will fail to find foo.b because it will land on foo.bE, then look at foo.a and finally
         // failing to find the prefix which is actually present in the container (foo.b[0]).
         // Here we're doing another pass looking specifically for collection prefix.
         containsPrefix = Array.BinarySearch(_sortedValues, prefix + "[", prefixComparer) > -1;
      }

      return containsPrefix;
   }

   // Given "foo.bar", "foo.hello", "something.other", foo[abc].baz and asking for prefix "foo" will return:
   // - "bar"/"foo.bar"
   // - "hello"/"foo.hello"
   // - "abc"/"foo[abc]"

   internal IDictionary<string, string>
   GetKeysFromPrefix(string prefix) {

      var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      foreach (var entry in _originalValues) {

         if (entry != null) {

            if (entry.Length == prefix.Length) {
               // No key in this entry
               continue;
            }

            if (prefix.Length == 0) {
               GetKeyFromEmptyPrefix(entry, result);
            } else if (entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
               GetKeyFromNonEmptyPrefix(prefix, entry, result);
            }
         }
      }

      return result;
   }

   static void
   GetKeyFromEmptyPrefix(string entry, IDictionary<string, string> results) {

      var dotPosition = entry.IndexOf('.');
      var bracketPosition = entry.IndexOf('[');
      var delimiterPosition = -1;

      if (dotPosition == -1) {
         if (bracketPosition != -1) {
            delimiterPosition = bracketPosition;
         }
      } else {
         if (bracketPosition == -1) {
            delimiterPosition = dotPosition;
         } else {
            delimiterPosition = Math.Min(dotPosition, bracketPosition);
         }
      }

      var key = (delimiterPosition == -1) ? entry
         : entry.Substring(0, delimiterPosition);

      results[key] = key;
   }

   static void
   GetKeyFromNonEmptyPrefix(string prefix, string entry, IDictionary<string, string> results) {

      string key;
      string fullName;
      var keyPosition = prefix.Length + 1;

      switch (entry[prefix.Length]) {
         case '.':

            var dotPosition = entry.IndexOf('.', keyPosition);

            if (dotPosition == -1) {
               dotPosition = entry.Length;
            }

            key = entry.Substring(keyPosition, dotPosition - keyPosition);
            fullName = entry.Substring(0, dotPosition);
            break;

         case '[':

            var bracketPosition = entry.IndexOf(']', keyPosition);

            if (bracketPosition == -1) {
               // Malformed for dictionary
               return;
            }

            key = entry.Substring(keyPosition, bracketPosition - keyPosition);
            fullName = entry.Substring(0, bracketPosition + 1);
            break;

         default:
            return;
      }

      if (!results.ContainsKey(key)) {
         results.Add(key, fullName);
      }
   }

   internal static bool
   IsPrefixMatch(string prefix, string? testString) {

      if (testString is null) {
         return false;
      }

      if (prefix.Length == 0) {
         return true; // shortcut - non-null testString matches empty prefix
      }

      if (prefix.Length > testString.Length) {
         return false; // not long enough
      }

      if (!testString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
         return false; // prefix doesn't match
      }

      if (testString.Length == prefix.Length) {
         return true; // exact match
      }

      // invariant: testString.Length > prefix.Length
      switch (testString[prefix.Length]) {
         case '.':
         case '[':
            return true; // known delimiters

         default:
            return false; // not known delimiter
      }
   }

   class PrefixComparer : IComparer<String> {

      readonly string
      _prefix;

      public
      PrefixComparer(string prefix) {
         _prefix = prefix;
      }

      public int
      Compare(string? x, string? y) {

         var testString = Object.ReferenceEquals(x, _prefix) ? y : x;

         if (IsPrefixMatch(_prefix, testString)) {
            return 0;
         }

         return StringComparer.OrdinalIgnoreCase.Compare(x, y);
      }
   }
}
