using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf.Interop;
using WindowState = OpenTK.Windowing.Common.WindowState;

namespace OpenTK.Wpf 
{
    /// This contains the DirectX and OpenGL contexts used in this control.
    internal sealed class DxGlContext : IDisposable 
    {
        /// <summary>The DirectX context. This is basically the root of all DirectX state.</summary>
        public DXInterop.IDirect3D9Ex DxContext { get; }

        /// <summary>The DirectX device handle. This is the graphics card we're running on.</summary> 
        public DXInterop.IDirect3DDevice9Ex DxDevice { get; }

        /// <summary>The OpenGL Context. This is basically the root of all OpenGL state.</summary>
        public IGraphicsContext GraphicsContext { get; }

        /// <summary>An OpenGL handle to the DirectX device. Created and used by the WGL_dx_interop extension.</summary>
        public IntPtr GLDeviceHandle { get; }
        
        /// <summary>The GLFW window that provides the OpenGL context. Null if a context was provided externally.</summary>
        private NativeWindow? GlfwWindow { get; }

#if DEBUG
        private readonly static DebugProc DebugProcCallback = Window_DebugProc;
#endif

        public DxGlContext(GLWpfControlSettings settings)
        {
            // if the graphics context is null, we use the shared context.
            if (settings.ContextToUse != null)
            {
                GraphicsContext = settings.ContextToUse;
            }
            else
            {
                NativeWindowSettings nws = NativeWindowSettings.Default;
                nws.StartFocused = false;
                nws.StartVisible = false;
                nws.NumberOfSamples = 0;
                nws.SharedContext = settings.SharedContext;
                // If we ask GLFW for 1.0, we should get the highest level context available with full compat.
                nws.APIVersion = new Version(settings.MajorVersion, settings.MinorVersion);
                nws.Flags = ContextFlags.Offscreen | settings.ContextFlags;
                // We have to ask for any compat in this case.
                nws.Profile = settings.Profile;
                nws.WindowBorder = WindowBorder.Hidden;
                nws.WindowState = WindowState.Minimized;
                GlfwWindow = new NativeWindow(nws);
                GraphicsContext = GlfwWindow.Context;
                GraphicsContext.MakeCurrent();

                IBindingsContext provider = settings.BindingsContext ?? new GLFWBindingsContext();
                Wgl.LoadBindings(provider);

                bool hasNVDXInterop = false;
                unsafe
                {
                    IntPtr hwnd = GLFW.GetWin32Window(GlfwWindow.WindowPtr);
                    IntPtr hdc = DXInterop.GetDC(hwnd);
                    string exts = Wgl.Arb.GetExtensionsString(hdc);
                    DXInterop.ReleaseDC(hwnd, hdc);

                    foreach (string ext in exts.Split(' '))
                    {
                        if (ext == "WGL_NV_DX_interop" || ext == "NV_DX_interop")
                        {
                            hasNVDXInterop = true;
                            break;
                        }
                    }
                }

                if (hasNVDXInterop == false)
                {
                    Dispose();
                    throw new PlatformNotSupportedException("NV_DX_interop extension is not suppored. This extensions is currently needed for GLWpfControl to work.");
                }

#if DEBUG
                GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);
#endif
            }

            DXInterop.Direct3DCreate9Ex(DXInterop.DefaultSdkVersion, out DXInterop.IDirect3D9Ex dxContext);
            DxContext = dxContext;

            PresentationParameters deviceParameters = new PresentationParameters
            {
                Windowed = 1,
                SwapEffect = SwapEffect.Discard,
                DeviceWindowHandle = IntPtr.Zero,
                PresentationInterval = 0,
                BackBufferFormat = Format.X8R8G8B8,
                BackBufferWidth = 1,
                BackBufferHeight = 1,
                AutoDepthStencilFormat = Format.Unknown,
                BackBufferCount = 1,
                EnableAutoDepthStencil = 0,
                // Add D3DPRESENTFLAG_DISCARD_DEPTHSTENCIL?
                Flags = 0,
                FullScreen_RefreshRateInHz = 0,
                MultiSampleQuality = 0,
                MultiSampleType = MultisampleType.D3DMULTISAMPLE_NONE,
            };

            dxContext.CreateDeviceEx(
                0,
                DeviceType.HAL,
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.PureDevice | CreateFlags.FpuPreserve,
                ref deviceParameters,
                IntPtr.Zero,
                out DXInterop.IDirect3DDevice9Ex dxDevice);
            DxDevice = dxDevice;

            GLDeviceHandle = Wgl.DXOpenDeviceNV(dxDevice.Handle);
            if (GLDeviceHandle == IntPtr.Zero)
            {
                int error = DXInterop.GetLastError();
                Dispose();
                throw new Win32Exception(error);
            }
        }

        public void Dispose()
        {
            int error = 0;
            if (GLDeviceHandle != IntPtr.Zero && Wgl.DXCloseDeviceNV(GLDeviceHandle) == false)
            {
                error = DXInterop.GetLastError();
            }

            GlfwWindow?.Dispose();
            if (DxDevice.Handle != IntPtr.Zero)
            {
                DxDevice.Release();
            }
            if (DxContext.Handle != IntPtr.Zero)
            {
                DxContext.Release();
            }

            if (error != 0)
            {
                throw new Win32Exception(error);
            }
        }

#if DEBUG
        private static void Window_DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
        {
            string message = Marshal.PtrToStringAnsi(messagePtr, length);

            bool showMessage = true;

            switch (source)
            {
                case DebugSource.DebugSourceApplication:
                    showMessage = false;
                    break;
                case DebugSource.DontCare:
                case DebugSource.DebugSourceApi:
                case DebugSource.DebugSourceWindowSystem:
                case DebugSource.DebugSourceShaderCompiler:
                case DebugSource.DebugSourceThirdParty:
                case DebugSource.DebugSourceOther:
                default:
                    showMessage = true;
                    break;
            }

            if (showMessage)
            {
                switch (severity)
                {
                    case DebugSeverity.DontCare:
                        Debug.Print($"[DontCare] {message}");
                        break;
                    case DebugSeverity.DebugSeverityNotification:
                        //Debug.Print($"[Notification] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityHigh:
                        Debug.Print($"[Error] [{source}] {message}");
                        //Debug.Break();
                        break;
                    case DebugSeverity.DebugSeverityMedium:
                        Debug.Print($"[Warning] [{source}] {message}");
                        break;
                    case DebugSeverity.DebugSeverityLow:
                        Debug.Print($"[Info] [{source}] {message}");
                        break;
                    default:
                        Debug.Print($"[default] {message}");
                        break;
                }
            }
        }
#endif
    }
}
