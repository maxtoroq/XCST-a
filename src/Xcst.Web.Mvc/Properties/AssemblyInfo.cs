using System;
using System.Runtime.InteropServices;
using System.Web;
using PreApplicationStartCode = Xcst.Web.Mvc.PreApplicationStartCode;

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: PreApplicationStartMethod(typeof(PreApplicationStartCode), nameof(PreApplicationStartCode.Start))]
