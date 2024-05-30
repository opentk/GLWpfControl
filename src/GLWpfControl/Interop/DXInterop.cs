using OpenTK.Graphics.OpenGL;
using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;
using System.Windows.Interop;

namespace OpenTK.Wpf.Interop
{
    internal static class DXInterop
    {
        // We disable this so we can do struct _VTable
#pragma warning disable IDE1006 // Naming Styles

        public const uint DefaultSdkVersion = 32;

        [DllImport("Kernel32.dll")]
        public static extern int GetLastError();

        public static void Direct3DCreate9Ex(uint SdkVersion, out IDirect3D9Ex context)
        {
            int result = Direct3DCreate9Ex(SdkVersion, out context);
            CheckHResult(result);

            [DllImport("d3d9.dll")]
            static extern int Direct3DCreate9Ex(uint SdkVersion, out IDirect3D9Ex ctx);
        }

        private delegate int NativeCreateDeviceEx(IDirect3D9Ex contextHandle, int adapter, DeviceType deviceType, IntPtr focusWindowHandle, CreateFlags behaviorFlags, ref PresentationParameters presentationParameters, IntPtr fullscreenDisplayMode, out IDirect3DDevice9Ex deviceHandle);
        private delegate int NativeCreateRenderTarget(IDirect3DDevice9Ex deviceHandle, int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool lockable, out IDirect3DSurface9 surfaceHandle, ref IntPtr sharedHandle);
        private delegate int NativeCreateDepthStencilSurface(IDirect3DDevice9Ex deviceHandle, int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool discard, out IDirect3DSurface9 surfaceHandle, ref IntPtr sharedHandle);
        private delegate uint NativeRelease(IntPtr resourceHandle);

        private delegate uint NativeGetDesc(IDirect3DSurface9 surfaceHandle, out D3DSURFACE_DESC pDesc);

        public static void CheckHResult(int hresult)
        {
            Marshal.ThrowExceptionForHR(hresult);
        }

        public static readonly Guid IID_IDirect3D9Ex = new Guid(0x02177241, 0x69FC, 0x400C, 0x8F, 0xF1, 0x93, 0xA4, 0x4D, 0xF6, 0x86, 0x1D);

        public unsafe struct IUnknown
        {
#pragma warning disable CS0649
            public struct _VTable
            {
                public IntPtr QueryInterface;
                public IntPtr AddRef;
                public IntPtr Release;
            }
#pragma warning restore CS0649

            public _VTable** VTable;

            public readonly IntPtr Handle => (IntPtr)VTable;

            // FIXME: This is only temporary while we have COM objects refered to by IntPtr
            public static explicit operator IUnknown(IntPtr ptr) => new IUnknown() { VTable = (_VTable**)ptr };

            public uint Release()
            {
                NativeRelease method = Marshal.GetDelegateForFunctionPointer<NativeRelease>((*VTable)->Release);
                // FIXME: Figure out how we want to reference things
                return method((IntPtr)VTable);
            }
        }

        public unsafe struct IDirect3D9Ex
        {
#pragma warning disable CS0649
            public struct _VTable
            {
                public IntPtr QueryInterface;
                public IntPtr AddRef;
                public IntPtr Release;
                public IntPtr RegisterSoftwareDevice;
                public IntPtr GetAdapterCount;
                public IntPtr GetAdapterIdentifier;
                public IntPtr GetAdapterModeCount;
                public IntPtr EnumAdapterModes;
                public IntPtr GetAdapterDisplayMode;
                public IntPtr CheckDeviceType;
                public IntPtr CheckDeviceFormat;
                public IntPtr CheckDeviceMultiSampleType;
                public IntPtr CheckDepthStencilMatch;
                public IntPtr CheckDeviceFormatConversion;
                public IntPtr GetDeviceCaps;
                public IntPtr GetAdapterMonitor;
                public IntPtr CreateDevice;
                public IntPtr GetAdapterModeCountEx;
                public IntPtr EnumAdapterModesEx;
                public IntPtr GetAdapterDisplayModeEx;
                public IntPtr CreateDeviceEx;
                public IntPtr GetAdapterLUID;
            }

            public _VTable** VTable;
#pragma warning restore CS0649

            public readonly IntPtr Handle => (IntPtr)VTable;

            public static explicit operator IDirect3D9Ex(IntPtr ptr) => new IDirect3D9Ex() { VTable = (_VTable**)ptr };

            public uint Release()
            {
                NativeRelease method = Marshal.GetDelegateForFunctionPointer<NativeRelease>((*VTable)->Release);
                // FIXME: Figure out how we want to reference things
                return method((IntPtr)VTable);
            }

            public void CreateDeviceEx(int adapter, DeviceType deviceType, IntPtr focusWindowHandle, CreateFlags behaviorFlags, ref PresentationParameters presentationParameters, IntPtr fullscreenDisplayMode, out IDirect3DDevice9Ex deviceHandle)
            {
                NativeCreateDeviceEx method = Marshal.GetDelegateForFunctionPointer<NativeCreateDeviceEx>((*VTable)->CreateDeviceEx);

                int result = method(this, adapter, deviceType, focusWindowHandle, behaviorFlags, ref presentationParameters, fullscreenDisplayMode, out deviceHandle);

                CheckHResult(result);
            }
        }

        public unsafe struct IDirect3DDevice9Ex
        {
#pragma warning disable CS0649
            public struct _VTable
            {
                /*** IUnknown methods ***/
                public IntPtr QueryInterface;
                public IntPtr  AddRef;
                public IntPtr  Release;

                /*** IDirect3DDevice9 methods ***/
                public IntPtr TestCooperativeLevel;
                public IntPtr GetAvailableTextureMem;
                public IntPtr EvictManagedResources;
                public IntPtr GetDirect3D;
                public IntPtr GetDeviceCaps;
                public IntPtr GetDisplayMode;
                public IntPtr GetCreationParameters;
                public IntPtr SetCursorProperties;
                public IntPtr SetCursorPosition;
                public IntPtr ShowCursor;
                public IntPtr CreateAdditionalSwapChain;
                public IntPtr GetSwapChain;
                public IntPtr GetNumberOfSwapChains;
                public IntPtr Reset;
                public IntPtr Present;
                public IntPtr GetBackBuffer;
                public IntPtr GetRasterStatus;
                public IntPtr SetDialogBoxMode;
                public IntPtr SetGammaRamp;
                public IntPtr GetGammaRamp;
                public IntPtr CreateTexture;
                public IntPtr CreateVolumeTexture;
                public IntPtr CreateCubeTexture;
                public IntPtr CreateVertexBuffer;
                public IntPtr CreateIndexBuffer;
                public IntPtr CreateRenderTarget;
                public IntPtr CreateDepthStencilSurface;
                public IntPtr UpdateSurface;
                public IntPtr UpdateTexture;
                public IntPtr GetRenderTargetData;
                public IntPtr GetFrontBufferData;
                public IntPtr StretchRect;
                public IntPtr ColorFill;
                public IntPtr CreateOffscreenPlainSurface;
                public IntPtr SetRenderTarget;
                public IntPtr GetRenderTarget;
                public IntPtr SetDepthStencilSurface;
                public IntPtr GetDepthStencilSurface;
                public IntPtr BeginScene;
                public IntPtr EndScene;
                public IntPtr Clear;
                public IntPtr SetTransform;
                public IntPtr GetTransform;
                public IntPtr MultiplyTransform;
                public IntPtr SetViewport;
                public IntPtr GetViewport;
                public IntPtr SetMaterial;
                public IntPtr GetMaterial;
                public IntPtr SetLight;
                public IntPtr GetLight;
                public IntPtr LightEnable;
                public IntPtr GetLightEnable;
                public IntPtr SetClipPlane;
                public IntPtr GetClipPlane;
                public IntPtr SetRenderState;
                public IntPtr GetRenderState;
                public IntPtr CreateStateBlock;
                public IntPtr BeginStateBlock;
                public IntPtr EndStateBlock;
                public IntPtr SetClipStatus;
                public IntPtr GetClipStatus;
                public IntPtr GetTexture;
                public IntPtr SetTexture;
                public IntPtr GetTextureStageState;
                public IntPtr SetTextureStageState;
                public IntPtr GetSamplerState;
                public IntPtr SetSamplerState;
                public IntPtr ValidateDevice;
                public IntPtr SetPaletteEntries;
                public IntPtr GetPaletteEntries;
                public IntPtr SetCurrentTexturePalette;
                public IntPtr GetCurrentTexturePalette;
                public IntPtr SetScissorRect;
                public IntPtr GetScissorRect;
                public IntPtr SetSoftwareVertexProcessing;
                public IntPtr GetSoftwareVertexProcessing;
                public IntPtr SetNPatchMode;
                public IntPtr GetNPatchMode;
                public IntPtr DrawPrimitive;
                public IntPtr DrawIndexedPrimitive;
                public IntPtr DrawPrimitiveUP;
                public IntPtr DrawIndexedPrimitiveUP;
                public IntPtr ProcessVertices;
                public IntPtr CreateVertexDeclaration;
                public IntPtr SetVertexDeclaration;
                public IntPtr GetVertexDeclaration;
                public IntPtr SetFVF;
                public IntPtr GetFVF;
                public IntPtr CreateVertexShader;
                public IntPtr SetVertexShader;
                public IntPtr GetVertexShader;
                public IntPtr SetVertexShaderConstantF;
                public IntPtr GetVertexShaderConstantF;
                public IntPtr SetVertexShaderConstantI;
                public IntPtr GetVertexShaderConstantI;
                public IntPtr SetVertexShaderConstantB;
                public IntPtr GetVertexShaderConstantB;
                public IntPtr SetStreamSource;
                public IntPtr GetStreamSource;
                public IntPtr SetStreamSourceFreq;
                public IntPtr GetStreamSourceFreq;
                public IntPtr SetIndices;
                public IntPtr GetIndices;
                public IntPtr CreatePixelShader;
                public IntPtr SetPixelShader;
                public IntPtr GetPixelShader;
                public IntPtr SetPixelShaderConstantF;
                public IntPtr GetPixelShaderConstantF;
                public IntPtr SetPixelShaderConstantI;
                public IntPtr GetPixelShaderConstantI;
                public IntPtr SetPixelShaderConstantB;
                public IntPtr GetPixelShaderConstantB;
                public IntPtr DrawRectPatch;
                public IntPtr DrawTriPatch;
                public IntPtr DeletePatch;
                public IntPtr CreateQuery;
                public IntPtr SetConvolutionMonoKernel;
                public IntPtr ComposeRects;
                public IntPtr PresentEx;
                public IntPtr GetGPUThreadPriority;
                public IntPtr SetGPUThreadPriority;
                public IntPtr WaitForVBlank;
                public IntPtr CheckResourceResidency;
                public IntPtr SetMaximumFrameLatency;
                public IntPtr GetMaximumFrameLatency;
                public IntPtr CheckDeviceState;
                public IntPtr CreateRenderTargetEx;
                public IntPtr CreateOffscreenPlainSurfaceEx;
                public IntPtr CreateDepthStencilSurfaceEx;
                public IntPtr ResetEx;
                public IntPtr GetDisplayModeEx;
            }

            public _VTable** VTable;
#pragma warning restore CS0649

            public readonly IntPtr Handle => (IntPtr)VTable;

            public uint Release()
            {
                NativeRelease method = Marshal.GetDelegateForFunctionPointer<NativeRelease>((*VTable)->Release);
                // FIXME: Figure out how we want to reference things
                return method((IntPtr)VTable);
            }

            public void CreateRenderTarget(int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool lockable, out IDirect3DSurface9 surfaceHandle, ref IntPtr sharedHandle)
            {
                NativeCreateRenderTarget method = Marshal.GetDelegateForFunctionPointer<NativeCreateRenderTarget>((*VTable)->CreateRenderTarget);

                int result = method(this, width, height, format, multisample, multisampleQuality, lockable, out surfaceHandle, ref sharedHandle);

                CheckHResult(result);
            }

            public void CreateDepthStencilSurface(int width, int height, Format format, MultisampleType multisample, int multisampleQuality, bool discard, out IDirect3DSurface9 surfaceHandle, ref IntPtr sharedHandle)
            {
                NativeCreateDepthStencilSurface method = Marshal.GetDelegateForFunctionPointer<NativeCreateDepthStencilSurface>((*VTable)->CreateDepthStencilSurface);

                int result = method(this, width, height, format, multisample, multisampleQuality, discard, out surfaceHandle, ref sharedHandle);

                CheckHResult(result);
            }
        }

#pragma warning disable CS0649
        public struct D3DSURFACE_DESC
        {
            public D3DFormat Format;
            public D3DResourceType Type;
            public D3DUsage Usage;
            public D3DPool Pool;
            public MultisampleType MultiSampleType;
            public uint MultiSampleQuality;
            public uint Width;
            public uint Height;
        }
#pragma warning restore CS0649

        public unsafe struct IDirect3DSurface9
        {
#pragma warning disable CS0649
            public struct _VTable
            {
                /*** IUnknown methods ***/
                public IntPtr QueryInterface;
                public IntPtr AddRef;
                public IntPtr Release;

                /*** IDirect3DResource9 methods ***/
                public IntPtr GetDevice;
                public IntPtr SetPrivateData;
                public IntPtr GetPrivateData;
                public IntPtr FreePrivateData;
                public IntPtr SetPriority;
                public IntPtr GetPriority;
                public IntPtr PreLoad;
                public new IntPtr GetType;
                public IntPtr GetContainer;
                public IntPtr GetDesc;
                public IntPtr LockRect;
                public IntPtr UnlockRect;
                public IntPtr GetDC;
                public IntPtr ReleaseDC;
            }

            public _VTable** VTable;
#pragma warning restore CS0649

            public readonly IntPtr Handle => (IntPtr)VTable;

            public uint GetDesc(out D3DSURFACE_DESC pDesc)
            {
                NativeGetDesc method = Marshal.GetDelegateForFunctionPointer<NativeGetDesc>((*VTable)->GetDesc);

                return method(this, out pDesc);
            }

            public uint Release()
            {
                NativeRelease method = Marshal.GetDelegateForFunctionPointer<NativeRelease>((*VTable)->Release);
                // FIXME: Figure out how we want to reference things
                return method((IntPtr)VTable);
            }
        }
    }
}
