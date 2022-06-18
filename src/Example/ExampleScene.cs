using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Example {
    /// Example class handling the rendering for OpenGL.
    public static class ExampleScene {

        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        public static void Ready() {
            Console.WriteLine("GlWpfControl is now ready");
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
        }

        public static void Render(float alpha = 1.0f) {
            var hue = (float) _stopwatch.Elapsed.TotalSeconds * 0.15f % 1;
            var c = Color4.FromHsv(new Vector4(alpha * hue, alpha * 0.75f, alpha * 0.75f, alpha));
            GL.ClearColor(c);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();
            GL.Begin(PrimitiveType.Triangles);

            GL.Color4(Color4.Red);
            GL.Vertex2(0.0f, 0.5f);

            GL.Color4(Color4.Green);
            GL.Vertex2(0.58f, -0.5f);

            GL.Color4(Color4.Blue);
            GL.Vertex2(-0.58f, -0.5f);

            GL.End();
            GL.Finish();
        }
    }
}
