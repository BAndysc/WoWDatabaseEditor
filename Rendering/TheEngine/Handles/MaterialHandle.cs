namespace TheEngine.Handles
{
    public struct MaterialHandle
    {
        internal int Handle { get; }

        internal MaterialHandle(int id)
        {
            Handle = id;
        }
    }
}
