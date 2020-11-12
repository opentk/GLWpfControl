using System;
using System.Diagnostics;
using System.Timers;
using OpenTK.Wpf;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace GLWpfControlExample {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private TimeSpan _elapsedTime;
        private Timer _slowTimer = new Timer(500);

        public MainWindow() {
            InitializeComponent();
            _elapsedTime = new TimeSpan();
            var settings = new GLWpfControlSettings();
            settings.MajorVersion = 3;
            settings.MinorVersion = 3;
            settings.GraphicsProfile = ContextProfile.Compatability;
            OpenTkControl.Start(settings);
            settings.RenderContinuously = false;
			InsetControl.Start(settings);
            _slowTimer.Enabled = true;
            _slowTimer.Start();
            var localDispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            
            _slowTimer.Elapsed += delegate {
                localDispatcher.Invoke(() => { InsetControl.InvalidateVisual(); });
            };

        }

        private void OpenTkControl_OnRender(TimeSpan delta) {
            _elapsedTime += delta;
            DrawStuff();
        }

        private void DrawStuff() {
            var c = Color4.FromHsv(new Vector4((float) _elapsedTime.TotalSeconds * 0.1f % 1, 1f, 0.9999f, 1));
            GL.ClearColor(c);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.ScissorTest);
            var xPos = 50 + (ActualWidth - 250) * (0.5 + 0.5 * Math.Sin(_stopwatch.Elapsed.TotalSeconds));
            GL.Scissor((int) xPos, 25, 50, 50);
            GL.ClearColor(Color4.Blue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.ScissorTest);
            GL.LoadIdentity();
            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
            GL.Vertex2(0.0f, 1.0f);
            GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
            GL.Vertex2(0.87f, -0.5f);
            GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
            GL.Vertex2(-0.87f, -0.5f);
            GL.End();
            GL.Finish();
        }

        private void InsetControl_OnRender(TimeSpan delta) {
            var c = Color4.FromHsv(new Vector4((float) _elapsedTime.TotalSeconds * 0.35f % 1, 0.75f, 0.75f, 1));
            GL.ClearColor(c);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.ScissorTest);
            var xPos = 50 + (ActualWidth - 250) * (0.5 + 0.5 * Math.Cos(_stopwatch.Elapsed.TotalSeconds));
            GL.Scissor((int) xPos, 25, 50, 50);
            GL.ClearColor(Color4.Green);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.ScissorTest);
            GL.LoadIdentity();
            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
            GL.Vertex2(0.0f, 1.0f);
            GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
            GL.Vertex2(0.87f, -0.5f);
            GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
            GL.Vertex2(-0.87f, -0.5f);
            GL.End();
        }
    }
}
