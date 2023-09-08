namespace SPB.Graphics
{
    public struct ColorFormat
    {
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }

        public int BitsPerPixel => Red + Green + Blue + Alpha;

        public static ColorFormat Zero => new ColorFormat(0, 0, 0, 0);

        public ColorFormat(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
    }
}