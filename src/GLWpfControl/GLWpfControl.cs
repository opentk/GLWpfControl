using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.Wgl;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Window = System.Windows.Window;
using WindowState = OpenTK.Windowing.Common.WindowState;

namespace OpenTK.Wpf
{
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    ///     To use this component, call the <see cref="Start(GLWpfControlSettings)"/> method.
    ///     Bind to the <see cref="Render"/> event only after <see cref="Start(GLWpfControlSettings)"/> is called.
    /// </summary>
    public sealed class GLWpfControl : FrameworkElement
    {

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private TimeSpan _lastFrameStamp;

        // ReSharper disable once NotAccessedField.Local
        private IntPtr _dx9Context;
        private static IGraphicsContext _commonContext;
        private static int _activeControlCount = 0;
        private IGraphicsContext _context;
        private bool _hasSyncFenceAvailable;

        private volatile bool _needsRedraw = true;


        private GLWpfControlSettings _settings;
        private GLWpfControlRendererDx _renderer;
        private HwndSource _hwnd;

        /// Called whenever rendering should occur.
        public event Action<TimeSpan> Render;
        
        /// Called once per frame after render. This does not synchronize with the copy to the screen.
        /// This is only for extremely advanced use, where a non-display out task needs to run.
        /// Examples of these are an async Pixel Buffer Object transfer or Transform Feedback.
        /// If you do not know what these are, do not use this function.
        public event Action AsyncRender;


        /// <summary>
        /// Gets called after the control has finished initializing and is ready to render
        /// </summary>
        public event Action Ready;

        // The image that the control uses to update stuff
        private readonly D3DImage _d3dImage;

        // Transformations and size 
        private Rect _imageRectangle;
        private TranslateTransform _translateTransform;
        private ScaleTransform _flipYTransform;
        private NativeWindow _glfwWindow;

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        public int Framebuffer => _renderer?.FrameBuffer ?? 0;


        /// If this control is rendering continuously.
        /// If this is false, then redrawing will only occur when <see cref="UIElement.InvalidateVisual"/> is called.
        public bool RenderContinuously {
            get => _settings.RenderContinuously;
            set => _settings.RenderContinuously = value;
        }

        /// <summary>
        ///     Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettings)"/> must be called.
        /// </summary>
        public GLWpfControl() {
            _d3dImage = new D3DImage(96, 96);
        }

        /// Starts the control and rendering, using the settings provided.
        public void Start(GLWpfControlSettings settings)
        {
            _settings = settings.Copy();
            _hasSyncFenceAvailable = _settings.MajorVersion >= 4 || (_settings.MajorVersion == 3 && _settings.MinorVersion >= 2);
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
                _renderer = new GLWpfControlRendererDx(width, height, _d3dImage, _hasSyncFenceAvailable);
            }

            _imageRectangle = new Rect(0, 0, RenderSize.Width, RenderSize.Height);
            _translateTransform = new TranslateTransform(0, RenderSize.Height);
            _flipYTransform = new ScaleTransform(1, -1);

            Ready?.Invoke();
        }

        private void InitOpenGLContext() {
            if (_commonContext == null) {
                var nws = NativeWindowSettings.Default;
                nws.StartFocused = false;
                nws.StartVisible = false;
                nws.NumberOfSamples = 0;
                nws.APIVersion = new Version(_settings.MajorVersion,_settings.MinorVersion);
                nws.Flags = ContextFlags.Offscreen;
                nws.Profile = _settings.GraphicsProfile;
                nws.WindowBorder = WindowBorder.Hidden;
                nws.WindowState = WindowState.Minimized;
                _glfwWindow = new NativeWindow(nws) {IsVisible = false};
                var provider = new GLFWBindingsContext();
                Wgl.LoadBindings(provider);
                // retrieve window handle/info
                var window = Window.GetWindow(this);
                var baseHandle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
                _hwnd = new HwndSource(0, 0, 0, 0, 0, "GLWpfControl", baseHandle);

                _commonContext = _glfwWindow.Context;
                // GL init
                // var mode = new GraphicsMode(ColorFormat.Empty, 0, 0, 0, 0, 0, false);
                // _commonContext = new GraphicsContext(mode, _windowInfo, _settings.MajorVersion, _settings.MinorVersion,
                //     _settings.GraphicsContextFlags);
                // _commonContext.LoadAll();
                _commonContext.MakeCurrent();
            }
            _context = _commonContext;
            Interlocked.Increment(ref _activeControlCount);
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (_context == null)
            {
                return;
            }

            ReleaseOpenGLResources();
            _glfwWindow?.Dispose();
            _hwnd?.Dispose();
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            if (_needsRedraw) {
                InvalidateVisual();
                _needsRedraw = RenderContinuously;
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            var curFrameStamp = _stopwatch.Elapsed;
            var deltaT = curFrameStamp - _lastFrameStamp;
            _lastFrameStamp = curFrameStamp;
            if (_renderer != null) {
                _renderer.PreRender();
                Render?.Invoke(deltaT);
                _renderer.PostRender();
                AsyncRender?.Invoke();
                _renderer.UpdateImage();
                
                // Transforms are applied in reverse order
                drawingContext.PushTransform(_translateTransform);              // Apply translation to the image on the Y axis by the height. This assures that in the next step, where we apply a negative scale the image is still inside of the window
                drawingContext.PushTransform(_flipYTransform);                  // Apply a scale where the Y axis is -1. This will rotate the image by 180 deg

                drawingContext.DrawImage(_d3dImage, _imageRectangle);            // Draw the image source 

                drawingContext.Pop();                                           // Remove the scale transform
                drawingContext.Pop();                                           // Remove the translation transform
                
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
                _translateTransform.Y = info.NewSize.Height;
                _renderer.DeleteBuffers();
                _renderer = new GLWpfControlRendererDx((int) _imageRectangle.Width, (int) _imageRectangle.Height, _d3dImage, _hasSyncFenceAvailable);
                InvalidateVisual();
            }
            base.OnRenderSizeChanged(info);
        }

        private void ReleaseOpenGLResources()
        {
            _renderer?.DeleteBuffers();
            if (!_settings.IsUsingExternalContext) {
                _context = null;
                var newCount = Interlocked.Decrement(ref _activeControlCount);
                if (newCount == 0) {
                    _glfwWindow?.Dispose();
                }
            }
        }
    }
}
