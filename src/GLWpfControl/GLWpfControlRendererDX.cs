using System;
using OpenTK.Graphics.Wgl;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf
{

    /// Renderer that uses DX_Interop for a fast-path.
    internal sealed class GLWpfControlRendererDx {

        private readonly IntPtr _glHandle;

        private readonly IntPtr _dxContextHandle;

        private readonly IntPtr _dxDeviceHandle;

        public IntPtr DeviceHandle => _dxDeviceHandle;

        // private readonly WGLInterop _wglInterop;
        
        private readonly bool _hasSyncFenceAvailable;


        public IntPtr GLHandle => _glHandle;


        public GLWpfControlRendererDx(bool hasSyncFenceAvailable) 
        {
            // _wglInterop = new WGLInterop();
            _hasSyncFenceAvailable = hasSyncFenceAvailable;

            _glHandle = IntPtr.Zero;

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

        public DXGLRenderSurface CreateRenderSurface(int width, int height)
        {
            return new DXGLRenderSurface(this, width, height, _hasSyncFenceAvailable);
        }

    }
}
