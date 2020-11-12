using OpenTK.Graphics;
using OpenTK.Windowing.Common;

namespace OpenTK.Wpf {
    
    public sealed class GLWpfControlSettings {
        
        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }
        
        public ContextFlags GraphicsContextFlags { get; set; } = ContextFlags.Default;
        
        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        public bool RenderContinuously = true;

        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                RenderContinuously = RenderContinuously,
            };
            return c;
        }

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;

    }
}
