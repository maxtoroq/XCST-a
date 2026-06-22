using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xcst.Web;
using Xcst.Web.Mvc;

namespace aspnetcore;

public class Startup {

   public void
   ConfigureServices(IServiceCollection services) {

      services
         .AddMvcCore(opts => {
            opts.ModelMetadataDetailsProviders.Add(new Xcst.Web.Mvc.ModelBinding.MetadataDetailsProvider());
         })
         .AddDataAnnotations()
         .AddViews();

      services.AddAntiforgery();

      services.Configure((XcstViewOptions opts) => {

         opts.DisplayTemplateFactory = LoadDisplayTemplate;
         opts.EditorTemplateFactory = LoadEditorTemplate;

         opts.EditorCssClass = info =>
            (info.TagName != "input") ? "form-control"
            : info.InputType switch {
               "checkbox" or "radio" => "form-check-input",
               "file" => "form-control-file",
               "range" => "form-control-range",
               "hidden" => null,
               _ => "form-control"
            };
      });
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

   public void
   Configure(IApplicationBuilder app, IWebHostEnvironment env) {

      if (env.IsDevelopment()) {
         app.UseDeveloperExceptionPage();
      }

      app.UseStaticFiles();
      app.UseXcstPages(new[] { GetType().Assembly });
   }
}
