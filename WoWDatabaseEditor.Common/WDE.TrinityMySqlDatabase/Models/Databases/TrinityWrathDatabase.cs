using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityWrathDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureWrath> Creature => GetTable<MySqlCreatureWrath>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMySqlSpellDbc> SpellDbc => GetTable<TrinityMySqlSpellDbc>();
    public ITable<MySqlGameObjectWrath> GameObject => GetTable<MySqlGameObjectWrath>();
    public ITable<MySqlItemTemplate> ItemTemplate => GetTable<MySqlItemTemplate>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonWrath> CreatureAddon => GetTable<MySqlCreatureAddonWrath>();
    public ITable<MySqlCreatureTemplateAddon> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddon>();
    public ITable<MySqlWrathQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlWrathQuestTemplateAddon>();
    public ITable<MySqlBroadcastTextLocale> BroadcastTextLocale => GetTable<MySqlBroadcastTextLocale>();
    public ITable<MySqlEventScriptLine> EventScripts => GetTable<MySqlEventScriptLine>();
    public ITable<MySqlWaypointScriptLine> WaypointScripts => GetTable<MySqlWaypointScriptLine>();
    public ITable<MySqlSpellScriptLine> SpellScripts => GetTable<MySqlSpellScriptLine>();
    public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
}