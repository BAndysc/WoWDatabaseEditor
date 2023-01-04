using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class AzerothDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => this.GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureAzeroth> Creature => this.GetTable<MySqlCreatureAzeroth>();
    public ITable<MySqlBroadcastTextAzeroth> BroadcastTexts => this.GetTable<MySqlBroadcastTextAzeroth>();
    public ITable<ACoreString> Strings => this.GetTable<ACoreString>();
    public ITable<AzerothMySqlSpellDbc> SpellDbc => this.GetTable<AzerothMySqlSpellDbc>();
    public ITable<MySqlGameObjectWrath> GameObject => this.GetTable<MySqlGameObjectWrath>();
    public ITable<MySqlItemTemplate> ItemTemplate => this.GetTable<MySqlItemTemplate>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => this.GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonAC> CreatureAddon => this.GetTable<MySqlCreatureAddonAC>();
    public ITable<MySqlCreatureTemplateAddonAC> CreatureTemplateAddon => this.GetTable<MySqlCreatureTemplateAddonAC>();
    public ITable<MySqlAzerothQuestTemplateAddon> QuestTemplateAddon => this.GetTable<MySqlAzerothQuestTemplateAddon>();
    public ITable<MySqlEventScriptNoCommentLine> EventScripts => this.GetTable<MySqlEventScriptNoCommentLine>();
    public ITable<MySqlWaypointScriptNoCommentLine> WaypointScripts => this.GetTable<MySqlWaypointScriptNoCommentLine>();
    public ITable<MySqlSpellScriptNoCommentLine> SpellScripts => this.GetTable<MySqlSpellScriptNoCommentLine>();
    public ITable<AzerothMySqlSmartScriptLine> SmartScript => this.GetTable<AzerothMySqlSmartScriptLine>();
    public ITable<NonMasterWaypointData> WaypointData => this.GetTable<NonMasterWaypointData>();
}