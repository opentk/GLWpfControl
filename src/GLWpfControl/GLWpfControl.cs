using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTK.Wpf
{
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    ///     To use this component, call the <see cref="Start(GLWpfControlSettings)"/> method.
    ///     Bind to the <see cref="Render"/> event only after <see cref="Start(GLWpfControlSettings)"/> is called.
    /// </summary>
    public sealed class GLWpfControl : FrameworkElement
    {
        static GLWpfControl()
        {
            Toolkit.Init(new ToolkitOptions
            {
                Backend = PlatformBackend.PreferNative
            });
        }

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private TimeSpan _lastFrameStamp;

        // ReSharper disable once NotAccessedField.Local
        private IntPtr _dx9Context;
        private IGraphicsContext _context;
        private IWindowInfo _windowInfo;

        private GLWpfControlSettings _settings;
        private GLWpfControlRendererDx _renderer;
        private HwndSource _hwnd;

        /// Called whenever rendering should occur.
        public event Action<TimeSpan> Render;

        /// <summary>
        /// Gets called after the control has finished initializing and is ready to render
        /// </summary>
        public event Action Ready;

        // The image that the control uses to update stuff
        private readonly D3DImage _dxImage;

        // Transformations and size 
        private Rect _imageRectangle;

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        public int Framebuffer => _renderer?.FrameBuffer ?? 0;

        /// <summary>
        ///     Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettings)"/> must be called.
        /// </summary>
        public GLWpfControl() {
            _dxImage = new D3DImage(72, 72);
        }

        /// Starts the control and rendering, using the settings provided.
        public void Start(GLWpfControlSettings settings)
        {
            _settings = settings;
            IsVisibleChanged += (_, args) => {
                if ((bool) args.NewValue) {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (_context != null) {
                return;
            }
            
            _dx9Context = DxInterop.Direct3DCreate9(DxInterop.D3DSdkVersion);
            
            if (_settings.ContextToUse == null)
            {
                InitOpenGLContext();
            }
            else {
                _context = _settings.ContextToUse;
            }

            if (_renderer == null) {
                var width = (int)RenderSize.Width;
                var height = (int)RenderSize.Height;
                _renderer = new GLWpfControlRendererDx(width, height, _dxImage);
            }

            _imageRectangle = new Rect(0, 0, RenderSize.Width, RenderSize.Height);

            Ready?.Invoke();
        }

        private void InitOpenGLContext() {
            
            // retrieve window handle/info
            var window = Window.GetWindow(this);
            var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
            _hwnd = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);
            _windowInfo = Utilities.CreateWindowsWindowInfo(_hwnd.Handle);
            
            // GL init
            var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
            _context = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion,
                _settings.GraphicsContextFlags);
            _context.LoadAll();
            _context.MakeCurrent(_windowInfo);
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (_context == null)
            {
                return;
            }

            ReleaseOpenGLResources();
            _windowInfo?.Dispose();
            _hwnd?.Dispose();
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext) {
            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;
            if (_renderer != null) {
                _renderer.PreRender();
                Render?.Invoke(deltaT);
                _renderer.PostRender();
                _renderer.UpdateImage();
                
                drawingContext.DrawImage(_dxImage, _imageRectangle);
            }

            base.OnRender(drawingContext);
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            if (_renderer == null)
            {
                return;
            }
            if (info.WidthChanged || info.HeightChanged)
            {
                _imageRectangle.Width = info.NewSize.Width;
                _imageRectangle.Height = info.NewSize.Height;
                InvalidateVisual();
            }
            base.OnRenderSizeChanged(info);
        }

        private void ReleaseOpenGLResources()
        {
            _renderer?.DeleteBuffers();
            if (!_settings.IsUsingExternalContext) {
                _context?.Dispose();
                _context = null;
            }
        }
    }
}
