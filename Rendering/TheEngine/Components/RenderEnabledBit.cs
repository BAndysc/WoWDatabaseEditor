using TheAvaloniaOpenGL.Resources;
using TheEngine.ECS;
using TheEngine.Entities;

namespace TheEngine.Components
{
    public class MaterialInstanceRenderData : IManagedComponentData
    {
        public Dictionary<int, INativeBuffer>? structuredBuffers { get; private set; }

        public void SetBuffer(Material material, string name, INativeBuffer buffer)
        {
            var loc = material.GetUniformLocation(name);
            if (loc == -1)
                return;
            structuredBuffers ??= new();
            structuredBuffers[loc] = buffer;
        }

        public void Activate(Material material, int slot)
        {
            if (structuredBuffers == null)
                return;
            
            foreach (var buffer in structuredBuffers)
            {
                buffer.Value.Activate(slot);
                material.Shader.SetUniformInt(buffer.Key, slot);
                slot++;
            }
        }
    }
    
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