using System;
using System.Collections.Generic;
using System.Linq;
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

        public D3DDevice Device { get; private set; }

        private readonly uint _adapterCount;

        private readonly List<D3DDevice> _devices;
        
        /// The OpenGL Context. This is basically the root of all OpenGL state.
        public IGraphicsContext GraphicsContext { get; }
        
        /// The shared context we (may) want to lazily create/use.
        private static IGraphicsContext _sharedContext;
        private static GLWpfControlSettings _sharedContextSettings;
        /// List of extra resources to dispose along with the shared context.
        private static IDisposable[] _sharedContextResources;
        /// The number of active controls using the shared context.
        private static int _sharedContextReferenceCount;


        public DxGlContext([NotNull] GLWpfControlSettings settings) {
            
            // if the graphics context is null, we use the shared context.
            if (settings.ContextToUse != null)
            {
                GraphicsContext = settings.ContextToUse;
            }
            else
            {
                GraphicsContext = GetOrCreateSharedOpenGLContext(settings);
            }

            DXInterop.Direct3DCreate9Ex(DXInterop.DefaultSdkVersion, out var dxContextHandle);
            DxContextHandle = dxContextHandle;

            _adapterCount = DXInterop.GetAdapterCount(dxContextHandle);

            _devices = Enumerable.Range(0, (int)_adapterCount)
                .Select(i => D3DDevice.CreateDevice(dxContextHandle, i))
                .ToList();

            Device = _devices.First();
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
                nws.WindowBorder = WindowBorder.Hidden;
                nws.WindowState = WindowState.Minimized;
                var glfwWindow = new NativeWindow(nws);
                var provider = new GLFWBindingsContext();
                Wgl.LoadBindings(provider);
                // we're already in a window context, so we can just cheat by creating a new dependency object here rather than passing any around.
                var depObject = new DependencyObject();
                // retrieve window handle/info
                var window = Window.GetWindow(depObject);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                var hwndSource = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);

                _sharedContext = glfwWindow.Context;
                _sharedContextSettings = settings;
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

        public void SetDeviceFromMonitor(IntPtr monitor)
        {
            // Keep the default adapter device if monitor is null.
            // In this (worst and unlikely) case, nothing will be drawn, but it won't lead in 
            // NullReference exceptions for null devices.
            if(monitor == IntPtr.Zero)
            {
                Device = _devices[0];
                return;
            }

            D3DDevice dev = null;

            for (int i = 0; i < _adapterCount; i++)
            {
                var d3dMonitor = DXInterop.GetAdapterMonitor(DxContextHandle, (uint)i);
                if (d3dMonitor == monitor)
                {
                    dev = _devices[i];
                    break;
                }
            }

            if (dev == null)
            {
                // This can happen when the control runs on a laptop with an external display in duplicated mode
                // and the user closes the lid (see issue #39).
                // In this particular case, only recreating the context will be useful.

                throw new AdapterMonitorNotFoundException("Adapter was not found for given monitor handle");
            }

            Device = dev;
        }

        public void Dispose() {

            Device = null;
            foreach(var dev in _devices)
            {
                dev.Dispose();
            }

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
