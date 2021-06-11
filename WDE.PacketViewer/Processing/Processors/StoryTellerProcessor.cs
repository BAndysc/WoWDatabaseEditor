using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using WDE.Common.Database;
using WDE.Common.DBC;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class StoryTellerProcessor : PacketProcessor<Nothing>
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
        private TextWriter writer;
        private DateTime? lastTime;
        private Dictionary<UniversalGuid, int> guids = new Dictionary<UniversalGuid, int>();
        private int currentShortGUID;
        private UniversalGuid? playerGuid;
        private Dictionary<UniversalGuid, Dictionary<int, uint>> Auras = new Dictionary<UniversalGuid, Dictionary<int, uint>>();
        Dictionary<uint, Dictionary<uint, string>> gossips = new Dictionary<uint, Dictionary<uint, string>>();

        private Dictionary<UniversalGuid, Entity> entities = new();

        public StoryTellerProcessor(IDatabaseProvider databaseProvider, 
            IDbcStore dbcStore,
            ISpellStore spellStore,
            TextWriter writer)
        {
            this.databaseProvider = databaseProvider;
            this.dbcStore = dbcStore;
            this.spellStore = spellStore;
            this.writer = writer;
        }

        private void AppendLine(PacketBase packet, string text)
        {
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
            writer.WriteLine("   " + text + " ["+packet.Number+"]");
        }
        
        public string? GetPrettyFormat(UniversalHighGuid type, uint id, int shortGUID = 0)
        {
            if (type == UniversalHighGuid.Creature)
            {
                var cr = databaseProvider.GetCreatureTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id})(GUID {shortGUID})";
            }
            else if (type == UniversalHighGuid.GameObject)
            {
                var cr = databaseProvider.GetGameObjectTemplate(id);
                if (cr == null)
                    return null;
                return $"{cr.Name} ({id})(GUID {shortGUID})";
            }
            return null;
        }
        
        private string NiceGuid(UniversalGuid? guid, bool withFull = false)
        {
            if (guid == null)
                return "(null guid)";
            int shortGUID = currentShortGUID;
            if (guids.ContainsKey(guid))
                shortGUID = guids[guid];
            else
                guids[guid] = currentShortGUID++;

            if (guid.Type == UniversalHighGuid.Player)
            {
                if (playerGuid == guid)
                    return "Player (me)";
                return $"Player {shortGUID}";
            }
            if (guid.Type == UniversalHighGuid.Creature || guid.Type == UniversalHighGuid.Vehicle || guid.Type == UniversalHighGuid.Pet)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGUID);
                return pretty ?? "Creature " + guid.Entry + (shortGUID > 0 ? " (GUID " + shortGUID + ")" : "") + (withFull?" "+(guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8"):"");
            }
            if (guid.Type == UniversalHighGuid.GameObject)
            {
                var pretty = GetPrettyFormat(guid.Type, guid.Entry, shortGUID);
                return pretty ?? "GameObject " + guid.Entry + (shortGUID > 0 ? " (GUID " + shortGUID + ")" : "") + (withFull ? " " + (guid.Guid64?.Low ?? guid.Guid128.Low).ToString("X8") : "");
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
        
        protected override Nothing Process(PacketBase basePacket, PacketChat packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Sender) + " says: " + packet.Text);
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketPlayerLogin packet)
        {
            playerGuid = packet.PlayerGuid;
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketEmote packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Sender) + " plays emote: " + GetStringFromDbc(dbcStore.EmoteStore, packet.Emote));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketPlaySound packet)
        {
            AppendLine(basePacket, NiceGuid(packet.Source) + " plays sound: " + GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            AppendLine(basePacket, $"music: {packet.Music} plays");
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            AppendLine(basePacket, $"{NiceGuid(packet.Source)} plays object sound: {GetStringFromDbc(dbcStore.SoundStore, (int)packet.Sound)} to {NiceGuid(packet.Target)}");
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            if (!Auras.ContainsKey(packet.Unit))
                Auras[packet.Unit] = new Dictionary<int, uint>();
            AppendLine(basePacket, NiceGuid(packet.Unit) + " auras update:");
            foreach (var update in packet.Updates)
            {
                if (update.Remove && Auras[packet.Unit].ContainsKey(update.Slot))
                {
                    AppendLine(basePacket,
                        "    removed aura: " +
                        (spellStore.HasSpell(Auras[packet.Unit][update.Slot]) ? spellStore.GetName(Auras[packet.Unit][update.Slot]) + " (" + Auras[packet.Unit][update.Slot] + " ) " : Auras[packet.Unit][update.Slot]));
                    Auras[packet.Unit].Remove(update.Slot);
                }
                else if (!update.Remove)
                {
                    Auras[packet.Unit][update.Slot] = update.Spell;
                    AppendLine(basePacket,
                        "    applied aura: " +
                        (spellStore.HasSpell(Auras[packet.Unit][update.Slot]) ? spellStore.GetName(Auras[packet.Unit][update.Slot]) + " (" + Auras[packet.Unit][update.Slot] + " ) " : Auras[packet.Unit][update.Slot]));
                }
            }
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketSpellGo packet)
        {
            string targetLine = "";
            int targetCount = packet.Data.HitTargets.Count;

            if (targetCount == 1)
            {
                targetLine = " at target: " + NiceGuid(packet.Data.HitTargets[0]);
            }
            else if (targetCount > 0)
            {
                targetLine = " at " + targetCount + " targets";

                int index = 0;
                targetLine += "\n       Spell Targets: {";
                foreach (var guid in packet.Data.HitTargets)
                {
                    targetLine += "\n            [" + (index++) + "] " + NiceGuid(guid);
                }
                targetLine += "\n       }";
            }

            AppendLine(basePacket, NiceGuid(packet.Data.Caster) + " casts: " + 
                                   (spellStore.HasSpell(packet.Data.Spell) ? spellStore.GetName(packet.Data.Spell) + " (" + packet.Data.Spell + " ) " : packet.Data.Spell) + targetLine);
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketGossipMessage packet)
        {
            gossips[packet.MenuId] = new Dictionary<uint, string>();

            for (int i = 0; i < packet.Options.Count; ++i) 
                gossips[packet.MenuId].Add(packet.Options[i].OptionIndex, packet.Options[i].Text);
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketGossipSelect packet)
        {
            if (gossips.ContainsKey(packet.MenuId) && gossips[packet.MenuId].ContainsKey(packet.OptionId))
                AppendLine(basePacket, "Player choose option: " + gossips[packet.MenuId][packet.OptionId]);
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketGossipHello packet)
        {
            AppendLine(basePacket, "Player talks to: " + NiceGuid(packet.GossipSource));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            var template = databaseProvider.GetQuestTemplate(packet.QuestId);
            AppendLine(basePacket, "Player accepts quest: " + (template == null ? packet.QuestId : $"{template.Name} ({packet.QuestId})"));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketQuestGiverQuestComplete packet)
        {
            var template = databaseProvider.GetQuestTemplate(packet.QuestId);
            AppendLine(basePacket, "Player rewards quest: " + (template == null ? packet.QuestId : $"{template.Name} ({packet.QuestId})"));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketSpellClick packet)
        {
            AppendLine(basePacket, "Player spell click on: " + NiceGuid(packet.Target));
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketMonsterMove packet)
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

            AppendLine(basePacket, NiceGuid(packet.Mover) + sb.ToString());
            return base.Process(basePacket, packet);
        }

        protected override Nothing Process(PacketBase basePacket, PacketPhaseShift packet)
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

        protected override Nothing Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var destroyed in packet.Destroyed)
            {
                if (destroyed.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed);
                AppendLine(basePacket, "Destroyed " + NiceGuid(destroyed));
            }

            foreach (var destroyed in packet.OutOfRange)
            {
                if (destroyed.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.Remove(destroyed);
                AppendLine(basePacket, "Out of range: " + NiceGuid(destroyed));
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
                AppendLine(basePacket, "Created " + NiceGuid(created.Guid));
                PrintValues(basePacket, created.Guid, created.Values, null);
            }
            
            foreach (var updated in packet.Updated)
            {
                if (updated.Guid.Type is UniversalHighGuid.Item or UniversalHighGuid.DynamicObject)
                    continue;
                entities.TryGetValue(updated.Guid, out var entity);
                AppendLine(basePacket, "Updated " + NiceGuid(updated.Guid));
                PrintValues(basePacket, updated.Guid, updated.Values, entity);
            }
            return base.Process(basePacket, packet);
        }

        private void PrintValues(PacketBase basePacket, UniversalGuid guid, UpdateValues values, Entity? entity)
        {
            var newEntity = entity ?? new Entity();
            if (entity == null)
                entities[guid] = newEntity;
            
            foreach (var val in values.Ints)
            {
                if (entity != null && entity.Ints.TryGetValue(val.Key, out var intValue) && intValue != val.Value)
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})");
                else
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}");
                newEntity.Ints[val.Key] = val.Value;
            }
            
            foreach (var val in values.Floats)
            {
                if (entity != null && entity.Floats.TryGetValue(val.Key, out var intValue) && Math.Abs(intValue - val.Value) > 0.01f)
                    AppendLine(basePacket, $"     {val.Key} = {val.Value} (old: {intValue})");
                else
                    AppendLine(basePacket, $"     {val.Key} = {val.Value}");
                newEntity.Floats[val.Key] = val.Value;
            }
            
            foreach (var val in values.Quaternions)
                AppendLine(basePacket, $"     {val.Key} = {val.Value}");
            
            foreach (var val in values.Guids)
            {
                if (entity != null && entity.Guids.TryGetValue(val.Key, out var intValue) && !intValue.Equals(val.Value))
                    AppendLine(basePacket, $"     {val.Key} = '{NiceGuid(val.Value)}' (old: '{NiceGuid(intValue)}')");
                else
                    AppendLine(basePacket, $"     {val.Key} = {NiceGuid(val.Value)}");
                newEntity.Guids[val.Key] = val.Value;
            }
        }
    }
}