using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace OpenTK.Wpf
{
    class DXGLRenderSurface : IDisposable
    {

        private readonly GLWpfControlRendererDx _device;

        private readonly bool _hasSyncFenceAvailable;

        private IntPtr _syncFence;

        private int _glFrameBuffer;

        private int _glDepthRenderBuffer;

        private int _glSharedTexture;

        private IntPtr _dxSurfaceHandle;

        private IntPtr _dxSharedHandle;

        private IntPtr[] _glDxInteropSharedHandles;

        public int Width { get; }

        public int Height { get; }

        public int FrameBuffer
        {
            get
            {
                EnsureSurfaceCreated();
                return _glFrameBuffer;
            }
        }

        public DXGLRenderSurface(GLWpfControlRendererDx device, int width, int height, bool hasSyncFenceAvailable)
        {
            Width = width;
            Height = height;

            _device = device;
            _hasSyncFenceAvailable = hasSyncFenceAvailable;
        }

        public void Render(D3DImage image, Action render, Action asyncRender)
        {
            if (disposedValue)
                throw new ObjectDisposedException("DXGLRenderSurface");

            EnsureSurfaceCreated();

            image.Lock();
            Wgl.DXLockObjectsNV(_device.GLHandle, 1, _glDxInteropSharedHandles);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _glFrameBuffer);
            GL.Viewport(0, 0, image.PixelWidth, image.PixelHeight);

            render?.Invoke();

            // post-render
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            asyncRender?.Invoke();
            SyncOperations();

            // update image
            Wgl.DXUnlockObjectsNV(_device.GLHandle, 1, _glDxInteropSharedHandles);
            image.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _dxSurfaceHandle);
            image.AddDirtyRect(new Int32Rect(0, 0, image.PixelWidth, image.PixelHeight));
            image.Unlock();

        }

        private void EnsureSurfaceCreated()
        {
            if (disposedValue)
                throw new ObjectDisposedException("DXGLRenderSurface");

            if (_glFrameBuffer != 0)
                return;

            DXInterop.CreateRenderTarget(
                _device.DeviceHandle,
                Width,
                Height,
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
                _device.GLHandle,
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
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Width, Height);
            GL.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer,
                _glDepthRenderBuffer);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private void SyncOperations()
        {
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
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (_dxSurfaceHandle != IntPtr.Zero)
                {
                    Wgl.DXUnregisterObjectNV(_device.GLHandle, _glDxInteropSharedHandles[0]);
                }

                if (_glFrameBuffer != 0)
                {
                    GL.DeleteFramebuffer(_glFrameBuffer);
                }
                    
                if (_glDepthRenderBuffer != 0)
                {
                    GL.DeleteRenderbuffer(_glDepthRenderBuffer);
                }
                    
                if (_glSharedTexture != 0)
                {
                    GL.DeleteTexture(_glSharedTexture);
                }

                disposedValue = true;
            }
        }

        ~DXGLRenderSurface()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
