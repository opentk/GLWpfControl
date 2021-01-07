using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf
{

    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRenderer {
        
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly DxGlContext _context;
        
        public event Action<TimeSpan> GLRender;
        public event Action GLAsyncRender;
        
        private DxGLFramebuffer _framebuffer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _framebuffer?.GLFramebufferHandle ?? 0;

        /// The OpenGL Framebuffer width
        public int Width => _framebuffer?.FramebufferWidth ?? 0;
        
        /// The OpenGL Framebuffer height
        public int Height => _framebuffer?.FramebufferHeight ?? 0;
        
        private TimeSpan _lastFrameStamp;


        public GLWpfControlRenderer(GLWpfControlSettings settings)
        {
            _context = new DxGlContext(settings);
        }


        public void SetSize(int width, int height, double dpiScaleX, double dpiScaleY) {
            if (_framebuffer == null || _framebuffer.Width != width || _framebuffer.Height != height) {
                _framebuffer?.Dispose();
                _framebuffer = null;
                if (width > 0 && height > 0) {
                    _framebuffer = new DxGLFramebuffer(_context.Device, width, height, dpiScaleX, dpiScaleY);
                }
            }
        }

        public void Render(DrawingContext drawingContext, Point point) {
            if (_framebuffer == null) {
                return;
            }
            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            PreRender(point);
            GLRender?.Invoke(deltaT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Flush();
            GLAsyncRender?.Invoke();
            PostRender();
            
            // Transforms are applied in reverse order
            drawingContext.PushTransform(_framebuffer.TranslateTransform);              // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
            drawingContext.PushTransform(_framebuffer.FlipYTransform);                  // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg

            // dpi scaled rectangle from the image
            var rect = new Rect(0, 0, _framebuffer.D3dImage.Width, _framebuffer.D3dImage.Height);
            drawingContext.DrawImage(_framebuffer.D3dImage, rect);            // Draw the image source 

            drawingContext.Pop();                                                       // Remove the scale transform
            drawingContext.Pop();                                                       // Remove the translation transform
        }

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender(Point point)
        {
            // TODO: checking for the correct adapter is expensive and should not be made during the Render phase.
            // It's safe to do it periodically (eg: a timer).

            // check to set the device related to the adapter that is displaying the control
            _context.SetDeviceFromCurrentAdapter(point);

            if(_framebuffer.Device != _context.Device) // if the surface is related to a device that does not match the current adapter
            {
                // recreate a framebuffer with the current device and the same size as the previous one
                var newFramebuffer = new DxGLFramebuffer(_context.Device, _framebuffer.Width, _framebuffer.Height, _framebuffer.DpiScaleX, _framebuffer.DpiScaleY);
                _framebuffer.Dispose();
                _framebuffer = newFramebuffer;
            }

            _framebuffer.D3dImage.Lock();
            Wgl.DXLockObjectsNV(_context.Device.GLDeviceHandle, 1, new [] {_framebuffer.DxInteropRegisteredHandle});
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.GLFramebufferHandle);
            GL.Viewport(0, 0, _framebuffer.FramebufferWidth, _framebuffer.FramebufferHeight);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.Device.GLDeviceHandle, 1, new [] {_framebuffer.DxInteropRegisteredHandle});
            _framebuffer.D3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _framebuffer.DxRenderTargetHandle);
            _framebuffer.D3dImage.AddDirtyRect(new Int32Rect(0, 0, _framebuffer.FramebufferWidth, _framebuffer.FramebufferHeight));
            _framebuffer.D3dImage.Unlock();
        }
    }
}
