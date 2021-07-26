using System.Collections.Generic;
using WDE.Common.DBC;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
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
}