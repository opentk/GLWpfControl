using System;
using System.Diagnostics;
using System.Windows;
using GLWpfControl;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace GLWpfControlExample {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public MainWindow() {
            InitializeComponent();
            var settings = new GLWpfControlSettings();
            OpenTkControl.Start(settings);
        }

        private void OpenTkControl_OnRender() {
            var t = 0.5f + 0.5f*Math.Sin(_stopwatch.Elapsed.TotalSeconds);
            var v = Vector3.Lerp(Vector3.UnitX, Vector3.UnitZ, (float) t);
            var c = new Color4(v.X, 0.5f,v.Z, 1.0f);
            GL.ClearColor(c);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.ScissorTest);
            GL.Scissor(25, 25, 50, 50);
            GL.ClearColor(Color4.Blue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.ScissorTest);
        }
    }
}
