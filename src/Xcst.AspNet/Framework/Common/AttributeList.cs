﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace System.Web.Mvc {

   /// <summary>
   /// Wrapper for AttributeCollection to provide generic collection implementation.
   /// </summary>
   sealed class AttributeList : IList<Attribute> {

      readonly AttributeCollection _attributes;

      public int Count =>_attributes.Count;

      public bool IsReadOnly => true;

      public Attribute this[int index] {
         get {
            return _attributes[index];
         }
         set {
            throw new NotSupportedException();
         }
      }

      public AttributeList(AttributeCollection attributes) {

         Contract.Assert(attributes != null);
         _attributes = attributes;
      }

      public void Add(Attribute attribute) {
         throw new NotSupportedException();
      }

      public void Clear() {
         throw new NotSupportedException();
      }

      public bool Contains(Attribute attribute) {
         return _attributes.Contains(attribute);
      }

      public void CopyTo(Attribute[] target, int startIndex) {
         _attributes.CopyTo(target, startIndex);
      }

      public IEnumerator<Attribute> GetEnumerator() {

         for (int i = 0; i < _attributes.Count; i++) {
            yield return _attributes[i];
         }
      }

      IEnumerator IEnumerable.GetEnumerator() {
         return ((IEnumerable)_attributes).GetEnumerator();
      }

      public int IndexOf(Attribute attribute) {

         for (int i = 0; i < _attributes.Count; i++) {
            if (attribute == _attributes[i]) {
               return i;
            }
         }

         return -1;
      }

      public void Insert(int index, Attribute attribute) {
         throw new NotSupportedException();
      }

      bool ICollection<Attribute>.Remove(Attribute attribute) {
         throw new NotSupportedException();
      }

      public void RemoveAt(int index) {
         throw new NotSupportedException();
      }
   }
}
