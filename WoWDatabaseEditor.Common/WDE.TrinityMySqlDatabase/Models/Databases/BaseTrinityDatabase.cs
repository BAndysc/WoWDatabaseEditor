﻿using System;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models
{
    public abstract class BaseTrinityDatabase : DataConnection
    {
        public BaseTrinityDatabase() : base("Trinity")
        {
        }

        public ITable<MySqlAreaTriggerScript> AreaTriggerScript => GetTable<MySqlAreaTriggerScript>();
        public ITable<MySqlGameObjectTemplate> GameObjectTemplate => GetTable<MySqlGameObjectTemplate>();
        public ITable<MySqlQuestTemplate> QuestTemplate => GetTable<MySqlQuestTemplate>();
        public ITable<MySqlQuestRequestItem> QuestRequestItems => GetTable<MySqlQuestRequestItem>();
        public ITable<MySqlMasterQuestTemplate> MasterQuestTemplate => GetTable<MySqlMasterQuestTemplate>();
        public ITable<MySqlQuestTemplateAddonWithScriptName> QuestTemplateAddonWithScriptName => GetTable<MySqlQuestTemplateAddonWithScriptName>();
        public ITable<MySqlAreaTriggerTemplate> AreaTriggerTemplate => GetTable<MySqlAreaTriggerTemplate>();
        public ITable<MySqlGameEvent> GameEvents => GetTable<MySqlGameEvent>();
        public ITable<MySqlConversationTemplate> ConversationTemplate => GetTable<MySqlConversationTemplate>();
        public ITable<MySqlConditionLine> Conditions => GetTable<MySqlConditionLine>();
        public ITable<MySqlSpellScriptName> SpellScriptNames => GetTable<MySqlSpellScriptName>();
        public ITable<MySqlGossipMenuLine> GossipMenus => GetTable<MySqlGossipMenuLine>();
        public ITable<MySqlGossipMenuOptionWrath> GossipMenuOptions => GetTable<MySqlGossipMenuOptionWrath>();
        public ITable<MySqlGossipMenuOptionMaster> GossipMenuOptionsMaster => GetTable<MySqlGossipMenuOptionMaster>();
        public ITable<MySqlNpcText> NpcTexts => GetTable<MySqlNpcText>();
        public ITable<MySqlCreatureClassLevelStat> CreatureClassLevelStats => GetTable<MySqlCreatureClassLevelStat>();
        public ITable<CoreCommandHelp> Commands => GetTable<CoreCommandHelp>();
        public ITable<MySqlPointOfInterest> PointsOfInterest => GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => GetTable<MySqlCreatureText>();
        public ITable<MySqlAreaTriggerCreateProperties> AreaTriggerCreateProperties => GetTable<MySqlAreaTriggerCreateProperties>();
        public ITable<MySqlSceneTemplate> SceneTemplates => GetTable<MySqlSceneTemplate>();
        public ITable<MySqlCreatureEquipmentTemplate> EquipmentTemplate => GetTable<MySqlCreatureEquipmentTemplate>();
        public ITable<MySqlGameEventCreature> GameEventCreature => GetTable<MySqlGameEventCreature>();
        public ITable<MySqlGameEventGameObject> GameEventGameObject => GetTable<MySqlGameEventGameObject>();
        public ITable<ScriptWaypoint> ScriptWaypoint => GetTable<ScriptWaypoint>();
        public ITable<SmartScriptWaypoint> SmartScriptWaypoint => GetTable<SmartScriptWaypoint>();
        public ITable<MySqlSpawnGroupSpawn> SpawnGroupSpawns => GetTable<MySqlSpawnGroupSpawn>();
        public ITable<MySqlSpawnGroupTemplate> SpawnGroupTemplate => GetTable<MySqlSpawnGroupTemplate>();
        public ITable<MySqlSmartScriptLine> BaseSmartScript => GetTable<MySqlSmartScriptLine>();
        public ITable<ItemLootTemplate> ItemLootTemplate => GetTable<ItemLootTemplate>();
        public ITable<PickpocketingLootTemplate> PickpocketingLootTemplate => GetTable<PickpocketingLootTemplate>();
        public ITable<CreatureLootTemplate> CreatureLootTemplate => GetTable<CreatureLootTemplate>();
        public ITable<DisenchantLootTemplate> DisenchantLootTemplate => GetTable<DisenchantLootTemplate>();
        public ITable<ProspectingLootTemplate> ProspectingLootTemplate => GetTable<ProspectingLootTemplate>();
        public ITable<MillingLootTemplate> MillingLootTemplate => GetTable<MillingLootTemplate>();
        public ITable<ReferenceLootTemplate> ReferenceLootTemplate => GetTable<ReferenceLootTemplate>();
        public ITable<SpellLootTemplate> SpellLootTemplate => GetTable<SpellLootTemplate>();
        public ITable<MailLootTemplate> MailLootTemplate => GetTable<MailLootTemplate>();
        public ITable<GameObjectLootTemplate> GameObjectLootTemplate => GetTable<GameObjectLootTemplate>();
        public ITable<FishingLootTemplate> FishingLootTemplate => GetTable<FishingLootTemplate>();
        public ITable<SkinningLootTemplate> SkinningLootTemplate => GetTable<SkinningLootTemplate>();
    }
}