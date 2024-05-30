using System;

namespace OpenTK.Wpf.Interop
{
    [Flags]
    internal enum CreateFlags : uint
    {
        FpuPreserve = 2,
        Multithreaded = 4,
        PureDevice = 16,
        HardwareVertexProcessing = 64,
    }

    internal enum DeviceType
    {
        /// <summary>
        /// Hardware rasterization. Shading is done with software, hardware, or mixed transform and lighting.
        /// </summary>
        HAL = 1,
    }

    internal enum D3DFormat
    {
        D3DFMT_UNKNOWN = 0,

        D3DFMT_R8G8B8 = 20,
        D3DFMT_A8R8G8B8 = 21,
        D3DFMT_X8R8G8B8 = 22,
        D3DFMT_R5G6B5 = 23,
        D3DFMT_X1R5G5B5 = 24,
        D3DFMT_A1R5G5B5 = 25,
        D3DFMT_A4R4G4B4 = 26,
        D3DFMT_R3G3B2 = 27,
        D3DFMT_A8 = 28,
        D3DFMT_A8R3G3B2 = 29,
        D3DFMT_X4R4G4B4 = 30,
        D3DFMT_A2B10G10R10 = 31,
        D3DFMT_A8B8G8R8 = 32,
        D3DFMT_X8B8G8R8 = 33,
        D3DFMT_G16R16 = 34,
        D3DFMT_A2R10G10B10 = 35,
        D3DFMT_A16B16G16R16 = 36,

        D3DFMT_A8P8 = 40,
        D3DFMT_P8 = 41,

        D3DFMT_L8 = 50,
        D3DFMT_A8L8 = 51,
        D3DFMT_A4L4 = 52,

        D3DFMT_V8U8 = 60,
        D3DFMT_L6V5U5 = 61,
        D3DFMT_X8L8V8U8 = 62,
        D3DFMT_Q8W8V8U8 = 63,
        D3DFMT_V16U16 = 64,
        D3DFMT_A2W10V10U10 = 67,

        D3DFMT_UYVY = 'U' | 'Y' << 8 | 'V' << 16 | 'Y' << 24,
        D3DFMT_R8G8_B8G8 = 'R' | 'G' << 8 | 'B' << 16 | 'G' << 24,
        D3DFMT_YUY2 = 'Y' | 'U' << 8 | 'Y' << 16 | '2' << 24,
        D3DFMT_G8R8_G8B8 = 'G' | 'R' << 8 | 'G' << 16 | 'B' << 24,
        D3DFMT_DXT1 = 'D' | 'X' << 8 | 'T' << 16 | '1' << 24,
        D3DFMT_DXT2 = 'D' | 'X' << 8 | 'T' << 16 | '2' << 24,
        D3DFMT_DXT3 = 'D' | 'X' << 8 | 'T' << 16 | '3' << 24,
        D3DFMT_DXT4 = 'D' | 'X' << 8 | 'T' << 16 | '4' << 24,
        D3DFMT_DXT5 = 'D' | 'X' << 8 | 'T' << 16 | '5' << 24,

        D3DFMT_D16_LOCKABLE = 70,
        D3DFMT_D32 = 71,
        D3DFMT_D15S1 = 73,
        D3DFMT_D24S8 = 75,
        D3DFMT_D24X8 = 77,
        D3DFMT_D24X4S4 = 79,
        D3DFMT_D16 = 80,

        D3DFMT_D32F_LOCKABLE = 82,
        D3DFMT_D24FS8 = 83,

        D3DFMT_D32_LOCKABLE = 84,
        D3DFMT_S8_LOCKABLE = 85,

        D3DFMT_L16 = 81,

        D3DFMT_VERTEXDATA = 100,
        D3DFMT_INDEX16 = 101,
        D3DFMT_INDEX32 = 102,

        D3DFMT_Q16W16V16U16 = 110,

        D3DFMT_MULTI2_ARGB8 = 'M' | 'E' << 8 | 'T' << 16 | '1' << 24,

        D3DFMT_R16F = 111,
        D3DFMT_G16R16F = 112,
        D3DFMT_A16B16G16R16F = 113,

        D3DFMT_R32F = 114,
        D3DFMT_G32R32F = 115,
        D3DFMT_A32B32G32R32F = 116,

        D3DFMT_CxV8U8 = 117,

        D3DFMT_A1 = 118,
        D3DFMT_A2B10G10R10_XR_BIAS = 119,
        D3DFMT_BINARYBUFFER = 199,

        D3DFMT_FORCE_DWORD = 0x7fffffff
    }

    internal enum Format
    {
        Unknown = 0,
        /// <summary>
        /// 32-bit ARGB pixel format with alpha, using 8 bits per channel.
        /// </summary>
        A8R8G8B8 = 21,
        /// <summary>
        /// 32-bit RGB pixel format, where 8 bits are reserved for each color.
        /// </summary>
        X8R8G8B8 = 22,

        /// <summary>
        /// 32-bit z-buffer bit depth using 24 bits for the depth channel and 8 bits for the stencil channel.
        /// </summary>
        D24S8 = 75,
    }

    internal enum MultisampleType : int
    {
        D3DMULTISAMPLE_NONE = 0,
        D3DMULTISAMPLE_NONMASKABLE = 1,
        D3DMULTISAMPLE_2_SAMPLES = 2,
        D3DMULTISAMPLE_3_SAMPLES = 3,
        D3DMULTISAMPLE_4_SAMPLES = 4,
        D3DMULTISAMPLE_5_SAMPLES = 5,
        D3DMULTISAMPLE_6_SAMPLES = 6,
        D3DMULTISAMPLE_7_SAMPLES = 7,
        D3DMULTISAMPLE_8_SAMPLES = 8,
        D3DMULTISAMPLE_9_SAMPLES = 9,
        D3DMULTISAMPLE_10_SAMPLES = 10,
        D3DMULTISAMPLE_11_SAMPLES = 11,
        D3DMULTISAMPLE_12_SAMPLES = 12,
        D3DMULTISAMPLE_13_SAMPLES = 13,
        D3DMULTISAMPLE_14_SAMPLES = 14,
        D3DMULTISAMPLE_15_SAMPLES = 15,
        D3DMULTISAMPLE_16_SAMPLES = 16,
        D3DMULTISAMPLE_FORCE_DWORD = unchecked((int)0xffffffff),
    }

    internal enum SwapEffect
    {
        Discard = 1,
    }

    internal enum D3DResourceType
    {
        D3DRTYPE_SURFACE = 1,
        D3DRTYPE_VOLUME = 2,
        D3DRTYPE_TEXTURE = 3,
        D3DRTYPE_VOLUMETEXTURE = 4,
        D3DRTYPE_CUBETEXTURE = 5,
        D3DRTYPE_VERTEXBUFFER = 6,
        D3DRTYPE_INDEXBUFFER = 7,
        D3DRTYPE_FORCE_DWORD = 0x7fffffff
    }

    internal enum D3DPool
    {
        D3DPOOL_DEFAULT = 0,
        D3DPOOL_MANAGED = 1,
        D3DPOOL_SYSTEMMEM = 2,
        D3DPOOL_SCRATCH = 3,
        D3DPOOL_FORCE_DWORD = 0x7fffffff
    }

    [Flags]
    internal enum D3DUsage : uint
    {
        /// <summary>
        /// The resource will be a depth stencil buffer. D3DUSAGE_DEPTHSTENCIL can only be used with D3DPOOL_DEFAULT.
        /// </summary>
        D3DUSAGE_DEPTHSTENCIL = 0x00000002,

        /// <summary>
        /// The resource will be a render target. D3DUSAGE_RENDERTARGET can only be used with D3DPOOL_DEFAULT.
        /// </summary>
        D3DUSAGE_RENDERTARGET = 0x00000001,

        D3DUSAGE_DYNAMIC = 0x00000200,
    }
}
