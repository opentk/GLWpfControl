using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace OpenTkControl {
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    /// </summary>
    public sealed partial class GLWpfControl {

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private IGraphicsContext _context;
        private IWindowInfo _windowInfo;
        private GLWpfControlRenderer _renderer;
        private bool _isReadyToRender;
        private GLWpfControlSettings _settings;

        /// Called whenever rendering should occur.
        public event Action Render;
        
        static GLWpfControl() {
            Toolkit.Init(new ToolkitOptions {
                Backend = PlatformBackend.PreferNative
            });
        }

        /// <summary>
        ///     Creates the <see cref="GLWpfControl" />/>
        /// </summary>
        public GLWpfControl() {
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this)) {
                InitOpenGL();
            }
        }

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
                InitOpenGl();
            };
            Unloaded += (sender, args) => {
                if (_context == null) {
                    return;
                }

                OnUnloaded();
                ReleaseOpenGLResources();
            };

            SizeChanged += (sender, args) => {
                if (_renderer == null) {
                    return;
                }
                _renderer.DeleteBuffers();
                
                var width = (int) ActualWidth;
                var height = (int) ActualHeight;
                _renderer = new GLWpfControlRenderer(width, height, Image, _settings.UseSoftwareRender, _settings.PixelBufferObjectCount);
            };
        }

        private void InitOpenGL() {
            
        }

        private void OnCompTargetRender(object sender, EventArgs e) {
            if (_isReadyToRender) {
                // if we're in the slow path, we skip every second frame. 
                _isReadyToRender = false;
                return;
            }

            _isReadyToRender = true;

            if (!ReferenceEquals(GraphicsContext.CurrentContext, _context)) {
                _context.MakeCurrent(_windowInfo);
            }

            var before = _stopwatch.ElapsedMilliseconds;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _renderer.FrameBuffer);
            Render?.Invoke();
            _renderer.UpdateImage();
            var after = _stopwatch.ElapsedMilliseconds;
            var duration = after - before;
            if (duration < 10.0) {
                _isReadyToRender = false;   
            }
        }

        private void OnLoaded() {
            var window = Window.GetWindow(this);
            if (window is null) {
                throw new GraphicsException($"{nameof(GLWpfControl)} must exist within a window.");
            }

            _windowInfo = Utilities.CreateWindowsWindowInfo(new WindowInteropHelper(window).Handle);
        }

        private void OnUnloaded() {
            _windowInfo = null;
            ReleaseOpenGLResources();
        }

        private void InitOpenGl() {
            var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
            _context = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion, _settings.GraphicsContextFlags);
            _context.LoadAll();
            _context.MakeCurrent(_windowInfo);
            var width = (int) ActualWidth;
            var height = (int) ActualHeight;
            _renderer = new GLWpfControlRenderer(width, height, Image, _settings.UseSoftwareRender, _settings.PixelBufferObjectCount);
        }

        private void ReleaseOpenGLResources() {
            _renderer.DeleteBuffers();
            _context.Dispose();
            _context = null;
        }
    }
}
