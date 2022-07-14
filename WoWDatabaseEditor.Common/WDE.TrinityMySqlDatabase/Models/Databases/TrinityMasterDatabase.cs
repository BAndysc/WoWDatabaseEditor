using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityMasterDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateMaster> CreatureTemplate => GetTable<MySqlCreatureTemplateMaster>();
    public ITable<MySqlCreatureMaster> Creature => GetTable<MySqlCreatureMaster>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMasterMySqlServersideSpell> SpellDbc => GetTable<TrinityMasterMySqlServersideSpell>();
    public ITable<MySqlGameObjectCata> GameObject => GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCreatureModelInfoShadowlands> CreatureModelInfo => GetTable<MySqlCreatureModelInfoShadowlands>();
    public ITable<MySqlCreatureAddon> CreatureAddon => GetTable<MySqlCreatureAddon>();
    public ITable<MySqlCreatureTemplateAddon> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddon>();
    public ITable<MySqlWrathQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlWrathQuestTemplateAddon>();
}