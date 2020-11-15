using System;
using System.Runtime.InteropServices;

namespace OpenTK.Wpf.Interop
{
    internal static class DXInterop
    {
        public const uint DefaultSdkVersion = 32;
        private const int CreateDeviceEx_Offset = 20;
        private const int CreateRenderTarget_Offset = 28;

        private delegate int NativeCreateDeviceEx(IntPtr contextHandle, int adapter, DeviceType deviceType, IntPtr focusWindowHandle, CreateFlags behaviorFlags, ref PresentationParameters presentationParameters, IntPtr fullscreenDisplayMode, out IntPtr deviceHandle);
        private delegate int NativeCreateRenderTarget(IntPtr deviceHandle, int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool lockable, out IntPtr surfaceHandle, ref IntPtr sharedHandle);

        [DllImport("d3d9.dll")]
        public static extern int Direct3DCreate9Ex(uint SdkVersion, out IntPtr ctx);

        public static int CreateDeviceEx(IntPtr contextHandle, int adapter, DeviceType deviceType, IntPtr focusWindowHandle, CreateFlags behaviorFlags, ref PresentationParameters presentationParameters, IntPtr fullscreenDisplayMode, out IntPtr deviceHandle)
        {
            IntPtr vTable = Marshal.ReadIntPtr(contextHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, CreateDeviceEx_Offset * IntPtr.Size);
            NativeCreateDeviceEx method = Marshal.GetDelegateForFunctionPointer<NativeCreateDeviceEx>(functionPointer);
            return method(contextHandle, adapter, deviceType, focusWindowHandle, behaviorFlags, ref presentationParameters, fullscreenDisplayMode, out deviceHandle);
        }

        public static int CreateRenderTarget(IntPtr deviceHandle, int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool lockable, out IntPtr surfaceHandle, ref IntPtr sharedHandle)
        {
            IntPtr vTable = Marshal.ReadIntPtr(deviceHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, CreateRenderTarget_Offset * IntPtr.Size);
            NativeCreateRenderTarget method = Marshal.GetDelegateForFunctionPointer<NativeCreateRenderTarget>(functionPointer);
            return method(deviceHandle, width, height, format, multisample, multisampleQuality, lockable, out surfaceHandle, ref sharedHandle);
        }

    }

}
