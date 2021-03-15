using System;
using System.Runtime.InteropServices;

namespace OpenTK.Wpf.Interop
{
    internal static class DXInterop
    {
        public const uint DefaultSdkVersion = 32;
        private const int CreateDeviceEx_Offset = 20;
        private const int GetAdapterCount_Offset = 4;
        private const int CreateRenderTarget_Offset = 28;
        private const int TestCooperativeLevel_Offset = 3;
        private const int Release_Offset = 2;
        private const int GetAdapterMonitor_Offset = 15;
        private const int CheckDeviceState_Offset = 128;

        private delegate int NativeCreateDeviceEx(IntPtr contextHandle, int adapter, DeviceType deviceType, IntPtr focusWindowHandle, CreateFlags behaviorFlags, ref PresentationParameters presentationParameters, IntPtr fullscreenDisplayMode, out IntPtr deviceHandle);
        private delegate uint NativeGetAdapterCount(IntPtr contextHandle);
        private delegate int NativeCreateRenderTarget(IntPtr deviceHandle, int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool lockable, out IntPtr surfaceHandle, ref IntPtr sharedHandle);
        private delegate uint NativeRelease(IntPtr resourceHandle);
        private delegate uint NativeTestCooperativeLevel(IntPtr deviceHandle);
        private delegate IntPtr NativeGetAdapterMonitor(IntPtr contextHandle, uint index);
        private delegate int NativeCheckDeviceState(IntPtr deviceHandle, IntPtr hwndDestinationWindow);

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

        public static uint Release(IntPtr resourceHandle)
        {
            IntPtr vTable = Marshal.ReadIntPtr(resourceHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, Release_Offset * IntPtr.Size);
            NativeRelease method = Marshal.GetDelegateForFunctionPointer<NativeRelease>(functionPointer);
            return method(resourceHandle);
        }

        public static uint GetAdapterCount(IntPtr contextHandle)
        {
            IntPtr vTable = Marshal.ReadIntPtr(contextHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, GetAdapterCount_Offset * IntPtr.Size);
            NativeGetAdapterCount method = Marshal.GetDelegateForFunctionPointer<NativeGetAdapterCount>(functionPointer);
            return method(contextHandle);
        }

        public static bool TestCooperativeLevel(IntPtr deviceHandle)
        {
            IntPtr vTable = Marshal.ReadIntPtr(deviceHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, TestCooperativeLevel_Offset * IntPtr.Size);
            NativeTestCooperativeLevel method = Marshal.GetDelegateForFunctionPointer<NativeTestCooperativeLevel>(functionPointer);
            var result = method(deviceHandle);

            return result == 0;
        }

        public static IntPtr GetAdapterMonitor(IntPtr contextHandle, uint index)
        {
            IntPtr vTable = Marshal.ReadIntPtr(contextHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, GetAdapterMonitor_Offset * IntPtr.Size);
            NativeGetAdapterMonitor method = Marshal.GetDelegateForFunctionPointer<NativeGetAdapterMonitor>(functionPointer);
            return method(contextHandle, index);
        }

        public static int CheckDeviceState(IntPtr deviceHandle, IntPtr hwndWindow)
        {
            IntPtr vTable = Marshal.ReadIntPtr(deviceHandle, 0);
            IntPtr functionPointer = Marshal.ReadIntPtr(vTable, CheckDeviceState_Offset * IntPtr.Size);
            NativeCheckDeviceState method = Marshal.GetDelegateForFunctionPointer<NativeCheckDeviceState>(functionPointer);
            return method(deviceHandle, hwndWindow);
        }

    }

}
