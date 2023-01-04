using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityWrathDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => this.GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureWrath> Creature => this.GetTable<MySqlCreatureWrath>();
    public ITable<MySqlBroadcastText> BroadcastTexts => this.GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => this.GetTable<TrinityString>();
    public ITable<TrinityMySqlSpellDbc> SpellDbc => this.GetTable<TrinityMySqlSpellDbc>();
    public ITable<MySqlGameObjectWrath> GameObject => this.GetTable<MySqlGameObjectWrath>();
    public ITable<MySqlItemTemplate> ItemTemplate => this.GetTable<MySqlItemTemplate>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => this.GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonWrath> CreatureAddon => this.GetTable<MySqlCreatureAddonWrath>();
    public ITable<MySqlCreatureTemplateAddon> CreatureTemplateAddon => this.GetTable<MySqlCreatureTemplateAddon>();
    public ITable<MySqlWrathQuestTemplateAddon> QuestTemplateAddon => this.GetTable<MySqlWrathQuestTemplateAddon>();
    public ITable<MySqlBroadcastTextLocale> BroadcastTextLocale => this.GetTable<MySqlBroadcastTextLocale>();
    public ITable<MySqlEventScriptLine> EventScripts => this.GetTable<MySqlEventScriptLine>();
    public ITable<MySqlWaypointScriptLine> WaypointScripts => this.GetTable<MySqlWaypointScriptLine>();
    public ITable<MySqlSpellScriptLine> SpellScripts => this.GetTable<MySqlSpellScriptLine>();
    public ITable<MySqlSmartScriptLine> SmartScript => this.GetTable<MySqlSmartScriptLine>();
    public ITable<NonMasterWaypointData> WaypointData => this.GetTable<NonMasterWaypointData>();
}