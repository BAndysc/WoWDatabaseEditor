using TheEngine.ECS;

namespace TheEngine.Handles
{
    public struct StaticRenderHandle
    {
        public Entity Handle { get; }

        internal StaticRenderHandle(Entity id)
        {
            Handle = id;
        }
    }
    
    public struct DynamicRenderHandle
    {
        public Entity Handle { get; }

        internal DynamicRenderHandle(Entity id)
        {
            Handle = id;
        }
    }
    
}
