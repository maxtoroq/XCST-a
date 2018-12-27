<%@ Application Language="C#" %>
<%@ Import Namespace="System.Web.Mvc" %>
<%@ Import Namespace="Xcst.Web.Configuration" %>
<%@ Import Namespace="Xcst.Web.Mvc" %>

<script runat="server">

   void Application_Start(object sender, EventArgs e) {

      var config = XcstWebConfiguration.Instance;

      config.DisplayTemplates.TemplateFactory = LoadDisplayTemplate;

      config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;

      config.EditorTemplates.TemplateFactory = LoadEditorTemplate;
   }

   XcstViewPage LoadDisplayTemplate(string templateName, ViewContext context) {

      switch (templateName) {
         case nameof(Object):
            return new Samples.Templates.Display.ObjectPackage();

         default:
            return null;
      }
   }

   XcstViewPage LoadEditorTemplate(string templateName, ViewContext context) {

      switch (templateName) {
         case nameof(Boolean):
            return new Samples.Templates.Editor.BooleanPackage();

         case nameof(Object):
            return new Samples.Templates.Editor.ObjectPackage();

         default:
            return null;
      }
   }

</script>
