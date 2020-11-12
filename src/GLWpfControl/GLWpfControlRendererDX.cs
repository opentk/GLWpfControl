using System;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using SharpDX.Direct3D9;

namespace OpenTK.Wpf {
    
    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRendererDx {
        /// arrays used to lock the shared GL object handles.
        private readonly IntPtr[] _glDxInteropSharedHandles;
        private readonly IntPtr _glHandle;
        private readonly IntPtr _dxSharedhandle;
        private readonly int _glSharedTexture;
        private readonly int _glFrameBuffer;
        private readonly int _glDepthRenderBuffer;
        private readonly Device _dxDevice;
        private readonly Surface _dxSurface;
        private readonly D3DImage _image;

        // private readonly WGLInterop _wglInterop;
        
        private readonly bool _hasSyncFenceAvailable;
        private IntPtr _syncFence;

        public int FrameBuffer => _glFrameBuffer;

        public GLWpfControlRendererDx(int width, int height, D3DImage imageControl, bool hasSyncFenceAvailable) {
            // _wglInterop = new WGLInterop();

            _hasSyncFenceAvailable = hasSyncFenceAvailable;

            _image = imageControl;
            _glHandle = IntPtr.Zero;
            _dxSharedhandle = IntPtr.Zero;
            
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
                    BackBufferFormat = Format.A8R8G8B8,
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

            _glHandle = Wgl.DXOpenDeviceNV(_dxDevice.NativePointer);
            Wgl.DXSetResourceShareHandleNV(_dxSurface.NativePointer, _dxSharedhandle);

            var genHandle = Wgl.DXRegisterObjectNV(
                _glHandle,
                _dxSurface.NativePointer,
                (uint)_glSharedTexture,
                (uint)TextureTarget.Texture2D,
                WGL_NV_DX_interop.AccessReadWrite);
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

            // // wait 10 seconds for the sync to complete.
            // if (_hasSyncFenceAvailable) {
            //     // timeout is in nanoseconds
            //     var syncRes = GL.ClientWaitSync(_syncFence, ClientWaitSyncFlags.None, 10_000_000);
            //     if (syncRes != WaitSyncStatus.ConditionSatisfied) {
            //         throw new TimeoutException("Synchronization failed because the sync could not be completed in a reasonable time.");
            //     }
            // }
            // else {
            //     GL.Flush();
            // }
            //
            GL.Flush();
            Wgl.DXUnlockObjectsNV(_glHandle, 1, _glDxInteropSharedHandles);
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _dxSurface.NativePointer);
            _image.AddDirtyRect(new Int32Rect(0, 0, _image.PixelWidth, _image.PixelHeight));
            _image.Unlock();
        }
        
        public void PreRender()
        {
            _image.Lock();
            Wgl.DXLockObjectsNV(_glHandle, 1, _glDxInteropSharedHandles);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glFrameBuffer);
            GL.Viewport(0, 0, _image.PixelWidth, _image.PixelHeight);
        }

        public void PostRender()
        {
            // unbind, flush and finish
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            // if (_hasSyncFenceAvailable) {
            //     _syncFence = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            // }
        }
    }
}
