using System;
using System.Diagnostics.Contracts;
using OpenTK.Windowing.Common;

#nullable enable

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
        public IGraphicsContext? ContextToUse { get; set; }

        /// <summary>
        /// May be null. If so, default bindings context will be used.
        /// </summary>
        [CLSCompliant(false)]
        public IBindingsContext? BindingsContext { get; set; }

        /// <summary>
        /// The OpenGL context flags to use. Same as <see cref="ContextFlags"/>.
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete("Use ContextFlags instead.")]
        public ContextFlags GraphicsContextFlags { get => ContextFlags; set => ContextFlags = value; }

        /// <summary>
        /// The OpenGL context flags to use. <see cref="ContextFlags.Offscreen"/> will always be set for DirectX interop purposes.
        /// </summary>
        [CLSCompliant(false)]
        public ContextFlags ContextFlags { get; set; } = ContextFlags.Default;

        /// <summary>
        /// The OpenGL profile to use. Same as <see cref="Profile"/>.
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete("Use Profile instead.")]
        public ContextProfile GraphicsProfile { get => Profile; set => Profile = value; }

        /// <summary>
        /// The OpenGL profile to use.
        /// </summary>
        [CLSCompliant(false)]
        public ContextProfile Profile { get; set; } = ContextProfile.Any;

        /// <summary>The major OpenGL version number.</summary>
        public int MajorVersion { get; set; } = 3;
        /// <summary>The minor OpenGL version number.</summary>
        public int MinorVersion { get; set; } = 3;

        /// <summary>
        /// How many MSAA samples should the framebuffer have in the range [0, 16]. 0 and 1 result in no MSAA.
        /// </summary>
        public int Samples { get; set; } = 0;

        /// <summary>If we are using an external context for the control.</summary>
        public bool IsUsingExternalContext => ContextToUse != null;

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
