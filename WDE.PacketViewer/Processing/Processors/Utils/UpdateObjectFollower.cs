using System.Collections.Generic;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IUpdateObjectFollower : IPacketProcessor<bool>
    {
        public bool HasBeenCreated(UniversalGuid guid);
        
        public long? GetInt(UniversalGuid guid, string field);
        public float? GetFloat(UniversalGuid guid, string field);
        public UniversalGuid? GetGuid(UniversalGuid guid, string field);
        
        public long GetIntOrDefault(UniversalGuid guid, string field, long @default);
        public float GetFloatOrDefault(UniversalGuid guid, string field, float @default);
        
        public bool TryGetInt(UniversalGuid guid, string field, out long number);
        public bool TryGetIntOrDefault(UniversalGuid guid, string field, out long number);
        
        public bool TryGetFloat(UniversalGuid guid, string field, out float number);
        public bool TryGetGuid(UniversalGuid guid, string field, out UniversalGuid number);
    }
    
    [AutoRegister]
    public class UpdateObjectFollower : PacketProcessor<bool>, IUpdateObjectFollower
    {
        private class Entity
        {
            public Dictionary<string, long> Ints { get; } = new();
            public Dictionary<string, float> Floats { get; } = new();
            public Dictionary<string, UniversalGuid> Guids { get; } = new();
        }

        private readonly HashSet<UniversalGuid> everCreated = new();
        private readonly Dictionary<UniversalGuid, Entity> entities = new();

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            foreach (ref readonly var destroyed in packet.Destroyed.AsSpan())
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
            }

            foreach (ref readonly var destroyed in packet.OutOfRange.AsSpan())
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
            }

            foreach (ref readonly var created in packet.Created.AsSpan())
            {
                if (created.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                everCreated.Add(created.Guid);
                var entity = entities[created.Guid] = new Entity();
                foreach (var pair in created.Values.Ints())
                    entity.Ints[pair.Key] = pair.Value;
                foreach (var pair in created.Values.Floats())
                    entity.Floats[pair.Key] = pair.Value;
                foreach (var pair in created.Values.Guids())
                    entity.Guids[pair.Key] = pair.Value;
            }
            
            foreach (ref readonly var updated in packet.Updated.AsSpan())
            {
                if (updated.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                if (!entities.TryGetValue(updated.Guid, out var entity))
                    entity = entities[updated.Guid] = new();
                
                foreach (var pair in updated.Values.Ints())
                    entity.Ints[pair.Key] = pair.Value;
                foreach (var pair in updated.Values.Floats())
                    entity.Floats[pair.Key] = pair.Value;
                foreach (var pair in updated.Values.Guids())
                    entity.Guids[pair.Key] = pair.Value;
            }
            return base.Process(in basePacket, in packet);
        }

        public bool HasBeenCreated(UniversalGuid guid)
        {
            return everCreated.Contains(guid);
        }

        public long? GetInt(UniversalGuid guid, string field)
        {
            if (entities.TryGetValue(guid, out var entity))
                if (entity.Ints.TryGetValue(field, out var val))
                    return val;
            return null;
        }

        public float? GetFloat(UniversalGuid guid, string field)
        {
            if (entities.TryGetValue(guid, out var entity))
                if (entity.Floats.TryGetValue(field, out var val))
                    return val;
            return null;
        }

        public UniversalGuid? GetGuid(UniversalGuid guid, string field)
        {
            if (entities.TryGetValue(guid, out var entity))
                if (entity.Guids.TryGetValue(field, out var val))
                    return val;
            return null;
        }

        public long GetIntOrDefault(UniversalGuid guid, string field, long @default)
        {
            return GetInt(guid, field) ?? @default;
        }

        public float GetFloatOrDefault(UniversalGuid guid, string field, float @default)
        {
            return GetFloat(guid, field) ?? @default;
        }

        public bool TryGetInt(UniversalGuid guid, string field, out long number)
        {
            var num = GetInt(guid, field);
            number = num ?? 0;
            return num.HasValue;
        }

        public bool TryGetFloat(UniversalGuid guid, string field, out float number)
        {
            var num = GetFloat(guid, field);
            number = num ?? 0;
            return num.HasValue;
        }

        public bool TryGetGuid(UniversalGuid guid, string field, out UniversalGuid number)
        {
            var num = GetGuid(guid, field);
            number = num ?? default;
            return num.HasValue;
        }
        
        public bool TryGetIntOrDefault(UniversalGuid guid, string field, out long number)
        {
            TryGetInt(guid, field, out number);
            return true;
        }
    }
}