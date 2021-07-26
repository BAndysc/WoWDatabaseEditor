using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Module.Attributes;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [AutoRegister]
    public class StoryTellerDumper : PacketProcessor<bool>, IPacketTextDumper
    {
        private class Entity
        {
            public Dictionary<string, long> Ints { get; } = new();
            public Dictionary<string, float> Floats { get; } = new();
            public Dictionary<string, UniversalGuid> Guids { get; } = new();
        }

        private readonly IDatabaseProvider databaseProvider;
        private readonly IDbcStore dbcStore;
        private readonly ISpellStore spellStore;
        private readonly StringBuilder sb;
        private readonly TextWriter writer;
        private DateTime? lastTime;
        private readonly Dictionary<UniversalGuid, int> guids = new();
        private int currentShortGuid;
        private UniversalGuid? playerGuid;
        private readonly Dictionary<UniversalGuid, Dictionary<int, uint>> auras = new();
        private readonly Dictionary<uint, Dictionary<uint, string>> gossips = new();

        private readonly Dictionary<UniversalGuid, Entity> entities = new();

        public StoryTellerDumper(IDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            ISpellStore spellStore)
        {
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.sb = new();
            this.writer = new StringWriter(sb);
        }

        private string? pendingAppend;

        private void SetAppendOnNext(string? line)
        {
            pendingAppend = line;
        }
        
        private void AppendLine(PacketBase packet, string text, bool skipPacketNumber = false)
        {
            if (pendingAppend != null)
            {
                var pending = pendingAppend;
                pendingAppend = null;
                AppendLine(packet, pending);
            }
            
            if (!lastTime.HasValue)
                lastTime = packet.Time.ToDateTime();
            else
            {
                TimeSpan diff = packet.Time.ToDateTime().Subtract(lastTime.Value);
                if (diff.TotalMilliseconds > 100)
                {
                    writer.WriteLine();
                    writer.WriteLine("After " + diff.TotalMilliseconds + " ms");
                    lastTime = packet.Time.ToDateTime();
                }
            }
            if (!skipPacketNumber)
                writer.WriteLine("   " + text + " ["+packet.Number+"]");
            else
                writer.WriteLine("   " + text);
        }
        
        public string? GetPrettyFormat(UniversalHighGuid type, uint id, int shortGuid = 0)
        {
            if (type == UniversalHighGuid.Creature || type == UniversalHighGuid.Vehicle || type == UniversalHighGuid.Pet)
            {
                var cr = databaseProvider.GetCreatureTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id})(GUID {shortGuid})";
            }
            else if (type == UniversalHighGuid.GameObject || type == UniversalHighGuid.Transport || type == UniversalHighGuid.WorldTransaction)
            {
                var cr = databaseProvider.GetGameObjectTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id})(GUID {shortGuid})";
            }
            return null;
        }
        
        private string NiceGuid(UniversalGuid? guid, bool withFull = false)
        {
            if (guid == null)
                return "(null guid)";
            int shortGuid = currentShortGuid;
            if (guids.ContainsKey(guid))
                shortGuid = guids[guid];
            else
                guids[guid] = currentShortGuid++;

            if (guid.Type == UniversalHighGuid.Player)
            {
                if (playerGuid?.Equals(guid) ?? false)
                    return "Player (me)";
                return $"Player {shortGuid}";
            }
            if (guid.Type == UniversalHighGuid.Creature || guid.Type == UniversalHighGuid.Vehicle || guid.Type == UniversalHighGuid.Pet)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGuid);
                return pretty ?? "Creature " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull?" "+(guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8"):"");
            }
            if (guid.Type == UniversalHighGuid.GameObject)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGuid);
                return pretty ?? "GameObject " + guid.Entry + (shortGuid > 0 ? " (GUID " + shortGuid + ")" : "") + (withFull ? " " + (guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8") : "");
            }
            if (playerGuid != null && playerGuid.Equals(guid))
                return "Player";

            return guid.ToString();
        }

        private string GetStringFromDbc(Dictionary<long, string> dbc, int id)
        {
            if (dbc.TryGetValue(id, out var name))
                return $"{name} ({id})";
            return id.ToString();
        }
        
        protected override bool Process(PacketBase basePacket, PacketChat packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Sender) + " says: " + packet.Text);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayerLogin packet)
        {
            playerGuid = packet.PlayerGuid;
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Sender) + " plays emote: " + GetStringFromDbc(dbcStore.EmoteStore, packet.Emote));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Source) + " plays sound: " + GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            AppendLine(basePacket, $"music: {packet.Music} plays");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            AppendLine(basePacket, $"{NiceGuid(packet.Source)} plays object sound: {GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound)} to {NiceGuid(packet.Target)}");
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            if (!auras.ContainsKey(packet.Unit))
                auras[packet.Unit] = new Dictionary<int, uint>();
            AppendLine(basePacket, NiceGuid(packet.Unit) + " auras update:");
            foreach (var update in packet.Updates)
            {
                if (update.Remove && auras[packet.Unit].ContainsKey(update.Slot))
                {
                    AppendLine(basePacket,
                        "    removed aura: " +
                        (spellStore.HasSpell(auras[packet.Unit][update.Slot]) ? spellStore.GetName(auras[packet.Unit][update.Slot]) + " (" + auras[packet.Unit][update.Slot] + " ) " : auras[packet.Unit][update.Slot]));
                    auras[packet.Unit].Remove(update.Slot);
                }
                else if (!update.Remove)
                {
                    auras[packet.Unit][update.Slot] = update.Spell;
                    AppendLine(basePacket,
                        "    applied aura: " +
                        (spellStore.HasSpell(auras[packet.Unit][update.Slot]) ? spellStore.GetName(auras[packet.Unit][update.Slot]) + " (" + auras[packet.Unit][update.Slot] + ") " : auras[packet.Unit][update.Slot]));
                }
            }
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellGo packet)
        {
            string targetLine = "";
            int targetCount = packet.Data.HitTargets.Count;

            if (targetCount == 1)
            {
                targetLine = "at target: " + NiceGuid(packet.Data.HitTargets[0]);
            }
            else if (targetCount > 0)
            {
                targetLine = "at " + targetCount + " targets";

                int index = 0;
                targetLine += "\n       Spell Targets: {";
                foreach (var guid in packet.Data.HitTargets)
                {
                    targetLine += "\n            [" + (index++) + "] " + NiceGuid(guid);
                }
                targetLine += "\n       }";
            }

            AppendLine(basePacket, NiceGuid(packet.Data.Caster) + " casts: " + 
                                   (spellStore.HasSpell(packet.Data.Spell) ? spellStore.GetName(packet.Data.Spell) + " (" + packet.Data.Spell + ") " : packet.Data.Spell) + targetLine);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipMessage packet)
        {
            gossips[packet.MenuId] = new Dictionary<uint, string>();

            for (int i = 0; i < packet.Options.Count; ++i) 
                gossips[packet.MenuId].Add(packet.Options[i].OptionIndex, packet.Options[i].Text);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipSelect packet)
        {
            if (gossips.ContainsKey(packet.MenuId) && gossips[packet.MenuId].ContainsKey(packet.OptionId))
                AppendLine(basePacket, "Player choose option: " + gossips[packet.MenuId][packet.OptionId]);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketGossipHello packet)
        {
            AppendLine(basePacket, "Player talks to: " + NiceGuid(packet.GossipSource));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            var template = databaseProvider.GetQuestTemplate(packet.QuestId);
            AppendLine(basePacket, "Player accepts quest: " + (template == null ? packet.QuestId : $"{template.Name} ({packet.QuestId})"));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketQuestGiverQuestComplete packet)
        {
            var template = databaseProvider.GetQuestTemplate(packet.QuestId);
            AppendLine(basePacket, "Player rewards quest: " + (template == null ? packet.QuestId : $"{template.Name} ({packet.QuestId})"));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellClick packet)
        {
            AppendLine(basePacket, "Player spell click on: " + NiceGuid(packet.Target));
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            StringBuilder sb = new StringBuilder();

            if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation)
                sb.Append(" looks at " + packet.LookOrientation);
            else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookTarget)
                sb.Append(" looks at " + NiceGuid(packet.LookTarget.Target));
            else if (packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookPosition)
                sb.Append($" looks at ({packet.LookPosition.X}, {packet.LookPosition.Y}, {packet.LookPosition.Z})");
            else
            {
                if (packet.Points.Count + packet.PackedPoints.Count == 0)
                    sb.Append(" stops");
                else
                {
                    if (packet.Points.Count + packet.PackedPoints.Count == 1)
                    {
                        var vec = packet.PackedPoints.Count == 1 ? packet.PackedPoints[0] : packet.Points[0];
                        sb.Append($" goes to: ({vec.X}, {vec.Y}, {vec.Z})");
                    }
                    else
                    {
                        sb.AppendLine(" goes by " + (packet.Points.Count + packet.PackedPoints.Count) + " waypoints: {");

                        foreach (var waypoint in packet.PackedPoints)
                            sb.AppendLine($"               ({waypoint.X}, {waypoint.Y}, {waypoint.Z})");

                        foreach (var point in packet.Points)
                            sb.AppendLine($"               ({point.X}, {point.Y}, {point.Z})");

                        sb.Append("       }");
                    }
                }
            }

            AppendLine(basePacket, NiceGuid(packet.Mover) + sb);
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketPhaseShift packet)
        {
            StringBuilder sb = new StringBuilder();
            List<string> phases = new List<string>();
            foreach (uint phase in packet.Phases)
            {
                phases.Add(GetStringFromDbc(dbcStore.PhaseStore, (int)phase));
            }

            if (phases.Count > 0)
                sb.AppendLine("switch phase to: "+ phases[0]);
            for (int i = 1; i < phases.Count; ++i)
            {
                sb.Append("                    " + phases[i]);
                if (i < phases.Count - 1)
                    sb.AppendLine();
            }

            AppendLine(basePacket, sb.ToString());
            return base.Process(basePacket, packet);
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
                AppendLine(basePacket, "Destroyed " + NiceGuid(destroyed.Guid));
            }

            foreach (var destroyed in packet.OutOfRange)
            {
                if (destroyed.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed.Guid);
                AppendLine(basePacket, "Out of range: " + NiceGuid(destroyed.Guid));
            }

            foreach (var created in packet.Created)
            {
                if (created.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                var entity = new Entity();
                foreach (var pair in created.Values.Ints)
                    entity.Ints[pair.Key] = pair.Value;
                foreach (var pair in created.Values.Floats)
                    entity.Floats[pair.Key] = pair.Value;
                foreach (var pair in created.Values.Guids)
                    entity.Guids[pair.Key] = pair.Value;
                SetAppendOnNext("Created " + NiceGuid(created.Guid) + " at " + VecToString(created.Movement?.Position ?? created.Stationary?.Position));
                PrintValues(basePacket, created.Guid, created.Values, null, false);
                SetAppendOnNext(null);
            }
            
            foreach (var updated in packet.Updated)
            {
                if (updated.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.TryGetValue(updated.Guid, out var entity);
                SetAppendOnNext("Updated " + NiceGuid(updated.Guid));
                PrintValues(basePacket, updated.Guid, updated.Values, entity, true);
                SetAppendOnNext(null);
            }
            return base.Process(basePacket, packet);
        }

        private string VecToString(Vec3? pos)
        {
            if (pos == null)
                return "(unknown)";
            return $"({pos.X}, {pos.Y}, {pos.Z})";
        }

        private void PrintValues(PacketBase basePacket, UniversalGuid guid, UpdateValues values, Entity? entity, bool isUpdate)
        {
            var newEntity = entity ?? new Entity();
            if (entity == null)
                entities[guid] = newEntity;
            
            foreach (var val in values.Ints)
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Ints[val.Key] = val.Value;
                    continue;
                }

                if (entity == null || !entity.Ints.TryGetValue(val.Key, out var intValue))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}", true);
                else if (intValue != val.Value)
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})", true);
                newEntity.Ints[val.Key] = val.Value;
            }
            
            foreach (var val in values.Floats)
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Floats[val.Key] = val.Value;
                    continue;
                }
                
                if (entity == null || !entity.Floats.TryGetValue(val.Key, out var intValue))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}", true);
                else if (Math.Abs(intValue - val.Value) > 0.01f)
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})", true);
                newEntity.Floats[val.Key] = val.Value;
            }
            
            foreach (var val in values.Guids)
            {
                if (!IsUpdateFieldInteresting(val.Key, isUpdate))
                {
                    newEntity.Guids[val.Key] = val.Value;
                    continue;
                }
                
                if (entity == null || !entity.Guids.TryGetValue(val.Key, out var intValue))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}", true);
                else if (!intValue.Equals(val.Value))
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})", true);
                newEntity.Guids[val.Key] = val.Value;
            }
        }

        private bool IsUpdateFieldInteresting(string field, bool isUpdate)
        {
            switch (field)
            {
                case "UNIT_FIELD_BYTES_0":
                case "UNIT_FIELD_BYTES_1":
                case "UNIT_FIELD_BYTES_2":
                case "UNIT_FIELD_FACTIONTEMPLATE":
                case "UNIT_FIELD_FLAGS":
                case "UNIT_FIELD_FLAGS_2":
                case "UNIT_NPC_EMOTESTATE":
                case "GAMEOBJECT_BYTES_1":
                    return true;
            }

            if (isUpdate)
            {
                switch (field)
                {
                    case "UNIT_FIELD_DISPLAYID":
                    case "OBJECT_FIELD_SCALE_X":
                    case "UNIT_FIELD_LEVEL":
                    case "UNIT_VIRTUAL_ITEM_SLOT_ID":
                    case "GAMEOBJECT_FACTION":
                        return true;
                }
            }

            return false;
        }

        public string Generate()
        {
            return sb.ToString();
        }
    }
}