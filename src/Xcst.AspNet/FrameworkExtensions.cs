// Copyright 2015 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web.Mvc;

namespace Xcst.Web {

   static class FrameworkExtensions {

#if !ASPNETLIB
      static readonly Func<ModelMetadata, Type> getRealModelType =
         (Func<ModelMetadata, Type>)Delegate.CreateDelegate(typeof(Func<ModelMetadata, Type>), typeof(ModelMetadata).GetProperty("RealModelType", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(nonPublic: true));

      static readonly Func<TemplateInfo, HashSet<object>> getVisitedObjects =
         (Func<TemplateInfo, HashSet<object>>)Delegate.CreateDelegate(typeof(Func<TemplateInfo, HashSet<object>>), typeof(TemplateInfo).GetProperty("VisitedObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(nonPublic: true));

      static readonly Action<TemplateInfo, HashSet<object>> setVisitedObjects =
         (Action<TemplateInfo, HashSet<object>>)Delegate.CreateDelegate(typeof(Action<TemplateInfo, HashSet<object>>), typeof(TemplateInfo).GetProperty("VisitedObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetSetMethod(nonPublic: true));
#endif

      public static string GroupName(this ModelMetadata metadata) {
#if ASPNETLIB
         return metadata.GroupName;
#else
         return null;
#endif
      }

      public static Type RealModelType(this ModelMetadata metadata) {
#if ASPNETLIB
         return metadata.RealModelType;
#else
         return getRealModelType(metadata);
#endif
      }

      public static HashSet<object> VisitedObjects(this TemplateInfo templateInfo) {
#if ASPNETLIB
         return templateInfo.VisitedObjects;
#else
         return getVisitedObjects(templateInfo);
#endif
      }

      public static void VisitedObjects(this TemplateInfo templateInfo, HashSet<object> value) {
#if ASPNETLIB
         templateInfo.VisitedObjects = value;
#else
         setVisitedObjects(templateInfo, value);
#endif
      }

      public static bool HasNonDefaultEditFormat(this ModelMetadata metadata) {
#if ASPNETLIB
         return metadata.HasNonDefaultEditFormat;
#else
         return (bool)metadata.GetType()
            .GetProperty("HasNonDefaultEditFormat", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(metadata);
#endif
      }

#if !ASPNETLIB
      public static object GetModelStateValue(this HtmlHelper htmlHelper, string key, Type destinationType) {

         if (htmlHelper.ViewData.ModelState.TryGetValue(key, out ModelState modelState)
            && modelState.Value != null) {

            return modelState.Value.ConvertTo(destinationType, culture: null);
         }

         return null;
      }

      public static string EvalString(this HtmlHelper htmlHelper, string key) {
         return Convert.ToString(htmlHelper.ViewData.Eval(key), CultureInfo.CurrentCulture);
      }

      public static string EvalString(this HtmlHelper htmlHelper, string key, string format) {
         return Convert.ToString(htmlHelper.ViewData.Eval(key, format), CultureInfo.CurrentCulture);
      }

      public static bool EvalBoolean(this HtmlHelper htmlHelper, string key) {
         return Convert.ToBoolean(htmlHelper.ViewData.Eval(key), CultureInfo.InvariantCulture);
      }

      public static FormContext GetFormContextForClientValidation(this ViewContext viewContext) {
         return (viewContext.ClientValidationEnabled) ? viewContext.FormContext : null;
      }
#endif

      internal static ViewContext Clone(
            this ViewContext context,
#if !ASPNETLIB
            IView view = null,
#endif
            ViewDataDictionary viewData = null,
            TempDataDictionary tempData = null,
            TextWriter writer = null) {

         return new ViewContext(
            context,
#if !ASPNETLIB
            view ?? context.View,
#endif
            viewData ?? context.ViewData,
            tempData ?? context.TempData
#if !ASPNETLIB
            , writer ?? context.Writer
#endif
         ) {
            ClientValidationEnabled = context.ClientValidationEnabled,
            UnobtrusiveJavaScriptEnabled = context.UnobtrusiveJavaScriptEnabled,
            ValidationMessageElement = context.ValidationMessageElement,
            ValidationSummaryMessageElement = context.ValidationSummaryMessageElement
         };
      }
   }
}
