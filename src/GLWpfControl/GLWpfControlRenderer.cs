using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf
{

    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRenderer {

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly DxGlContext _context;

        public event Action<TimeSpan> GLRender;
        public event Action GLAsyncRender;

        //private DxGLFramebuffer _framebuffer;

        /// <summary>The width of this buffer in pixels.</summary>
        public int FramebufferWidth { get; private set; }

        /// <summary>The height of this buffer in pixels.</summary>
        public int FramebufferHeight { get; private set; }

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle { get; private set; }

        /// The OpenGL Framebuffer width
        public int Width => D3dImage == null ? FramebufferWidth : 0;
        
        /// The OpenGL Framebuffer height
        public int Height => D3dImage == null ? FramebufferHeight : 0;

        public D3DImage D3dImage { get; private set; }

        public IntPtr DxRenderTargetHandle { get; private set; }

        public IntPtr DxInteropRegisteredHandle { get; private set; }

        public int GLFramebufferHandle { get; private set; }
        private int GLSharedTextureHandle { get; set; }
        private int GLDepthRenderBufferHandle { get; set; }

        public TranslateTransform TranslateTransform { get; private set; }
        public ScaleTransform FlipYTransform { get; private set; }

        private TimeSpan _lastFrameStamp;

        public GLWpfControlRenderer(GLWpfControlSettings settings)
        {
            _context = new DxGlContext(settings);
        }

        public void SetSize(int width, int height, double dpiScaleX, double dpiScaleY, Format format) {
            if (D3dImage == null || FramebufferWidth != width || FramebufferHeight != height) {
                //D3dImage?.Dispose();
                if (D3dImage != null)
                {
                    GL.DeleteFramebuffer(GLFramebufferHandle);
                    GL.DeleteRenderbuffer(GLDepthRenderBufferHandle);
                    GL.DeleteTexture(GLSharedTextureHandle);
                    Wgl.DXUnregisterObjectNV(_context.GlDeviceHandle, DxInteropRegisteredHandle);
                    DXInterop.Release(DxRenderTargetHandle);
                }
                D3dImage = null;

                if (width > 0 && height > 0) {
                    //_framebuffer = new DxGLFramebuffer(_context, width, height, dpiScaleX, dpiScaleY, format);

                    FramebufferWidth = (int)Math.Ceiling(width * dpiScaleX);
                    FramebufferHeight = (int)Math.Ceiling(height * dpiScaleY);

                    var dxSharedHandle = IntPtr.Zero; // Unused windows-vista legacy sharing handle. Must always be null.
                    DXInterop.CreateRenderTarget(
                        _context.DxDeviceHandle,
                        FramebufferWidth,
                        FramebufferHeight,
                        format,
                        MultisampleType.None,
                        0,
                        false,
                        out var dxRenderTargetHandle,
                        ref dxSharedHandle);
                    DxRenderTargetHandle = dxRenderTargetHandle;

                    Wgl.DXSetResourceShareHandleNV(dxRenderTargetHandle, dxSharedHandle);

                    GLFramebufferHandle = GL.GenFramebuffer();
                    GLSharedTextureHandle = GL.GenTexture();

                    var genHandle = Wgl.DXRegisterObjectNV(
                        _context.GlDeviceHandle,
                        dxRenderTargetHandle,
                        (uint)GLSharedTextureHandle,
                        (uint)TextureTarget.Texture2D,
                        WGL_NV_DX_interop.AccessReadWrite);

                    DxInteropRegisteredHandle = genHandle;

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0,
                        TextureTarget.Texture2D,
                        GLSharedTextureHandle, 0);

                    GLDepthRenderBufferHandle = GL.GenRenderbuffer();
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, GLDepthRenderBufferHandle);
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, FramebufferWidth, FramebufferHeight);

                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        RenderbufferTarget.Renderbuffer,
                        GLDepthRenderBufferHandle);
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.StencilAttachment,
                        RenderbufferTarget.Renderbuffer,
                        GLDepthRenderBufferHandle);

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);


                    D3dImage = new D3DImage(96.0 * dpiScaleX, 96.0 * dpiScaleY);

                    TranslateTransform = new TranslateTransform(0, height);
                    FlipYTransform = new ScaleTransform(1, -1);

                }
            }
        }

        public void Render(DrawingContext drawingContext) {
            if (D3dImage == null) {
                return;
            }
            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            // Lock the interop object, DX calls to the framebuffer are no longer valid
            D3dImage.Lock();
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] { DxInteropRegisteredHandle });
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);
            GL.Viewport(0, 0, FramebufferWidth, FramebufferHeight);

            GLRender?.Invoke(deltaT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GLAsyncRender?.Invoke();

            // Unlock the interop object, this acts as a synchronization point. OpenGL draws to the framebuffer are no longer valid.
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] { DxInteropRegisteredHandle });
            D3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, DxRenderTargetHandle, true);
            D3dImage.AddDirtyRect(new Int32Rect(0, 0, FramebufferWidth, FramebufferHeight));
            D3dImage.Unlock();

            // Transforms are applied in reverse order
            drawingContext.PushTransform(TranslateTransform);              // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
            drawingContext.PushTransform(FlipYTransform);                  // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg

            // dpi scaled rectangle from the image
            var rect = new Rect(0, 0, D3dImage.Width, D3dImage.Height);
            drawingContext.DrawImage(D3dImage, rect);            // Draw the image source 

            drawingContext.Pop();                                          // Remove the scale transform
            drawingContext.Pop();                                          // Remove the translation transform
        }
    }
}
