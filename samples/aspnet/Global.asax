<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="Xcst.Web.Configuration" %>

<script runat="server">

   void Application_Start(object sender, EventArgs e) {

      var config = XcstWebConfiguration.Instance;

      config.Editors.EditorCssClassFunction = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;

      config.Editors.OmitPasswordValue = true;
   }

</script>
