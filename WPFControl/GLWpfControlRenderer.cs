using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenTkControl {
    internal sealed class GLWpfControlRenderer {
        
        [DllImport("kernel32.dll")]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);
        
        private readonly WriteableBitmap _bitmap;
        private readonly int _colorBuffer;
        private readonly int _depthBuffer;

        private readonly Image _imageControl;
        private readonly bool _isSoftwareRenderer;
        private readonly int[] _pixelBuffers;
        private bool _hasRenderedAFrame = false;
        
        public int FrameBuffer { get; }


        public int Width => _bitmap.PixelWidth;
        public int Height => _bitmap.PixelHeight;
        public int PixelBufferObjectCount => _pixelBuffers.Length;

        public GLWpfControlRenderer(int width, int height, Image imageControl, bool isSoftwareRenderer, int pixelBufferCount) {

            _imageControl = imageControl;
            _isSoftwareRenderer = isSoftwareRenderer;
            // the bitmap we're blitting to in software mode.
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            // set up the framebuffer
            FrameBuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBuffer);

            _depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthBuffer);

            _colorBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _colorBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Rgba8, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                RenderbufferTarget.Renderbuffer, _colorBuffer);

            var error = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (error != FramebufferErrorCode.FramebufferComplete) {
                throw new GraphicsErrorException("Error creating frame buffer: " + error);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

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

        public void DeleteBuffers() {
            GL.DeleteFramebuffer(FrameBuffer);
            GL.DeleteRenderbuffer(_depthBuffer);
            GL.DeleteRenderbuffer(_colorBuffer);
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

        public void UpdateImage() {
            if (_isSoftwareRenderer) {
                UpdateImageSoftware();
            }
            else {
                UpdateImageSoftware();
            }

            _hasRenderedAFrame = true;
        }

        private void UpdateImageSoftware() {
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
    }
}
