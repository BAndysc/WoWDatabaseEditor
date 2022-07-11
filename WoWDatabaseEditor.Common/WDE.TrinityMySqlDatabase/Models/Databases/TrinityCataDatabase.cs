using LinqToDB;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityCataDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureCata> Creature => GetTable<MySqlCreatureCata>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMySqlSpellDbc> SpellDbc => GetTable<TrinityMySqlSpellDbc>();
    public ITable<MySqlGameObjectCata> GameObject => GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCataQuestTemplate> CataQuestTemplate => GetTable<MySqlCataQuestTemplate>();
    public ITable<MySqlCataQuestTemplateAddon> CataQuestTemplateAddon => GetTable<MySqlCataQuestTemplateAddon>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonCata> CreatureAddon => GetTable<MySqlCreatureAddonCata>();
    public ITable<MySqlCreatureTemplateAddonCata> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddonCata>();
    public ITable<WaypointDataCata> WaypointDataCata => GetTable<WaypointDataCata>();
    public ITable<SmartScriptWaypointCata> SmartScriptWaypointCata => GetTable<SmartScriptWaypointCata>();
}