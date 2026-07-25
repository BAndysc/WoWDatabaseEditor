namespace SPB.Windowing
{
    public class NativeHandle
    {
        public IntPtr RawHandle { get; }

        public NativeHandle(IntPtr rawHandle)
        {
            RawHandle = rawHandle;
        }
    }
}