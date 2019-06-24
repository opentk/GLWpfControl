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
        private TimeSpan _elapsedTime;

        public MainWindow() {
            InitializeComponent();
            _elapsedTime = new TimeSpan();
            var settings = new GLWpfControlSettings();
            OpenTkControl.Start(settings);
        }

        private void OpenTkControl_OnRender(TimeSpan delta) {
            _elapsedTime += delta;
            var c = Color4.FromHsv(new Vector4((float)_elapsedTime.TotalSeconds * 0.1f % 1, 1f, 0.9999f, 1));
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
