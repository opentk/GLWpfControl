using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf {
    
    /// Class containing the DirectX Render Surface and OpenGL Framebuffer Object
    /// Instances of this class are created and deleted as required by the renderer.
    /// Note that this does not implement the full <see cref="IDisposable"/> pattern,
    /// as OpenGL resources cannot be freed from the finalizer thread.
    /// The calling class must correctly dispose of this by calling <see cref="Dispose"/>
    /// Prior to releasing references. 
    internal sealed class DxGLFramebuffer : IDisposable {
        
        /// The width of this buffer in pixels
        public int Width { get; }
        
        /// The height of this buffer in pixels
        public int Height { get; }
        
        /// The OpenGL Framebuffer handle
        public int GLFramebuffer { get; }

        /// The shared texture handle
        private int GLSharedTexture { get; }


        public DxGLFramebuffer() {

        }
        
        
        public void Dispose() {
            GL.DeleteBuffer(GLFramebuffer);
            GL.DeleteTexture(GLSharedTexture);
        }
    }
}
