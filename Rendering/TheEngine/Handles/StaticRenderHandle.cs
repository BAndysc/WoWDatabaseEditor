using TheEngine.ECS;

namespace TheEngine.Handles
{
    public struct StaticRenderHandle
    {
        internal Entity Handle { get; }

        internal StaticRenderHandle(Entity id)
        {
            Handle = id;
        }
    }
    
    public struct DynamicRenderHandle
    {
        internal Entity Handle { get; }

        internal DynamicRenderHandle(Entity id)
        {
            Handle = id;
        }
    }
    
}
