﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Web.Mvc {

   interface IViewPageActivator {
      object Create(ControllerContext controllerContext, Type type);
   }
}
