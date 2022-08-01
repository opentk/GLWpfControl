using System;
using System.Diagnostics.Contracts;
using OpenTK.Windowing.Common;

namespace OpenTK.Wpf {
    public sealed class GLWpfControlSettings : ICloneable {
        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        public bool RenderContinuously { get; set; } = true;

        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        public bool UseDeviceDpi { get; set; } = true;

        /// If this parameter is set to true, the alpha channel of the color passed to the function GL.ClearColor
        /// will determine the level of transparency of this control
        public bool TransparentBackground { get; set; } = false;

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
        
        /// Determines if two settings would result in the same context being created.
        [Pure]
        internal static bool WouldResultInSameContext(GLWpfControlSettings a, GLWpfControlSettings b) {
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

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
