using System;
using System.ComponentModel.DataAnnotations;

namespace Xcst.Web.Tests.Extension.Editor.Templates.Enum;

public enum EnumWithDisplayName {

   [Display(Name = "#1")]
   First,

   [Display(Name = "#2")]
   Second
}
