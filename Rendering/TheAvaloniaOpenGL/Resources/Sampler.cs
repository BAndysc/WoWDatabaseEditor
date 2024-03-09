namespace TheAvaloniaOpenGL.Resources
{
    public class Sampler : IDisposable
    {
        private readonly IDevice device;
        //private SamplerState sampler;
        //private int sampler;

        internal Sampler(IDevice device)
        {
            //sampler = device.GenSampler();
            /*SamplerStateDescription samplerDesc = new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLodBias = 0,
                MaximumAnisotropy = 1,
                ComparisonFunction = Comparison.Always,
                BorderColor = new Color4(1, 0, 0, 1),
                MinimumLod = 0,
                MaximumLod = float.MaxValue
            };
            sampler = new SamplerState(device, samplerDesc);*/

            this.device = device;
        }

        // Call from render thread only
        public void Activate(int slot)
        {
            throw new Exception();
            //device.BindSampler(slot, sampler);
            //device.ImmediateContext.PixelShader.SetSampler(slot, sampler);
        }

        public void Dispose()
        {
            //sampler.Dispose();
        }
    }
}
