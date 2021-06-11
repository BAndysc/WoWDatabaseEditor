using System.Collections.Generic;
using WDE.Common.DBC;
using WDE.Common.Utils;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureGameObjectNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketQueryCreatureResponse packet)
        {
            yield return ("Creature", packet.Name, packet.Entry);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketQueryGameObjectResponse packet)
        {
            yield return ("Gameobject", packet.Name, packet.Entry);
        }
    }
    
    public class SpellNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        private readonly ISpellStore spellStore;

        public SpellNameProcessor(ISpellStore spellStore)
        {
            this.spellStore = spellStore;
        }
        
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketSpellGo packet)
        {
            if (spellStore.HasSpell(packet.Data.Spell))
                yield return ("Spell", spellStore.GetName(packet.Data.Spell), packet.Data.Spell);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketSpellStart packet)
        {
            if (spellStore.HasSpell(packet.Data.Spell))
                yield return ("Spell", spellStore.GetName(packet.Data.Spell), packet.Data.Spell);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            foreach (var update in packet.Updates)
            {
                if (update.Remove)
                    continue;
                
                if (spellStore.HasSpell(update.Spell))
                    yield return ("Spell", spellStore.GetName(update.Spell), update.Spell);
            }
        }
    }

    public class NameAsEnumProcessor : IPacketProcessor<bool>, IPacketToTextProcessor
    {
        private readonly IPacketProcessor<IEnumerable<(string prefix, string name, uint entry)>> inner;
        private HashSet<uint> existing = new();
        private List<string> entries = new();

        public NameAsEnumProcessor(IPacketProcessor<IEnumerable<(string prefix, string name, uint entry)>> inner)
        {
            this.inner = inner;
        }
        
        public bool Process(PacketHolder packet)
        {
            var results = inner.Process(packet);
            if (results == null)
                return false;

            foreach (var result in results)
            {
                if (!existing.Add(result.entry))
                    continue;

                var name = (result.prefix + " " + result.name).ToEnumName();
                entries.Add($"{name,-30} = {result.entry}");
            }

            return true;
        }

        public string Generate()
        {
            return string.Join(",\n", entries);
        }
    }
}