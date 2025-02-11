using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Windowing.Common;
using OpenTK.Wpf.Interop;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

#nullable enable

namespace OpenTK.Wpf.Renderers
{
    /// <summary>Renderer that uses DX_Interop for a fast-path.</summary>
    internal sealed class GLWpfControlTexture2DRenderer : IGLWpfControlRenderer
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private readonly DxGlContext _context;

        public event Action<TimeSpan>? GLRender;

        [Obsolete("There is no difference between GLRender and GLAsyncRender. Use GLRender.")]
        public event Action? GLAsyncRender;

        /// <summary>The width of this buffer in pixels.</summary>
        public int FramebufferWidth { get; set; }

        /// <summary>The height of this buffer in pixels.</summary>
        public int FramebufferHeight { get; private set; }

        public bool SupportsMSAA => false;

        /// <summary>The OpenGL Framebuffer width</summary>
        public int Width => D3dImage == null ? FramebufferWidth : 0;

        /// <summary>The OpenGL Framebuffer height</summary>
        public int Height => D3dImage == null ? FramebufferHeight : 0;

        public IGraphicsContext GLContext => _context.GraphicsContext;

        public D3DImage? D3dImage { get; private set; }

        /// <summary>
        /// D3D9 surface handle, which will be the surface shared with the GL Framebuffer
        /// </summary>
        public DXInterop.IDirect3DSurface9 DxRenderTargetHandle { get; private set; }
        public IntPtr DxInteropRegisteredHandle { get; private set; }

        /// <summary>The OpenGL framebuffer handle.</summary>
        public int GLFramebufferHandle { get; private set; }
        public int GLSharedTextureHandle { get; private set; }
        public int GLDepthStencilRenderbufferHandle { get; private set; }

        public TranslateTransform TranslateTransform { get; private set; }
        public ScaleTransform FlipYTransform { get; private set; }

        private TimeSpan _lastFrameStamp;

        public GLWpfControlTexture2DRenderer(DxGlContext context)
        {
            _context = context;

            // Placeholder transforms.
            TranslateTransform = new TranslateTransform(0, 0);
            FlipYTransform = new ScaleTransform(1, 1);
        }

        public void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, MultisampleType _)
        {
            int newWidth = (int)Math.Ceiling(width * dpiScaleX);
            int newHeight = (int)Math.Ceiling(height * dpiScaleY);

            if (D3dImage == null || FramebufferWidth != newWidth || FramebufferHeight != newHeight)
            {
                ReleaseFramebufferResources();

                if (width > 0 && height > 0)
                {
                    FramebufferWidth = newWidth;
                    FramebufferHeight = newHeight;

                    IntPtr dxSharedHandle = IntPtr.Zero;

                    // Create a D3D9 surface of a size that matches the framebuffer size
                    _context.DxDevice.CreateRenderTarget(
                        FramebufferWidth,
                        FramebufferHeight,
                        format,
                        MultisampleType.D3DMULTISAMPLE_NONE,
                        0,
                        false,
                        out DXInterop.IDirect3DSurface9 dxRenderTargetHandle,
                        ref dxSharedHandle);

                    DxRenderTargetHandle = dxRenderTargetHandle;

                    bool success;
                    success = Wgl.DXSetResourceShareHandleNV(DxRenderTargetHandle.Handle, dxSharedHandle);
                    if (success == false)
                    {
                        Debug.WriteLine("Failed to set resource share handle for color render target.");
                    }
#if DEBUG
                    {
                        DxRenderTargetHandle.GetDesc(out DXInterop.D3DSURFACE_DESC desc);

                        Debug.WriteLine($"Render target desc: {desc.Format}, {desc.Type}, {desc.Usage}, {desc.Pool}, {desc.MultiSampleType}, {desc.MultiSampleQuality}, {desc.Width}, {desc.Height}");
                    }
#endif

                    GLFramebufferHandle = GL.GenFramebuffer();
                    GLSharedTextureHandle = GL.GenTexture();
                    GLDepthStencilRenderbufferHandle = GL.GenRenderbuffer();

                    DxInteropRegisteredHandle = Wgl.DXRegisterObjectNV(
                        _context.GLDeviceHandle,
                        DxRenderTargetHandle.Handle,
                        (uint)GLSharedTextureHandle,
                        (uint)TextureTarget.Texture2D,
                        WGL_NV_DX_interop.AccessReadWrite);

                    if (DxInteropRegisteredHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Could not register color render target. 0x{DXInterop.GetLastError():X8}");
                    }

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, GLDepthStencilRenderbufferHandle);

                    GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                                          FramebufferAttachment.ColorAttachment0,
                                          GLSharedTextureHandle,
                                          0);

                    GL.TexImage2D(TextureTarget.Texture2D,
                                  0,
                                  PixelInternalFormat.Rgba32f,
                                  newWidth,
                                  newHeight,
                                  0,
                                  PixelFormat.Rgba,
                                  PixelType.Float,
                                  IntPtr.Zero);
                    GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                                           RenderbufferStorage.Depth24Stencil8,
                                           newWidth,
                                           newHeight);
                    GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                                               FramebufferAttachment.DepthStencilAttachment,
                                               RenderbufferTarget.Renderbuffer,
                                               GLDepthStencilRenderbufferHandle);

                    if (!GL.IsFramebuffer(GLFramebufferHandle))
                    {
                        Debug.WriteLine("Failed to create framebuffer.");
                    }
                    if (!GL.IsTexture(GLSharedTextureHandle))
                    {
                        Debug.WriteLine("Failed to create texture");
                    }
                    if (!GL.IsRenderbuffer(GLDepthStencilRenderbufferHandle))
                    {
                        Debug.WriteLine("Failed to create renderbuffer");
                    }

                    // FIXME: This will report unsupported but it will not do that in Render()...?
                    FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);
                    if (status != FramebufferErrorCode.FramebufferComplete)
                    {
                        Debug.WriteLine($"Framebuffer is not complete: {status}");
                    }

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                    D3dImage = new D3DImage(96.0 * dpiScaleX, 96.0 * dpiScaleY);

                    TranslateTransform = new TranslateTransform(0, height);
                    FlipYTransform = new ScaleTransform(1, -1);
                }
            }
        }

        /// <summary>
        /// Releases all resources related to the framebuffer.
        /// </summary>
        public void ReleaseFramebufferResources()
        {
            _context.GraphicsContext.MakeCurrent();

            if (D3dImage != null)
            {
                Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropRegisteredHandle);
                DxRenderTargetHandle.Release();
                GL.DeleteFramebuffer(GLFramebufferHandle);
                GL.DeleteTexture(GLSharedTextureHandle);
                GL.DeleteRenderbuffer(GLDepthStencilRenderbufferHandle);
            }
            D3dImage = null;
        }

        public void Render(DrawingContext drawingContext)
        {
            if (D3dImage == null)
            {
                return;
            }

            _context.GraphicsContext.MakeCurrent();

            TimeSpan curFrameStamp = _stopwatch.Elapsed;
            TimeSpan deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            // Lock the interop object, DX calls to the framebuffer are no longer valid
            D3dImage.Lock();
            D3dImage.SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, DxRenderTargetHandle.Handle, true);
            bool success = Wgl.DXLockObjectsNV(_context.GLDeviceHandle, 1, new[] { DxInteropRegisteredHandle });
            if (success == false)
            {
                Debug.WriteLine("Failed to lock objects!");
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);
            GL.Viewport(0, 0, FramebufferWidth, FramebufferHeight);

            GLRender?.Invoke(deltaT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GLAsyncRender?.Invoke();

            // Unlock the interop object, this acts as a synchronization point. OpenGL draws to the framebuffer are no longer valid.
            success = Wgl.DXUnlockObjectsNV(_context.GLDeviceHandle, 1, new[] { DxInteropRegisteredHandle });
            if (success == false)
            {
                Debug.WriteLine("Failed to unlock objects!");
            }

            D3dImage.AddDirtyRect(new Int32Rect(0, 0, FramebufferWidth, FramebufferHeight));
            D3dImage.Unlock();

            // Transforms are applied in reverse order
            // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
            drawingContext.PushTransform(TranslateTransform);
            // Apply a scale where the Y axis is -1. This will flip the image vertically.
            drawingContext.PushTransform(FlipYTransform);

            // Dpi scaled rectangle from the image
            Rect rect = new Rect(0, 0, D3dImage.Width, D3dImage.Height);
            // Draw the image source
            drawingContext.DrawImage(D3dImage, rect);

            // Remove the scale transform and the translation transform
            drawingContext.Pop();
            drawingContext.Pop();
        }

        public void Dispose()
        {
            ReleaseFramebufferResources();
            _context.Dispose();
        }
    }
}
