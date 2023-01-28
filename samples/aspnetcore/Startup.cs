using System;
using System.Web.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xcst.Web.Configuration;
using Xcst.Web.Mvc;
using Xcst.Web.Builder;

namespace aspnetcore;

public class Startup {

   // This method gets called by the runtime. Use this method to add services to the container.
   // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
   public void
   ConfigureServices(IServiceCollection services) {

      services.AddMvcCore(opts => {
         opts.ModelMetadataDetailsProviders.Add(new Xcst.Web.Mvc.ModelBinding.MetadataDetailsProvider());
      }).AddDataAnnotations();

      services.AddAntiforgery();
      //services.AddDistributedMemoryCache();
      //services.AddSession();
      //services.AddHttpContextAccessor();
   }

   // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
   public void
   Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider svp) {

      if (env.IsDevelopment()) {
         app.UseDeveloperExceptionPage();
      }

      app.UseStaticFiles();
      //app.UseSession();

      app.UseXcstPrecompiledPages(new[] { GetType().Assembly }, config => ConfigureXcstWeb(config, svp));
   }

   static void
   ConfigureXcstWeb(XcstWebConfiguration config, IServiceProvider svp) {

      config.DisplayTemplates.TemplateFactory = LoadDisplayTemplate;
      config.EditorTemplates.TemplateFactory = LoadEditorTemplate;

      config.EditorTemplates.EditorCssClass = (info, defaultClass) =>
         (info.InputType == InputType.Text
            || info.InputType == InputType.Password
            || info.TagName != "input") ? "form-control"
            : null;

      //IHttpContextAccessor contextAccessor = svp.GetService<IHttpContextAccessor>()!;
      //Xcst.Web.Runtime.UrlUtil.CurrentHttpContext = () => contextAccessor.HttpContext!;
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
