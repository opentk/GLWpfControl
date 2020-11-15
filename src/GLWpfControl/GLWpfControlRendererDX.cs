using System;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf {
    
    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRendererDx {
        /// arrays used to lock the shared GL object handles.
        private readonly IntPtr[] _glDxInteropSharedHandles;
        private readonly IntPtr _glHandle;
        private IntPtr _dxContextHandle;
        private IntPtr _dxDeviceHandle;
        private IntPtr _dxSurfaceHandle;
        private IntPtr _dxSharedHandle;
        private readonly int _glSharedTexture;
        private readonly int _glFrameBuffer;
        private readonly int _glDepthRenderBuffer;

        private readonly D3DImage _image;

        // private readonly WGLInterop _wglInterop;
        
        private readonly bool _hasSyncFenceAvailable;
        private IntPtr _syncFence;

        public int FrameBuffer => _glFrameBuffer;

        public GLWpfControlRendererDx(int width, int height, D3DImage imageControl, bool hasSyncFenceAvailable) 
        {
            // _wglInterop = new WGLInterop();
            _hasSyncFenceAvailable = hasSyncFenceAvailable;

            _image = imageControl;
            _glHandle = IntPtr.Zero;
            _dxSharedHandle = IntPtr.Zero;

            DXInterop.Direct3DCreate9Ex(DXInterop.DefaultSdkVersion, out _dxContextHandle);

            var deviceParameters = new PresentationParameters
            {
                Windowed = 1,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = IntPtr.Zero,
                PresentationInterval = 0,
                BackBufferFormat = Format.X8R8G8B8, // this is like A8 R8 G8 B8, but avoids issues with Gamma correction being applied twice. 
                BackBufferWidth = width,
                BackBufferHeight = height,
                AutoDepthStencilFormat = Format.Unknown,
                BackBufferCount = 1,
                EnableAutoDepthStencil = 0,
                Flags = 0,
                FullScreen_RefreshRateInHz = 0,
                MultiSampleQuality = 0,
                MultiSampleType = MultisampleType.None
            };

            DXInterop.CreateDeviceEx(
                _dxContextHandle,
                0,
                DeviceType.HAL, // use hardware rasterization
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing |
                CreateFlags.Multithreaded |
                CreateFlags.PureDevice,
                ref deviceParameters,
                IntPtr.Zero,
                out _dxDeviceHandle);
            
            DXInterop.CreateRenderTarget(
                _dxDeviceHandle,
                width,
                height,
                Format.X8R8G8B8,// this is like A8 R8 G8 B8, but avoids issues with Gamma correction being applied twice.
                MultisampleType.None,
                0,
                false,
                out _dxSurfaceHandle,
                ref _dxSharedHandle);

            _glFrameBuffer = GL.GenFramebuffer();
            _glSharedTexture = GL.GenTexture();

            _glHandle = Wgl.DXOpenDeviceNV(_dxDeviceHandle);
            Wgl.DXSetResourceShareHandleNV(_dxSurfaceHandle, _dxSharedHandle);

            var genHandle = Wgl.DXRegisterObjectNV(
                _glHandle,
                _dxSurfaceHandle,
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

            //TODO: Release unmanaged resources

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
            _image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _dxSurfaceHandle);
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
