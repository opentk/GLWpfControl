using System;

namespace OpenTK.Wpf.Interop
{
    [Flags]
    internal enum CreateFlags
    {
        Multithreaded = 4,
        PureDevice = 16,
        HardwareVertexProcessing = 64,

    }

    internal enum DeviceType
    {
        HAL = 1,

    }

    internal enum Format
    {
        Unknown = 0,
        A8R8G8B8 = 21,
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
