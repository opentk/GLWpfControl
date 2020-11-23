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
        private IntPtr[] _glDxInteropSharedHandles;
        private readonly IntPtr _glHandle;
        private IntPtr _dxContextHandle;
        private IntPtr _dxDeviceHandle;
        private IntPtr _dxSurfaceHandle;
        private IntPtr _dxSharedHandle;
        private int _glSharedTexture;
        private int _glFrameBuffer;
        private int _glDepthRenderBuffer;

        private readonly D3DImage _image;

        // private readonly WGLInterop _wglInterop;
        
        private readonly bool _hasSyncFenceAvailable;
        private IntPtr _syncFence;

        public int FrameBuffer => _glFrameBuffer;

        private int width = 1;
        private int height = 1;
        private bool recreatingSurfaceNeeded = true;

        public GLWpfControlRendererDx(D3DImage imageControl, bool hasSyncFenceAvailable) 
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
                BackBufferWidth = 1,
                BackBufferHeight = 1,
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

            _glHandle = Wgl.DXOpenDeviceNV(_dxDeviceHandle);
        }

        private void EnsureSurfaceCreated()
        {
            if (!recreatingSurfaceNeeded)
                return; 

            DeleteBuffers();

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

            Wgl.DXSetResourceShareHandleNV(_dxSurfaceHandle, _dxSharedHandle);

            _glFrameBuffer = GL.GenFramebuffer();
            _glSharedTexture = GL.GenTexture();

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

            recreatingSurfaceNeeded = false;
        }

        public void DeleteBuffers() {
            
            if(_dxSurfaceHandle != IntPtr.Zero)
            {
                Wgl.DXUnregisterObjectNV(_glHandle, _glDxInteropSharedHandles[0]);
                _glDxInteropSharedHandles = null;

                _dxSurfaceHandle = IntPtr.Zero;
                _dxSharedHandle = IntPtr.Zero;
            }

            if (_glFrameBuffer != 0)
                GL.DeleteFramebuffer(_glFrameBuffer);

            if(_glDepthRenderBuffer != 0)
                GL.DeleteRenderbuffer(_glDepthRenderBuffer);

            if(_glSharedTexture != 0)
                GL.DeleteTexture(_glSharedTexture);

            _glFrameBuffer = 0;
            _glDepthRenderBuffer = 0;
            _glSharedTexture = 0;
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
            EnsureSurfaceCreated();

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

        public void Resize(int renderWidth, int renderHeight)
        {
            if (renderWidth == width && renderHeight == height)
                return;

            width = renderWidth;
            height = renderHeight;
            recreatingSurfaceNeeded = true;
        }
    }
}
