using System.Buffers;
using OpenGLBindings;
using SixLabors.ImageSharp.PixelFormats;

namespace TheAvaloniaOpenGL.Resources
{
    public class TextureArray : TextureBase
    {
        internal TextureArray(IDevice device, int width, int height, Rgba32[][][] pixels)
        :base(device, width, height, TextureTarget.Texture2DArray)
        {
            int mipsCount = pixels[0].Length;
            int texturesCount = pixels.Length;
            var data = ArrayPool<Rgba32>.Shared.Rent(width * height * texturesCount);
            for (int i = 0; i < mipsCount; ++i)
            {
                for (int j = 0; j < texturesCount; ++j)
                {
                    var jThTexiThMip = pixels[j][i];
                    Array.Copy(jThTexiThMip, 0, data, j * width * height, width * height);
                }

                unsafe
                {
                    fixed (Rgba32* dataPtr = data)
                        device.TexImage3D(TextureTarget.Texture2DArray, i, InternalFormat.Rgba, width, height, texturesCount, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr(dataPtr));
                    device.CheckError("setting array");
                }

                width /= 2;
                height /= 2;
            }
            ArrayPool<Rgba32>.Shared.Return(data);
            device.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMaxLevel, mipsCount - 1);
            

            UnbindTexture();
        }
    }
}
