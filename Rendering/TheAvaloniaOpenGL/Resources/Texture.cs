using OpenGLBindings;
using SixLabors.ImageSharp.PixelFormats;

namespace TheAvaloniaOpenGL.Resources
{
    public sealed class Texture : TextureBase
    {
        private static byte[] tempBufferEmptyZeroBytes = new byte[0];
        
        internal unsafe Texture(IDevice device, uint[]? pixels, int width, int height, TextureFormat textureFormat = TextureFormat.R8G8B8A8)
            : base(device, width, height, TextureTarget.Texture2D)
        {
            ToInternalFormat(textureFormat, out var internalFormat, out var pixelFormat, out var pixelType);
            var expectedSize = width * height * SizeOf(pixelType) * ComponentsCount(pixelFormat);
            if (pixels == null)
            {
                if (tempBufferEmptyZeroBytes.Length < expectedSize)
                    tempBufferEmptyZeroBytes = new byte[expectedSize];
    
                fixed(void* pData = tempBufferEmptyZeroBytes)
                    device.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, new IntPtr(pData));
            }
            else
            {
                if (pixels.Length * 4 != expectedSize)
                    throw new Exception($"Expected {expectedSize} bytes, got {pixels.Length * 4} bytes.");

                fixed (void* pData = pixels)
                {
                    device.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, new IntPtr(pData));
                }   
            }
            //GenerateMipmaps();
            device.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            UnbindTexture();
        }

        internal unsafe Texture(IDevice device, Rgba32[][]? pixels, int width, int height, bool generateMips)
            :base(device, width, height, TextureTarget.Texture2D)
        {
            if (pixels == null)
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(0));
            }
            else
            {
                for (int mipLevel = 0; mipLevel < pixels.Length; ++mipLevel)
                {
                    fixed (Rgba32* pData = pixels[mipLevel])
                    {
                        device.TexImage2D(TextureTarget.Texture2D, mipLevel, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                            PixelType.UnsignedByte, new IntPtr(pData));
                        width /= 2;
                        height /= 2;
                    }
                }

                if (!generateMips)
                    device.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, pixels.Length - 1);
            }
    
            if (generateMips)
                GenerateMipmaps();
            SetFiltering(FilteringMode.Linear);
            UnbindTexture();
        }
        
        internal unsafe Texture(IDevice device, Rgba32* pixels, int width, int height, bool generateMips)
            :base(device, width, height, TextureTarget.Texture2D)
        {
            if (pixels == null)
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(0));
            }
            else
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(pixels));

                if (!generateMips)
                    device.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            }
    
            if (generateMips)
                GenerateMipmaps();
            SetFiltering(FilteringMode.Linear);
            UnbindTexture();
        }
        
        internal unsafe Texture(IDevice device, float[]? pixels, int width, int height)
            :base (device, width, height, TextureTarget.Texture2D)
        {
            if (pixels == null)
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, width, height, 0, PixelFormat.Red, PixelType.Float, new IntPtr(0));
            }
            else
            {
                fixed (float* pData = pixels)
                {
                    device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, width, height, 0, PixelFormat.Red,
                        PixelType.Float, new IntPtr(pData));
                }
                device.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
                SetFiltering(FilteringMode.Nearest);
            }
            UnbindTexture();
        }
        
        internal unsafe Texture(IDevice device, Vector4[]? pixels, int width, int height)
        :base (device, width, height, TextureTarget.Texture2D)
        {
            if (pixels == null)
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            }
            else
            {
                fixed (Vector4* pData = pixels)
                {
                    device.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba,
                        PixelType.Float, new IntPtr(pData));
                }
                device.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
                SetFiltering(FilteringMode.Nearest);
            }
            UnbindTexture();
        }
        
        
        private static int SizeOf(PixelType pixelType)
        {
            switch (pixelType)
            {
                case PixelType.Byte:
                case PixelType.UnsignedByte:
                    return 1;
                case PixelType.Short:
                case PixelType.UnsignedShort:
                case PixelType.HalfFloat:
                    return 2;
                case PixelType.Int:
                case PixelType.UnsignedInt:
                case PixelType.Float:
                    return 4;
                case PixelType.UnsignedByte332:
                case PixelType.UnsignedShort4444:
                case PixelType.UnsignedShort5551:
                case PixelType.UnsignedInt8888:
                case PixelType.UnsignedInt1010102:
                case PixelType.UnsignedByte233Reversed:
                case PixelType.UnsignedShort565:
                case PixelType.UnsignedShort565Reversed:
                case PixelType.UnsignedShort4444Reversed:
                case PixelType.UnsignedShort1555Reversed:
                case PixelType.UnsignedInt8888Reversed:
                case PixelType.UnsignedInt2101010Reversed:
                case PixelType.UnsignedInt248:
                case PixelType.UnsignedInt10F11F11FRev:
                case PixelType.UnsignedInt5999Rev:
                case PixelType.Float32UnsignedInt248Rev:
                default:
                    throw new ArgumentOutOfRangeException(nameof(pixelType), pixelType, null);
            }
        }
        
        private static int ComponentsCount(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.UnsignedShort:
                case PixelFormat.UnsignedInt:
                case PixelFormat.ColorIndex:
                case PixelFormat.StencilIndex:
                case PixelFormat.DepthComponent:
                case PixelFormat.Red:
                case PixelFormat.Green:
                case PixelFormat.Blue:
                case PixelFormat.Alpha:
                case PixelFormat.Luminance:
                case PixelFormat.LuminanceAlpha:
                case PixelFormat.DepthStencil:
                case PixelFormat.RedInteger:
                case PixelFormat.GreenInteger:
                case PixelFormat.BlueInteger:
                case PixelFormat.AlphaInteger:
                    return 1;
                case PixelFormat.Rg:
                case PixelFormat.RgInteger:
                    return 2;
                case PixelFormat.Rgb:
                case PixelFormat.Bgr:
                case PixelFormat.RgbInteger:
                case PixelFormat.BgrInteger:
                    return 3;
                case PixelFormat.Rgba:
                case PixelFormat.Bgra:
                case PixelFormat.RgbaInteger:
                case PixelFormat.BgraInteger:
                case PixelFormat.CmykExt:
                case PixelFormat.AbgrExt:
                    return 4;
                // no idea
                case PixelFormat.CmykaExt:
                case PixelFormat.Ycrcb422Sgix:
                case PixelFormat.Ycrcb444Sgix:
                case PixelFormat.R5G6B5IccSgix:
                case PixelFormat.R5G6B5A8IccSgix:
                case PixelFormat.Alpha16IccSgix:
                case PixelFormat.Luminance16IccSgix:
                case PixelFormat.Luminance16Alpha8IccSgix:
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
