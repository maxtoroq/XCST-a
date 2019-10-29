using System;
using System.Web.Mvc;

namespace AspNetMvc {

   public class HomeController : Controller {

      public ActionResult Index() {
         return View();
      }
   }
}
