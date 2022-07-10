using TheAvaloniaOpenGL.Resources;
using TheEngine.ECS;
using TheEngine.Entities;
using TheMaths;

namespace TheEngine.Components
{
    public class MaterialInstanceRenderData : IManagedComponentData
    {
        public Dictionary<string, INativeBuffer>? bufferByName { get; private set; }
        public Dictionary<int, int>? ints { get; private set; }
        public Dictionary<int, int>? instancedInts { get; private set; }
        public Dictionary<int, INativeBuffer>? structuredBuffers { get; private set; }
        public Dictionary<int, INativeBuffer>? instancedStructuredBuffers { get; private set; }
        
        public INativeBuffer? GetBuffer(string name)
        {
            if (bufferByName != null && bufferByName.TryGetValue(name, out var buf))
                return buf;
            return null;
        }
        
        public void SetBuffer(Material material, string name, INativeBuffer buffer)
        {
            SetInstancedBuffer(material, name, buffer);
            var loc = material.GetUniformLocation(name);
            if (loc == -1)
                return;
            bufferByName ??= new();
            bufferByName[name] = buffer;
            structuredBuffers ??= new();
            structuredBuffers[loc] = buffer;
        }
        
        internal void SetInstancedBuffer(Material material, string name, INativeBuffer buffer)
        {
            var instancedLoc = material.GetInstancedUniformLocation(name);
            if (instancedLoc.HasValue && instancedLoc != -1)
            {
                instancedStructuredBuffers ??= new();
                instancedStructuredBuffers[instancedLoc.Value] = buffer;
            }
        }

        public void SetInt(Material material, string name, int value)
        {
            var loc = material.GetUniformLocation(name);
            var instancedLoc = material.GetInstancedUniformLocation(name);
            if (loc != -1)
            {
                ints ??= new();
                ints[loc] = value;
            }
            if (instancedLoc.HasValue && instancedLoc != -1)
            {
                instancedInts ??= new();
                instancedInts[instancedLoc.Value] = value;
            }
        }
        
        public void Activate(Material material, bool instanced, int slot)
        {
            if (instanced)
            {
                if (instancedInts != null)
                {
                    foreach (var i in instancedInts)
                        material.Shader.SetUniformInt(i.Key, i.Value);
                }

                if (instancedStructuredBuffers != null)
                {
                    foreach (var buffer in instancedStructuredBuffers)
                    {
                        buffer.Value.Activate(slot);
                        material.Shader.SetUniformInt(buffer.Key, slot);
                        slot++;
                    }   
                }
            }
            else
            {
                if (ints != null)
                {
                    foreach (var i in ints)
                        material.Shader.SetUniformInt(i.Key, i.Value);
                }
                
                if (structuredBuffers != null)
                {
                    foreach (var buffer in structuredBuffers)
                    {
                        buffer.Value.Activate(slot);
                        material.Shader.SetUniformInt(buffer.Key, slot);
                        slot++;
                    }
                }
            }
        }

        public void Clear()
        {
            ints?.Clear();
            instancedInts?.Clear();
            instancedStructuredBuffers?.Clear();
            structuredBuffers?.Clear();
            bufferByName?.Clear();
        }
    }

    public struct ShareRenderEnabledBit : IComponentData
    {
        public Entity OtherEntity;
    }

    public struct RenderEnabledBit : IComponentData
    {
        // bit 0 - is actually enabled
        // bit 1 - is force disabled
        private byte enabled;

        private RenderEnabledBit(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public void Enable()
        {
            enabled |= 0b01;
        }

        public void Disable()
        {
            enabled &= 0b10;
        }

        public bool IsForceDisabled()
        {
            return (enabled & 0b10) == 0b10;
        }

        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                enabled |= 0b10;
            }
            else
            {
                enabled &= 0b1;
            }
        }

        public static implicit operator bool(RenderEnabledBit d) => d.enabled == 1;
        public static explicit operator RenderEnabledBit(bool b) => new RenderEnabledBit(b);
    }

    public static class RenderEnabledBitExtensions
    {
        public static void SetForceDisabledRendering(this Entity entity, IEntityManager entityManager, bool disabled)
        {
            entityManager.GetComponent<RenderEnabledBit>(entity).SetDisabled(disabled);
        }
    }
    
    public struct PerformCullingBit : IComponentData
    {
    }
}