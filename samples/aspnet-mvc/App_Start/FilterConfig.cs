﻿using System.Web.Mvc;

namespace AspNetMvc {

   public class FilterConfig {

      public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
         filters.Add(new HandleErrorAttribute());
      }
   }
}
