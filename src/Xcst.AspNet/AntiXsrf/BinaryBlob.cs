﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace System.Web.Helpers.AntiXsrf {

   // Represents a binary blob (token) that contains random data.
   // Useful for binary data inside a serialized stream.

   [DebuggerDisplay("{" + nameof(DebuggerString) + "}")]
   sealed class BinaryBlob : IEquatable<BinaryBlob> {

      static readonly RNGCryptoServiceProvider _prng = new RNGCryptoServiceProvider();

      readonly byte[] _data;

      public int BitLength => checked(_data.Length * 8);

      [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by debugger.")]
      private string DebuggerString {
         get {
            var sb = new StringBuilder("0x", 2 + (_data.Length * 2));
            for (int i = 0; i < _data.Length; i++) {
               sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", _data[i]);
            }
            return sb.ToString();
         }
      }

      // Generates a new token using a specified bit length.

      public BinaryBlob(int bitLength)
         : this(bitLength, GenerateNewToken(bitLength)) { }

      // Generates a token using an existing binary value.

      public BinaryBlob(int bitLength, byte[] data) {

         if (bitLength < 32 || bitLength % 8 != 0) throw new ArgumentOutOfRangeException(nameof(bitLength));
         if (data == null || data.Length != bitLength / 8) throw new ArgumentOutOfRangeException(nameof(data));

         _data = data;
      }

      public override bool Equals(object obj) {
         return Equals(obj as BinaryBlob);
      }

      public bool Equals(BinaryBlob other) {

         if (other == null) {
            return false;
         }

         Contract.Assert(this._data.Length == other._data.Length);
         return CryptoUtil.AreByteArraysEqual(this._data, other._data);
      }

      public byte[] GetData() {
         return _data;
      }

      public override int GetHashCode() {

         // Since data should contain uniformly-distributed entropy, the
         // first 32 bits can serve as the hash code.

         Contract.Assert(_data != null && _data.Length >= (32 / 8));
         return BitConverter.ToInt32(_data, 0);
      }

      static byte[] GenerateNewToken(int bitLength) {
         byte[] data = new byte[bitLength / 8];
         _prng.GetBytes(data);
         return data;
      }
   }
}
