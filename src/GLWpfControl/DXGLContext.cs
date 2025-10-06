using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;
using OpenTK.Wpf.Interop;
using Window = System.Windows.Window;

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
        
        /// <summary>The window that provides the OpenGL context. Null if a context was provided externally.</summary>
        public IWindowInfo WindowInfo { get; }

        /// <summary>Does the context own this <see cref="IWindowInfo"/> or not.</summary>
        internal bool OwnWindowInfo { get; }

#if DEBUG
        private readonly static DebugProc DebugProcCallback = Window_DebugProc;
#endif

        public DxGlContext(GLWpfControlSettings settings)
        {
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
                Flags = 0,
                FullScreen_RefreshRateInHz = 0,
                MultiSampleQuality = 0,
                MultiSampleType = MultisampleType.D3DMULTISAMPLE_NONE,
            };

            dxContext.CreateDeviceEx(
                0,
                DeviceType.HAL,
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing |
                CreateFlags.Multithreaded |
                CreateFlags.PureDevice |
                CreateFlags.FpuPreserve,
                ref deviceParameters,
                IntPtr.Zero,
                out DXInterop.IDirect3DDevice9Ex dxDevice);
            DxDevice = dxDevice;

            // if the graphics context is null, we use the shared context.
            if (settings.ContextToUse != null) {
                GraphicsContext = settings.ContextToUse;

                if (settings.WindowInfo == null)
                {
                    throw new InvalidOperationException("When setting ContextToUse you also need to set the WindowInfo property.");
                }
                WindowInfo = settings.WindowInfo;
                OwnWindowInfo = false;
            }
            else {
                if (settings.WindowInfo != null)
                {
                    throw new InvalidOperationException("When ContextToUse is set to null the WindowInfo setting also needs to be set to null.");
                }

                // we're already in a window context, so we can just cheat by creating a new dependency object here rather than passing any around.
                var depObject = new DependencyObject();
                // retrieve window handle/info
                var window = Window.GetWindow(depObject);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                var hwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);

                WindowInfo = Utilities.CreateWindowsWindowInfo(hwndSource.Handle);
                OwnWindowInfo = true;

                var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);

                GraphicsContext = new GraphicsContext(mode, WindowInfo, settings.SharedContext, settings.MajorVersion, settings.MinorVersion, settings.ContextFlags);
                GraphicsContext.LoadAll();
                GraphicsContext.MakeCurrent(WindowInfo);

#if DEBUG
                GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
                GL.Enable(EnableCap.DebugOutput);
                GL.Enable(EnableCap.DebugOutputSynchronous);
#endif
            }

            GLDeviceHandle = Wgl.DXOpenDeviceNV(DxDevice.Handle);
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
            if (OwnWindowInfo)
            {
                WindowInfo?.Dispose();
            }
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
