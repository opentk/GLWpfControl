using System;
using System.Runtime.InteropServices;

namespace OpenTK.Wpf
{
    internal static class DxInterop
    {
        internal const uint D3DSdkVersion = 32;

        [DllImport("d3d9.dll")]
        internal static extern IntPtr Direct3DCreate9(uint sdkVersion);
    }
}
