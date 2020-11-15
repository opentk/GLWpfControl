using System;
using System.Runtime.InteropServices;

namespace OpenTK.Wpf.Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PresentationParameters
    {
        public int BackBufferWidth;
        public int BackBufferHeight;
        public Format BackBufferFormat;
        public uint BackBufferCount;
        public MultisampleType MultiSampleType;
        public int MultiSampleQuality;
        public SwapEffect SwapEffect;
        public IntPtr DeviceWindowHandle;
        public int Windowed;
        public int EnableAutoDepthStencil;
        public Format AutoDepthStencilFormat;
        public int Flags;
        public int FullScreen_RefreshRateInHz;
        public int PresentationInterval;
    }
}
