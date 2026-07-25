using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IDatabaseProvider
    {
        bool IsConnected { get; }

        Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync();
        Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync();
        Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync();
        Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int questSortId);
        Task<IReadOnlyList<INpcText>> GetNpcTextsAsync();
        Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync();

        Task<ICreatureTemplate?> GetCreatureTemplate(uint entry);

        Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry);

        Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry);

        Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry);
        Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry);
        Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync();

        Task<IQuestTemplate?> GetQuestTemplate(uint entry);
        Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId);
        Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex);
        Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId);
        Task<IQuestRequestItem?> GetQuestRequestItem(uint entry);

        Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync();
        Task<IGossipMenu?> GetGossipMenuAsync(uint menuId);
        Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);
        Task<INpcText?> GetNpcText(uint entry);
        Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync();

        Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync();
        Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry);

        Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList);
        Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type);

        Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId);

        Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(ConditionKeyMask keyMask, ConditionKey manualKey);
        Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(ConditionKeyMask keyMask, ICollection<ConditionKey> manualKeys);

        Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId);

        Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType);

        Task<IReadOnlyList<IQuestScriptName>> GetQuestScriptNames(uint questId) =>
            Task.FromResult<IReadOnlyList<IQuestScriptName>>(new List<IQuestScriptName>());

        Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync();
        Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync();
        Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId);

        Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text);
        Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id);

        Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text);

        Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry);
        Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry);
        Task<IReadOnlyList<ICreature>> GetCreaturesAsync();
        Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync();
        Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids);
        Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids);
        Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map);
        Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map);

        /// <summary>Highest creature.guid (0 when the table is empty). The default is correct
        /// but loads the whole table; backends override it with a real MAX query.</summary>
        async Task<uint> GetMaxCreatureGuid()
        {
            var creatures = await GetCreaturesAsync();
            return creatures.Count == 0 ? 0 : creatures.Max(c => c.Guid);
        }

        /// <summary>Highest gameobject.guid (0 when the table is empty). The default is correct
        /// but loads the whole table; backends override it with a real MAX query.</summary>
        async Task<uint> GetMaxGameObjectGuid()
        {
            var gameObjects = await GetGameObjectsAsync();
            return gameObjects.Count == 0 ? 0 : gameObjects.Max(g => g.Guid);
        }

        Task<IReadOnlyList<ITrinityString>> GetStringsAsync();
        Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync();
        Task<IReadOnlyList<IDatabaseSpellEffectDbc>> GetSpellEffectDbcAsync() => Task.FromResult<IReadOnlyList<IDatabaseSpellEffectDbc>>(new List<IDatabaseSpellEffectDbc>());
        Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync();
        Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync();
        Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id);
        Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type);
        Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id);
        Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations();
        // CMaNGOS-only spawn-group extras; null on cores without the tables
        Task<IReadOnlyList<ISpawnGroupRandomEntry>?> GetSpawnGroupRandomEntriesAsync() => Task.FromResult<IReadOnlyList<ISpawnGroupRandomEntry>?>(null);
        Task<IReadOnlyList<ISpawnGroupLinkedGroup>?> GetSpawnGroupLinkedGroupsAsync() => Task.FromResult<IReadOnlyList<ISpawnGroupLinkedGroup>?>(null);
        Task<IReadOnlyList<ISpawnGroupSquadMember>?> GetSpawnGroupSquadsAsync() => Task.FromResult<IReadOnlyList<ISpawnGroupSquadMember>?>(null);

        // CMaNGOS-style spawn pools; null on cores without the tables (Trinity's unified
        // pool_members can implement these later; HttpDatabase pass-through can be added when needed)
        Task<IReadOnlyList<IPoolTemplate>?> GetPoolTemplatesAsync() => Task.FromResult<IReadOnlyList<IPoolTemplate>?>(null);
        Task<IPoolTemplate?> GetPoolTemplateByIdAsync(uint entry) => Task.FromResult<IPoolTemplate?>(null);
        Task<IReadOnlyList<IPoolCreatureMember>?> GetPoolCreaturesAsync() => Task.FromResult<IReadOnlyList<IPoolCreatureMember>?>(null);
        Task<IReadOnlyList<IPoolGameObjectMember>?> GetPoolGameObjectsAsync() => Task.FromResult<IReadOnlyList<IPoolGameObjectMember>?>(null);
        Task<IReadOnlyList<IPoolCreatureEntryMember>?> GetPoolCreatureEntryPoolsAsync() => Task.FromResult<IReadOnlyList<IPoolCreatureEntryMember>?>(null);
        Task<IReadOnlyList<IPoolGameObjectEntryMember>?> GetPoolGameObjectEntryPoolsAsync() => Task.FromResult<IReadOnlyList<IPoolGameObjectEntryMember>?>(null);
        Task<IReadOnlyList<IPoolNesting>?> GetPoolNestingsAsync() => Task.FromResult<IReadOnlyList<IPoolNesting>?>(null);

        // CMaNGOS-only creature linking (guid + entry variants); null on cores without the tables
        Task<IReadOnlyList<ICreatureLinking>?> GetCreatureLinkingsAsync() => Task.FromResult<IReadOnlyList<ICreatureLinking>?>(null);
        Task<IReadOnlyList<ICreatureLinkingTemplate>?> GetCreatureLinkingTemplatesAsync() => Task.FromResult<IReadOnlyList<ICreatureLinkingTemplate>?>(null);

        // CMaNGOS-only world safe locs (graveyards) + spell target positions; null on cores without the tables
        Task<IReadOnlyList<IWorldSafeLoc>?> GetWorldSafeLocsAsync() => Task.FromResult<IReadOnlyList<IWorldSafeLoc>?>(null);
        Task<IReadOnlyList<IGraveyardLink>?> GetGraveyardLinksAsync() => Task.FromResult<IReadOnlyList<IGraveyardLink>?>(null);
        Task<IReadOnlyList<ISpellTargetPosition>?> GetSpellTargetPositionsAsync() => Task.FromResult<IReadOnlyList<ISpellTargetPosition>?>(null);

        Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IReadOnlyList<IItem>?>(null);

        Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);

        Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => Task.FromResult<IReadOnlyList<IEventScriptLine>>(new List<IEventScriptLine>());
        Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) => Task.FromResult<IReadOnlyList<IEventScriptLine>>(new List<IEventScriptLine>());

        Task<IReadOnlyList<IEventAiLine>> GetEventAi(int id) => Task.FromResult<IReadOnlyList<IEventAiLine>>(new List<IEventAiLine>());
        Task<IReadOnlyList<IEventAiLine>> FindEventAiLinesBy(IEnumerable<(EventAiLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => Task.FromResult<IReadOnlyList<IEventAiLine>>(new List<IEventAiLine>());

        Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync();
        Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId);
        Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync();

        Task<IPhaseName?> GetPhaseNameAsync(uint phaseId);
        Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync();

        Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId);

        Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons();
        Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();
        Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates();
        Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates();
        Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync();
        Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync();
        Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid);
        Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid);
        Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry);
        Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid);
        Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid);
        Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid);
        Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);

        Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId);
        Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count = 1);
        Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId);
        Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId);
        Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid);
        Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId);
        Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId);

        // TC master's per-path waypoint_path metadata row. Default null so only master overrides it.
        // NB: like every default method here it MUST be explicitly forwarded in
        // CachedDatabaseProvider and WorldDatabaseDecorator, or their callers silently get null.
        Task<IWaypointPathHeader?> GetWaypointPathHeader(uint pathId) =>
            Task.FromResult<IWaypointPathHeader?>(null);

        // creature_formations (leader↔member follow links). Default empty so only the cores that
        // actually have the table override it (see the Trinity providers + the two wrappers).
        Task<IReadOnlyList<ICreatureFormation>> GetCreatureFormations() =>
            Task.FromResult<IReadOnlyList<ICreatureFormation>>(new List<ICreatureFormation>());

        Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry);
        Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type);
        Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry);
        Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type);

        Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId);
        Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId);
        Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId);
        Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId);
        Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId);

        Task<IReadOnlyList<IConversationActor>> GetConversationActors();
        Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates();

        Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid);
        Task<IReadOnlyList<ICoreCommandHelp>> GetCommands();
        Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId);

        Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId);
        Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId);

        // bulk variants of the two above (every quest at once) - for "does this entry start/end any
        // quest" lookups, e.g. the 3D view's NPC status icons. Null = provider without the data.
        Task<IReadOnlyList<IQuestRelation>?> GetAllQuestStarters() => Task.FromResult<IReadOnlyList<IQuestRelation>?>(null);
        Task<IReadOnlyList<IQuestRelation>?> GetAllQuestEnders() => Task.FromResult<IReadOnlyList<IQuestRelation>?>(null);

        Task<IReadOnlyList<IQuestFactionChange>> GetQuestFactionChanges() => Task.FromResult<IReadOnlyList<IQuestFactionChange>>([]);

        Task<IReadOnlyList<IGarrisonMissionTemplate>> GetGarrisonMissionTemplates() => Task.FromResult<IReadOnlyList<IGarrisonMissionTemplate>>([]);
        Task<IGarrisonMissionTemplate?> GetGarrisonMissionTemplate(int entry) => Task.FromResult<IGarrisonMissionTemplate?>(null);

        // @todo: make it async one day
        IList<IPhaseName>? GetPhaseNames();

        public enum SmartLinePropertyType
        {
            Event,
            Action,
            Target,
            Source
        }

        public enum EventAiLinePropertyType
        {
            Event,
            Action1,
            Action2,
            Action3
        }

        [Flags]
        public enum ConditionKeyMask
        {
            SourceGroup = 1,
            SourceEntry = 2,
            SourceId = 4,
            All = SourceGroup | SourceEntry | SourceId,
            None = 0
        }

        public struct ConditionKey : IEquatable<ConditionKey>
        {
            public readonly int SourceType;
            public readonly long? SourceGroup;
            public readonly int? SourceEntry;
            public readonly int? SourceId;

            public ConditionKey(int sourceType, long? sourceGroup = null, int? sourceEntry = null, int? sourceId = null)
            {
                SourceType = sourceType;
                SourceGroup = sourceGroup;
                SourceEntry = sourceEntry;
                SourceId = sourceId;
            }

            public ConditionKey WithGroup(long group)
            {
                return new ConditionKey(SourceType, group, SourceEntry, SourceId);
            }

            public ConditionKey WithEntry(int entry)
            {
                return new ConditionKey(SourceType, SourceGroup, entry, SourceId);
            }

            public ConditionKey WithId(int id)
            {
                return new ConditionKey(SourceType, SourceGroup, SourceEntry, id);
            }

            public ConditionKey WithMask(ConditionKeyMask mask)
            {
                return new ConditionKey(SourceType,
                    mask.HasFlagFast(ConditionKeyMask.SourceGroup) ? SourceGroup : null,
                    mask.HasFlagFast(ConditionKeyMask.SourceEntry) ? SourceEntry : null,
                    mask.HasFlagFast(ConditionKeyMask.SourceId) ? SourceId : null);
            }

            public bool Equals(ConditionKey other)
            {
                return SourceType == other.SourceType && SourceGroup == other.SourceGroup && SourceEntry == other.SourceEntry && SourceId == other.SourceId;
            }

            public override bool Equals(object? obj)
            {
                return obj is ConditionKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(SourceType, SourceGroup, SourceEntry, SourceId);
            }

            public static bool operator ==(ConditionKey left, ConditionKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ConditionKey left, ConditionKey right)
            {
                return !left.Equals(right);
            }
        }
    }

    [UniqueProvider]
    public interface IMangosDatabaseProvider : IDatabaseProvider
    {
        Task<IReadOnlyList<IDbScriptRandomTemplate>?> GetScriptRandomTemplates(uint id, RandomTemplateType type)
        {
            return Task.FromResult<IReadOnlyList<IDbScriptRandomTemplate>?>(null);
        }

        Task<ICreatureAiSummon?> GetCreatureAiSummon(uint entry);

        /// <summary>All rows of one dbscripts_on_* table with the given script id, ordered by priority.</summary>
        Task<IReadOnlyList<IDbScriptLine>> GetDbScript(string tableName, uint id)
        {
            return Task.FromResult<IReadOnlyList<IDbScriptLine>>(new List<IDbScriptLine>());
        }

        /// <summary>Distinct script ids present in one dbscripts_on_* table (for pickers).</summary>
        Task<IReadOnlyList<uint>> GetDbScriptIds(string tableName)
        {
            return Task.FromResult<IReadOnlyList<uint>>(new List<uint>());
        }

        /// <summary>Rows of the `conditions` table with the given condition_entry values.</summary>
        Task<IReadOnlyList<IMangosConditionLine>> GetConditionsByEntries(IReadOnlyList<uint> entries)
        {
            return Task.FromResult<IReadOnlyList<IMangosConditionLine>>(new List<IMangosConditionLine>());
        }

        /// <summary>Highest condition_entry in the `conditions` table (0 when empty).</summary>
        Task<uint> GetMaxConditionEntry()
        {
            return Task.FromResult(0u);
        }

        /// <summary>One row of the `unit_condition` table, null when missing.</summary>
        Task<IMangosUnitConditionLine?> GetUnitConditionById(int id)
        {
            return Task.FromResult<IMangosUnitConditionLine?>(null);
        }

        /// <summary>Lowest unit_condition.Id (0 when the table is empty). Ids are signed;
        /// custom rows use negative ids.</summary>
        Task<int> GetMinUnitConditionId()
        {
            return Task.FromResult(0);
        }

        public enum RandomTemplateType
        {
            Text = 0,
            RelayScript = 1
        }
    }

}
