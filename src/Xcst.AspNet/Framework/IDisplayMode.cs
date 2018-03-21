// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace System.Web.WebPages {

   /// <summary>
   /// An interface that provides DisplayInfo for a virtual path and request. An IDisplayMode may modify the virtual path before checking
   /// if it exists. CanHandleContext is called to determine if the Display Mode is available to return display info for the request.
   /// GetDisplayInfo should return null if the virtual path does not exist. For an example implementation, see DefaultDisplayMode.
   /// DisplayModeId is used to cache the non-null result of a call to GetDisplayInfo and should be unique for each Display Mode. See
   /// DisplayModes for the built-in Display Modes and their ids.
   /// </summary>

   interface IDisplayMode {

      string DisplayModeId { get; }
      bool CanHandleContext(HttpContextBase httpContext);
      DisplayInfo GetDisplayInfo(HttpContextBase httpContext, string virtualPath, Func<string, bool> virtualPathExists);
   }

   /// <summary>
   /// DisplayInfo wraps the resolved file path and IDisplayMode for a request and path.
   /// The returned IDisplayMode can be used to resolve other page elements for the request.
   /// </summary>

   class DisplayInfo {

      /// <summary>
      /// The Display Mode used to resolve a virtual path.
      /// </summary>

      public IDisplayMode DisplayMode { get; private set; }

      /// <summary>
      /// Resolved path of a file that exists.
      /// </summary>

      public string FilePath { get; private set; }

      public DisplayInfo(string filePath, IDisplayMode displayMode) {

         if (filePath == null) throw new ArgumentNullException(nameof(filePath));
         if (displayMode == null) throw new ArgumentNullException(nameof(displayMode));

         this.FilePath = filePath;
         this.DisplayMode = displayMode;
      }
   }

   /// <summary>
   /// The <see cref="DefaultDisplayMode"/> can take any suffix and determine if there is a corresponding
   /// file that exists given a path and request by transforming the path to contain the suffix.
   /// Add a new DefaultDisplayMode to the Modes collection to handle a new suffix or inherit from
   /// DefaultDisplayMode to provide custom logic to transform paths with a suffix.
   /// </summary>

   class DefaultDisplayMode : IDisplayMode {

      readonly string _suffix;

      /// <summary>
      /// When set, the <see cref="DefaultDisplayMode"/> will only be available to return Display Info for a request
      /// if the ContextCondition evaluates to true.
      /// </summary>

      public Func<HttpContextBase, bool> ContextCondition { get; set; }

      public virtual string DisplayModeId => _suffix;

      public DefaultDisplayMode()
         : this(DisplayModeProvider.DefaultDisplayModeId) { }

      public DefaultDisplayMode(string suffix) {
         _suffix = suffix ?? String.Empty;
      }

      public bool CanHandleContext(HttpContextBase httpContext) {
         return this.ContextCondition == null || this.ContextCondition(httpContext);
      }

      /// <summary>
      /// Returns DisplayInfo with the transformed path if it exists.
      /// </summary>

      public virtual DisplayInfo GetDisplayInfo(HttpContextBase httpContext, string virtualPath, Func<string, bool> virtualPathExists) {

         string transformedFilename = TransformPath(virtualPath, _suffix);

         if (transformedFilename != null
            && virtualPathExists(transformedFilename)) {

            return new DisplayInfo(transformedFilename, this);
         }

         return null;
      }

      /// <summary>
      /// Transforms paths according to the following rules:
      /// \some\path.blah\file.txt.zip -> \some\path.blah\file.txt.suffix.zip
      /// \some\path.blah\file -> \some\path.blah\file.suffix
      /// </summary>

      protected virtual string TransformPath(string virtualPath, string suffix) {

         if (String.IsNullOrEmpty(suffix)) {
            return virtualPath;
         }

         string extension = Path.GetExtension(virtualPath);

         return Path.ChangeExtension(virtualPath, suffix + extension);
      }
   }

   sealed class DisplayModeProvider {

      //public static readonly string MobileDisplayModeId = "Mobile";
      public static readonly string DefaultDisplayModeId = String.Empty;
      static readonly object _displayModeKey = new object();
      static readonly DisplayModeProvider _instance = new DisplayModeProvider();

      readonly List<IDisplayMode> _displayModes = new List<IDisplayMode> {
         //new DefaultDisplayMode(MobileDisplayModeId) {
         //   ContextCondition = context => context.GetOverriddenBrowser().IsMobileDevice
         //},
         new DefaultDisplayMode()
      };

      /// <summary>
      /// Restricts the search for Display Info to Display Modes either equal to or following the current
      /// Display Mode in Modes. For example, a page being rendered in the Default Display Mode will not
      /// display Mobile partial views in order to achieve a consistent look and feel.
      /// </summary>

      public bool RequireConsistentDisplayMode { get; set; }

      public static DisplayModeProvider Instance => _instance;

      /// <summary>
      /// All Display Modes that are available to handle a request.
      /// </summary>

      public IList<IDisplayMode> Modes => _displayModes;

      internal DisplayModeProvider() {
         // The type is a psuedo-singleton. A user would gain nothing from constructing it since we won't use anything but DisplayModeProvider.Instance internally.
      }

      int FindFirstAvailableDisplayMode(IDisplayMode currentDisplayMode, bool requireConsistentDisplayMode) {

         if (requireConsistentDisplayMode
            && currentDisplayMode != null) {

            int first = _displayModes.IndexOf(currentDisplayMode);

            return (first >= 0) ? first : _displayModes.Count;
         }

         return 0;
      }

      /// <summary>
      /// Returns any IDisplayMode that can handle the given request.
      /// </summary>

      public IEnumerable<IDisplayMode> GetAvailableDisplayModesForContext(HttpContextBase httpContext, IDisplayMode currentDisplayMode) {
         return GetAvailableDisplayModesForContext(httpContext, currentDisplayMode, this.RequireConsistentDisplayMode);
      }

      internal IEnumerable<IDisplayMode> GetAvailableDisplayModesForContext(HttpContextBase httpContext, IDisplayMode currentDisplayMode, bool requireConsistentDisplayMode) {

         int first = FindFirstAvailableDisplayMode(currentDisplayMode, requireConsistentDisplayMode);

         for (int i = first; i < _displayModes.Count; i++) {

            IDisplayMode mode = _displayModes[i];

            if (mode.CanHandleContext(httpContext)) {
               yield return mode;
            }
         }
      }

      /// <summary>
      /// Returns DisplayInfo from the first IDisplayMode in Modes that can handle the given request and locate the virtual path.
      /// If currentDisplayMode is not null and RequireConsistentDisplayMode is set to true the search for DisplayInfo will only
      /// start with the currentDisplayMode.
      /// </summary>

      public DisplayInfo GetDisplayInfoForVirtualPath(string virtualPath, HttpContextBase httpContext, Func<string, bool> virtualPathExists, IDisplayMode currentDisplayMode) {
         return GetDisplayInfoForVirtualPath(virtualPath, httpContext, virtualPathExists, currentDisplayMode, this.RequireConsistentDisplayMode);
      }

      internal DisplayInfo GetDisplayInfoForVirtualPath(string virtualPath, HttpContextBase httpContext, Func<string, bool> virtualPathExists, IDisplayMode currentDisplayMode, bool requireConsistentDisplayMode) {

         // Performance sensitive

         int first = FindFirstAvailableDisplayMode(currentDisplayMode, requireConsistentDisplayMode);

         for (int i = first; i < _displayModes.Count; i++) {

            IDisplayMode mode = _displayModes[i];

            if (mode.CanHandleContext(httpContext)) {

               DisplayInfo info = mode.GetDisplayInfo(httpContext, virtualPath, virtualPathExists);

               if (info != null) {
                  return info;
               }
            }
         }

         return null;
      }

      internal static IDisplayMode GetDisplayMode(HttpContextBase context) {
         return context?.Items[_displayModeKey] as IDisplayMode;
      }

      internal static void SetDisplayMode(HttpContextBase context, IDisplayMode displayMode) {

         if (context != null) {
            context.Items[_displayModeKey] = displayMode;
         }
      }
   }
}
