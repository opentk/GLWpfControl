using System;
using System.Runtime.InteropServices;

namespace OpenTK.Wpf
{
    internal sealed class WGLInterop
    {
        [DllImport("OPENGL32.dll", EntryPoint = "wglGetProcAddress", ExactSpelling = true, SetLastError = true)]
        internal extern static IntPtr GetProcAddress(string lpszProc);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXSetResourceShareHandleNV(IntPtr dxObject, IntPtr shareHandle);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr wglDXOpenDeviceNV(IntPtr dxDevice);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXCloseDeviceNV(IntPtr hDevice);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate IntPtr wglDXRegisterObjectNV(IntPtr hDevice, IntPtr dxObject, uint name, uint typeEnum, uint accessEnum);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXUnregisterObjectNV(IntPtr hDevice, IntPtr hObject);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXObjectAccessNV(IntPtr hObject, uint accessEnum);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXLockObjectsNV(IntPtr hDevice, int count, IntPtr[] hObjectsPtr);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate bool wglDXUnlockObjectsNV(IntPtr hDevice, int count, IntPtr[] hObjectsPtr);

        internal const uint WGL_ACCESS_READ_ONLY_NV = 0x0000;
        internal const uint WGL_ACCESS_READ_WRITE_NV = 0x0001;
        internal const uint WGL_ACCESS_WRITE_DISCARD_NV = 0x0002;

        internal wglDXCloseDeviceNV WglDXCloseDeviceNV;
        internal wglDXLockObjectsNV WglDXLockObjectsNV;
        internal wglDXObjectAccessNV WglDXObjectAccessNV;
        internal wglDXOpenDeviceNV WglDXOpenDeviceNV;
        internal wglDXRegisterObjectNV WglDXRegisterObjectNV;
        internal wglDXSetResourceShareHandleNV WglDXSetResourceShareHandleNV;
        internal wglDXUnlockObjectsNV WglDXUnlockObjectsNV;
        internal wglDXUnregisterObjectNV WglDXUnregisterObjectNV;

        internal WGLInterop()
        {
            WglDXCloseDeviceNV = Assign<wglDXCloseDeviceNV>();
            WglDXLockObjectsNV = Assign<wglDXLockObjectsNV>();
            WglDXObjectAccessNV = Assign<wglDXObjectAccessNV>();
            WglDXOpenDeviceNV = Assign<wglDXOpenDeviceNV>();
            WglDXRegisterObjectNV = Assign<wglDXRegisterObjectNV>();
            WglDXSetResourceShareHandleNV = Assign<wglDXSetResourceShareHandleNV>();
            WglDXUnlockObjectsNV = Assign<wglDXUnlockObjectsNV>();
            WglDXUnregisterObjectNV = Assign<wglDXUnregisterObjectNV>();
        }

        private bool IsValid(IntPtr address)
        {
            // See https://www.opengl.org/wiki/Load_OpenGL_Functions
            long a = address.ToInt64();
            bool is_valid = (a < -1) || (a > 3);
            return is_valid;
        }

        private T Assign<T>()
        {
            var name = typeof(T).Name;
            var address = GetProcAddress(name);

            if(address != IntPtr.Zero && IsValid(address))
                return Marshal.GetDelegateForFunctionPointer<T>(address);

            throw new NotSupportedException();
        }
    }
}
