using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using OpenTK.Wpf.Interop;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTK.Wpf
{
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    ///     To use this component, call the <see cref="Start(GLWpfControlSettings)"/> method.
    ///     Bind to the <see cref="Render"/> event only after <see cref="Start(GLWpfControlSettings)"/> is called.
    ///
    ///     Please do not extend this class. It has no support for that.
    /// </summary>
    public class GLWpfControl : FrameworkElement, IDisposable
    {
        static GLWpfControl()
        {
            // Default to Focusable=true.
            FocusableProperty.OverrideMetadata(typeof(GLWpfControl), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Called whenever rendering should occur.
        /// GLWpfControl makes sure that it's OpenGL context is current when this event happens.
        /// </summary>
        public event Action<TimeSpan> Render;

        /// <summary>
        /// Called once per frame after render. This does not synchronize with the copy to the screen.
        /// This is only for extremely advanced use, where a non-display out task needs to run.
        /// Examples of these are an async Pixel Buffer Object transfer or Transform Feedback.
        /// If you do not know what these are, do not use this function.
        /// 
        /// GLWpfControl makes sure that it's OpenGL context is current when this event happens.
        /// </summary>
        [Obsolete("There is no difference between Render and AsyncRender. Use Render.")]
        public event Action AsyncRender;

        /// <summary>
        /// Gets called after the control has finished initializing and is ready to render
        /// </summary>
        public event Action Ready;

        /// <summary>
        /// Represents the dependency property for <see cref="Settings"/>.
        /// </summary>
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(GLWpfControlSettings), typeof(GLWpfControl));

        private GLWpfControlRenderer _renderer;

        /// <summary>
        /// Indicates whether the <see cref="Start"/> function has been invoked.
        /// </summary>
        private bool _isStarted;

        /// <summary>
        /// Gets or sets the settings used when initializing the control.
        /// </summary>
        /// <value>
        /// The settings used when initializing the control.
        /// </value>
        public GLWpfControlSettings Settings
        {
            get { return (GLWpfControlSettings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        /// If no framebuffer is available (because this control is not visible, etc etc, then it should be 0).
        /// </summary>
        public int Framebuffer => _renderer?.GLFramebufferHandle ?? 0;

        /// <summary>
        /// If this control is rendering continuously.
        /// If this is false, then redrawing will only occur when <see cref="UIElement.InvalidateVisual"/> is called.
        /// </summary>
        public bool RenderContinuously {
            get => Settings.RenderContinuously;
            set => Settings.RenderContinuously = value;
        }

        /// <summary>
        /// Pixel width of the underlying OpenGL framebuffer.
        /// It could differ from UIElement.RenderSize if UseDeviceDpi setting is set.
        /// To be used for operations related to OpenGL viewport calls (glViewport, glScissor, ...).
        /// </summary>
        public int FrameBufferWidth => _renderer?.Width ?? 0;

        /// <summary>
        /// Pixel height of the underlying OpenGL framebuffer.
        /// It could differ from UIElement.RenderSize if UseDeviceDpi setting is set.
        /// To be used for operations related to OpenGL viewport calls (glViewport, glScissor, ...).
        /// </summary>
        public int FrameBufferHeight => _renderer?.Height ?? 0;

        /// <summary>
        /// The currently used OpenGL context, or null if no OpenGL context is created.
        /// It is not safe to call <see cref="IGraphicsContext.MakeCurrent"/> on this context on any other thread
        /// than the one the <see cref="GLWpfControl"/> is running on.
        /// </summary>
        public IGraphicsContext Context => _renderer?.GLContext;

        /// <summary>
        /// The <see cref="IWindowInfo"/> related to this controls OpenGL context.
        /// </summary>
        public IWindowInfo WindowInfo => _renderer?._context.WindowInfo;

        /// <summary>
        /// If MSAA backbuffers can be created for this GLWpfControl.
        /// If false any attempt to create an MSAA framebuffer will be ignored.
        /// </summary>
        [Obsolete("This property will always return true.")]
        public bool SupportsMSAA => true;

        private TimeSpan? _lastRenderTime = TimeSpan.FromSeconds(-1);

        [Obsolete("This property has no effect. See RegisterToEventsDirectly.")]
		public bool CanInvokeOnHandledEvents { get; set; } = true;

        [Obsolete("If you want to receive keyboard events without having focus you can use EventManager.RegisterClassHandler yourself. The control is by default focusable and will get key events when focused. This property will have no effect.")]
		public bool RegisterToEventsDirectly { get; set; } = true;

        /// <summary>
        /// Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettings)"/> must be called.
        /// </summary>
        public GLWpfControl() : base()
        {
        }

        /// <summary>
        /// Starts the control and rendering, using the settings provided via the <see cref="Settings"/> property.
        /// </summary>
        public void Start()
        {
            // Start with default settings if none were provided.
            if (Settings == null)
                Settings = new GLWpfControlSettings();

            Start(Settings);
        }

        /// <summary>
        /// Starts the control and rendering, using the settings provided.
        /// </summary>
        /// <param name="settings">
        /// The settings used to construct the underlying graphics context.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="Start"/> function must only be called once for a given <see cref="GLWpfControl"/>.
        /// </exception>
        public void Start(GLWpfControlSettings settings)
        {
            if (_isStarted) {
                throw new InvalidOperationException($"{nameof(Start)} must only be called once for a given {nameof(GLWpfControl)}");
            }

            _isStarted = true;

            Settings = settings.Clone();
            _renderer = new GLWpfControlRenderer(Settings);
            _renderer.GLRender += timeDelta => Render?.Invoke(timeDelta);
            _renderer.GLAsyncRender += () => AsyncRender?.Invoke();
            IsVisibleChanged += (_, args) => {
                if ((bool) args.NewValue) {
                    CompositionTarget.Rendering += OnCompTargetRender;
                }
                else {
                    CompositionTarget.Rendering -= OnCompTargetRender;
                }
            };

            Loaded += (a, b) => InvalidateVisual();
            Unloaded += (a, b) => OnUnloaded();

            Ready?.Invoke();
        }
        
        public void MakeCurrent()
        {
            _renderer._context.GraphicsContext.MakeCurrent(_renderer._context.WindowInfo);
        }

        private void OnUnloaded()
        {
            if (_isStarted)
            {
                _renderer?.ReleaseFramebufferResources();
            }
        }

        /// <summary>
        /// Disposes the native resources allocated by this control.
        /// After this function has been called this control will no longer render anything 
        /// until <see cref="Start()"/> has been called again.
        /// </summary>
        public void Dispose()
        {
            _renderer?.Dispose();
            _isStarted = false;
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            TimeSpan? currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if(currentRenderTime == _lastRenderTime)
            {
                // It's possible for Rendering to call back twice in the same frame
                // so only render when we haven't already rendered in this frame.
                // Reference: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-direct3d9-content-in-wpf?view=netframeworkdesktop-4.8#to-import-direct3d9-content
                return;
            }
            
            _lastRenderTime = currentRenderTime;

            if (RenderContinuously) InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            bool isDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isDesignMode) {
                DrawDesignTimeHelper(this, drawingContext);
            }
            else if (_renderer != null && _isStarted == true)
            {
                if (Settings != null)
                {
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;

                    if (Settings.UseDeviceDpi)
                    {
                        PresentationSource presentationSource = PresentationSource.FromVisual(this);
                        // this can be null in the case of not having any visual on screen, such as a tabbed view.
                        if (presentationSource != null)
                        {
                            Debug.Assert(presentationSource.CompositionTarget != null, "presentationSource.CompositionTarget != null");

                            Matrix transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                            dpiScaleX = transformToDevice.M11;
                            dpiScaleY = transformToDevice.M22;
                        }
                    }
                    
                    Format format = Settings.TransparentBackground ? Format.A8R8G8B8 : Format.X8R8G8B8;

                    _renderer.ReallocateFramebufferIfNeeded(RenderSize.Width, RenderSize.Height, dpiScaleX, dpiScaleY, format, Settings.Samples);
                }

                _renderer.Render(drawingContext);
            }
            else
            {
                DrawUnstartedControlHelper(this, drawingContext);
            }
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            base.OnRenderSizeChanged(info);

            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isInDesignMode)
            {
                return;
            }
            
            if ((info.WidthChanged || info.HeightChanged) && (info.NewSize.Width > 0 && info.NewSize.Height > 0))
            {
                InvalidateVisual();
            }
        }

        internal static void DrawDesignTimeHelper(GLWpfControl control, DrawingContext drawingContext)
        {
            if (control.Visibility == Visibility.Visible && control.ActualWidth > 0 && control.ActualHeight > 0)
            {
                const string LabelText = "GL WPF CONTROL";
                double width = control.ActualWidth;
                double height = control.ActualHeight;
                double size = 1.5 * Math.Min(width, height) / LabelText.Length;
                Typeface tf = new Typeface("Arial");
                DpiScale dpi = VisualTreeHelper.GetDpi(control);
                FormattedText ft = new FormattedText(LabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, Brushes.White, dpi.PixelsPerDip)
                {
                    TextAlignment = TextAlignment.Center
                };
                Pen redPen = new Pen(Brushes.DarkBlue, 2.0);
                Rect rect = new Rect(1, 1, width - 1, height - 1);
                drawingContext.DrawRectangle(Brushes.Black, redPen, rect);
                drawingContext.DrawLine(new Pen(Brushes.DarkBlue, 2.0),
                    new Point(0.0, 0.0),
                    new Point(control.ActualWidth, control.ActualHeight));
                drawingContext.DrawLine(new Pen(Brushes.DarkBlue, 2.0),
                    new Point(control.ActualWidth, 0.0),
                    new Point(0.0, control.ActualHeight));
                drawingContext.DrawText(ft, new Point(width / 2, (height - ft.Height) / 2));
            }
        }

        internal static void DrawUnstartedControlHelper(GLWpfControl control, DrawingContext drawingContext)
        {
            if (control.Visibility == Visibility.Visible && control.ActualWidth > 0 && control.ActualHeight > 0)
            {
                double width = control.ActualWidth;
                double height = control.ActualHeight;
                drawingContext.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, width, height));

                if (!Debugger.IsAttached) // Do not show the message if we're not debugging
                {
                    return;
                }

                const string UnstartedLabelText = "OpenGL content. Call Start() on the control to begin rendering.";
                const int Size = 12;
                Typeface tf = new Typeface("Arial");

                DpiScale dpi = VisualTreeHelper.GetDpi(control);
                FormattedText ft = new FormattedText(UnstartedLabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, Size, Brushes.White, dpi.PixelsPerDip)
                {
                    TextAlignment = TextAlignment.Left,
                    MaxTextWidth = width
                };

                drawingContext.DrawText(ft, new Point(0, 0));
            }
        }
    }
}
