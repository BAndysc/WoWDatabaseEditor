using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class AzerothDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureAzeroth> Creature => GetTable<MySqlCreatureAzeroth>();
    public ITable<MySqlBroadcastTextAzeroth> BroadcastTexts => GetTable<MySqlBroadcastTextAzeroth>();
    public ITable<ACoreString> Strings => GetTable<ACoreString>();
    public ITable<AzerothMySqlSpellDbc> SpellDbc => GetTable<AzerothMySqlSpellDbc>();
    public ITable<MySqlGameObjectWrath> GameObject => GetTable<MySqlGameObjectWrath>();
    public ITable<MySqlItemTemplate> ItemTemplate => GetTable<MySqlItemTemplate>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonAC> CreatureAddon => GetTable<MySqlCreatureAddonAC>();
    public ITable<MySqlCreatureTemplateAddonAC> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddonAC>();
    public ITable<MySqlAzerothQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlAzerothQuestTemplateAddon>();
    public ITable<MySqlEventScriptNoCommentLine> EventScripts => GetTable<MySqlEventScriptNoCommentLine>();
    public ITable<MySqlWaypointScriptNoCommentLine> WaypointScripts => GetTable<MySqlWaypointScriptNoCommentLine>();
    public ITable<MySqlSpellScriptNoCommentLine> SpellScripts => GetTable<MySqlSpellScriptNoCommentLine>();
}