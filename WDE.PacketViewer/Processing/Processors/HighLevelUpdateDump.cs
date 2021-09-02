using System;
using System.Collections.Generic;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    [SingleInstance]
    public class HighLevelUpdateDump
    {
        private Dictionary<string, List<Entry>> dumpers = new();

        private struct Entry
        {
            public IntOffset? offset;
            public uint value;
            public string text;
            public string text2;
            public bool mask;
            public Func<uint, uint, bool>? predicate;
        }
        
        private enum IntOffset
        {
            Bytes2Sheath = 0,
            GameobjectBytes1State = 0
        }
        
        public HighLevelUpdateDump()
        {
            Register("UNIT_FIELD_BYTES_2", IntOffset.Bytes2Sheath, 0, "Unit(s) unsheathe their weapons");
            Register("UNIT_FIELD_BYTES_2", IntOffset.Bytes2Sheath, 1, "Unit(s) sheathe their melee weapons");
            
            Register("GAMEOBJECT_BYTES_1", IntOffset.GameobjectBytes1State, 0, "Unit(s) de-activate a game object");
            Register("GAMEOBJECT_BYTES_1", IntOffset.GameobjectBytes1State, 1, "Unit(s) activate a game object");
            
            Register("UNIT_VIRTUAL_ITEM_SLOT_ID", 0, "Reset the item in the mainhand slot for unit(s)");
            Register("UNIT_VIRTUAL_ITEM_SLOT_ID", (o, n) => o == 0 && n != 0, "Set the item in the mainhand slot for unit(s)");
            
            RegisterFlag("UNIT_FIELD_FLAGS", 256, "Set \"Immune To PC\" flag for unit(s)", "Reset \"Immune To PC\" flag for unit(s)");
            RegisterFlag("UNIT_FIELD_FLAGS", 512, "Set \"Immune To NPC\" flag for unit(s)", "Reset \"Immune To NPC\" flag for unit(s)");

            RegisterFlag("UNIT_NPC_FLAGS", 1, "Unit(s) become a gossip", "Reset Unit(s) become a gossip");
            RegisterFlag("UNIT_NPC_FLAGS", 2, "Unit(s) become a quest giver", "Reset Unit(s) become a quest giver");
            RegisterFlag("UNIT_NPC_FLAGS", 16, "Unit(s) become a trainer", "Reset Unit(s) become a trainer");
        }

        public IEnumerable<string> Produce(string field, uint oldValue, uint newValue)
        {
            if (!dumpers.TryGetValue(field, out var entries))
                yield break;
            
            foreach (var entry in entries)
            {
                if (entry.predicate != null)
                {
                    if (entry.predicate(oldValue, newValue))
                        yield return entry.text;
                }
                else if (entry.mask)
                {
                    if ((oldValue & entry.value) == 0 && (newValue & entry.value) == entry.value)
                        yield return entry.text;
                    if ((oldValue & entry.value) == entry.value && (newValue & entry.value) == 0)
                        yield return entry.text2;
                }
                else 
                {
                    if (entry.offset.HasValue)
                    {
                        oldValue = (oldValue >> (int)entry.offset) & 0xFF;
                        newValue = (newValue >> (int)entry.offset) & 0xFF;   
                    }

                    if (oldValue != entry.value && newValue == entry.value)
                        yield return entry.text;
                }
            }
        }

        private void AddEntry(string field, Entry entry)
        {
            if (!dumpers.TryGetValue(field, out var entries))
            {
                entries = new();
                dumpers[field] = entries;
            }
            
            entries.Add(entry);
        }
        
        private void RegisterFlag(string field, uint flag, string whenSet, string whenReset)
        {
            AddEntry(field, new Entry(){value = flag, mask = true, text = whenSet, text2 = whenReset});
        }

        private void Register(string field, IntOffset? offset, uint val, string text)
        {
            AddEntry(field, new Entry(){offset = offset, value = val, text = text});
        }
        
        private void Register(string field, uint val, string text)
        {
            Register(field, null, val, text);
        }
        
        private void Register(string field, Func<uint, uint, bool> pred, string text)
        {
            AddEntry(field, new Entry(){predicate = pred, text = text});
        }
    }
}