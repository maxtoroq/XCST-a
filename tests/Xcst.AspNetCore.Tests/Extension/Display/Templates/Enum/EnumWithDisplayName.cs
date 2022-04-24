using System;
using System.ComponentModel.DataAnnotations;

namespace Xcst.Web.Tests.Extension.Display.Templates.Enum {

   public enum EnumWithDisplayName {

      [Display(Name = "#1")]
      First,

      [Display(Name = "#2")]
      Second
   }
}
