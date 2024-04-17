using System;
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

        public ITable<MySqlAreaTriggerScript> AreaTriggerScript => this.GetTable<MySqlAreaTriggerScript>();
        public ITable<MySqlGameObjectTemplate> GameObjectTemplate => this.GetTable<MySqlGameObjectTemplate>();
        public ITable<MySqlQuestTemplate> QuestTemplate => this.GetTable<MySqlQuestTemplate>();
        public ITable<MySqlQuestRequestItem> QuestRequestItems => this.GetTable<MySqlQuestRequestItem>();
        public ITable<MySqlMasterQuestTemplate> MasterQuestTemplate => this.GetTable<MySqlMasterQuestTemplate>();
        public ITable<MySqlQuestTemplateAddonWithScriptName> QuestTemplateAddonWithScriptName => this.GetTable<MySqlQuestTemplateAddonWithScriptName>();
        public ITable<MySqlAreaTriggerTemplate> AreaTriggerTemplate => this.GetTable<MySqlAreaTriggerTemplate>();
        public ITable<MySqlGameEvent> GameEvents => this.GetTable<MySqlGameEvent>();
        public ITable<MySqlConversationTemplate> ConversationTemplate => this.GetTable<MySqlConversationTemplate>();
        public ITable<MySqlConditionLine> Conditions => this.GetTable<MySqlConditionLine>();
        public ITable<MySqlSpellScriptName> SpellScriptNames => this.GetTable<MySqlSpellScriptName>();
        public ITable<MySqlGossipMenuLine> GossipMenus => this.GetTable<MySqlGossipMenuLine>();
        public ITable<MySqlGossipMenuOptionWrath> GossipMenuOptions => this.GetTable<MySqlGossipMenuOptionWrath>();
        public ITable<MySqlGossipMenuOptionMaster> GossipMenuOptionsMaster => this.GetTable<MySqlGossipMenuOptionMaster>();
        public ITable<MySqlNpcText> NpcTexts => this.GetTable<MySqlNpcText>();
        public ITable<MySqlCreatureClassLevelStat> CreatureClassLevelStats => this.GetTable<MySqlCreatureClassLevelStat>();
        public ITable<CoreCommandHelp> Commands => this.GetTable<CoreCommandHelp>();
        public ITable<MySqlPointOfInterest> PointsOfInterest => this.GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => this.GetTable<MySqlCreatureText>();
        public ITable<MySqlAreaTriggerCreateProperties> AreaTriggerCreateProperties => this.GetTable<MySqlAreaTriggerCreateProperties>();
        public ITable<MySqlSceneTemplate> SceneTemplates => this.GetTable<MySqlSceneTemplate>();
        public ITable<MySqlCreatureEquipmentTemplate> EquipmentTemplate => this.GetTable<MySqlCreatureEquipmentTemplate>();
        public ITable<MySqlGameEventCreature> GameEventCreature => this.GetTable<MySqlGameEventCreature>();
        public ITable<MySqlGameEventGameObject> GameEventGameObject => this.GetTable<MySqlGameEventGameObject>();
        public ITable<ScriptWaypoint> ScriptWaypoint => this.GetTable<ScriptWaypoint>();
        public ITable<SmartScriptWaypoint> SmartScriptWaypoint => this.GetTable<SmartScriptWaypoint>();
        public ITable<MySqlSpawnGroupSpawn> SpawnGroupSpawns => this.GetTable<MySqlSpawnGroupSpawn>();
        public ITable<MySqlSpawnGroupTemplate> SpawnGroupTemplate => this.GetTable<MySqlSpawnGroupTemplate>();
        public ITable<MySqlSmartScriptLine> BaseSmartScript => this.GetTable<MySqlSmartScriptLine>();
        public ITable<ItemLootTemplate> ItemLootTemplate => this.GetTable<ItemLootTemplate>();
        public ITable<PickpocketingLootTemplate> PickpocketingLootTemplate => this.GetTable<PickpocketingLootTemplate>();
        public ITable<CreatureLootTemplate> CreatureLootTemplate => this.GetTable<CreatureLootTemplate>();
        public ITable<DisenchantLootTemplate> DisenchantLootTemplate => this.GetTable<DisenchantLootTemplate>();
        public ITable<ProspectingLootTemplate> ProspectingLootTemplate => this.GetTable<ProspectingLootTemplate>();
        public ITable<MillingLootTemplate> MillingLootTemplate => this.GetTable<MillingLootTemplate>();
        public ITable<ReferenceLootTemplate> ReferenceLootTemplate => this.GetTable<ReferenceLootTemplate>();
        public ITable<SpellLootTemplate> SpellLootTemplate => this.GetTable<SpellLootTemplate>();
        public ITable<MailLootTemplate> MailLootTemplate => this.GetTable<MailLootTemplate>();
        public ITable<GameObjectLootTemplate> GameObjectLootTemplate => this.GetTable<GameObjectLootTemplate>();
        public ITable<FishingLootTemplate> FishingLootTemplate => this.GetTable<FishingLootTemplate>();
        public ITable<SkinningLootTemplate> SkinningLootTemplate => this.GetTable<SkinningLootTemplate>();
        public ITable<CreatureQuestEnder> CreatureQuestEnders => this.GetTable<CreatureQuestEnder>();
        public ITable<CreatureQuestStarter> CreatureQuestStarters => this.GetTable<CreatureQuestStarter>();
        public ITable<GameObjectQuestEnder> GameObjectQuestEnders => this.GetTable<GameObjectQuestEnder>();
        public ITable<GameObjectQuestStarter> GameObjectQuestStarters => this.GetTable<GameObjectQuestStarter>();
    }
}