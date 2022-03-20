using System;
using JetBrains.Annotations;
using OpenTK.Windowing.Common;

namespace OpenTK.Wpf {
    public sealed class GLWpfControlSettings {
        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        public bool RenderContinuously { get; set; } = true;

        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        public bool UseDeviceDpi { get; set; } = true;

        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        public IGraphicsContext ContextToUse { get; set; }

        /// May be null. If so, default bindings context will be used.
        public IBindingsContext BindingsContext { get; set; }

        public ContextFlags GraphicsContextFlags { get; set; } = ContextFlags.Default;
        public ContextProfile GraphicsProfile { get; set; } = ContextProfile.Any;

        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// If we are using an external context for the control.
        public bool IsUsingExternalContext => ContextToUse != null;
        
        /// Creates a copy of the settings.
        internal GLWpfControlSettings Copy() {
            var c = new GLWpfControlSettings {
                ContextToUse = ContextToUse,
                BindingsContext = BindingsContext,
                GraphicsContextFlags = GraphicsContextFlags,
                GraphicsProfile = GraphicsProfile,
                MajorVersion = MajorVersion,
                MinorVersion = MinorVersion,
                RenderContinuously = RenderContinuously,
                UseDeviceDpi = UseDeviceDpi
            };
            return c;
        }

        /// Determines if two settings would result in the same context being created.
        [Pure]
        internal static bool WouldResultInSameContext([NotNull] GLWpfControlSettings a, [NotNull] GLWpfControlSettings b) {
            if (a.MajorVersion != b.MajorVersion) {
                return false;
            }

            if (a.MinorVersion != b.MinorVersion) {
                return false;
            }

            if (a.GraphicsProfile != b.GraphicsProfile) {
                return false;
            }

            if (a.GraphicsContextFlags != b.GraphicsContextFlags) {
                return false;
            }

            return true;

        }
    }
}
