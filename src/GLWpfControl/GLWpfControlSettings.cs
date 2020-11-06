using OpenTK.Graphics;

namespace OpenTK.Wpf {
    
    public sealed class GLWpfControlSettings {
        
        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public GraphicsContext ContextToUse { get; set; }
        
        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;
        
        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                MajorVersion = MajorVersion,
                MinorVersion = MajorVersion
            };
            return c;
        }

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;

    }
}
