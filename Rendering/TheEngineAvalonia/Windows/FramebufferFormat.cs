namespace SPB.Graphics
{
    public struct FramebufferFormat
    {
        public ColorFormat Color { get; }
        public ColorFormat Accumulator { get; }
        public uint Samples { get; }
        public byte DepthBits { get; }
        public byte StencilBits { get; }
        public bool Stereo { get; }
        public uint Buffers { get; }

        public static FramebufferFormat Default => new FramebufferFormat(new ColorFormat(8, 8, 8, 8), 16, 0, ColorFormat.Zero, 0, 2, false);

        public FramebufferFormat(ColorFormat color, byte depthBits, byte stencilBits, ColorFormat accumulator, uint samples, uint buffers, bool stereo)
        {
            Color = color;
            DepthBits = depthBits;
            StencilBits = stencilBits;
            Accumulator = accumulator;
            Samples = samples;
            Buffers = buffers;
            Stereo = stereo;
        }
    }
}