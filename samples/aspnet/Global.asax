<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="Xcst.Web.Configuration" %>
<%@ Import Namespace="Xcst.Web.Mvc" %>

<script runat="server">

   void Application_Start(object sender, EventArgs e) {

      var config = XcstWebConfiguration.Instance;

      config.DisplayTemplates.TemplateFactory = LoadDisplayTemplate;
      config.EditorTemplates.TemplateFactory = LoadEditorTemplate;

      config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;
   }

   static XcstViewPage LoadDisplayTemplate(string templateName, ViewContext context) {

      switch (templateName) {
         case nameof(Object):
            return new AspNet.DisplayTemplates.ObjectPackage();

         default:
            return null;
      }
   }

   static XcstViewPage LoadEditorTemplate(string templateName, ViewContext context) {

      switch (templateName) {
         case nameof(Boolean):
            return new AspNet.EditorTemplates.BooleanPackage();

         case nameof(Object):
            return new AspNet.EditorTemplates.ObjectPackage();

         default:
            return null;
      }
   }

</script>
