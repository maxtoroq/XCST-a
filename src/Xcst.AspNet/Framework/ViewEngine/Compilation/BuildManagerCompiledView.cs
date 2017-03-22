// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Web.Mvc.Properties;

namespace System.Web.Mvc {

   abstract class BuildManagerCompiledView : IView {

      internal IViewPageActivator ViewPageActivator;
      IBuildManager _buildManager;
      ControllerContext _controllerContext;

      internal IBuildManager BuildManager {
         get {
            if (_buildManager == null) {
               _buildManager = new BuildManagerWrapper();
            }
            return _buildManager;
         }
         set { _buildManager = value; }
      }

      public string ViewPath { get; protected set; }

      protected BuildManagerCompiledView(ControllerContext controllerContext, string viewPath)
         : this(controllerContext, viewPath, null) { }

      protected BuildManagerCompiledView(ControllerContext controllerContext, string viewPath, IViewPageActivator viewPageActivator)
         : this(controllerContext, viewPath, viewPageActivator, null) { }

      internal BuildManagerCompiledView(ControllerContext controllerContext, string viewPath, IViewPageActivator viewPageActivator, IDependencyResolver dependencyResolver) {

         if (controllerContext == null) throw new ArgumentNullException(nameof(controllerContext));
         if (String.IsNullOrEmpty(viewPath)) throw new ArgumentException(MvcResources.Common_NullOrEmpty, nameof(viewPath));

         _controllerContext = controllerContext;

         this.ViewPath = viewPath;
         this.ViewPageActivator = viewPageActivator ?? new BuildManagerViewEngine.DefaultViewPageActivator(dependencyResolver);
      }

      public virtual void Render(ViewContext viewContext, TextWriter writer) {

         if (viewContext == null) throw new ArgumentNullException(nameof(viewContext));

         object instance = null;

         Type type = this.BuildManager.GetCompiledType(ViewPath);

         if (type != null) {
            instance = this.ViewPageActivator.Create(_controllerContext, type);
         }

         if (instance == null) {
            throw new InvalidOperationException(
               String.Format(
                  CultureInfo.CurrentCulture,
                  MvcResources.CshtmlView_ViewCouldNotBeCreated,
                  this.ViewPath));
         }

         RenderView(viewContext, writer, instance);
      }

      protected abstract void RenderView(ViewContext viewContext, TextWriter writer, object instance);
   }
}
