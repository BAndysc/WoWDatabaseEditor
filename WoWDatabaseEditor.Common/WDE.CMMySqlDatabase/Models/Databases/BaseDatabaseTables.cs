using LinqToDB;
using LinqToDB.Data;
using PropertyChanged.SourceGenerator;
using WDE.CMMySqlDatabase.Models.Wrath;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models
{
    public abstract class BaseDatabaseTables : DataConnection
    {
        public BaseDatabaseTables() : base("CMaNGOS-WoTLK-World")
        {
        }

        public ITable<BroadcastTextLocale> BroadcastTextLocale => this.GetTable<BroadcastTextLocale>();
        public ITable<QuestTemplateWoTLK> QuestTemplate => this.GetTable<QuestTemplateWoTLK>();
        public ITable<GameEventWoTLK> GameEvents => this.GetTable<GameEventWoTLK>();
        public ITable<SpellScriptNameWoTLK> SpellScriptNames => this.GetTable<SpellScriptNameWoTLK>();
        public ITable<GossipMenuLineWoTLK> GossipMenus => this.GetTable<GossipMenuLineWoTLK>();
        public ITable<GossipMenuOption> GossipMenuOptions => this.GetTable<GossipMenuOption>();
        public ITable<NpcTextWoTLK> NpcTexts => this.GetTable<NpcTextWoTLK>();
        public ITable<CoreCommandHelp> Commands => this.GetTable<CoreCommandHelp>();
        public ITable<MySqlPointOfInterest> PointsOfInterest => this.GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => this.GetTable<MySqlCreatureText>();
        public ITable<MySqlEventScriptLine> EventScripts => this.GetTable<MySqlEventScriptLine>();
        public ITable<MySqlWaypointScriptLine> WaypointScripts => this.GetTable<MySqlWaypointScriptLine>();
        public ITable<MySqlSpellScriptLine> SpellScripts => this.GetTable<MySqlSpellScriptLine>();
        public ITable<MySqlAreaTriggerCreateProperties> AreaTriggerCreateProperties => this.GetTable<MySqlAreaTriggerCreateProperties>();
        public ITable<MySqlSceneTemplate> SceneTemplates => this.GetTable<MySqlSceneTemplate>();
        public ITable<GameEventCreature> GameEventCreature => this.GetTable<GameEventCreature>();
        public ITable<GameEventGameObject> GameEventGameObject => this.GetTable<GameEventGameObject>();
        public ITable<CreatureEquipmentTemplate> CreatureEquipmentTemplate => this.GetTable<CreatureEquipmentTemplate>();
        public ITable<MangosCreatureMovement> CreatureMovement => this.GetTable<MangosCreatureMovement>();
        public ITable<MangosCreatureMovementTemplate> CreatureMovementTemplate => this.GetTable<MangosCreatureMovementTemplate>();
        public ITable<MangosWaypoint> WaypointPath => this.GetTable<MangosWaypoint>();
        public ITable<MangosWaypointPathName> WaypointPathName => this.GetTable<MangosWaypointPathName>();
        public ITable<EventAiLine> CreatureAiScripts => this.GetTable<EventAiLine>();
        public ITable<DbScriptRandomTemplate> DbScriptRandomTemplates => this.GetTable<DbScriptRandomTemplate>();
        public ITable<CreatureAiSummon> CreatureAiSummons => this.GetTable<CreatureAiSummon>();
        public ITable<SpawnGroup> SpawnGroupTemplate => this.GetTable<SpawnGroup>();
        public ITable<SpawnGroupSpawn> SpawnGroupSpawns => this.GetTable<SpawnGroupSpawn>();
        public ITable<SpawnGroupFormation> SpawnGroupFormations => this.GetTable<SpawnGroupFormation>();
        public ITable<ReferenceLootTemplateName> ReferenceLootTemplateNames => this.GetTable<ReferenceLootTemplateName>();
        public ITable<ItemLootTemplate> ItemLootTemplate => this.GetTable<ItemLootTemplate>();
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
        public ITable<PickpocketingLootTemplate> PickpocketingLootTemplate => this.GetTable<PickpocketingLootTemplate>();
        public ITable<CreatureQuestEnder> CreatureQuestEnders => this.GetTable<CreatureQuestEnder>();
        public ITable<CreatureQuestStarter> CreatureQuestStarters => this.GetTable<CreatureQuestStarter>();
        public ITable<GameObjectQuestEnder> GameObjectQuestEnders => this.GetTable<GameObjectQuestEnder>();
        public ITable<GameObjectQuestStarter> GameObjectQuestStarters => this.GetTable<GameObjectQuestStarter>();
    }
}