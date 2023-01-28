// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Xcst.Web.Mvc;

static class TypeDescriptorHelper {

   public static ICustomTypeDescriptor
   Get(Type type) =>
      new AssociatedMetadataTypeTypeDescriptionProvider(type)
         .GetTypeDescriptor(type);
}
