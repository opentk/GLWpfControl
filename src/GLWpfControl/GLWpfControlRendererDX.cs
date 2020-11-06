using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SharpDX.Direct3D9;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenTK.Wpf {
    
    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRendererDx {
        /// arrays used to lock the shared GL object handles.
        private readonly IntPtr[] _glDxInteropSharedHandles;
        private readonly IntPtr _glHandle;
        private readonly IntPtr _dxSharedhandle;
        private int _glSharedTexture;
        private int _glFrameBuffer;
        private int _glDepthRenderBuffer;
        private Device _dxDevice;
        private Surface _dxSurface;
        
        private readonly WGLInterop _wglInterop;
        private D3DImage _image;

        public int FrameBuffer { get; }

        public int Width { get; set; }
        public int Height { get; set; }

        public GLWpfControlRendererDx(int width, int height, D3DImage imageControl) {

            _image = imageControl;
            _glHandle = IntPtr.Zero;
            _dxSharedhandle = IntPtr.Zero;
            
            _wglInterop = new WGLInterop();
            
            
            _dxDevice = new Device(DxInterop.Direct3DCreate9(DxInterop.D3DSdkVersion));
            _dxDevice = new DeviceEx(
                new Direct3DEx(),
                0,
                DeviceType.Hardware,
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing |
                CreateFlags.Multithreaded |
                CreateFlags.PureDevice,
                new PresentParameters()
                {
                    Windowed = true,
                    SwapEffect = SwapEffect.Discard,
                    DeviceWindowHandle = IntPtr.Zero,
                    PresentationInterval = PresentInterval.Default,
                    BackBufferFormat = Format.Unknown,
                    BackBufferWidth = width,
                    BackBufferHeight = height
                });

            _dxSurface = Surface.CreateRenderTarget(
                _dxDevice,
                width,
                height,
                Format.A8R8G8B8,
                MultisampleType.None,
                0,
                false,
                ref _dxSharedhandle);

            _glFrameBuffer = GL.GenFramebuffer();
            _glSharedTexture = GL.GenTexture();

            _glHandle = _wglInterop.WglDXOpenDeviceNV(_dxDevice.NativePointer);
            _wglInterop.WglDXSetResourceShareHandleNV(_dxSurface.NativePointer, _dxSharedhandle);

            var genHandle = _wglInterop.WglDXRegisterObjectNV(
                _glHandle,
                _dxSurface.NativePointer,
                (uint)_glSharedTexture,
                (uint)TextureTarget.Texture2D,
                WGLInterop.WGL_ACCESS_READ_WRITE_NV);
            _glDxInteropSharedHandles = new[] { genHandle };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glFrameBuffer);
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                _glSharedTexture, 0);

            _glDepthRenderBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _glDepthRenderBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                 RenderbufferTarget.Renderbuffer,
                 _glDepthRenderBuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void DeleteBuffers() {
            GL.DeleteFramebuffer(_glFrameBuffer);
            GL.DeleteRenderbuffer(_glDepthRenderBuffer);
            GL.DeleteTexture(_glSharedTexture);
            _dxSurface.Dispose();
            _dxDevice.Dispose();
        }

        public void UpdateImage() {
            if (_image == null)
                return;

            _image.Lock();
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _dxSurface.NativePointer);
            _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            _image.Unlock();
        }
        
        public void PreRender()
        {
            _wglInterop.WglDXLockObjectsNV(_glHandle, 1, _glDxInteropSharedHandles);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glFrameBuffer);
        }

        public void PostRender()
        {
            GL.Finish(); //TODO: replace with a gl read/fence barrier thing.
            _wglInterop.WglDXUnlockObjectsNV(_glHandle, 1, _glDxInteropSharedHandles);
        }
        
        
    }
}
