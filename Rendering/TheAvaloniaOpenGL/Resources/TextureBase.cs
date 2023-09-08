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

        protected static void ToInternalFormat(TextureFormat textureFormat, out PixelInternalFormat internalFormat, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            
            switch (textureFormat)
            {
                case TextureFormat.R8G8B8A8:
                    internalFormat = PixelInternalFormat.Rgba;
                    pixelFormat = PixelFormat.Rgba;
                    pixelType = PixelType.UnsignedByte;
                    break;
                case TextureFormat.R32f:
                    internalFormat = PixelInternalFormat.R32f;
                    pixelFormat = PixelFormat.Red;
                    pixelType = PixelType.Float;
                    break;
                case TextureFormat.R32ui:
                    internalFormat = PixelInternalFormat.R32ui;
                    pixelFormat = PixelFormat.RedInteger;
                    pixelType = PixelType.UnsignedInt;
                    break;
                case TextureFormat.DepthComponent:
                    internalFormat = PixelInternalFormat.DepthComponent32f;
                    pixelFormat = PixelFormat.DepthComponent;
                    pixelType = PixelType.Float;
                    break;
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

        public static TextureBase[] activeTextures = new TextureBase[32];

        public void Activate(int slot)
        {
            //if (activeTextures[slot] == this)
            //    return;
            //activeTextures[slot] = this;
            device.ActiveTextureUnit(slot);
            BindTexture();
        }

        public virtual void SetFiltering(FilteringMode mode)
        {
            BindTexture();
            device.TexParameteri(textureTarget, TextureParameterName.TextureMinFilter, mode == FilteringMode.Nearest ? (int)TextureMinFilter.NearestMipmapNearest : (int)TextureMinFilter.LinearMipmapLinear);
            device.TexParameteri(textureTarget, TextureParameterName.TextureMagFilter, mode == FilteringMode.Nearest ? (int)TextureMagFilter.Nearest : (int)TextureMagFilter.Linear);
            UnbindTexture();
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
        
            BindTexture();
            device.TexParameteri(textureTarget, TextureParameterName.TextureWrapS, (int)openGlMode);
            device.TexParameteri(textureTarget, TextureParameterName.TextureWrapT, (int)openGlMode);
            UnbindTexture();
        }
    
        public void Dispose()
        {
            device.DeleteTexture(Handle);
        }
    }
}