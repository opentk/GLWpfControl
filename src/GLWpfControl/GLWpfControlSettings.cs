using System;
using System.Diagnostics.Contracts;
using OpenTK.Windowing.Common;

namespace OpenTK.Wpf {
    public sealed class GLWpfControlSettings {

        /// <summary>
        /// If the render event is fired continuously whenever required.
        /// Disable this if you want manual control over when the rendered surface is updated.
        /// </summary>
        public bool RenderContinuously { get; set; } = true;

        /// <summary>
        /// If this is set to false, the control will render without any DPI scaling.
        /// This will result in higher performance and a worse image quality on systems with >100% DPI settings, such as 'Retina' laptop screens with 4K UHD at small sizes.
        /// This setting may be useful to get extra performance on mobile platforms.
        /// </summary>
        public bool UseDeviceDpi { get; set; } = true;

        /// <summary>
        /// If this parameter is set to true, the alpha channel of the color passed to the function GL.ClearColor
        /// will determine the level of transparency of this control
        /// </summary>
        public bool TransparentBackground { get; set; } = false;

        /// <summary>
        /// May be null. If defined, an external context will be used, of which the caller is responsible
        /// for managing the lifetime and disposal of.
        /// </summary>
        [CLSCompliant(false)]
        public IGraphicsContext ContextToUse { get; set; }

        /// <summary>
        /// May be null. If so, default bindings context will be used.
        /// </summary>
        [CLSCompliant(false)]
        public IBindingsContext BindingsContext { get; set; }

        [CLSCompliant(false)]
        public ContextFlags GraphicsContextFlags { get; set; } = ContextFlags.Default;

        [CLSCompliant(false)]
        public ContextProfile GraphicsProfile { get; set; } = ContextProfile.Any;

        public int MajorVersion { get; set; } = 3;
        public int MinorVersion { get; set; } = 3;

        /// <summary>If we are using an external context for the control.</summary>
        public bool IsUsingExternalContext => ContextToUse != null;

        /// <summary>Determines if two settings would result in the same context being created.</summary>
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

        /// <summary>
        /// Makes a shallow clone of this <see cref="GLWpfControlSettings"/> object.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public GLWpfControlSettings Clone()
        {
            return (GLWpfControlSettings)this.MemberwiseClone();
        }
    }
}
