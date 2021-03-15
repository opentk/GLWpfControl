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
        private DxGlContext _context;
        private readonly GLWpfControlSettings _settings;
        
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

        private IntPtr currentMonitor = IntPtr.Zero;

        public GLWpfControlRenderer(GLWpfControlSettings settings)
        {
            _settings = settings;
            _context = new DxGlContext(settings);
        }

        /// <summary>
        /// Set the monitor on which the renderer will draw to, based on a screen position.
        /// </summary>
        /// <param name="point"></param>
        public void SetMonitorFromPoint(Point point)
        {
            currentMonitor = User32Interop.MonitorFromPoint(new POINT((int)point.X, (int)point.Y), MonitorOptions.MONITOR_DEFAULTTONULL);
        }

        public void SetSize(int width, int height, double dpiScaleX, double dpiScaleY) {
            if (_framebuffer == null || _framebuffer.Width != width || _framebuffer.Height != height) {
                _framebuffer?.Dispose();
                _framebuffer = null;
                if (width > 0 && height > 0) {
                    EnsureContextIsCreated();
                    _framebuffer = new DxGLFramebuffer(_context.Device, width, height, dpiScaleX, dpiScaleY);
                }
            }
        }

        public void Render(DrawingContext drawingContext) {

            if (!CanRender()) {
                return;
            }

            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            PreRender();
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
        private void PreRender()
        {
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

        private void EnsureContextIsCreated()
        {
            if(_context == null)
            {
                _context = new DxGlContext(_settings);
            }
        }

        /// <summary>
        /// This method performs different type of checks to ensure that the renderer is ready to draw a new frame.
        /// </summary>
        /// <returns></returns>
        private bool CanRender()
        {
            // If the renderer does not know on which monitor (adapter) it will be rendering onto, it can't draw
            // (it's waiting on a SetMonitorFromPoint() call).
            if (currentMonitor == IntPtr.Zero)
            {
                return false;
            }

            // Drawing not possible if there's not a framebuffer (for example, if we're waiting on a SetSize() call).
            if (_framebuffer == null)
            {
                return false;
            }

            // Check if the current framebuffer belongs to the device associated with the current monitor.

            try
            {
                // check to set the device related to the adapter that is displaying the control
                _context.SetDeviceFromMonitor(currentMonitor);

                // if the surface is related to a device that does not match the current adapter
                if (_framebuffer != null && _framebuffer.Device != _context.Device)
                {
                    // remove the framebuffer, so that it is created in following SetSize() calls
                    // with the correct device
                    _framebuffer.Dispose();
                    _framebuffer = null;

                    return false;
                }

                return true;
            }
            catch (AdapterMonitorNotFoundException)
            {
                // No adapter was found that match the current monitor.
                // Clear everything and do not draw a new frame. WPF doesn't like to do business with a surface
                // belonging to this monitor.
                // When the new context will be created, it will be able to detect the monitor and the adapters linked.

                _framebuffer?.Dispose();
                _framebuffer = null;
                _context.Dispose();
                _context = null;

                // To save us from the possibility that currentMonitor holds an invalid handle, clear it and wait for
                // the next SetMonitorFromPoint() call, which will give a valid pointer.
                currentMonitor = IntPtr.Zero;

                return false;
            }
        }
    }
}
