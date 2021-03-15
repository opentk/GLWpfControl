using OpenTK.Graphics.Wgl;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK.Wpf.Interop
{
    class D3DDevice : IDisposable
    {

        public IntPtr Handle { get; }

        public IntPtr GLDeviceHandle { get; }

        public int Adapter { get; }

        private D3DDevice(IntPtr deviceHandle, int adapter, IntPtr glDeviceHandle)
        {
            Handle = deviceHandle;
            Adapter = adapter;
            GLDeviceHandle = glDeviceHandle;

            IsDeviceValid();
        }

        public static D3DDevice CreateDevice(IntPtr contextHandle, int adapter)
        {
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
                contextHandle,
                adapter,
                DeviceType.HAL, // use hardware rasterization
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing |
                CreateFlags.Multithreaded |
                CreateFlags.PureDevice,
                ref deviceParameters,
                IntPtr.Zero,
                out var dxDeviceHandle);

            var glDeviceHandle = Wgl.DXOpenDeviceNV(dxDeviceHandle);

            return new D3DDevice(dxDeviceHandle, adapter, glDeviceHandle);
        }

        public bool IsDeviceValid()
        {
            return DXInterop.TestCooperativeLevel(Handle);
        }

        public void Dispose()
        {
            Wgl.DXCloseDeviceNV(Handle);
            DXInterop.Release(Handle);
        }
    }
}
