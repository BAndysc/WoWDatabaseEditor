using System;
using OpenGLBindings;
using SixLabors.ImageSharp.PixelFormats;
using TheMaths;

namespace TheAvaloniaOpenGL.Resources
{
    public sealed class Texture : TextureBase
    {
        internal unsafe Texture(IDevice device, uint[]? pixels, int width, int height, TextureFormat textureFormat = TextureFormat.R8G8B8A8)
            : base(device, width, height, TextureTarget.Texture2D)
        {
            ToInternalFormat(textureFormat, out var internalFormat, out var pixelFormat, out var pixelType);
            if (pixels == null)
            {
                device.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, new IntPtr(0));
            }
            else
            {
                fixed (void* pData = pixels)
                {
                    device.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, new IntPtr(pData));
                }   
            }
            GenerateMipmaps();
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
    }
}
