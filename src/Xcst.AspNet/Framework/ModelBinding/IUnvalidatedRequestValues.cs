﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Specialized;

namespace System.Web.Mvc {

   delegate IUnvalidatedRequestValues UnvalidatedRequestValuesAccessor(ControllerContext controllerContext);

   // Used for mocking the UnvalidatedRequestValues type in System.Web.WebPages

   interface IUnvalidatedRequestValues {
      NameValueCollection Form { get; }
      NameValueCollection QueryString { get; }
      string this[string key] { get; }
   }

   // Concrete implementation for the IUnvalidatedRequestValues helper interface

   sealed class UnvalidatedRequestValuesWrapper : IUnvalidatedRequestValues {

      readonly UnvalidatedRequestValuesBase _unvalidatedValues;

      public NameValueCollection Form => _unvalidatedValues.Form;

      public NameValueCollection QueryString => _unvalidatedValues.QueryString;

      public string this[string key] => _unvalidatedValues[key];

      public UnvalidatedRequestValuesWrapper(UnvalidatedRequestValuesBase unvalidatedValues) {
         _unvalidatedValues = unvalidatedValues;
      }
   }
}
