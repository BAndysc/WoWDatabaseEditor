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
    public ITable<MySqlSpawnGroupTemplate> SpawnGroupTemplate => GetTable<MySqlSpawnGroupTemplate>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddon> CreatureAddon => GetTable<MySqlCreatureAddon>();
    public ITable<MySqlCreatureTemplateAddon> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddon>();
}