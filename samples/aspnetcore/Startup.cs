using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xcst.Web.Builder;
using Xcst.Web.Mvc;

namespace aspnetcore;

public class Startup {

   // This method gets called by the runtime. Use this method to add services to the container.
   // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
   public void
   ConfigureServices(IServiceCollection services) {

      services
         .AddMvcCore(opts => {
            opts.ModelMetadataDetailsProviders.Add(new Xcst.Web.Mvc.ModelBinding.MetadataDetailsProvider());
         })
         .AddDataAnnotations()
         .AddViews();

      services.AddAntiforgery();
   }

   // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
   public void
   Configure(IApplicationBuilder app, IWebHostEnvironment env) {

      if (env.IsDevelopment()) {
         app.UseDeveloperExceptionPage();
      }

      app.UseStaticFiles();
      app.UseXcstPrecompiledPages(new[] { GetType().Assembly }, opts => ConfigureXcstWeb(opts));
   }

   static void
   ConfigureXcstWeb(XcstWebOptions opts) {

      opts.DisplayTemplateFactory = LoadDisplayTemplate;
      opts.EditorTemplateFactory = LoadEditorTemplate;

      opts.EditorCssClass = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;
   }

   static XcstViewPage?
   LoadDisplayTemplate(string templateName, ViewContext context) =>
      templateName switch {
         nameof(Object) => new DisplayTemplates.ObjectPackage(),
         _ => null,
      };

   static XcstViewPage?
   LoadEditorTemplate(string templateName, ViewContext context) =>
      templateName switch {
         nameof(Boolean) => new EditorTemplates.BooleanPackage(),
         nameof(Object) => new EditorTemplates.ObjectPackage(),
         _ => null,
      };
}
