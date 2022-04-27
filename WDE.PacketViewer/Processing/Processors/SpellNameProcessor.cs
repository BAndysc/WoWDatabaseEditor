using System.Collections.Generic;
using WDE.Common.DBC;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class SpellNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        private readonly ISpellStore spellStore;
        private readonly IDbcStore dbcStore;

        public SpellNameProcessor(ISpellStore spellStore, IDbcStore dbcStore)
        {
            this.spellStore = spellStore;
            this.dbcStore = dbcStore;
        }
        
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketSpellGo packet)
        {
            if (spellStore.HasSpell(packet.Data.Spell))
                yield return ("Spell", spellStore.GetName(packet.Data.Spell)!, packet.Data.Spell);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketSpellStart packet)
        {
            if (spellStore.HasSpell(packet.Data.Spell))
                yield return ("Spell", spellStore.GetName(packet.Data.Spell)!, packet.Data.Spell);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            foreach (var update in packet.Updates)
            {
                if (update.Remove)
                    continue;
                
                if (spellStore.HasSpell(update.Spell))
                    yield return ("Spell", spellStore.GetName(update.Spell)!, update.Spell);
            }
        }

        protected override IEnumerable<(string prefix, string name, uint entry)>? Process(PacketBase basePacket, PacketPlaySound packet)
        {
            if (dbcStore.SoundStore.TryGetValue(packet.Sound, out var name))
                yield return ("Sound", name, packet.Sound);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)>? Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            if (dbcStore.SoundStore.TryGetValue(packet.Sound, out var name))
                yield return ("Sound", name, packet.Sound);
        }
    }
}