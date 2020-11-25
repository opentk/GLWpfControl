using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Example {
    /// Example class handling the rendering for OpenGL.
    public static class ExampleScene {

        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        
        private static readonly Lazy<int> TriangleVao = new Lazy<int>(CreateTriangleVertexArray);

        private static unsafe int CreateTriangleVertexArray() {
            
            Console.WriteLine("\t\tCreating triangle VAO");
            var positions = new[] {
                new Vector2(0.0f, 0.5f),
                new Vector2(0.58f, -0.5f),
                new Vector2(-0.58f, -0.5f),
            };

            var colors = new[] {
                Color4.Red,
                Color4.Green,
                Color4.Blue,
            };
            Console.WriteLine("\t\t\tCreating positions buffer...");
            var positionsBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionsBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(Vector2) * positions.Length, positions, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            Console.WriteLine("\t\t\tCreating colors buffer...");
            var colorsBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorsBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(Color4) * colors.Length, colors, BufferUsageHint.StaticDraw);

            Console.WriteLine("\t\t\tCreating vertex array object...");
            var vertexArray = GL.GenVertexArray();
            GL.BindVertexArray(vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionsBuffer);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0,2,VertexAttribPointerType.Float, false,0,0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorsBuffer);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1,4,VertexAttribPointerType.Float, false,0,0);
            
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
            Console.WriteLine("\t\t\tReturning vertex array object...");
            return vertexArray;
        }

        public static void Ready() {
            // Console.WriteLine("GlWpfControl is now ready");
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
        }

        public static void Render() {
            var hue = (float) _stopwatch.Elapsed.TotalSeconds * 0.15f % 1;
            var c = Color4.FromHsv(new Vector4(hue, 0.75f, 0.75f, 1));
            // Console.WriteLine("\tClearing...");
            GL.ClearColor(c);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();
            
            // Console.WriteLine("\tBinding vertex array");
            GL.BindVertexArray(TriangleVao.Value);
            // Console.WriteLine("\tDrawing...");
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
            // GL.End();
            // Console.WriteLine("\tFinishing...");
            GL.Finish();
            // Console.WriteLine("\tFinished!...");
            //
            //
            // GL.Begin(PrimitiveType.Triangles);
            //
            // GL.Color4(Color4.Red);
            // GL.Vertex2(0.0f, 0.5f);
            //
            // GL.Color4(Color4.Green);
            // GL.Vertex2(0.58f, -0.5f);
            //
            // GL.Color4(Color4.Blue);
            // GL.Vertex2(-0.58f, -0.5f);
            //
            // GL.End();
            // GL.Finish();
        }
    }
}
