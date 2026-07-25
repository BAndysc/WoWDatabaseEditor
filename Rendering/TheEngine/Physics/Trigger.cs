using TheEngine.ECS;

namespace TheEngine.Physics
{
    /// <summary>
    /// Tag component: the collidable still reports touch/overlap events but generates
    /// no contact response (sensor), Unity-style.
    /// </summary>
    public struct Trigger : IComponentData
    {
    }
}
