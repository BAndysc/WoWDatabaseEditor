using LinqToDB;

namespace WDE.TrinityMySqlDatabase.Models;

public class AzerothDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureWrath> Creature => GetTable<MySqlCreatureWrath>();
    public ITable<MySqlBroadcastTextAzeroth> BroadcastTexts => GetTable<MySqlBroadcastTextAzeroth>();
    public ITable<ACoreString> Strings => GetTable<ACoreString>();
    public ITable<AzerothMySqlSpellDbc> SpellDbc => GetTable<AzerothMySqlSpellDbc>();
    public ITable<MySqlGameObjectWrath> GameObject => GetTable<MySqlGameObjectWrath>();
}