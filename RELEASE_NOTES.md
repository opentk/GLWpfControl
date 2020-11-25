### 4.0.0-pre.10
    * Fix crash due to context mangling in tabbed views

### 4.0.0-pre.9
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

### 4.0.0-pre.8
    * Total rewrite of the backend
    * All memory leaks removed
    * Faster loading
    * Faster resizing
    * Less memory usage
    * Reduced duplicate rendering
    * New design time preview
    * Simpler examples
    * Update to OpenTK 4.3.0

### 4.0.0-pre.7
    * Fix design mode crash in Visual Studio.

### 4.0.0-pre.6
    * Update to OpenTK 4.3.0

### 4.0.0-pre.5
    * Fix for one-frame delay on startup (no more flashing screen) (@bezo97)

### 4.0.0-pre.4
    * Add support for DPI Scaling + optional config values to ignore this. (@marcotod1410)
    * Added Framebuffer Size to API. (@ marcotod1410)
    * Fix render initialization if not visible at the start (@marcotod1410)
    * Remove dependency on SharpDX and replace with custom bindings (@bezo97)

### 4.0.0-pre.3
    * Fix crash if control was to collapsed on startup.

### 4.0.0-pre.2
    * Fix Gamma/Linear color space issue (Thanks @Justin113D)

### 4.0.0-pre.1
    * Dotnet Core Support
    * Retarget to OpenTK 4.2.0

### 3.1.1
    * Backport of fix gamma/colour space issues (Thanks @Justin113D)

### 3.1.0
    * Add support for non-continuous event-based rendering via InvalidateVisual().
    * Fix Incorrect minor version in OpenGL Settings.

### 3.0.1
    * Fix SharpDX.Direct3D9 dependency.

### 3.0.0
    * >10x performance increase via DirectX interop. Huge thanks to @Zcore.
    * Simplified API
    * Removed software render path
    * Added automatic context sharing by default

### 2.1.0
	* Allow support for external contexts across multiple controls.

### 2.0.3
    * Improve fix for event-ordering crash on some systems.

### 2.0.2
    * Possible fix for event-ordering crash on some systems.

### 2.0.1
    * Fix resize events not being raised.

### 2.0.0
    * Moved namespace to OpenTK.Wpf.
    * GLWpfControl now extends FrameworkElement instead of Control.
    * Moved to pure-code solution for greater simplicity.
    * Added some extra-paranoid null checking.
    
### 1.1.2
    * Possible fix for NPE on renderer access.

### 1.1.1
    * Automatically set the viewport for the user.

### 1.1.0
    * Use own HWND for improved performance (Thanks to @Eschryn)
    * Add time delta to the render event.
    * Better handling of resizing via delayed updates.
    * Remove slow-path detection (2x performance on low-end devices!)
    * Fix duplicate OpenGL resource unloading.
    
### 1.0.1
    * Add API to access the control's framebuffer.

### 1.0.0
	* Initial release

