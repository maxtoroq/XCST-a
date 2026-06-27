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

      services.Configure((Xcst.Web.Mvc.ViewOptions opts) => {

         opts.DisplayTemplateFactory = LoadDisplayTemplate;
         opts.EditorTemplateFactory = LoadEditorTemplate;

         opts.LabelCssClass = req => req ? "req" : null;

         opts.EditorCssClass = (elementName, inputType) =>
            inputType switch {
               null => (elementName == "select") ?
                  "form-select"
                  : "form-control",
               "checkbox" or "radio" => "form-check-input",
               "range" => "form-range",
               "hidden" => null,
               _ => "form-control",
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
