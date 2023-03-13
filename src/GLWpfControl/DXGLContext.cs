using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using OpenTK.Graphics.Wgl;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf.Interop;
using Window = System.Windows.Window;
using WindowState = OpenTK.Windowing.Common.WindowState;

namespace OpenTK.Wpf 
{
    /// This contains the DirectX and OpenGL contexts used in this control.
    internal sealed class DxGlContext : IDisposable 
    {
        /// <summary>The directX context. This is basically the root of all DirectX state.</summary>
        public DXInterop.IDirect3D9Ex DxContext { get; }

        /// <summary>The directX device handle. This is the graphics card we're running on.</summary> 
        public DXInterop.IDirect3DDevice9Ex DxDevice { get; }

        /// <summary>The OpenGL Context. This is basically the root of all OpenGL state.</summary>
        public IGraphicsContext GraphicsContext { get; }

        /// <summary>An OpenGL handle to the DirectX device. Created and used by the WGL_dx_interop extension.</summary>
        public IntPtr GLDeviceHandle { get; }

        /// <summary>The shared context we (may) want to lazily create/use.</summary>
        private static IGraphicsContext _sharedContext;
        private static GLWpfControlSettings _sharedContextSettings;
        
        private static NativeWindow GlfwWindow;
        private static HwndSource HwndSource;

        /// <summary>The number of active controls using the shared context.</summary>
        private static int _sharedContextReferenceCount;

        public DxGlContext(GLWpfControlSettings settings)
        {
            DXInterop.CheckHResult(DXInterop.Direct3DCreate9Ex(DXInterop.DefaultSdkVersion, out DXInterop.IDirect3D9Ex dxContext));
            DxContext = dxContext;

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

            dxContext.CreateDeviceEx(
                0,
                DeviceType.HAL,
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing | CreateFlags.Multithreaded | CreateFlags.PureDevice,
                ref deviceParameters,
                IntPtr.Zero,
                out DXInterop.IDirect3DDevice9Ex dxDevice);

            DxDevice = dxDevice;

            // if the graphics context is null, we use the shared context.
            if (settings.ContextToUse != null) {
                GraphicsContext = settings.ContextToUse;
            }
            else {
                GraphicsContext = GetOrCreateSharedOpenGLContext(settings);
            }

            GLDeviceHandle = Wgl.DXOpenDeviceNV(dxDevice.Handle);
        }

        private static IGraphicsContext GetOrCreateSharedOpenGLContext(GLWpfControlSettings settings)
        {
            if (_sharedContext != null)
            {
                var isSameContext = GLWpfControlSettings.WouldResultInSameContext(settings, _sharedContextSettings);
                if (!isSameContext)
                {
                    throw new ArgumentException($"The provided {nameof(GLWpfControlSettings)} would result" +
                                                $"in a different context creation to one previously created. To fix this," +
                                                $" either ensure all of your context settings are identical, or provide an " +
                                                $"external context via the '{nameof(GLWpfControlSettings.ContextToUse)}' field.");
                }
            }
            else
            {
                var nws = NativeWindowSettings.Default;
                nws.StartFocused = false;
                nws.StartVisible = false;
                nws.NumberOfSamples = 0;
                // if we ask GLFW for 1.0, we should get the highest level context available with full compat.
                nws.APIVersion = new Version(settings.MajorVersion, settings.MinorVersion);
                nws.Flags = ContextFlags.Offscreen | settings.GraphicsContextFlags;
                // we have to ask for any compat in this case.
                nws.Profile = settings.GraphicsProfile;
                nws.WindowBorder = WindowBorder.Hidden;
                nws.WindowState = WindowState.Minimized;
                GlfwWindow = new NativeWindow(nws);
                var provider = settings.BindingsContext ?? new GLFWBindingsContext();
                Wgl.LoadBindings(provider);
                // we're already in a window context, so we can just cheat by creating a new dependency object here rather than passing any around.
                var depObject = new DependencyObject();
                // retrieve window handle/info
                var window = Window.GetWindow(depObject);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                HwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);

                _sharedContext = GlfwWindow.Context;
                _sharedContextSettings = settings;
                _sharedContext.MakeCurrent();
            }

            // FIXME:
            // This has a race condition where we think we still have the
            // shared context available but it's been deleted when we get here.
            Interlocked.Increment(ref _sharedContextReferenceCount);
            return _sharedContext;
        }

        public void Dispose()
        {
            // we only dispose of the graphics context if we're using the shared one.
            if (ReferenceEquals(_sharedContext, GraphicsContext))
            {
                if (Interlocked.Decrement(ref _sharedContextReferenceCount) == 0)
                {
                    GlfwWindow.Dispose();
                    HwndSource.Dispose();
                }
            }
        }
    }
}
