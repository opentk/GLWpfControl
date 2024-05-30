using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Windowing.Common;
using OpenTK.Wpf.Interop;

#nullable enable

namespace OpenTK.Wpf
{
    /// <summary>Renderer that uses DX_Interop for a fast-path.</summary>
    internal sealed class GLWpfControlRenderer : IDisposable {

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private readonly DxGlContext _context;

        public event Action<TimeSpan>? GLRender;
        public event Action? GLAsyncRender;

        /// <summary>The width of this buffer in pixels.</summary>
        public int FramebufferWidth { get; private set; }

        /// <summary>The height of this buffer in pixels.</summary>
        public int FramebufferHeight { get; private set; }

        /// <summary>The OpenGL framebuffer handle.</summary>
        public int FrameBufferHandle { get; private set; }

        /// <summary>The DirectX multisample type.</summary>
        public MultisampleType MultisampleType { get; private set; }

        /// <summary>The OpenGL Framebuffer width</summary>
        public int Width => D3dImage == null ? FramebufferWidth : 0;

        /// <summary>The OpenGL Framebuffer height</summary>
        public int Height => D3dImage == null ? FramebufferHeight : 0;

        public IGraphicsContext? GLContext => _context.GraphicsContext;

        public D3DImage? D3dImage { get; private set; }

        public DXInterop.IDirect3DSurface9 DxColorRenderTarget { get; private set; }
        public DXInterop.IDirect3DSurface9 DxDepthStencilRenderTarget { get; private set; }

        public IntPtr DxInteropColorRenderTargetRegisteredHandle { get; private set; }
        public IntPtr DxInteropDepthStencilRenderTargetRegisteredHandle { get; private set; }

        public int GLFramebufferHandle { get; private set; }
        private int GLSharedColorRenderbufferHandle { get; set; }
        private int GLSharedDepthRenderRenderbufferHandle { get; set; }

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

        public void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, MultisampleType msaaType)
        {
            int newWidth = (int)Math.Ceiling(width * dpiScaleX);
            int newHeight = (int)Math.Ceiling(height * dpiScaleY);

            if (D3dImage == null || FramebufferWidth != newWidth || FramebufferHeight != newHeight || MultisampleType != msaaType)
            {
                if (D3dImage != null)
                {
                    GL.DeleteFramebuffer(GLFramebufferHandle);
                    GL.DeleteRenderbuffer(GLSharedDepthRenderRenderbufferHandle);
                    GL.DeleteRenderbuffer(GLSharedColorRenderbufferHandle);
                    Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropColorRenderTargetRegisteredHandle);
                    Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropDepthStencilRenderTargetRegisteredHandle);
                    DxColorRenderTarget.Release();
                    DxDepthStencilRenderTarget.Release();
                }
                D3dImage = null;

                if (width > 0 && height > 0)
                {
                    FramebufferWidth = newWidth;
                    FramebufferHeight = newHeight;
                    MultisampleType = msaaType;

                    _context.DxDevice.CreateRenderTarget(
                        FramebufferWidth,
                        FramebufferHeight,
                        format,
                        msaaType,
                        0,
                        false,
                        out DXInterop.IDirect3DSurface9 dxColorRenderTarget,
                        ref Unsafe.NullRef<IntPtr>());
                    DxColorRenderTarget = dxColorRenderTarget;

                    _context.DxDevice.CreateDepthStencilSurface(
                        FramebufferWidth,
                        FramebufferHeight,
                        Format.D24S8,
                        msaaType,
                        0,
                        false,
                        out DXInterop.IDirect3DSurface9 dxDepthStencilRenderTarget,
                        ref Unsafe.NullRef<IntPtr>());
                    DxDepthStencilRenderTarget = dxDepthStencilRenderTarget;

#if DEBUG
                    {
                        DxColorRenderTarget.GetDesc(out DXInterop.D3DSURFACE_DESC desc);

                        Debug.WriteLine($"Render target desc: {desc.Format}, {desc.Type}, {desc.Usage}, {desc.Pool}, {desc.MultiSampleType}, {desc.MultiSampleQuality}, {desc.Width}, {desc.Height}");
                    }

                    {
                        DxDepthStencilRenderTarget.GetDesc(out DXInterop.D3DSURFACE_DESC desc);

                        Debug.WriteLine($"Render target desc: {desc.Format}, {desc.Type}, {desc.Usage}, {desc.Pool}, {desc.MultiSampleType}, {desc.MultiSampleQuality}, {desc.Width}, {desc.Height}");
                    }
#endif

                    GLFramebufferHandle = GL.GenFramebuffer();

                    TextureTarget colorTextureTarget = msaaType == MultisampleType.D3DMULTISAMPLE_NONE ? TextureTarget.Texture2D : TextureTarget.Texture2DMultisample;

                    GLSharedColorRenderbufferHandle = GL.GenRenderbuffer();
                    DxInteropColorRenderTargetRegisteredHandle = Wgl.DXRegisterObjectNV(
                        _context.GLDeviceHandle,
                        dxColorRenderTarget.Handle,
                        (uint)GLSharedColorRenderbufferHandle,
                        (uint)RenderbufferTarget.Renderbuffer,
                        WGL_NV_DX_interop.AccessReadWrite);

                    GLSharedDepthRenderRenderbufferHandle = GL.GenRenderbuffer();
                    DxInteropDepthStencilRenderTargetRegisteredHandle = Wgl.DXRegisterObjectNV(
                        _context.GLDeviceHandle,
                        dxDepthStencilRenderTarget.Handle,
                        (uint)GLSharedDepthRenderRenderbufferHandle,
                        (uint)RenderbufferTarget.Renderbuffer,
                        WGL_NV_DX_interop.AccessReadWrite);

                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLFramebufferHandle);

                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.ColorAttachment0,
                        RenderbufferTarget.Renderbuffer,
                        GLSharedColorRenderbufferHandle);

                    // FIXME: If we have a combined format, maybe set both at the same time?
                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.DepthAttachment,
                        RenderbufferTarget.Renderbuffer,
                        GLSharedDepthRenderRenderbufferHandle);

                    GL.FramebufferRenderbuffer(
                        FramebufferTarget.Framebuffer,
                        FramebufferAttachment.StencilAttachment,
                        RenderbufferTarget.Renderbuffer,
                        GLSharedDepthRenderRenderbufferHandle);

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

        public void Render(DrawingContext drawingContext)
        {
            if (D3dImage == null)
            {
                return;
            }

            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;

            // Lock the interop object, DX calls to the framebuffer are no longer valid
            D3dImage.Lock();
            D3dImage.SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, DxColorRenderTarget.Handle, true);
            bool success = Wgl.DXLockObjectsNV(_context.GLDeviceHandle, 2, new[] { DxInteropColorRenderTargetRegisteredHandle, DxInteropDepthStencilRenderTargetRegisteredHandle });
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
            success = Wgl.DXUnlockObjectsNV(_context.GLDeviceHandle, 2, new[] { DxInteropColorRenderTargetRegisteredHandle, DxInteropDepthStencilRenderTargetRegisteredHandle });
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
            var rect = new Rect(0, 0, D3dImage.Width, D3dImage.Height);
            // Draw the image source 
            drawingContext.DrawImage(D3dImage, rect);

            // Remove the scale transform and the translation transform
            drawingContext.Pop();
            drawingContext.Pop();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
