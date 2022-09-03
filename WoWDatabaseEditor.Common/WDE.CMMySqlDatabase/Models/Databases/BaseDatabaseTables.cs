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

        public ITable<BroadcastTextLocale> BroadcastTextLocale => GetTable<BroadcastTextLocale>();
        public ITable<QuestTemplateWoTLK> QuestTemplate => GetTable<QuestTemplateWoTLK>();
        public ITable<GameEventWoTLK> GameEvents => GetTable<GameEventWoTLK>();
        public ITable<SpellScriptNameWoTLK> SpellScriptNames => GetTable<SpellScriptNameWoTLK>();
        public ITable<GossipMenuLineWoTLK> GossipMenus => GetTable<GossipMenuLineWoTLK>();
        public ITable<GossipMenuOption> GossipMenuOptions => GetTable<GossipMenuOption>();
        public ITable<NpcTextWoTLK> NpcTexts => GetTable<NpcTextWoTLK>();
        public ITable<CoreCommandHelp> Commands => GetTable<CoreCommandHelp>();
        public ITable<MySqlPointOfInterest> PointsOfInterest => GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => GetTable<MySqlCreatureText>();
        public ITable<MySqlEventScriptLine> EventScripts => GetTable<MySqlEventScriptLine>();
        public ITable<MySqlWaypointScriptLine> WaypointScripts => GetTable<MySqlWaypointScriptLine>();
        public ITable<MySqlSpellScriptLine> SpellScripts => GetTable<MySqlSpellScriptLine>();
        public ITable<MySqlAreaTriggerCreateProperties> AreaTriggerCreateProperties => GetTable<MySqlAreaTriggerCreateProperties>();
        public ITable<MySqlSceneTemplate> SceneTemplates => GetTable<MySqlSceneTemplate>();
        public ITable<GameEventCreature> GameEventCreature => GetTable<GameEventCreature>();
        public ITable<GameEventGameObject> GameEventGameObject => GetTable<GameEventGameObject>();
        public ITable<CreatureEquipmentTemplate> CreatureEquipmentTemplate => GetTable<CreatureEquipmentTemplate>();
        public ITable<MangosCreatureMovement> CreatureMovement => GetTable<MangosCreatureMovement>();
        public ITable<MangosCreatureMovementTemplate> CreatureMovementTemplate => GetTable<MangosCreatureMovementTemplate>();
        public ITable<MangosWaypoint> WaypointPath => GetTable<MangosWaypoint>();
        public ITable<MangosWaypointPathName> WaypointPathName => GetTable<MangosWaypointPathName>();
        public ITable<EventAiLine> CreatureAiScripts => GetTable<EventAiLine>();
        public ITable<DbScriptRandomTemplate> DbScriptRandomTemplates => GetTable<DbScriptRandomTemplate>();
        public ITable<CreatureAiSummon> CreatureAiSummons => GetTable<CreatureAiSummon>();
        public ITable<SpawnGroup> SpawnGroupTemplate => GetTable<SpawnGroup>();
        public ITable<SpawnGroupSpawn> SpawnGroupSpawns => GetTable<SpawnGroupSpawn>();
        public ITable<SpawnGroupFormation> SpawnGroupFormations => GetTable<SpawnGroupFormation>();
    }
}