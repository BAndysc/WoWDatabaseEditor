using System.Runtime.InteropServices;

namespace TheMaths
{
    public class Utilities
    {
        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }
    }
}