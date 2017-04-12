using System;
using System.Web.Mvc;

namespace Samples {

   public class HomeController : Controller {

      public ActionResult Index() {
         return View();
      }
   }
}