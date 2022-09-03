using LinqToDB;
using WDE.CMMySqlDatabase.Models.Wrath;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models;

public class WoTLKDatabase : BaseDatabaseTables
{
    public ITable<CreatureTemplateWoTLK> CreatureTemplate => GetTable<CreatureTemplateWoTLK>();
    public ITable<GameObjectTemplateWoTLK> GameObjectTemplate => GetTable<GameObjectTemplateWoTLK>();
    public ITable<CreatureWoTLK> Creature => GetTable<CreatureWoTLK>();
    public ITable<CreatureAddonWrath> CreatureAddon => GetTable<CreatureAddonWrath>();
    public ITable<CreatureTemplateAddonWrath> CreatureTemplateAddon => GetTable<CreatureTemplateAddonWrath>();
    public ITable<BroadcastTextWoTLK> BroadcastTexts => GetTable<BroadcastTextWoTLK>();
    public ITable<SpellDbcWoTLK> SpellDbc => GetTable<SpellDbcWoTLK>();
    public ITable<GameObjectWoTLK> GameObject => GetTable<GameObjectWoTLK>();
    public ITable<MySqlItemTemplate> ItemTemplate => GetTable<MySqlItemTemplate>();
    public ITable<CreatureModelInfoWoTLK> CreatureModelInfo => GetTable<CreatureModelInfoWoTLK>();
    public ITable<CreatureClassLevelStatWoTLK> CreatureClassLevelStats => GetTable<CreatureClassLevelStatWoTLK>();

    public ITable<CmangosString> Strings => GetTable<CmangosString>();
}