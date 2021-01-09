using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;

namespace OpenTK.Wpf
{
    /// <summary>
    ///     Provides a native WPF control for OpenTK.
    ///     To use this component, call the <see cref="Start(GLWpfControlSettings)"/> method.
    ///     Bind to the <see cref="Render"/> event only after <see cref="Start(GLWpfControlSettings)"/> is called.
    ///
    ///     Please do not extend this class. It has no support for that.
    /// </summary>
    public class GLWpfControl : FrameworkElement
    {
        // -----------------------------------
        // EVENTS
        // -----------------------------------

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
        

        // -----------------------------------
        // Fields
        // -----------------------------------
        
        [CanBeNull] private GLWpfControlSettings _settings;
        [CanBeNull] private GLWpfControlRenderer _renderer;
        private bool _needsRedraw;

        // -----------------------------------
        // Properties
        // -----------------------------------

        /// The OpenGL Framebuffer Object used internally by this component.
        /// Bind to this instead of the default framebuffer when using this component along with other FrameBuffers for the final pass.
        /// If no framebuffer is available (because this control is not visible, etc etc, then it should be 0).
        public int Framebuffer => _renderer?.FrameBufferHandle ?? 0;


        /// If this control is rendering continuously.
        /// If this is false, then redrawing will only occur when <see cref="UIElement.InvalidateVisual"/> is called.
        public bool RenderContinuously {
            get => _settings.RenderContinuously;
            set => _settings.RenderContinuously = value;
        }

        /// Pixel width of the underlying OpenGL framebuffer.
        /// It could differ from UIElement.RenderSize if UseDeviceDpi setting is set.
        /// To be used for operations related to OpenGL viewport calls (glViewport, glScissor, ...).
        public int FrameBufferWidth => _renderer?.Width ?? 0;
        
        /// Pixel height of the underlying OpenGL framebuffer.
        /// It could differ from UIElement.RenderSize if UseDeviceDpi setting is set.
        /// To be used for operations related to OpenGL viewport calls (glViewport, glScissor, ...).
        public int FrameBufferHeight => _renderer?.Height ?? 0;

        private TimeSpan _lastRenderTime = TimeSpan.FromSeconds(-1);

        /// <summary>
        /// Used to create a new control. Before rendering can take place, <see cref="Start(GLWpfControlSettings)"/> must be called.
        /// </summary>
        public GLWpfControl() {
        }

        /// Starts the control and rendering, using the settings provided.
        public void Start(GLWpfControlSettings settings)
        {
            if (_settings != null) {
                throw new InvalidOperationException($"{nameof(Start)} must only be called once for a given {nameof(GLWpfControl)}");
            }
            _settings = settings.Copy();
            _needsRedraw = settings.RenderContinuously;
            _renderer = new GLWpfControlRenderer(_settings);
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

            Loaded += (a, b) => {
                InvalidateVisual();
            };
            Unloaded += (a, b) => OnUnloaded();
            Ready?.Invoke();
        }
        
        private void SetupRenderSize() {
            if (_renderer == null || _settings == null) {
                return;
            }

            var dpiScaleX = 1.0;
            var dpiScaleY = 1.0;

            if (_settings.UseDeviceDpi) {
                var presentationSource = PresentationSource.FromVisual(this);
                // this can be null in the case of not having any visual on screen, such as a tabbed view.
                if (presentationSource != null) {
                    Debug.Assert(presentationSource.CompositionTarget != null, "presentationSource.CompositionTarget != null");

                    var transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
                    dpiScaleX = transformToDevice.M11;
                    dpiScaleY = transformToDevice.M22;
                }
            }
            
            _renderer?.SetActiveDeviceFromLocation(PointToScreen(new Point(0, 0)));
            _renderer?.SetSize((int) RenderSize.Width, (int) RenderSize.Height, dpiScaleX, dpiScaleY);
        }

        private void OnUnloaded()
        {
            _renderer?.SetSize(0,0, 1, 1);
        }

        private void OnCompTargetRender(object sender, EventArgs e)
        {
            var currentRenderTime = (e as RenderingEventArgs)?.RenderingTime;
            if(currentRenderTime == _lastRenderTime)
            {
                // It's possible for Rendering to call back twice in the same frame
                // so only render when we haven't already rendered in this frame.
                // Reference: https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-direct3d9-content-in-wpf?view=netframeworkdesktop-4.8#to-import-direct3d9-content
                return;
            }

            _lastRenderTime = currentRenderTime.Value;

            if (_needsRedraw) {
                InvalidateVisual();
            }

            _needsRedraw = RenderContinuously;
        }

        protected override void OnRender(DrawingContext drawingContext) {
            var isDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isDesignMode) {
                DesignTimeHelper.DrawDesignTimeHelper(this, drawingContext);
            }
            else if(_renderer != null) {
                SetupRenderSize();
                _renderer?.Render(drawingContext);
            }
            else {
                UnstartedControlHelper.DrawUnstartedControlHelper(this, drawingContext);
            }

            base.OnRender(drawingContext);
        }
        
        protected override void OnRenderSizeChanged(SizeChangedInfo info)
        {
            var isInDesignMode = DesignerProperties.GetIsInDesignMode(this);
            if (isInDesignMode) {
                return;
            }
            
            if ((info.WidthChanged || info.HeightChanged) && (info.NewSize.Width > 0 && info.NewSize.Height > 0))
            {
                InvalidateVisual();
            }
            base.OnRenderSizeChanged(info);
        }
    }
}
