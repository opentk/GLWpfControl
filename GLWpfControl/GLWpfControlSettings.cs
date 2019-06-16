using OpenTK.Graphics;

namespace GLWpfControl {
    
    public sealed class GLWpfControlSettings {
        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;
        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;
        
        /// The number of pixel buffer objects in use for pixel transfer.
        /// Must be >= 1. Setting this higher will mean more delays between frames showing up on the WPF control
        /// in software mode, but greatly improved render performance. Defaults to 2.
        public int PixelBufferObjectCount { get; set; } = 2;
        
        /// If this is set to true, transfer of OpenGL to DirectX will be used instead.
        public bool UseSoftwareRender { get; set; }

        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                GraphicsContextFlags = GraphicsContextFlags,
                MajorVersion = MajorVersion,
                MinorVersion = MajorVersion,
                UseSoftwareRender = UseSoftwareRender,
                PixelBufferObjectCount = PixelBufferObjectCount
            };
            return c;
        }
        
    }
}
