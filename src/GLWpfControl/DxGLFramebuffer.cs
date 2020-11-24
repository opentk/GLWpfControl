using System;
using System.Windows.Interop;
using System.Windows.Media;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf {
    
    /// Class containing the DirectX Render Surface and OpenGL Framebuffer Object
    /// Instances of this class are created and deleted as required by the renderer.
    /// Note that this does not implement the full <see cref="IDisposable"/> pattern,
    /// as OpenGL resources cannot be freed from the finalizer thread.
    /// The calling class must correctly dispose of this by calling <see cref="Dispose"/>
    /// Prior to releasing references. 
    internal sealed class DxGLFramebuffer : IDisposable {

        private DxGlContext DxGlContext { get; }
        
        /// The width of this buffer in pixels
        public int FramebufferWidth { get; }
        
        /// The height of this buffer in pixels
        public int FramebufferHeight { get; }

        /// The width of the element in device-independent pixels
        public int Width { get; }

        /// The height of the element in device-independent pixels
        public int Height { get; }
        
        /// The DirectX Render target (framebuffer) handle.
        public IntPtr DxRenderTargetHandle { get; }
        
        /// The OpenGL Framebuffer handle
        public int GLFramebufferHandle { get; }

        /// The OpenGL shared texture handle (with DX)
        private int GLSharedTextureHandle { get; }

        /// The OpenGL depth render buffer handle.
        private int GLDepthRenderBufferHandle { get; }
        
        /// Specific wgl_dx_interop handle that marks the framebuffer as ready for interop.
        public IntPtr DxInteropRegisteredHandle { get; }

        
        public D3DImage D3dImage { get; }

        public TranslateTransform TranslateTransform { get; }
        public ScaleTransform FlipYTransform { get; }


        public DxGLFramebuffer([NotNull] DxGlContext context, int width, int height, double dpiScaleX, double dpiScaleY) {
            DxGlContext = context;
            Width = width;
            Height = height;
            FramebufferWidth = (int)Math.Ceiling(width * dpiScaleX);
            FramebufferHeight = (int)Math.Ceiling(height * dpiScaleY);
            
            var dxSharedHandle = IntPtr.Zero; // Unused windows-vista legacy sharing handle. Must always be null.
            DXInterop.CreateRenderTarget(
                context.DxDeviceHandle,
                FramebufferWidth,
                FramebufferHeight,
                Format.X8R8G8B8,// this is like A8 R8 G8 B8, but avoids issues with Gamma correction being applied twice.
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
                context.GlDeviceHandle,
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
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, FramebufferWidth, FramebufferHeight);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                GLDepthRenderBufferHandle);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            
            D3dImage = new D3DImage(96.0 * dpiScaleX, 96.0 * dpiScaleY);
            
            TranslateTransform = new TranslateTransform(0, height);
            FlipYTransform = new ScaleTransform(1, -1);
        }
        
        
        public void Dispose() {
            GL.DeleteBuffer(GLFramebufferHandle);
            GL.DeleteRenderbuffer(GLDepthRenderBufferHandle);
            GL.DeleteTexture(GLSharedTextureHandle);
            Wgl.DXUnregisterObjectNV(DxGlContext.GlDeviceHandle, DxInteropRegisteredHandle);
            DXInterop.Release(DxRenderTargetHandle);
        }
    }
}
