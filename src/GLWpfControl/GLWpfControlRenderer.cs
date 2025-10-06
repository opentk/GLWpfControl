using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf
{
    /// <summary>Renderer that uses DX_Interop for a fast-path.</summary>
    internal sealed class GLWpfControlRenderer : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        internal readonly DxGlContext _context;

        public event Action<TimeSpan> GLRender;
        [Obsolete("There is no difference between GLRender and GLAsyncRender. Use GLRender.")]
        public event Action GLAsyncRender;

        /// <summary>The width of this buffer in pixels.</summary>
        public int FramebufferWidth { get; private set; }

        /// <summary>The height of this buffer in pixels.</summary>
        public int FramebufferHeight { get; private set; }

        /// <summary>The number of Framebuffer MSAA samples.</summary>
        public int Samples { get; private set; }

        /// <summary>The OpenGL Framebuffer width</summary>
        public int Width => D3dImage != null ? FramebufferWidth : 0;

        /// <summary>The OpenGL Framebuffer height</summary>
        public int Height => D3dImage != null ? FramebufferHeight : 0;

        public IGraphicsContext GLContext => _context.GraphicsContext;

        public D3DImage D3dImage { get; private set; }

        public DXInterop.IDirect3DSurface9 DxColorRenderTarget { get; private set; }

        public IntPtr DxInteropColorRenderTargetRegisteredHandle { get; private set; }

        private int GLSharedFramebufferHandle { get; set; }
        private int GLSharedColorTextureHandle { get; set; }

        /// <summary>The OpenGL framebuffer handle.</summary>
        public int GLFramebufferHandle { get; private set; }
        private int GLColorTextureHandle { get; set; }
        private int GLDepthRenderRenderbufferHandle { get; set; }

        public TranslateTransform TranslateTransform { get; private set; }
        public ScaleTransform FlipYTransform { get; private set; }

        private TimeSpan _lastFrameStamp;

        public GLWpfControlRenderer(GLWpfControlSettings settings)
        {
            _context = new DxGlContext(settings);
            // Placeholder transforms.
            TranslateTransform = new TranslateTransform(0, 0);
            FlipYTransform = new ScaleTransform(1, 1);
        }

        public void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, int samples)
        {
            int newWidth = (int)Math.Ceiling(width * dpiScaleX);
            int newHeight = (int)Math.Ceiling(height * dpiScaleY);

            if (D3dImage == null || FramebufferWidth != newWidth || FramebufferHeight != newHeight || Samples != samples)
            {
                ReleaseFramebufferResources();

                if (width > 0 && height > 0)
                {
                    FramebufferWidth = newWidth;
                    FramebufferHeight = newHeight;
                    Samples = samples;

                    IntPtr dxColorRenderTargetShareHandle = IntPtr.Zero;
                    _context.DxDevice.CreateRenderTarget(
                        FramebufferWidth,
                        FramebufferHeight,
                        format,
                        MultisampleType.D3DMULTISAMPLE_NONE,
                        0,
                        false,
                        out DXInterop.IDirect3DSurface9 dxColorRenderTarget,
                        ref dxColorRenderTargetShareHandle);
                    DxColorRenderTarget = dxColorRenderTarget;

                    bool success;
                    success = Wgl.DXSetResourceShareHandleNV(DxColorRenderTarget.Handle, dxColorRenderTargetShareHandle);
                    if (success == false)
                    {
                        Debug.WriteLine("Failed to set resource share handle for color render target.");
                    }

#if DEBUG
                    {
                        DxColorRenderTarget.GetDesc(out DXInterop.D3DSURFACE_DESC desc);

                        Debug.WriteLine($"Render target desc: {desc.Format}, {desc.Type}, {desc.Usage}, {desc.Pool}, {desc.MultiSampleType}, {desc.MultiSampleQuality}, {desc.Width}, {desc.Height}");
                    }
#endif

                    int prevFramebuffer = GL.GetInteger(GetPName.FramebufferBinding);
                    int prevRenderbuffer = GL.GetInteger(GetPName.RenderbufferBinding);

                    GLSharedFramebufferHandle = GL.GenFramebuffer();

                    GLSharedColorTextureHandle = GL.GenTexture();
                    DxInteropColorRenderTargetRegisteredHandle = Wgl.DXRegisterObjectNV(
                        _context.GLDeviceHandle,
                        DxColorRenderTarget.Handle,
                        (uint)GLSharedColorTextureHandle,
                        (uint)TextureTarget.Texture2D,
                        WGL_NV_DX_interop.AccessReadWrite);
                    if (DxInteropColorRenderTargetRegisteredHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Could not register color render target. 0x{DXInterop.GetLastError():X8}");
                    }

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLSharedFramebufferHandle);
                    GL.FramebufferTexture2D(
                        FramebufferTarget.Framebuffer, 
                        FramebufferAttachment.ColorAttachment0, 
                        TextureTarget.Texture2D,
                        GLSharedColorTextureHandle,
                        0);

                    FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);
                    if (status != FramebufferErrorCode.FramebufferComplete)
                    {
                        Debug.WriteLine($"Shared framebuffer is not complete: {status}");
                    }

                    GLFramebufferHandle = GL.GenFramebuffer();

                    GLColorTextureHandle =  GL.GenTexture();
                    if (Samples > 1)
                    {
                        int prevT2dms = GL.GetInteger(GetPName.TextureBinding2DMultisample);
                        GL.BindTexture(TextureTarget.Texture2DMultisample, GLColorTextureHandle);
                        GL.TexImage2DMultisample(
                            TextureTargetMultisample.Texture2DMultisample,
                            Samples,
                            PixelInternalFormat.Rgba8, 
                            FramebufferWidth,
                            FramebufferHeight,
                            true);
                        GL.BindTexture(TextureTarget.Texture2DMultisample, prevT2dms);
                    }
                    else
                    {
                        // We don't need this renderbuffer for non MSAA rendering.
                        // - Noggin_bops 2025-07-03
                        // GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, FramebufferWidth, FramebufferHeight);
                    }

                    GLDepthRenderRenderbufferHandle = GL.GenRenderbuffer();
                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, GLDepthRenderRenderbufferHandle);
                    if (Samples > 1)
                    {
                        GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Samples, RenderbufferStorage.Depth24Stencil8, FramebufferWidth, FramebufferHeight);
                    }
                    else
                    {
                        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, FramebufferWidth, FramebufferHeight);
                    }

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);

                    if (Samples > 1)
                    {
                        GL.FramebufferTexture2D(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0,
                            TextureTarget.Texture2DMultisample,
                            GLColorTextureHandle,
                            0);
                    }
                    else
                    {
                        // If we are not doing MSAA we use the shared renderbuffer directly.
                        // - Noggin_bops 2025-07-03
                        GL.FramebufferTexture2D(
                            FramebufferTarget.Framebuffer,
                            FramebufferAttachment.ColorAttachment0,
                            TextureTarget.Texture2D,
                            GLSharedColorTextureHandle,
                            0);
                    }

                    // FIXME: What if we don't have a combined format?
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthStencilAttachment,
                        RenderbufferTarget.Renderbuffer,
                        GLDepthRenderRenderbufferHandle);

                    // FIXME: This will report unsupported but it will not do that in Render()...?
                    status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (status != FramebufferErrorCode.FramebufferComplete)
                    {
                        Debug.WriteLine($"Framebuffer is not complete: {status}");
                    }

                    GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, prevRenderbuffer);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFramebuffer);

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
            _context.GraphicsContext.MakeCurrent(_context.WindowInfo);

            if (D3dImage != null)
            {
                Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropColorRenderTargetRegisteredHandle);

                DxColorRenderTarget.Release();

                GL.DeleteFramebuffer(GLSharedFramebufferHandle);
                GL.DeleteTexture(GLSharedColorTextureHandle);

                GL.DeleteFramebuffer(GLFramebufferHandle);
                GL.DeleteTexture(GLColorTextureHandle);
                GL.DeleteRenderbuffer(GLDepthRenderRenderbufferHandle);
            }

            D3dImage = null;
        }

        /// <summary>
        /// Releases all resources related to the framebuffer.
        /// </summary>
        public void ReleaseFramebufferResources()
        {
            _context.GraphicsContext.MakeCurrent();

            if (D3dImage != null)
            {
                Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropColorRenderTargetRegisteredHandle);

                DxColorRenderTarget.Release();

                GL.DeleteFramebuffer(GLSharedFramebufferHandle);
                GL.DeleteTexture(GLSharedColorTextureHandle);

                GL.DeleteFramebuffer(GLFramebufferHandle);
                GL.DeleteTexture(GLColorTextureHandle);
                GL.DeleteRenderbuffer(GLDepthRenderRenderbufferHandle);
            }

            D3dImage = null;
        }

        public void Render(DrawingContext drawingContext)
        {
            if (D3dImage == null)
            {
                return;
            }

            _context.GraphicsContext.MakeCurrent(_context.WindowInfo);

            TimeSpan curFrameStamp = _stopwatch.Elapsed;
            TimeSpan deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            // Lock the interop object, DX calls to the framebuffer are no longer valid
            D3dImage.Lock();
            D3dImage.SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, DxColorRenderTarget.Handle, true);
            bool success = Wgl.DXLockObjectsNV(_context.GLDeviceHandle, 1, new[] { DxInteropColorRenderTargetRegisteredHandle });
            if (success == false)
            {
                Debug.WriteLine("Failed to lock objects!");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);
            GL.Viewport(0, 0, FramebufferWidth, FramebufferHeight);
            GLRender?.Invoke(deltaT);
            if (Samples > 1)
            {
                // If we have MSAA enabled we need to resolve from our OpenGL hosted multisampled renderbuffer
                // to the DX shared non-MSAA texture.
                // - Noggin_bops 2025-07-03
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, GLSharedFramebufferHandle);
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, GLFramebufferHandle);
                GL.BlitFramebuffer(0, 0, FramebufferWidth, FramebufferHeight, 0, 0, FramebufferWidth, FramebufferHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            }
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GLAsyncRender?.Invoke();

            // Unlock the interop object, this acts as a synchronization point. OpenGL draws to the framebuffer are no longer valid.
            success = Wgl.DXUnlockObjectsNV(_context.GLDeviceHandle, 1, new[] { DxInteropColorRenderTargetRegisteredHandle });
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
