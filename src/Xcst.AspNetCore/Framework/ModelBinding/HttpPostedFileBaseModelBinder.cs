// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace Xcst.Web.Mvc.ModelBinding;

public class HttpPostedFileBaseModelBinder : IModelBinder {

   public object?
   BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {

      if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));
      if (bindingContext is null) throw new ArgumentNullException(nameof(bindingContext));

      var theFile = controllerContext.HttpContext.Request.Form.Files
         .GetFile(bindingContext.ModelName);

      return ChooseFileOrNull(theFile);
   }

   // helper that returns the original file if there was content uploaded, null if empty
   internal static IFormFile?
   ChooseFileOrNull(IFormFile? rawFile) {

      // case 1: there was no <input type="file" ... /> element in the post
      if (rawFile is null) {
         return null;
      }

      // case 2: there was an <input type="file" ... /> element in the post, but it was left blank
      if (rawFile.Length == 0
         && String.IsNullOrEmpty(rawFile.FileName)) {

         return null;
      }

      // case 3: the file was posted
      return rawFile;
   }
}
