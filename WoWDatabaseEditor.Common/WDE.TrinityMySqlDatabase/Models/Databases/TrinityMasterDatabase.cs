using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityMasterDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateMaster> CreatureTemplate => GetTable<MySqlCreatureTemplateMaster>();
    public ITable<CreatureTemplateModel> CreatureTemplateModel => GetTable<CreatureTemplateModel>();
    public ITable<MySqlCreatureMaster> Creature => GetTable<MySqlCreatureMaster>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMasterMySqlServersideSpell> SpellDbc => GetTable<TrinityMasterMySqlServersideSpell>();
    public ITable<MySqlGameObjectCata> GameObject => GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCreatureModelInfoShadowlands> CreatureModelInfo => GetTable<MySqlCreatureModelInfoShadowlands>();
    public ITable<MySqlCreatureAddonMaster> CreatureAddon => GetTable<MySqlCreatureAddonMaster>();
    public ITable<MySqlCreatureTemplateAddonMaster> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddonMaster>();
    public ITable<MySqlWrathQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlWrathQuestTemplateAddon>();
    public ITable<MySqlPlayerChoice> PlayerChoice => GetTable<MySqlPlayerChoice>();
    public ITable<MySqlPlayerChoiceResponse> PlayerChoiceResponse => GetTable<MySqlPlayerChoiceResponse>();
    public ITable<MySqlQuestObjective> QuestObjective => GetTable<MySqlQuestObjective>();
    public ITable<MySqlPhaseName> PhaseNames => GetTable<MySqlPhaseName>();
    public ITable<MySqlEventScriptLine> EventScripts => GetTable<MySqlEventScriptLine>();
    public ITable<MySqlWaypointScriptLine> WaypointScripts => GetTable<MySqlWaypointScriptLine>();
    public ITable<MySqlSpellScriptLine> SpellScripts => GetTable<MySqlSpellScriptLine>();
    public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
}