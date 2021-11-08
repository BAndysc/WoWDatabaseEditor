using TheEngine.ECS;

namespace TheEngine.Components
{
    public struct RenderEnabledBit : IComponentData
    {
        private byte enabled;

        private RenderEnabledBit(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public void Enable()
        {
            enabled = 1;
        }

        public void Disable()
        {
            enabled = 0;
        }

        public static implicit operator bool(RenderEnabledBit d) => d.enabled == 1;
        public static explicit operator RenderEnabledBit(bool b) => new RenderEnabledBit(b);
    }
}