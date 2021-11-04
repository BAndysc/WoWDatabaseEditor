
namespace TheAvaloniaOpenGL.Resources
{
    public class TextureCube : IDisposable, ITexture
    {
        private readonly IDevice device;

        //internal ShaderResourceView TextureResource { get; }

        public int Width { get; }

        public int Height { get; }

        //private Texture2D texture { get; }

        internal TextureCube(IDevice device, int[][] pixels, int width, int height)
        {
            this.device = device;
            Width = width;
            Height = height;

            int stride = Width * 4;

            throw new Exception();
            /*List<SharpDX.DataBox> rectangles = new List<SharpDX.DataBox>();
            unsafe
            {
                foreach (var mip in pixels)
                {
                    fixed (int* p = mip)
                    {
                        IntPtr pp = (IntPtr)p;
                        rectangles.Add(new SharpDX.DataBox(pp, stride, 0));
                    }
                }
            }

            texture = new Texture2D(device, new Texture2DDescription()
            {
                Width = Width,
                Height = Height,
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource,
                Usage = ResourceUsage.Default,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
            }, rectangles.ToArray());

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription()
            {
                Format = texture.Description.Format,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.TextureCube,
            };
            srvDesc.TextureCube.MostDetailedMip = 0;
            srvDesc.TextureCube.MipLevels = 1;

            TextureResource = new ShaderResourceView(device, texture, srvDesc);*/
        }

        // Call from render thread only
        public void Activate(int slot)
        {
            throw new Exception();
            //device.ImmediateContext.PixelShader.SetShaderResource(slot, TextureResource);
        }

        public void SetFiltering(FilteringMode mode)
        {
            throw new Exception();
        }

        public void SetWrapping(WrapMode mode)
        {
            throw new Exception();
        }

        public void Dispose()
        {
            //TextureResource.Dispose();
            //texture.Dispose();
        }
    }
}
