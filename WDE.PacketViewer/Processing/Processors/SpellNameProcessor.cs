using System.Collections.Generic;
using System.Linq;
using WDE.Common.DBC;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public unsafe class SpellNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        private readonly ISpellStore spellStore;
        private readonly IDbcStore dbcStore;

        public SpellNameProcessor(ISpellStore spellStore, IDbcStore dbcStore)
        {
            this.spellStore = spellStore;
            this.dbcStore = dbcStore;
        }
        
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            if (packet.Data != null && spellStore.HasSpell(packet.Data->Spell))
                return ("Spell", spellStore.GetName(packet.Data->Spell)!, packet.Data->Spell).ToSingletonList();
            return Enumerable.Empty<(string prefix, string name, uint entry)>();
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
        {
            if (packet.Data != null && spellStore.HasSpell(packet.Data->Spell))
                return ("Spell", spellStore.GetName(packet.Data->Spell)!, packet.Data->Spell).ToSingletonList();
            return Enumerable.Empty<(string prefix, string name, uint entry)>();
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            List<(string prefix, string name, uint entry)> output = new();
            foreach (ref readonly var update in packet.Updates.AsSpan())
            {
                if (update.Remove)
                    continue;
                
                if (spellStore.HasSpell(update.Spell))
                    output.Add(("Spell", spellStore.GetName(update.Spell)!, update.Spell));
            }

            return output;
        }

        protected override IEnumerable<(string prefix, string name, uint entry)>? Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet)
        {
            if (dbcStore.SoundStore.TryGetValue(packet.Sound, out var name))
                return ("Sound", name, packet.Sound).ToSingletonList();
            return null;
        }

        protected override IEnumerable<(string prefix, string name, uint entry)>? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet)
        {
            if (dbcStore.SoundStore.TryGetValue(packet.Sound, out var name))
                return ("Sound", name, packet.Sound).ToSingletonList();
            return null;
        }
    }
}