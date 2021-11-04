namespace TheEngine.Handles
{
    public struct ShaderHandle
    {
        internal int Handle { get; }

        internal ShaderHandle(int id)
        {
            Handle = id;
        }
    }
}
