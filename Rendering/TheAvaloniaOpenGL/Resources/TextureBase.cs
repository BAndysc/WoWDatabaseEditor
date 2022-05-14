using System;
using OpenGLBindings;

namespace TheAvaloniaOpenGL.Resources
{
    public abstract class TextureBase : ITexture
    {
        private readonly IDevice device;
        private readonly TextureTarget textureTarget;
        public int Width { get; }
        public int Height { get; }

        internal readonly int Handle;

        protected static PixelInternalFormat ToInternalFormat(TextureFormat textureFormat)
        {
            switch (textureFormat)
            {
                case TextureFormat.R8G8B8A8:
                    return PixelInternalFormat.Rgba;
                case TextureFormat.R32f:
                    return PixelInternalFormat.R32f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(textureFormat), textureFormat, null);
            }
        }

        protected TextureBase(IDevice device, int width, int height, TextureTarget textureTarget)
        {
            this.device = device;
            this.textureTarget = textureTarget;
            Width = width;
            Height = height;

            Handle = device.GenTexture();
            BindTexture();
        }
    
        protected void BindTexture()
        {
            device.BindTexture(textureTarget, Handle);
        }

        protected internal void GenerateMipmaps()
        {
            BindTexture();
            device.GenerateMipmap(textureTarget);
        }

        protected void UnbindTexture()
        {
            device.BindTexture(textureTarget, 0);
        }
    
        public void Activate(int slot)
        {
            device.ActiveTextureUnit(slot);
            BindTexture();
        }

        public virtual void SetFiltering(FilteringMode mode)
        {
            device.BindTexture(textureTarget, Handle);
            device.TexParameteri(textureTarget, TextureParameterName.TextureMinFilter, mode == FilteringMode.Nearest ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.LinearMipmapLinear);
            device.TexParameteri(textureTarget, TextureParameterName.TextureMagFilter, mode == FilteringMode.Nearest ? (int)TextureMagFilter.Nearest : (int)TextureMagFilter.Linear);
            device.BindTexture(textureTarget, 0);
        }   

        public virtual void SetWrapping(WrapMode mode)
        {
            TextureWrapMode openGlMode = TextureWrapMode.Repeat;
            switch (mode)
            {
                case WrapMode.ClampToEdge:
                    openGlMode = TextureWrapMode.ClampToEdge;
                    break;
                case WrapMode.ClampToBorder:
                    openGlMode = TextureWrapMode.ClampToBorder;
                    break;
                case WrapMode.Repeat:
                    openGlMode = TextureWrapMode.Repeat;
                    break;
                case WrapMode.MirroredRepeat:
                    openGlMode = TextureWrapMode.MirroredRepeat;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        
            device.BindTexture(textureTarget, Handle);
            device.TexParameteri(textureTarget, TextureParameterName.TextureWrapS, (int)openGlMode);
            device.TexParameteri(textureTarget, TextureParameterName.TextureWrapT, (int)openGlMode);
            device.BindTexture(textureTarget, 0);
        }
    
        public void Dispose()
        {
            device.DeleteTexture(Handle);
        }
    }
}