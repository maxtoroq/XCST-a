// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IFormFile = Microsoft.AspNetCore.Http.IFormFile;

namespace System.Web.Mvc {

   public sealed class HttpFileCollectionValueProvider : DictionaryValueProvider<IFormFile[]> {

      static readonly Dictionary<string, IFormFile[]>
      _emptyDictionary = new();

      public
      HttpFileCollectionValueProvider(ControllerContext controllerContext)
         : base(GetHttpPostedFileDictionary(controllerContext), CultureInfo.InvariantCulture) { }

      static Dictionary<string, IFormFile[]>
      GetHttpPostedFileDictionary(ControllerContext controllerContext) {

         var files = controllerContext.HttpContext.Request.Form.Files;

         // fast-track common case of no files

         if (files.Count == 0) {
            return _emptyDictionary;
         }

         // build up the 1:many file mapping

         var mapping = new List<KeyValuePair<string, IFormFile>>();

         for (int i = 0; i < files.Count; i++) {

            IFormFile? file = HttpPostedFileBaseModelBinder.ChooseFileOrNull(files[i]);

            if (file?.FileName != null) {
               mapping.Add(new KeyValuePair<string, IFormFile>(file.FileName, file));
            }
         }

         // turn the mapping into a 1:many dictionary

         var grouped = mapping.GroupBy(el => el.Key, el => el.Value, StringComparer.OrdinalIgnoreCase);

         return grouped.ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
      }
   }

   public sealed class HttpFileCollectionValueProviderFactory : ValueProviderFactory {

      public override IValueProvider
      GetValueProvider(ControllerContext controllerContext) {

         if (controllerContext is null) throw new ArgumentNullException(nameof(controllerContext));

         return new HttpFileCollectionValueProvider(controllerContext);
      }
   }
}
