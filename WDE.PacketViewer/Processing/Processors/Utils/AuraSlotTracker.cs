using System.Collections.Generic;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public interface IAuraSlotTracker : IPacketProcessor<bool>
    {
        uint? GetSpellForAuraSlot(UniversalGuid guid, int slot);
    }
    
    [AutoRegister]
    public class AuraSlotTracker : IPacketProcessor<bool>, IAuraSlotTracker
    {
        private class AuraState
        { 
            public Dictionary<int, uint> SlotToSpell { get; } = new();
        }

        private Dictionary<UniversalGuid, AuraState> states = new();

        private UniversalGuid? lastAuraGuid;
        private List<int> pendingRemoveSlots = new();
        
        private AuraState Get(UniversalGuid guid)
        {
            if (states.TryGetValue(guid, out var state))
                return state;
            state = states[guid] = new();
            return state;
        }

        public uint? GetSpellForAuraSlot(UniversalGuid guid, int slot)
        {
            if (!states.TryGetValue(guid, out var state))
                return null;
            if (state.SlotToSpell.TryGetValue(slot, out var spell))
                return spell;
            return null;
        }

        private void ProcessUpdate(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            foreach (ref readonly var destroy in packet.Destroyed.AsSpan())
                states.Remove(destroy.Guid);
            
            foreach (ref readonly var destroy in packet.OutOfRange.AsSpan())
                states.Remove(destroy.Guid);
        }

        private void ProcessAuraUpdate(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            var state = Get(packet.Unit);
            lastAuraGuid = packet.Unit;
            foreach (ref readonly var update in packet.Updates.AsSpan())
            {
                if (update.Remove)
                {
                    pendingRemoveSlots.Add(update.Slot);
                }
                else
                {
                    state.SlotToSpell[update.Slot] = update.Spell;
                }
            }
        }
        
        public bool Process(ref readonly PacketHolder packet)
        {
            if (pendingRemoveSlots.Count > 0)
            {
                if (lastAuraGuid != null && states.TryGetValue(lastAuraGuid.Value, out var state))
                {
                    foreach (var slot in pendingRemoveSlots)
                        state.SlotToSpell.Remove(slot);
                }
                pendingRemoveSlots.Clear();
            }
            
            if (packet.KindCase == PacketHolder.KindOneofCase.AuraUpdate)
            {
                ProcessAuraUpdate(in packet.BaseData, in packet.AuraUpdate);
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.UpdateObject)
            {
                ProcessUpdate(in packet.BaseData, in packet.UpdateObject);
            }

            return true;
        }
    }
}