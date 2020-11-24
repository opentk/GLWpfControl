using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using JetBrains.Annotations;
using OpenTK.Graphics.Wgl;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf.Interop;
using Window = System.Windows.Window;
using WindowState = OpenTK.Windowing.Common.WindowState;

namespace OpenTK.Wpf {
    
    /// This contains the DirectX and OpenGL contexts used in this control.
    internal sealed class DxGlContext : IDisposable {
        
        /// The directX context. This is basically the root of all DirectX state.
        public IntPtr DxContextHandle { get; }
        
        /// The directX device handle. This is the graphics card we're running on.
        public IntPtr DxDeviceHandle { get; }
        
        /// The OpenGL Context. This is basically the root of all OpenGL state.
        public IGraphicsContext GraphicsContext { get; }
        
        /// An OpenGL handle to the DirectX device. Created and used by the WGL_dx_interop extension.
        public IntPtr GlDeviceHandle { get; }

        /// The shared context we (may) want to lazily create/use.
        private static IGraphicsContext _sharedContext;
        private static GLWpfControlSettings _sharedContextSettings;
        /// List of extra resources to dispose along with the shared context.
        private static IDisposable[] _sharedContextResources;
        /// The number of active controls using the shared context.
        private static int _sharedContextReferenceCount;


        public DxGlContext([NotNull] GLWpfControlSettings settings) {
            DXInterop.Direct3DCreate9Ex(DXInterop.DefaultSdkVersion, out var dxContextHandle);
            DxContextHandle = dxContextHandle;

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

            DXInterop.CreateDeviceEx(
                dxContextHandle,
                0,
                DeviceType.HAL, // use hardware rasterization
                IntPtr.Zero,
                CreateFlags.HardwareVertexProcessing |
                CreateFlags.Multithreaded |
                CreateFlags.PureDevice,
                ref deviceParameters,
                IntPtr.Zero,
                out var dxDeviceHandle);
            DxDeviceHandle = dxDeviceHandle;

            // if the graphics context is null, we use the shared context.
            if (settings.ContextToUse != null) {
                GraphicsContext = settings.ContextToUse;
            }
            else {
                GraphicsContext = GetOrCreateSharedOpenGLContext(settings);
            }

            GlDeviceHandle = Wgl.DXOpenDeviceNV(dxDeviceHandle);
        }

        private static IGraphicsContext GetOrCreateSharedOpenGLContext(GLWpfControlSettings settings) {
            if (_sharedContext != null) {
                var isSameContext = GLWpfControlSettings.WouldResultInSameContext(settings, _sharedContextSettings);
                if (!isSameContext) {
                    throw new ArgumentException($"The provided {nameof(GLWpfControlSettings)} would result" +
                                                $"in a different context creation to one previously created. To fix this," +
                                                $" either ensure all of your context settings are identical, or provide an " +
                                                $"external context via the '{nameof(GLWpfControlSettings.ContextToUse)}' field.");
                }
            } 
            
            else {
                var nws = NativeWindowSettings.Default;
                nws.StartFocused = false;
                nws.StartVisible = false;
                nws.NumberOfSamples = 0;
                // if we ask GLFW for 1.0, we should get the highest level context available with full compat.
                nws.APIVersion = new Version(settings.MajorVersion, settings.MinorVersion);
                nws.Flags = ContextFlags.Offscreen | settings.GraphicsContextFlags;
                // we have to ask for any compat in this case.
                nws.Profile = settings.GraphicsProfile;
                // nws.WindowBorder = WindowBorder.Hidden;
                // nws.WindowState = WindowState.Minimized;
                var glfwWindow = new NativeWindow(nws) {IsVisible = false};
                var provider = new GLFWBindingsContext();
                Wgl.LoadBindings(provider);
                // we're already in a window context, so we can just cheat by creating a new dependency object here rather than passing any around.
                var depObject = new DependencyObject();
                // retrieve window handle/info
                var window = Window.GetWindow(depObject);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                var hwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);

                _sharedContext = glfwWindow.Context;
                _sharedContextResources = new IDisposable[] {hwndSource, glfwWindow};
                // GL init
                // var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
                // _commonContext = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion,
                //     _settings.GraphicsContextFlags);
                // _commonContext.LoadAll();
                _sharedContext.MakeCurrent();
            }
            Interlocked.Increment(ref _sharedContextReferenceCount);
            return _sharedContext;
        }

        public void Dispose() {
            // we only dispose of the graphics context if we're using the shared one.
            if (ReferenceEquals(_sharedContext, GraphicsContext)) {
                if (Interlocked.Decrement(ref _sharedContextReferenceCount) == 0) {
                    foreach (var resource in _sharedContextResources) {
                        resource.Dispose();
                    }
                }
            }
        }
    }
}
