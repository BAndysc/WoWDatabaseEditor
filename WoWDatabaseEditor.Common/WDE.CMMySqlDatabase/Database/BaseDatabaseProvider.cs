using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.MySqlDatabaseCommon.CommonModels;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.MySqlDatabaseCommon.Services;
using WDE.CMMySqlDatabase.Models;

namespace WDE.CMMySqlDatabase.Database
{
    public abstract class BaseDatabaseProvider<T> : IAsyncDatabaseProvider, IAuthDatabaseProvider, IMangosDatabaseProvider where T : BaseDatabaseTables, new()
    {
        public bool IsConnected => true;
        public abstract Task<ICreatureTemplate?> GetCreatureTemplate(uint entry);
        public abstract IReadOnlyList<ICreatureTemplate> GetCreatureTemplates();
        
        public async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry)
        {
            return Array.Empty<ICreatureTemplateDifficulty>();
        }

        private readonly ICurrentCoreVersion currentCoreVersion;

        public BaseDatabaseProvider(IWorldDatabaseSettingsProvider settings,
            IAuthDatabaseSettingsProvider authSettings,
            DatabaseLogger databaseLogger,
            ICurrentCoreVersion currentCoreVersion)
        {
            this.currentCoreVersion = currentCoreVersion;
            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = databaseLogger.Log;
            DataConnection.DefaultSettings = new MySqlWorldSettings(settings.Settings, authSettings.Settings);
        }

        protected T Database() => new();
        
        public async Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry)
        {
            return new List<ICreatureText>();
        }

        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry)
        {
            return null;
        }
        
        public async Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
        {
            return new List<ISmartScriptLine>();
        }

        public IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type)
        {
            return new List<ISmartScriptLine>();
        }

        public async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId) => new List<IQuestObjective>();

        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => null;
        
        public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => null;

        public async Task<IQuestRequestItem?> GetQuestRequestItem(uint entry)
        {
            return null;
        }

        public IEnumerable<IGameEvent> GetGameEvents()
        {
            using var model = Database();
            return (from t in model.GameEvents orderby t.Entry select t).ToList<IGameEvent>();
        }
        
        public async Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync()
        {
            await using var model = Database();
            return await (from t in model.GameEvents orderby t.Entry select t).ToListAsync<IGameEvent>();
        }
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates()
        {
            return new List<IConversationTemplate>();
        }

        public abstract void ConnectOrThrow();
        public abstract Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesAsync();

        public async Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            return new List<IConversationTemplate>();
        }
        
        public IEnumerable<IGossipMenu> GetGossipMenus()
        {
            using var model = Database();

            List<GossipMenuLineWoTLK> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = model.GossipMenus.ToList();
            }
            else
            {
                gossips = (from gossip in model.GossipMenus
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToList();   
            }

            return gossips.GroupBy(g => g.MenuId)
                .Select(t => new GossipMenuWoTLK(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }
        
        public async Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync()
        {
            await using var model = Database();

            List<GossipMenuLineWoTLK> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = await model.GossipMenus.ToListAsync();
            }
            else
            {
                gossips = await (from gossip in model.GossipMenus
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToListAsync();   
            }

            return gossips.GroupBy(g => g.MenuId)
                .Select(t => new GossipMenuWoTLK(t.Key, t.Where(t => t.Text != null).Select(t => t.Text!).ToList()))
                .ToList<IGossipMenu>();
        }

        public async Task<IGossipMenu?> GetGossipMenuAsync(uint menuId)
        {
            await using var model = Database();

            List<GossipMenuLineWoTLK> gossips;
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
            {
                gossips = await model.GossipMenus.Where(g => g.MenuId == menuId).ToListAsync();
            }
            else
            {
                gossips = await (from gossip in model.GossipMenus
                    where gossip.MenuId == menuId
                    join p in model.NpcTexts on gossip.TextId equals p.Id into lj
                    from lp in lj.DefaultIfEmpty()
                    select gossip.SetText(lp)).ToListAsync();   
            }

            if (gossips.Count == 0)
                return null;

            return new GossipMenuWoTLK(menuId, gossips.Where(t => t.Text != null).Select(t => t.Text!).ToList());
        }

        public abstract Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);
        public abstract List<IGossipMenuOption> GetGossipMenuOptions(uint menuId);

        public async Task<INpcText?> GetNpcText(uint entry)
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return null;

            await using var model = Database();
            return await model.NpcTexts.FirstOrDefaultAsync(text => text.Id == entry);
        }

        public IEnumerable<INpcText> GetNpcTexts()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            using var model = Database();
            return (from t in model.NpcTexts orderby t.Id select t).ToList<INpcText>();
        }

        public async Task<IReadOnlyList<INpcText>> GetNpcTextsAsync()
        {
            if (currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(INpcText)))
                return new List<INpcText>();
            
            await using var model = Database();
            return await (from t in model.NpcTexts orderby t.Id select t).ToListAsync<INpcText>();
        }

        public abstract Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync();

        public async Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry)
        {
            return null;
        }
        
        public async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => null;

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates()
        {
            return new List<IAreaTriggerTemplate>();
        }

        public abstract IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats();

        public abstract Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync();
        
        public async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) => new List<ISmartScriptLine>();

        public async Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
        {
            return new List<IAreaTriggerTemplate>();
        }
        
        public abstract IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates();
        public abstract Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync();
        public abstract Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids);

        protected virtual IQueryable<QuestTemplateWoTLK> GetQuestsQuery(BaseDatabaseTables model)
        {
            return from t in model.QuestTemplate
                orderby t.Entry
                select t;
        }
        
        public virtual IReadOnlyList<IQuestTemplate> GetQuestTemplates()
        {
            using var model = Database();

            return GetQuestsQuery(model).ToList<IQuestTemplate>();
        }

        public virtual async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            await using var model = Database();
            var o = GetQuestsQuery(model);
            return await o.ToListAsync<IQuestTemplate>();
        }

        public virtual async Task<IQuestTemplate?> GetQuestTemplate(uint entry)
        {
            await using var model = Database();
            return model.QuestTemplate.FirstOrDefault(q => q.Entry == entry);
        }

        public async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int questSortId)
        {
            await using var model = Database();
            return await model.QuestTemplate.Where(t => t.QuestSortId == questSortId)
                .OrderBy(t => t.Entry)
                .ToListAsync<IQuestTemplate>();
        }

        protected abstract Task<ICreature?> GetCreatureByGuidAsync(T model, uint guid);
        protected abstract Task<IGameObject?> GetGameObjectByGuidAsync(T model, uint guid);
        protected abstract Task SetCreatureTemplateAI(T model, uint entry, string ainame, string scriptname);

        public async Task InstallConditions(IEnumerable<IConditionLine> conditionLines,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null)
        {
        }

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId)
        {
            return new List<IConditionLine>();
        }

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key)
        {
            return new List<IConditionLine>();
        }

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
        {
            return new List<IConditionLine>();
        }

        public async Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
        {
            return new List<int>();
        }

        public async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;

        public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId)
        {
            await using var model = Database();
            return await model.SpellScriptNames.Where(spell => spell.SpellId == spellId).ToListAsync();
        }

        public async Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text)
        {
            await using var model = Database();
            return await model.BroadcastTextLocale.FirstOrDefaultAsync(b => b.Text == text || b.Text1 == text);
        }

        public abstract IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry);
        public abstract Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid);
        public abstract IEnumerable<ICreature> GetCreaturesByEntry(uint entry);


        public abstract IReadOnlyList<ICreature> GetCreatures();
        public abstract Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map);
        public abstract Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map);
        public abstract IEnumerable<IGameObject> GetGameObjects();

        public async Task<IReadOnlyList<ICoreCommandHelp>> GetCommands()
        {
            using var model = Database();
            return model.Commands.ToList();
        }

        public abstract Task<IReadOnlyList<ITrinityString>> GetStringsAsync();
        public abstract Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync();

        public async Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync()
        {
            if (!Supports<IPointOfInterest>())
                return new List<IPointOfInterest>();
            
            await using var model = Database();
            return await model.PointsOfInterest.OrderBy(t => t.Id).ToListAsync<IPointOfInterest>();
        }

        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => Enumerable.Empty<ISmartScriptProject>();
        public abstract IBroadcastText? GetBroadcastTextByText(string text);
        public abstract Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text);
        public abstract Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id);

        public async Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            return new List<ISmartScriptLine>();
        }
        
        public virtual Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IReadOnlyList<IItem>?>(null);

        private ExpressionStarter<R> GenerateWhereConditionsForEventScript<R>(IEnumerable<(uint command, int dataIndex, long valueToSearch)> conditions) where R : IEventScriptLine
        {
            var predicate = PredicateBuilder.New<R>();
            foreach (var value in conditions)
            {
                if (value.dataIndex == 0)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataLong1 == (ulong)value.valueToSearch);
                else if (value.dataIndex == 1)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataLong2 == (ulong)value.valueToSearch);
                else if (value.dataIndex == 2)
                    predicate = predicate.Or(o => o.Command == value.command && o.DataInt == (int)value.valueToSearch);
            }
            return predicate;
        }

        public async Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions)
        {
            await using var model = Database();
            var events = await model.EventScripts.Where(GenerateWhereConditionsForEventScript<MySqlEventScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            var spells = await model.SpellScripts.Where(GenerateWhereConditionsForEventScript<MySqlSpellScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            var waypoints = await model.WaypointScripts.Where(GenerateWhereConditionsForEventScript<MySqlWaypointScriptLine>(conditions)).ToListAsync<IEventScriptLine>();
            return events.Concat(spells).Concat(waypoints).ToList();
        }

        public abstract Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync();

        public abstract Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId);

        public async Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => Array.Empty<IEventScriptLine>();

        public ISceneTemplate? GetSceneTemplate(uint sceneId)
        {
            if (!Supports<ISceneTemplate>())
                return null;
            using var model = Database();
            return model.SceneTemplates.FirstOrDefault(x => x.SceneId == sceneId);
        }
        
        public async Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId)
        {
            if (!Supports<ISceneTemplate>())
                return null;
            await using var model = Database();
            return await model.SceneTemplates.FirstOrDefaultAsync(x => x.SceneId == sceneId);
        }

        public async Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync()
        {
            if (!Supports<ISceneTemplate>())
                return null; 
            await using var model = Database();
            return await model.SceneTemplates.ToListAsync<ISceneTemplate>();
        }

        public async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => null;

        public async Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync() => null;
        
        public async Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId)
        {
            return Array.Empty<INpcSpellClickSpell>();
        }

        public IList<IPhaseName>? GetPhaseNames() => null;

        public abstract Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons();
        public abstract Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();

        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates()
        {
            return null;
        }

        public async Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync()
        {
            await using var model = Database();
            return await model.GameEventCreature.ToListAsync<IGameEventCreature>();
        }

        public async Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync()
        {
            await using var model = Database();
            return await model.GameEventGameObject.ToListAsync<IGameEventGameObject>();
        }

        public async Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid)
        {
            await using var model = Database();
            return await model.GameEventCreature.Where(x => x.Guid == guid).ToListAsync<IGameEventCreature>();
        }

        public async Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid)
        {
            await using var model = Database();
            return await model.GameEventGameObject.Where(x => x.Guid == guid).ToListAsync<IGameEventGameObject>();
        }

        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry)
        {
            return null;
        }

        public async Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates()
        {
            await using var model = Database();
            return await model.CreatureEquipmentTemplate.ToListAsync<IMangosCreatureEquipmentTemplate>();
        }

        public abstract Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid);

        public abstract Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid);
        public abstract Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid);
        public abstract Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);

        public async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => null;

        public async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count) => null;

        public async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => null;

        public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId)
        {
            await using var model = Database();
            return await model.WaypointPath.Where(x => x.PathId == pathId).OrderBy(x => x.PointId).ToListAsync<IMangosWaypoint>();
        }

        public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid)
        {
            await using var model = Database();
            return await model.CreatureMovement.Where(x => x.Guid == guid).OrderBy(x => x.PointId).ToListAsync<IMangosCreatureMovement>();
        }

        public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId)
        {
            await using var model = Database();
            var predicate = PredicateBuilder.New<MangosCreatureMovementTemplate>(x => x.Entry == entry);
            if (pathId.HasValue)
                predicate = predicate.And(x => x.PathId == pathId);
            return await model.CreatureMovementTemplate.Where(predicate).OrderBy(x => x.PointId).ToListAsync<IMangosCreatureMovementTemplate>();
        }

        public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId)
        {
            await using var model = Database();
            return await model.WaypointPathName.FirstOrDefaultAsync(x => x.PathId == pathId);
        }

        public async Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry)
        {
            if (type != LootSourceType.Reference)
                return null;
            await using var model = Database();
            return await model.ReferenceLootTemplateNames.FirstOrDefaultAsync(x => x.Entry == entry);
        }
        
        public async Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type)
        {
            if (type != LootSourceType.Reference)
                return Array.Empty<ILootTemplateName>();
            await using var model = Database();
            return await model.ReferenceLootTemplateNames.OrderBy(x => x.Entry).ToListAsync();
        }

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId);

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId);

        public abstract Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId);

        protected virtual bool SupportsProspectingLootTemplate => true;
        
        public async Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId)
        {
            if (lootId == 0)
                return Array.Empty<ILootEntry>();
            await using var database = Database();
            int minCountOrRef = -(int)lootId;
            var loot = new[]
            {
                await database.CreatureLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.GameObjectLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.ItemLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.FishingLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.SkinningLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.DisenchantLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                SupportsProspectingLootTemplate ? await database.ProspectingLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>() : new List<ILootEntry>(),
                await database.MailLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>(),
                await database.ReferenceLootTemplate.Where(x => x.MinCountOrReference == minCountOrRef).ToListAsync<ILootEntry>()
            };
            return loot.SelectMany(x => x).ToList();
        }

        public async Task<IReadOnlyList<IConversationActor>> GetConversationActors() => Array.Empty<IConversationActor>();

        public async Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates() => Array.Empty<IConversationActorTemplate>();

        public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type)
        {
            await using var database = Database();
            switch (type)
            {
                case LootSourceType.Item:
                    return await database.ItemLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.GameObject:
                    return await database.GameObjectLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Fishing:
                    return await database.FishingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Skinning:
                    return await database.SkinningLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Disenchant:
                    return await database.DisenchantLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Prospecting:
                    if (!SupportsProspectingLootTemplate)
                        return Array.Empty<ILootEntry>();
                    return await database.ProspectingLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Reference:
                    return await database.ReferenceLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Creature:
                    return await database.CreatureLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                case LootSourceType.Mail:
                    return await database.MailLootTemplate.OrderBy(x => x.Entry)
                        .ThenBy(x => x.GroupId)
                        .ThenBy(x => x.ItemOrCurrencyId)
                        .ToListAsync<ILootEntry>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry)
        {
            await using var database = Database();
            switch (type)
            {
                case LootSourceType.Item:
                    return await database.ItemLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.GameObject:
                    return await database.GameObjectLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Fishing:
                    return await database.FishingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Skinning:
                    return await database.SkinningLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Disenchant:
                    return await database.DisenchantLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Prospecting:
                    if (!SupportsProspectingLootTemplate)
                        return Array.Empty<ILootEntry>();
                    return await database.ProspectingLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Reference:
                    return await database.ReferenceLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Creature:
                    return await database.CreatureLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                case LootSourceType.Mail:
                    return await database.MailLootTemplate.Where(x => x.Entry == entry).ToListAsync<ILootEntry>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public abstract Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId);

        public async Task<IReadOnlyList<IEventAiLine>> GetEventAi(int entryOrGuid) 
        {
            await using var model = Database();
            return await model.CreatureAiScripts.Where(x => x.CreatureIdOrGuid == entryOrGuid).OrderBy(x => x.Id).ToListAsync<IEventAiLine>();
        }

        public async Task<IList<IAuthRbacPermission>> GetRbacPermissionsAsync()
        {
            return new List<IAuthRbacPermission>();
        }

        public async Task<IList<IAuthRbacLinkedPermission>> GetLinkedPermissionsAsync()
        {
            return new List<IAuthRbacLinkedPermission>();
        }
        
        public async Task<IReadOnlyList<IDbScriptRandomTemplate>?> GetScriptRandomTemplates(uint id, IMangosDatabaseProvider.RandomTemplateType type)
        {
            await using var model = Database();
            return await model.DbScriptRandomTemplates.Where(x => x.Id == id && x.Type == (int)type).ToListAsync<IDbScriptRandomTemplate>();
        }

        public async Task<ICreatureAiSummon?> GetCreatureAiSummon(uint entry)
        {
            await using var model = Database();
            return await model.CreatureAiSummons.FirstOrDefaultAsync(x => x.Id == entry);
        }

        private bool Supports<R>()
        {
            return !currentCoreVersion.Current.DatabaseFeatures.UnsupportedTables.Contains(typeof(R));
        }
        
        public async Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync()
        {
            await using var model = Database();
            return await model.SpawnGroupTemplate.ToListAsync<ISpawnGroupTemplate>();
        }
    
        public async Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync()
        {
            await using var model = Database();
            return await model.SpawnGroupSpawns.ToListAsync<ISpawnGroupSpawn>();
        }
    
        public async Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id)
        {
            await using var model = Database();
            return await model.SpawnGroupTemplate.FirstOrDefaultAsync<ISpawnGroupTemplate>(x => x.Id == id);
        }

        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id)
        {
            await using var model = Database();
            return await model.SpawnGroupFormations.FirstOrDefaultAsync<ISpawnGroupFormation>(x => x.Id == id);
        }

        public async Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations()
        {
            await using var model = Database();
            return await model.SpawnGroupFormations.ToListAsync<ISpawnGroupFormation>();
        }
    
        public async Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type)
        {
            await using var model = Database();
            return await model.SpawnGroupSpawns.InnerJoin(
                model.SpawnGroupTemplate, 
                (t1, t2) => t1.TemplateId == t2.Id && t2.Type == type,
                (t1, t2) => t1)
                .FirstOrDefaultAsync(t1 => t1.Guid == guid);
        }

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId)
        {
            await using var model = Database();
            var creatures = await model.CreatureQuestStarters.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            var gameobjects = await model.GameObjectQuestStarters.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            creatures.AddRange(gameobjects);
            return creatures;
        }

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId)
        {
            await using var model = Database();
            var creatures = await model.CreatureQuestEnders.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            var gameobjects = await model.GameObjectQuestEnders.Where(x => x.Quest == questId).ToListAsync<IQuestRelation>();
            creatures.AddRange(gameobjects);
            return creatures;
        }
    }
}
