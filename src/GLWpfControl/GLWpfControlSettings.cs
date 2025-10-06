using System;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTK.Wpf {
    public sealed class GLWpfControlSettings
    {
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
        /// May be null. If defined, an external context will be used, of which the caller is responsible for managing the lifetime and disposal of.
        /// If defined the <see cref="WindowInfo"/> property also needs to be set to a <see cref="IWindowInfo"/> that can be used when calling <see cref="GraphicsContext.MakeCurrent(IWindowInfo)"/>.
        /// The management of the context sent to the <see cref="GLWpfControl"/> becomes the responsibility of the <see cref="GLWpfControl"/>.
        /// Trying to call <see cref="IGraphicsContext.MakeCurrent"/> on this context on some other thread might lead to uninteded consequences.
        /// </summary>
        [CLSCompliant(false)]
        public IGraphicsContext ContextToUse { get; set; }

        /// <summary>
        /// When <see cref="ContextToUse"/> is set this property should contain the <see cref="IWindowInfo"/> related to the context, otherwise this property should be null.
        /// </summary>
        public IWindowInfo WindowInfo { get; set; }

        /// <summary>
        /// A optional context for context sharing.
        /// </summary>
        [CLSCompliant(false)]
        public IGraphicsContext SharedContext { get; set; }

        /// <summary>
        /// May be null. If so, default bindings context will be used.
        /// </summary>
        //[CLSCompliant(false)]
        //public IBindingsContext BindingsContext { get; set; }
        
        /// <summary>
        /// The OpenGL context flags to use. Same as <see cref="ContextFlags"/>.
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete("Use ContextFlags instead.")]
        public GraphicsContextFlags GraphicsContextFlags { get; set; } = GraphicsContextFlags.Default;

        /// <summary>
        /// The OpenGL context flags to use. <see cref="ContextFlags.Offscreen"/> will always be set for DirectX interop purposes.
        /// </summary>
        [CLSCompliant(false)]
        public GraphicsContextFlags ContextFlags { get; set; } = GraphicsContextFlags.Default;

        /// <summary>
        /// The OpenGL profile to use. Same as <see cref="Profile"/>.
        /// </summary>
        [CLSCompliant(false)]
        [Obsolete("Use Profile instead.")]
        public ContextProfileMask GraphicsProfile { get; set; }

        /// <summary>
        /// The OpenGL profile to use.
        /// </summary>
        [CLSCompliant(false)]
        public ContextProfileMask Profile { get; set; } = 0;

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
