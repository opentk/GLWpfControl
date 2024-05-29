using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpenTK.Wpf.Interop;
using System.Windows.Interop;

#nullable enable

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
        /// <summary>
        /// Called whenever rendering should occur.
        /// </summary>
        public event Action<TimeSpan>? Render;

        /// <summary>
        /// Called once per frame after render. This does not synchronize with the copy to the screen.
        /// This is only for extremely advanced use, where a non-display out task needs to run.
        /// Examples of these are an async Pixel Buffer Object transfer or Transform Feedback.
        /// If you do not know what these are, do not use this function.
        /// </summary>
        public event Action? AsyncRender;

        /// <summary>
        /// Gets called after the control has finished initializing and is ready to render
        /// </summary>
        public event Action? Ready;

        /// <summary>
        /// Represents the dependency property for <see cref="Settings"/>.
        /// </summary>
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(GLWpfControlSettings), typeof(GLWpfControl));

        private GLWpfControlRenderer? _renderer;

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
        public int Framebuffer => _renderer?.FrameBufferHandle ?? 0;

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

        private TimeSpan? _lastRenderTime = TimeSpan.FromSeconds(-1);
		
		public bool CanInvokeOnHandledEvents { get; set; } = true;
		
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
            Settings ??= new GLWpfControlSettings();

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

            // Inheriting directly from a FrameworkElement has issues with receiving certain events -- register for these events directly
            if (RegisterToEventsDirectly)
	        {
	            EventManager.RegisterClassHandler(typeof(Control), Keyboard.KeyDownEvent, new KeyEventHandler(OnKeyDown), CanInvokeOnHandledEvents);
		        EventManager.RegisterClassHandler(typeof(Control), Keyboard.KeyUpEvent, new KeyEventHandler(OnKeyUp), CanInvokeOnHandledEvents);
	        }
			
            Loaded += (a, b) => InvalidateVisual();
            Unloaded += (a, b) => OnUnloaded();

            Ready?.Invoke();
        }
        
        private void OnUnloaded()
        {
            // FIXME: Make this a separate function for releasing resources...
            // Currently this works as we are passing a zero width and height
            // which causes the renderer to not reallocate the framebuffer
            // after the previous one has been deleted.
            // - Noggin_bops 2024-05-29
            _renderer?.ReallocateFramebufferIfNeeded(0, 0, 1, 1, Format.X8R8G8B8, MultisampleType.D3DMULTISAMPLE_NONE);
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

        // Raise the events so they're received if you subscribe to the base control's events
        // There are others that should probably be sent -- focus doesn't seem to work for whatever reason
        internal void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource != this)
            {
                KeyEventArgs args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key);
                args.RoutedEvent = Keyboard.KeyDownEvent;
                RaiseEvent(args);
            }
        }
        
        internal void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource != this)
            {
                KeyEventArgs args = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key);
                args.RoutedEvent = Keyboard.KeyUpEvent;
                RaiseEvent(args);
            }
        }

        private void OnCompTargetRender(object? sender, EventArgs e)
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

            var isDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isDesignMode) {
                DrawDesignTimeHelper(this, drawingContext);
            }
            else if (_renderer != null)
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

                    MultisampleType msaaType = MultisampleType.D3DMULTISAMPLE_NONE;
                    // 2 to 16 are valid msaa values, clamp to 16.
                    if (Settings.Samples >= 2 && Settings.Samples <= 16)
                        msaaType = MultisampleType.D3DMULTISAMPLE_NONE + Settings.Samples;
                    else if (Settings.Samples > 16)
                        msaaType = MultisampleType.D3DMULTISAMPLE_16_SAMPLES;

                    _renderer.ReallocateFramebufferIfNeeded(RenderSize.Width, RenderSize.Height, dpiScaleX, dpiScaleY, format, msaaType);
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

            var isInDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isInDesignMode) {
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
                const string labelText = "GL WPF CONTROL";
                var width = control.ActualWidth;
                var height = control.ActualHeight;
                var size = 1.5 * Math.Min(width, height) / labelText.Length;
                var tf = new Typeface("Arial");
#pragma warning disable 618
                var ft = new FormattedText(labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, Brushes.White)
                {
                    TextAlignment = TextAlignment.Center
                };
#pragma warning restore 618
                var redPen = new Pen(Brushes.DarkBlue, 2.0);
                var rect = new Rect(1, 1, width - 1, height - 1);
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
                var width = control.ActualWidth;
                var height = control.ActualHeight;
                drawingContext.DrawRectangle(Brushes.Gray, null, new Rect(0, 0, width, height));

                if (!Debugger.IsAttached) // Do not show the message if we're not debugging
                {
                    return;
                }

                const string unstartedLabelText = "OpenGL content. Call Start() on the control to begin rendering.";
                const int size = 12;
                var tf = new Typeface("Arial");
                
                // FIXME: Fix scaling!
                var ft = new FormattedText(unstartedLabelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, tf, size, Brushes.White)
                {
                    TextAlignment = TextAlignment.Left,
                    MaxTextWidth = width
                };

                drawingContext.DrawText(ft, new Point(0, 0));
            }
        }
    }
}
