using OpenTK.Windowing.Common;

namespace OpenTK.Wpf {
    public sealed class GLWpfControlSettings {
        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        public bool RenderContinuously = true;

        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }

        public ContextFlags GraphicsContextFlags { get; set; } = ContextFlags.Default;
        public ContextProfile GraphicsProfile { get; set; } = ContextProfile.Any;

        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;
        public bool UseSRGB { get; set; } = false;

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;

        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                ContextToUse = ContextToUse,
                GraphicsContextFlags = GraphicsContextFlags,
                GraphicsProfile = GraphicsProfile,
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                RenderContinuously = RenderContinuously
            };
            return c;
        }
    }
}
