using System;

namespace OpenTK.Wpf.Interop
{
    [Flags]
    internal enum CreateFlags
    {
        FpuPreserve = 2,
        Multithreaded = 4,
        PureDevice = 16,
        HardwareVertexProcessing = 64,

    }

    internal enum DeviceType
    {
        /// <summary>
        /// Hardware rasterization. Shading is done with software, hardware, or mixed transform and lighting.
        /// </summary>
        HAL = 1,

    }

    internal enum Format
    {
        Unknown = 0,
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        A8R8G8B8 = 21,
        /// <summary>
        /// 32-bit RGB pixel format, where 8 bits are reserved for each color.
        /// </summary>
        X8R8G8B8 = 22, 

    }

    internal enum MultisampleType
    {
        None = 0,

    }

    internal enum SwapEffect
    {
        Discard = 1,

    }

}
