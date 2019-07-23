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

