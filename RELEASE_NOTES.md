## 4.3.1

Hotfix release to fix context handling in `4.3.0`.

* Added documentation comments about OpenGL context handling. (@NogginBops)
* Fixed issue where when multiple GLWpfControls only the last initialized controls OpenGL context would be current. (@NogginBops)

## 4.3.0

* Made each `GLWpfControl` have it's own OpenGL context allowing different controls to have different context settings. (@NogginBops)
* Enabled multisample anti-aliasing though `GLWpfControlSettings.Samples`. (@NogginBops)
* Implemented `IDisposable` for `GLWpfControl` that allows native DirectX and OpenGL resources to be freed. (@NogginBops)
* Made `GLWpfControl` have `Focusable` be `true` by default, solving a lot of the keyboard input event issues. (@NogginBops)
* Deprecated `GLWpfControlSettings.GraphicsContextFlags` in favor of `GLWpfControlSettings.ContextFlags`. (@NogginBops)
* Deprecated `GLWpfControlSettings.GraphicsProfile` in favor of `GLWpfControlSettings.Profile`. (@NogginBops)
* Added `GLWpfControlSettings.SharedContext` to allow context sharing. (@NogginBops)
* Deprecated `GLWpfControl.CanInvokeOnHandledEvents` and `GLWpfControl.RegisterToEventsDirectly`, updated readme to reflect this. (@NogginBops)
* Fixed rounding issues related to DPI scaling. (@NogginBops, @5E-324)
* Updated to depend on OpenTK 4.8.2. (@NogginBops, @softwareantics)
* Fixed memory leak where DirectX resouces would never be freed. (@NogginBops)

## 4.2.3

* Fix event issue, use `RegisterToEventsDirectly` and `CanInvokeOnHandledEvents` to customize event registering/handling. (@softwareantics)
* Internal cleanup that fixed issue where setting `RenderContinuously = false` caused an extra call to render. (@francotiveron)

## 4.2.2

* Fix issue where `4.2.1` was only compatible with `netcoreapp3.1-windows` and nothing else.

## 4.2.1

* Fix broken nuget package in `4.2.0`.

## 4.2.0
    * Add ability to make the control transparent by setting `GLWpfControlSettings.TransparentBackground` to true. (@luiscuenca)
    * Change the dependency on OpenTK to be >= 4.3.0 < 5.0.0. (@NogginBops)
    * Add ability to pass a custom `IBindingsContext` in `GLWpfControlSettings`. (@Kaktusbot)
    * Add stencil buffer to the framebuffer. (@Svabik)
    * Fixed issue where remote desktop would fail due to having to use a software implementation of OpenGL. (@marcotod1410)
    * Fixed so that `KeyDownEvent` and `KeyUpEvent` properly work in the control. (@BBoldenow)

## 4.1.0
    * Add NonReloadingTabControl.
    * Add example with new NonReloadingTabControl.

## 4.0.0
    * Fix resizing
    * Unseal GLWpfControl
    * Fix crash on framebuffer access before inits

## 4.0.0-pre.12
    * Improved rendering performance by avoiding duplicate render calls (@marcotod1410)
    * Fix FrameBufferWidth property returning height incorrectly.
    * Fix resizing

## 4.0.0-pre.11
    * Fix for resource deallocation issue.

## 4.0.0-pre.10
    * Fix crash due to context mangling in tabbed views

## 4.0.0-pre.9
    * Fix crash for tabbed window
    * Total rewrite of the backend
    * All memory leaks removed
    * Faster loading
    * Faster resizing
    * Less memory usage
    * Reduced duplicate rendering
    * New design time preview
    * Simpler examples
    * Update to OpenTK 4.3.0

## 4.0.0-pre.8
    * Total rewrite of the backend
    * All memory leaks removed
    * Faster loading
    * Faster resizing
    * Less memory usage
    * Reduced duplicate rendering
    * New design time preview
    * Simpler examples
    * Update to OpenTK 4.3.0

## 4.0.0-pre.7
    * Fix design mode crash in Visual Studio.

## 4.0.0-pre.6
    * Update to OpenTK 4.3.0

## 4.0.0-pre.5
    * Fix for one-frame delay on startup (no more flashing screen) (@bezo97)

## 4.0.0-pre.4
    * Add support for DPI Scaling + optional config values to ignore this. (@marcotod1410)
    * Added Framebuffer Size to API. (@ marcotod1410)
    * Fix render initialization if not visible at the start (@marcotod1410)
    * Remove dependency on SharpDX and replace with custom bindings (@bezo97)

## 4.0.0-pre.3
    * Fix crash if control was to collapsed on startup.

## 4.0.0-pre.2
    * Fix Gamma/Linear color space issue (Thanks @Justin113D)

## 4.0.0-pre.1
    * Dotnet Core Support
    * Retarget to OpenTK 4.2.0

## 3.1.1
    * Backport of fix gamma/colour space issues (Thanks @Justin113D)

## 3.1.0
    * Add support for non-continuous event-based rendering via InvalidateVisual().
    * Fix Incorrect minor version in OpenGL Settings.

## 3.0.1
    * Fix SharpDX.Direct3D9 dependency.

## 3.0.0
    * >10x performance increase via DirectX interop. Huge thanks to @Zcore.
    * Simplified API
    * Removed software render path
    * Added automatic context sharing by default

## 2.1.0
	* Allow support for external contexts across multiple controls.

## 2.0.3
    * Improve fix for event-ordering crash on some systems.

## 2.0.2
    * Possible fix for event-ordering crash on some systems.

## 2.0.1
    * Fix resize events not being raised.

## 2.0.0
    * Moved namespace to OpenTK.Wpf.
    * GLWpfControl now extends FrameworkElement instead of Control.
    * Moved to pure-code solution for greater simplicity.
    * Added some extra-paranoid null checking.
    
## 1.1.2
    * Possible fix for NPE on renderer access.

## 1.1.1
    * Automatically set the viewport for the user.

## 1.1.0
    * Use own HWND for improved performance (Thanks to @Eschryn)
    * Add time delta to the render event.
    * Better handling of resizing via delayed updates.
    * Remove slow-path detection (2x performance on low-end devices!)
    * Fix duplicate OpenGL resource unloading.
    
## 1.0.1
    * Add API to access the control's framebuffer.

## 1.0.0
	* Initial release

