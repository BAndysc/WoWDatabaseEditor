using TheAvaloniaOpenGL.Resources;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
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
        public Int4 InstanceData { get; set; }

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
        // bit 0 - is actually enabled (by the engine, i.e. not culled)
        // bit 1 - is force disabled (by the user)

        // bit 2-7: layer number
        private byte enabled;

        private RenderEnabledBit(bool b)
        {
            enabled = b ? (byte)1 : (byte)0;
        }

        public byte Layer
        {
            get => (byte)(enabled >> 2);
            set
            {
                if (value > 0b111111)
                    throw new Exception("Only layer between 0 and " + 0b111111 + " are supportd");
                enabled = (byte)(enabled & 0b11 | (value << 2));
            }
        }

        public bool IsForceDisabled()
        {
            return (enabled & 0b10) == 0b10;
        }

        internal bool IsCulled
        {
            get => (enabled & 0b1) == 0;
            set
            {
                if (value)
                {
                    enabled = (byte)(enabled & 0b11111110);
                }
                else
                {
                    enabled |= 1;
                }
            }
        }

        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                enabled |= 0b10;
            }
            else
            {
                enabled &= 0b11111101;
            }
        }

        public static implicit operator bool(RenderEnabledBit d) => (d.enabled & 0b11) == 1;
        public static explicit operator RenderEnabledBit(bool b) => new RenderEnabledBit(b);
    }

    public static class RenderEnabledBitExtensions
    {
        public static void SetForceDisabledRendering(this Entity entity, IEntityManager entityManager, bool disabled)
        {
            entityManager.GetComponent<RenderEnabledBit>(entity).SetDisabled(disabled);
        }

        public static void SetRenderLayer(this Entity entity, IEntityManager entityManager, RenderLayer renderLayer)
        {
            entityManager.GetComponent<RenderEnabledBit>(entity).Layer = renderLayer.Layer;
        }
    }
    
    public struct PerformCullingBit : IComponentData
    {
    }
}