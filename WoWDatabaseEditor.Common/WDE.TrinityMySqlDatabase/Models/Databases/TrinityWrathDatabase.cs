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
}