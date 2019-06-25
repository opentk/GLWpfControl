using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace GLWpfControl {
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    ///     To use this component, call the <see cref="Start(GLWpfControlSettings)"/> method.
    ///     Bind to the <see cref="Render"/> event only after <see cref="Start(GLWpfControlSettings)"/> is called.
    /// </summary>
    public sealed partial class GLWpfControl {
        private const int ResizeUpdateInterval = 100;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private long _resizeStartStamp;
        private TimeSpan _lastFrameStamp;

        private IGraphicsContext _context;
        private IWindowInfo _windowInfo;
        
        private GLWpfControlSettings _settings;
        private GLWpfControlRenderer _renderer;
        private HwndSource _hwnd;

        /// Called whenever rendering should occur.
        public event Action<TimeSpan> Render;
        
        static GLWpfControl() {
            Toolkit.Init(new ToolkitOptions {
                Backend = PlatformBackend.PreferNative
            });
        }

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        /// 
        public int Framebuffer => _renderer?.FrameBuffer ?? 0;

        /// <summary>
        ///     Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettings)"/> must be called.
        /// </summary>
        public GLWpfControl() {
            InitializeComponent();
        }

        /// Starts the control and rendering, using the settings provided.
        public void Start(GLWpfControlSettings settings) {
            _settings = settings; 
            
            IsVisibleChanged += (_, args) => {
                if ((bool) args.NewValue) {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };

            Loaded += (sender, args) => {
                if (_context != null) {
                    return;
                }

                OnLoaded();
            };
            Unloaded += (sender, args) => {
                if (_context == null) {
                    return;
                }

                OnUnloaded();
            };

            SizeChanged += (sender, args) => {
                if (_renderer == null) {
                    return;
                }

                _resizeStartStamp = _stopwatch.ElapsedMilliseconds;
            };
        }

        private void OnCompTargetRender(object sender, EventArgs e) {

            if (_resizeStartStamp != 0) {
                if (_resizeStartStamp + ResizeUpdateInterval > _stopwatch.ElapsedMilliseconds) {
                    return;
                }

                _renderer.DeleteBuffers();
                var width = (int) ActualWidth;
                var height = (int) ActualHeight;
                _renderer = new GLWpfControlRenderer(width, height, Image, _settings.UseHardwareRender, _settings.PixelBufferObjectCount);

                _resizeStartStamp = 0;
            }

            if (!ReferenceEquals(GraphicsContext.CurrentContext, _context)) {
                _context.MakeCurrent(_windowInfo);
            }

            var before = _stopwatch.ElapsedMilliseconds;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _renderer.FrameBuffer);
            TimeSpan deltaTime = _stopwatch.Elapsed - _lastFrameStamp;
            GL.Viewport(0,0,(int) ActualWidth, (int) ActualHeight);
            Render?.Invoke(deltaTime);
            _renderer.UpdateImage();
            _lastFrameStamp = _stopwatch.Elapsed;
        }

        private void OnLoaded() {
            var window = Window.GetWindow(this);
            var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
            _hwnd = new HwndSource(0,0,0,0,0,"GLWpfControl", baseHandle);

            _windowInfo = Utilities.CreateWindowsWindowInfo(_hwnd.Handle);
            InitOpenGL();
        }

        private void OnUnloaded() {
            ReleaseOpenGLResources();
            _windowInfo?.Dispose();
            _hwnd?.Dispose();
        }

        private void InitOpenGL() {
            var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
            _context = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion, _settings.GraphicsContextFlags);
            _context.LoadAll();
            _context.MakeCurrent(_windowInfo);
            var width = (int) ActualWidth;
            var height = (int) ActualHeight;
            _renderer = new GLWpfControlRenderer(width, height, Image, _settings.UseHardwareRender, _settings.PixelBufferObjectCount);
        }

        private void ReleaseOpenGLResources() {
            _renderer.DeleteBuffers();
            _context.Dispose();
            _context = null;
        }
    }
}
