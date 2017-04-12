<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="System.Web.Routing" %>
<%@ Import Namespace="Xcst.Web.Configuration" %>

<script RunAt="server">

   void Application_Start(object sender, EventArgs e) {

      RegisterRoutes(RouteTable.Routes);
      ConfigureXcst(XcstWebConfiguration.Instance);
   }

   void RegisterRoutes(RouteCollection routes) {

      routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

      routes.MapRoute("Default", "{controller}/{action}/{id}",
         new { controller = "Home", action = "Index", id = UrlParameter.Optional });
   }

   void ConfigureXcst(XcstWebConfiguration config) {

      config.Editors.EditorCssClass = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;
   }

</script>
