using System.Runtime.InteropServices;

namespace TheMaths
{
    public static class Utilities
    {
        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }

        public static void MinMax(this float f, ref float min, ref float max)
        {
            if (f < min)
                min = f;
            if (f > max)
                max = f;
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}