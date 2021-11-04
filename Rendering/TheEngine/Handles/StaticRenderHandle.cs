namespace TheEngine.Handles
{
    public struct StaticRenderHandle
    {
        internal int Handle { get; }

        internal StaticRenderHandle(int id)
        {
            Handle = id;
        }
    }
    
    public struct DynamicRenderHandle
    {
        internal int Handle { get; }

        internal DynamicRenderHandle(int id)
        {
            Handle = id;
        }
    }
}
