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
    internal sealed class GLWpfControlTexture2DRendererMSAA : IGLWpfControlRenderer
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

        public bool SupportsMSAA => true;

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

        public int GLFramebufferHandle => GLSharedFramebufferHandle;

        /// <summary>The OpenGL framebuffer handle.</summary>
        public int GLSharedFramebufferHandle { get; private set; }
        public int GLSharedTextureHandle { get; private set; }


        public int GLBlitFramebufferHandle { get; private set; }
        public int GLBlitTextureHandle { get; private set; }
        public int GLBlitDepthStencilRenderbufferHandle { get; private set; }

        public TranslateTransform TranslateTransform { get; private set; }
        public ScaleTransform FlipYTransform { get; private set; }

        private TimeSpan _lastFrameStamp;

        public GLWpfControlTexture2DRendererMSAA(DxGlContext context)
        {
            _context = context;

            // Placeholder transforms.
            TranslateTransform = new TranslateTransform(0, 0);
            FlipYTransform = new ScaleTransform(1, 1);
        }

        public void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, MultisampleType msaaType)
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

                    CreateSharedFramebuffer(format, newWidth, newHeight);

                    if (msaaType == MultisampleType.D3DMULTISAMPLE_NONE)
                    {
                        CreateBlitFramebuffer(newWidth, newHeight);
                    }
                    else
                    {
                        CreateBlitFramebufferMSAA(newWidth, newHeight, msaaType);
                    }

                    D3dImage = new D3DImage(96.0 * dpiScaleX, 96.0 * dpiScaleY);

                    TranslateTransform = new TranslateTransform(0, height);
                    FlipYTransform = new ScaleTransform(1, -1);
                }
            }
        }

        private void CreateBlitFramebuffer(int width, int height)
        {
            GLBlitTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, GLBlitTextureHandle);
            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.Rgba32f,
                          width,
                          height,
                          0,
                          PixelFormat.Rgba,
                          PixelType.Float,
                          IntPtr.Zero);

            GLBlitDepthStencilRenderbufferHandle = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, GLBlitDepthStencilRenderbufferHandle);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer,
                                   RenderbufferStorage.Depth24Stencil8,
                                   width,
                                   height);

            GLBlitFramebufferHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLBlitFramebufferHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                                    FramebufferAttachment.ColorAttachment0,
                                    TextureTarget.Texture2D,
                                    GLBlitTextureHandle,
                                    0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                                       FramebufferAttachment.DepthStencilAttachment,
                                       RenderbufferTarget.Renderbuffer,
                                       GLBlitDepthStencilRenderbufferHandle);

            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Debug.WriteLine($"Framebuffer is not complete: {status}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void CreateBlitFramebufferMSAA(int width, int height, MultisampleType msaaType)
        {
            int numSamples = msaaType == MultisampleType.D3DMULTISAMPLE_NONE
                                 ? 1
                                 : (int)msaaType;

            GLBlitTextureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, GLBlitTextureHandle);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample,
                                     numSamples,
                                     PixelInternalFormat.Rgba32f,
                                     width,
                                     height,
                                     true);

            GLBlitDepthStencilRenderbufferHandle = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, GLBlitDepthStencilRenderbufferHandle);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer,
                                              numSamples,
                                              RenderbufferStorage.Depth24Stencil8,
                                              width,
                                              height);

            GLBlitFramebufferHandle = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLBlitFramebufferHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                                    FramebufferAttachment.ColorAttachment0,
                                    TextureTarget.Texture2DMultisample,
                                    GLBlitTextureHandle,
                                    0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                                       FramebufferAttachment.DepthStencilAttachment,
                                       RenderbufferTarget.Renderbuffer,
                                       GLBlitDepthStencilRenderbufferHandle);

            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Debug.WriteLine($"Framebuffer is not complete: {status}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void CreateSharedFramebuffer(Format format, int width, int height)
        {
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

            GLSharedFramebufferHandle = GL.GenFramebuffer();
            GLSharedTextureHandle = GL.GenTexture();

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

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLSharedFramebufferHandle);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                                  FramebufferAttachment.ColorAttachment0,
                                  GLSharedTextureHandle,
                                  0);

            GL.TexImage2D(TextureTarget.Texture2D,
                          0,
                          PixelInternalFormat.Rgba32f,
                          width,
                          height,
                          0,
                          PixelFormat.Rgba,
                          PixelType.Float,
                          IntPtr.Zero);

            if (!GL.IsFramebuffer(GLSharedFramebufferHandle))
            {
                Debug.WriteLine("Failed to create framebuffer.");
            }
            if (!GL.IsTexture(GLSharedTextureHandle))
            {
                Debug.WriteLine("Failed to create texture");
            }

            // FIXME: This will report unsupported but it will not do that in Render()...?
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                Debug.WriteLine($"Framebuffer is not complete: {status}");
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
                GL.DeleteFramebuffers(2, new[] { GLSharedFramebufferHandle, GLBlitFramebufferHandle });
                GL.DeleteRenderbuffer(GLBlitDepthStencilRenderbufferHandle);
                GL.DeleteTextures(2, new[] { GLSharedTextureHandle, GLBlitTextureHandle });
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
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, GLBlitFramebufferHandle);
            GL.Viewport(0, 0, FramebufferWidth, FramebufferHeight);

            GLRender?.Invoke(deltaT);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GLAsyncRender?.Invoke();

            bool success = Wgl.DXLockObjectsNV(_context.GLDeviceHandle, 1, new[] { DxInteropRegisteredHandle });
            if (success == false)
            {
                Debug.WriteLine("Failed to lock objects!");
            }

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, GLBlitFramebufferHandle);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, GLSharedFramebufferHandle);

            GL.BlitFramebuffer(0, 0, FramebufferWidth, FramebufferHeight,
                               0, 0, FramebufferWidth, FramebufferHeight,
                               ClearBufferMask.ColorBufferBit,
                               BlitFramebufferFilter.Nearest);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

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
