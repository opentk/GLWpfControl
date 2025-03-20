using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    internal sealed class GLWpfControlRenderer : IDisposable
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        private readonly DxGlContext _context;

        public event Action<TimeSpan>? GLRender;
        [Obsolete("There is no difference between GLRender and GLAsyncRender. Use GLRender.")]
        public event Action? GLAsyncRender;

        /// <summary>The width of this buffer in pixels.</summary>
        public int FramebufferWidth { get; private set; }

        /// <summary>The height of this buffer in pixels.</summary>
        public int FramebufferHeight { get; private set; }

        /// <summary>The DirectX multisample type.</summary>
        public MultisampleType MultisampleType { get; private set; }

        /// <summary>The OpenGL Framebuffer width</summary>
        public int Width => D3dImage != null ? FramebufferWidth : 0;

        /// <summary>The OpenGL Framebuffer height</summary>
        public int Height => D3dImage != null ? FramebufferHeight : 0;

        public IGraphicsContext? GLContext => _context.GraphicsContext;

        public D3DImage? D3dImage { get; private set; }

        public DXInterop.IDirect3DSurface9 DxColorRenderTarget { get; private set; }
        public DXInterop.IDirect3DSurface9 DxDepthStencilRenderTarget { get; private set; }

        public IntPtr DxInteropColorRenderTargetRegisteredHandle { get; private set; }
        public IntPtr DxInteropDepthStencilRenderTargetRegisteredHandle { get; private set; }

        /// <summary>The OpenGL framebuffer handle.</summary>
        public int GLFramebufferHandle { get; private set; }
        private int GLSharedColorRenderbufferHandle { get; set; }
        private int GLSharedDepthRenderRenderbufferHandle { get; set; }

        public TranslateTransform TranslateTransform { get; private set; }
        public ScaleTransform FlipYTransform { get; private set; }

        private TimeSpan _lastFrameStamp;

        public readonly bool SupportsMSAA;

        public GLWpfControlRenderer(GLWpfControlSettings settings)
        {
            _context = new DxGlContext(settings);
            // Placeholder transforms.
            TranslateTransform = new TranslateTransform(0, 0);
            FlipYTransform = new ScaleTransform(1, 1);

            SupportsMSAA = SupportsMSAATest();
        }

        public bool SupportsMSAATest()
        {
            // A test to see whether we can create multisample render targets without
            // getting an exception...
            try
            {
                IntPtr dxColorRenderTargetShareHandle = IntPtr.Zero;
                _context.DxDevice.CreateRenderTarget(
                128,
                128,
                Format.X8R8G8B8,
                MultisampleType.D3DMULTISAMPLE_2_SAMPLES,
                0,
                false,
                out DXInterop.IDirect3DSurface9 dxColorRenderTarget,
                ref dxColorRenderTargetShareHandle);

                IntPtr dxDepthStencilRenderTargetShareHandle = IntPtr.Zero;
                _context.DxDevice.CreateDepthStencilSurface(
                    FramebufferWidth,
                    FramebufferHeight,
                    Format.D24S8,
                    MultisampleType.D3DMULTISAMPLE_2_SAMPLES,
                    0,
                    false,
                    out DXInterop.IDirect3DSurface9 dxDepthStencilRenderTarget,
                    ref dxDepthStencilRenderTargetShareHandle);
                DxDepthStencilRenderTarget = dxDepthStencilRenderTarget;

                dxColorRenderTarget.Release();
                dxDepthStencilRenderTarget.Release();

                return true;
            }
            catch(COMException)
            {
                Trace.TraceWarning("GLWpfControl was unable to create an MSAA framebuffer on this computer.");
                return false;
            }
        }

        public void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, MultisampleType msaaType)
        {
            int newWidth = (int)Math.Ceiling(width * dpiScaleX);
            int newHeight = (int)Math.Ceiling(height * dpiScaleY);

            // Disable MSAA if we've determined we don't support it.
            // It's better to create a normal backbuffer instead of crashing.
            if (SupportsMSAA == false)
            {
                msaaType = MultisampleType.D3DMULTISAMPLE_NONE;
            }

            // FIXME: It seems we can't use this function to detect if MSAA will work with NV_DX_interop or not...
            int result = _context.DxContext.CheckDeviceMultiSampleType(0, DeviceType.HAL, format, true, msaaType, out uint qualityLevels);

            if (D3dImage == null || FramebufferWidth != newWidth || FramebufferHeight != newHeight || MultisampleType != msaaType)
            {
                ReleaseFramebufferResources();

                if (width > 0 && height > 0)
                {
                    FramebufferWidth = newWidth;
                    FramebufferHeight = newHeight;
                    MultisampleType = msaaType;

                    IntPtr dxColorRenderTargetShareHandle = IntPtr.Zero;
                    _context.DxDevice.CreateRenderTarget(
                        FramebufferWidth,
                        FramebufferHeight,
                        format,
                        msaaType,
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

                    IntPtr dxDepthStencilRenderTargetShareHandle = IntPtr.Zero;
                    _context.DxDevice.CreateDepthStencilSurface(
                        FramebufferWidth,
                        FramebufferHeight,
                        Format.D24S8,
                        msaaType,
                        0,
                        false,
                        out DXInterop.IDirect3DSurface9 dxDepthStencilRenderTarget,
                        ref dxDepthStencilRenderTargetShareHandle);
                    DxDepthStencilRenderTarget = dxDepthStencilRenderTarget;

                    success = Wgl.DXSetResourceShareHandleNV(dxDepthStencilRenderTarget.Handle, dxDepthStencilRenderTargetShareHandle);
                    if (success == false)
                    {
                        Debug.WriteLine("Failed to set resource share handle for depth stencil render target.");
                    }

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
                        DxColorRenderTarget.Handle,
                        (uint)GLSharedColorRenderbufferHandle,
                        (uint)RenderbufferTarget.Renderbuffer,
                        WGL_NV_DX_interop.AccessReadWrite);
                    if (DxInteropColorRenderTargetRegisteredHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Could not register color render target. 0x{DXInterop.GetLastError():X8}");
                    }

                    GLSharedDepthRenderRenderbufferHandle = GL.GenRenderbuffer();
                    DxInteropDepthStencilRenderTargetRegisteredHandle = Wgl.DXRegisterObjectNV(
                        _context.GLDeviceHandle,
                        DxDepthStencilRenderTarget.Handle,
                        (uint)GLSharedDepthRenderRenderbufferHandle,
                        (uint)RenderbufferTarget.Renderbuffer,
                        WGL_NV_DX_interop.AccessReadWrite);
                    if (DxInteropDepthStencilRenderTargetRegisteredHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Could not register depth stencil render target. 0x{DXInterop.GetLastError():X8}");
                    }

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

        /// <summary>
        /// Releases all resources related to the framebuffer.
        /// </summary>
        public void ReleaseFramebufferResources()
        {
            _context.GraphicsContext.MakeCurrent();

            if (D3dImage != null)
            {
                Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropColorRenderTargetRegisteredHandle);
                Wgl.DXUnregisterObjectNV(_context.GLDeviceHandle, DxInteropDepthStencilRenderTargetRegisteredHandle);
                DxColorRenderTarget.Release();
                DxDepthStencilRenderTarget.Release();
                GL.DeleteFramebuffer(GLFramebufferHandle);
                GL.DeleteRenderbuffer(GLSharedDepthRenderRenderbufferHandle);
                GL.DeleteRenderbuffer(GLSharedColorRenderbufferHandle);
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
