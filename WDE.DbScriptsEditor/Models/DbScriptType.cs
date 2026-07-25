using System;
using System.Collections.Generic;

namespace WDE.DbScriptsEditor.Models
{
    // Each dbscripts_on_* table is a distinct script type.
    public enum DbScriptType
    {
        CreatureDeath,
        CreatureMovement,
        Event,
        GoUse,
        GoTemplateUse,
        Gossip,
        QuestStart,
        QuestEnd,
        Spell,
        Relay
    }

    public class DbScriptTypeInfo
    {
        public required DbScriptType Type { get; init; }
        public required string TableName { get; init; }
        public required string ReadableName { get; init; }
        // Parameter type used by the solution-item picker to choose a script id.
        public required string IdPicker { get; init; }
        // Contextual names for the original source / target of a step of this script type.
        public required string SourceLabel { get; init; }
        public required string TargetLabel { get; init; }
        // Possible static kinds of the original source / target, per the engine's ScriptsStart
        // call sites for this table (mangos-wotlk).
        public required DbScriptActorKind SourceKinds { get; init; }
        public required DbScriptActorKind TargetKinds { get; init; }
    }

    public static class DbScriptTypes
    {
        // SourceKinds/TargetKinds verified against mangos-wotlk ScriptsStart call sites:
        //  creature_death: Unit::Kill(victim, responsiblePlayer ?: killer)
        //  creature_movement: (Path|Waypoint)MovementGenerator (&unit, guid object ?: self)
        //  event: Map::StartEvent(Object*, Object*) — anything
        //  go_use / go_template_use: GameObject::Use(spellCaster, this)
        //  gossip: Player.cpp — NPC gossip (creature, player), GO gossip (player, GO)
        //  quest_start/quest_end: Player.cpp (questGiver — creature or GO, player)
        //  spell: SpellEffects.cpp (m_trueCaster — unit or GO trap, unitTarget|gameObjTarget)
        //  relay: arbitrary call sites — anything
        public static readonly IReadOnlyList<DbScriptTypeInfo> All = new List<DbScriptTypeInfo>
        {
            new() { Type = DbScriptType.CreatureDeath,    TableName = "dbscripts_on_creature_death",    ReadableName = "On Creature Death",   IdPicker = "CreatureParameter",         SourceLabel = "Dying creature", TargetLabel = "Killer",          SourceKinds = DbScriptActorKind.Creature,                                  TargetKinds = DbScriptActorKind.Unit },
            new() { Type = DbScriptType.CreatureMovement, TableName = "dbscripts_on_creature_movement", ReadableName = "On Creature Movement",IdPicker = "Parameter",                 SourceLabel = "Creature",       TargetLabel = "Creature (self)", SourceKinds = DbScriptActorKind.Creature,                                  TargetKinds = DbScriptActorKind.WorldObject },
            new() { Type = DbScriptType.Event,            TableName = "dbscripts_on_event",             ReadableName = "On Event",            IdPicker = "Parameter",                 SourceLabel = "Source",         TargetLabel = "Target",          SourceKinds = DbScriptActorKind.WorldObject,                               TargetKinds = DbScriptActorKind.WorldObject },
            new() { Type = DbScriptType.GoUse,            TableName = "dbscripts_on_go_use",            ReadableName = "On GO Use (guid)",    IdPicker = "GameobjectGUIDParameter",   SourceLabel = "User",           TargetLabel = "Gameobject",      SourceKinds = DbScriptActorKind.Unit,                                      TargetKinds = DbScriptActorKind.GameObject },
            new() { Type = DbScriptType.GoTemplateUse,    TableName = "dbscripts_on_go_template_use",   ReadableName = "On GO Use (entry)",   IdPicker = "GameobjectParameter",       SourceLabel = "User",           TargetLabel = "Gameobject",      SourceKinds = DbScriptActorKind.Unit,                                      TargetKinds = DbScriptActorKind.GameObject },
            new() { Type = DbScriptType.Gossip,           TableName = "dbscripts_on_gossip",            ReadableName = "On Gossip",           IdPicker = "Parameter",                 SourceLabel = "NPC / player",   TargetLabel = "Player / GO",     SourceKinds = DbScriptActorKind.Creature | DbScriptActorKind.Player,      TargetKinds = DbScriptActorKind.Player | DbScriptActorKind.GameObject },
            new() { Type = DbScriptType.QuestStart,       TableName = "dbscripts_on_quest_start",       ReadableName = "On Quest Start",      IdPicker = "QuestParameter",            SourceLabel = "Quest giver",    TargetLabel = "Player",          SourceKinds = DbScriptActorKind.Creature | DbScriptActorKind.GameObject,  TargetKinds = DbScriptActorKind.Player },
            new() { Type = DbScriptType.QuestEnd,         TableName = "dbscripts_on_quest_end",         ReadableName = "On Quest End",        IdPicker = "QuestParameter",            SourceLabel = "Quest taker",    TargetLabel = "Player",          SourceKinds = DbScriptActorKind.Creature | DbScriptActorKind.GameObject,  TargetKinds = DbScriptActorKind.Player },
            new() { Type = DbScriptType.Spell,            TableName = "dbscripts_on_spell",             ReadableName = "On Spell",            IdPicker = "SpellParameter",            SourceLabel = "Caster",         TargetLabel = "Spell target",    SourceKinds = DbScriptActorKind.Unit | DbScriptActorKind.GameObject,      TargetKinds = DbScriptActorKind.Unit | DbScriptActorKind.GameObject },
            new() { Type = DbScriptType.Relay,            TableName = "dbscripts_on_relay",             ReadableName = "On Relay",            IdPicker = "DbScriptRelayParameter",    SourceLabel = "Source",         TargetLabel = "Target",          SourceKinds = DbScriptActorKind.WorldObject,                               TargetKinds = DbScriptActorKind.WorldObject },
        };

        private static readonly Dictionary<DbScriptType, DbScriptTypeInfo> byType = BuildMap();

        private static Dictionary<DbScriptType, DbScriptTypeInfo> BuildMap()
        {
            var map = new Dictionary<DbScriptType, DbScriptTypeInfo>();
            foreach (var info in All)
                map[info.Type] = info;
            return map;
        }

        public static DbScriptTypeInfo GetInfo(DbScriptType type) => byType[type];

        public static string TableName(DbScriptType type) => byType[type].TableName;

        public static DbScriptType FromTableName(string tableName)
        {
            if (TryFromTableName(tableName, out var type))
                return type;
            throw new ArgumentException($"Unknown dbscript table '{tableName}'");
        }

        public static bool TryFromTableName(string tableName, out DbScriptType type)
        {
            foreach (var info in All)
            {
                if (string.Equals(info.TableName, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    type = info.Type;
                    return true;
                }
            }
            type = default;
            return false;
        }
    }
}
