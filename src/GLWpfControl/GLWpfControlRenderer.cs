using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenTK.Wpf {
    internal sealed class GLWpfControlRenderer {
        
        [DllImport("kernel32.dll")]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);
        
        private readonly WriteableBitmap _bitmap;
        private readonly int _colorBuffer;
        private readonly int _depthBuffer;
        private readonly int _colorBufferMS;
        private readonly int _depthBufferMS;

        private readonly Image _imageControl;
        private readonly bool _isHardwareRenderer;
        private readonly int[] _pixelBuffers;
        private bool _hasRenderedAFrame = false;
        
        public int FrameBuffer { get; }
        public int FrameBufferMS { get; }
        public int Samples { get; }
        public bool HasSamples => Samples == 4 || Samples == 8 || Samples == 16;

        public int Width => _bitmap.PixelWidth;
        public int Height => _bitmap.PixelHeight;
        public int PixelBufferObjectCount => _pixelBuffers.Length;

        public GLWpfControlRenderer(int width, int height, Image imageControl, bool isHardwareRenderer, int pixelBufferCount, int samples = 4) {

            _imageControl = imageControl;
            _isHardwareRenderer = isHardwareRenderer;
            // the bitmap we're blitting to in software mode.
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Samples = samples;

            FrameBuffer = GenerateFramebuffer(false, out _depthBuffer, out _colorBuffer);

            if(HasSamples)
                FrameBufferMS = GenerateFramebuffer(true, out _depthBufferMS, out _colorBufferMS);

            // generate the pixel buffers

            _pixelBuffers = new int[pixelBufferCount];
            // RGBA8 buffer
            var size = sizeof(byte) * 4 * width * height;
            for (var i = 0; i < _pixelBuffers.Length; i++) {
                var pb = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.PixelPackBuffer, pb);
                GL.BufferData(BufferTarget.PixelPackBuffer, size, IntPtr.Zero, BufferUsageHint.StreamRead);
                _pixelBuffers[i] = pb;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }

        private int GenerateFramebuffer(bool multisampled, out int rboDepth, out int rboColor) {
            // set up the framebuffer
            int fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

            if (!multisampled)
            {
                rboDepth = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, Width, Height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, rboDepth);

                rboColor = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboColor);
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, Width, Height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    RenderbufferTarget.Renderbuffer, rboColor); 
            }
            else
            {
                rboDepth = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Samples, RenderbufferStorage.DepthComponent24, Width, Height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                    RenderbufferTarget.Renderbuffer, rboDepth);

                rboColor = GL.GenRenderbuffer();
                GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboColor);
                GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, Samples, RenderbufferStorage.Rgba8, Width, Height);
                GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                    RenderbufferTarget.Renderbuffer, rboColor);
            }

            var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete)
            {
                throw new GraphicsErrorException("Error creating frame buffer: " + error);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return fbo;
        }

        public void DeleteBuffers() {
            GL.DeleteFramebuffer(FrameBuffer);
            GL.DeleteFramebuffer(FrameBufferMS);
            GL.DeleteRenderbuffer(_depthBuffer);
            GL.DeleteRenderbuffer(_colorBuffer);
            GL.DeleteRenderbuffer(_depthBufferMS);
            GL.DeleteRenderbuffer(_colorBufferMS);
            for (var i = 0; i < _pixelBuffers.Length; i++) {
                GL.DeleteBuffer(_pixelBuffers[i]);
            }
        }

        // shifts all of the PBOs along by 1.
        private void RotatePixelBuffers() {
            var fst = _pixelBuffers[0];
            for (var i = 1; i < _pixelBuffers.Length; i++) {
                _pixelBuffers[i - 1] = _pixelBuffers[i];
            }
            _pixelBuffers[_pixelBuffers.Length - 1] = fst;
        }

        internal void BeforeRender()
        {
            if (!HasSamples)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
            else
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferMS);
        }

        public void UpdateImage() {
            if (false && _isHardwareRenderer) {
                UpdateImageHardware();
            }
            else {
                UpdateImageSoftware();
            }

            _hasRenderedAFrame = true;
        }

        

        private void UpdateImageSoftware() {
            if (HasSamples)
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, FrameBufferMS);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, FrameBuffer);
                GL.BlitFramebuffer(0, 0, Width, Height, 0, 0, Width, Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest); 
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);
            // start the (async) pixel transfer.
            GL.BindBuffer(BufferTarget.PixelPackBuffer, _pixelBuffers[0]);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            GL.ReadPixels(0, 0, Width, Height, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            // rotate the pixel buffers.
            if (_hasRenderedAFrame) {
                RotatePixelBuffers();
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _pixelBuffers[0]);
            // copy the data over from a mapped buffer.
            _bitmap.Lock();
            var data = GL.MapBuffer(BufferTarget.PixelPackBuffer, BufferAccess.ReadOnly);
            CopyMemory(_bitmap.BackBuffer, data, (uint) (sizeof(byte) * 4 * Width * Height));
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            _bitmap.Unlock();
            GL.UnmapBuffer(BufferTarget.PixelPackBuffer);
            if (!ReferenceEquals(_imageControl.Source, _bitmap)) {
                _imageControl.Source = _bitmap;
            }

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
        }
        private void UpdateImageHardware() {
            // There are 2 options we can use here.
            // 1. Use a D3DSurface and WGL_NV_DX_interop to perform the rendering.
            //         This is still performing RTT (render to texture) and isn't as fast as just directly drawing the stuff onto the DX buffer.
            // 2. Steal the handles using hooks into DirectX, then use that to directly render.
            //         This is the fastest possible way, but it requires a whole lot of moving parts to get anything working properly.
                
            // references for (2):
                
            // Accessing WPF's Direct3D internals.
            // note: see the WPFD3dHack.zip file on the blog post
            // http://jmorrill.hjtcentral.com/Home/tabid/428/EntryId/438/How-to-get-access-to-WPF-s-internal-Direct3D-guts.aspx
                
            // Using API hooks from C# to get d3d internals
            // this would have to be adapted to WPF, but should/maybe work.
            // http://spazzarama.com/2011/03/14/c-screen-capture-and-overlays-for-direct3d-9-10-and-11-using-api-hooks/
            // https://github.com/spazzarama/Direct3DHook
            throw new NotImplementedException();
        }
        
    }
}
