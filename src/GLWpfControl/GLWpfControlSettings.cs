using OpenTK.Graphics;

namespace OpenTK.Wpf {
    
    public sealed class GLWpfControlSettings {
        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;
        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;
        
        /// The number of pixel buffer objects in use for pixel transfer.
        /// Must be >= 1. Setting this higher will mean more delays between frames showing up on the WPF control
        /// in software mode, but greatly improved render performance. Defaults to 2.
        public int PixelBufferObjectCount { get; set; } = 2;

        /// If this is set to true then direct mapping between OpenGL and WPF's D3D will be performed.
        /// If this is set to false, a slower but more compatible software copy is performed.
        public bool UseHardwareRender { get; set; } = true;

        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                GraphicsContextFlags = GraphicsContextFlags,
                MajorVersion = MajorVersion,
                MinorVersion = MajorVersion,
                UseHardwareRender = UseHardwareRender,
                PixelBufferObjectCount = PixelBufferObjectCount
            };
            return c;
        }
        
    }
}
