using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using OpenTK.Windowing.Common;
using OpenTK.Wpf.Interop;

namespace OpenTK.Wpf.Renderers
{
    internal interface IGLWpfControlRenderer : IDisposable
    {
        /// <summary>The OpenGL framebuffer handle.</summary>
        int GLFramebufferHandle { get; }

        /// <summary>The OpenGL Framebuffer width</summary>
        int Width { get; }

        /// <summary>The OpenGL Framebuffer height</summary>
        int Height { get; }

        IGraphicsContext? GLContext { get; }

        bool SupportsMSAA { get; }

        event Action<TimeSpan>? GLRender;

        [Obsolete("There is no difference between GLRender and GLAsyncRender. Use GLRender.")]
        event Action? GLAsyncRender;

        void Render(DrawingContext drawingContext);
        void ReallocateFramebufferIfNeeded(double width, double height, double dpiScaleX, double dpiScaleY, Format format, MultisampleType msaaType);
        void ReleaseFramebufferResources();
    }

    internal static class GLWpfControlRendererFactory
    {
        public static IGLWpfControlRenderer? CreateRenderer(GLWpfControlSettings settings)
        {
            var context = new DxGlContext(settings);

            return SupportsMSAATest(context)
                       ? new GLWpfControlDirectSurfaceRenderer(context)
                       : settings.Samples > 1
                           ? new GLWpfControlTexture2DRendererMSAA(context)
                           : new GLWpfControlTexture2DRenderer(context);

        }

        public static bool SupportsMSAATest(DxGlContext context)
        {
            // A test to see whether we can create multisample render targets without
            // getting an exception...
            try
            {
                IntPtr dxColorRenderTargetShareHandle = IntPtr.Zero;
                context.DxDevice.CreateRenderTarget(
                    128,
                    128,
                    Format.X8R8G8B8,
                    MultisampleType.D3DMULTISAMPLE_2_SAMPLES,
                    0,
                    false,
                    out DXInterop.IDirect3DSurface9 dxColorRenderTarget,
                    ref dxColorRenderTargetShareHandle);

                dxColorRenderTarget.Release();

                return true;
            }
            catch(COMException)
            {
                Trace.TraceWarning("GLWpfControl was unable to create an MSAA framebuffer on this computer.");
                return false;
            }
        }
    }
}