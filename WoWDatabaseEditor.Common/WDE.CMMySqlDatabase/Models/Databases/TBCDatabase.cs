using LinqToDB;
using WDE.CMMySqlDatabase.Models.TBC;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models;

public class TBCDatabase : BaseDatabaseTables
{
    public ITable<CreatureTemplateTBC> CreatureTemplate => GetTable<CreatureTemplateTBC>();
    public ITable<GameObjectTemplateTBC> GameObjectTemplate => GetTable<GameObjectTemplateTBC>();
    public ITable<CreatureTBC> Creature => GetTable<CreatureTBC>();
    public ITable<CreatureAddonTBC> CreatureAddon => GetTable<CreatureAddonTBC>();
    public ITable<CreatureTemplateAddonTBC> CreatureTemplateAddon => GetTable<CreatureTemplateAddonTBC>();
    public ITable<BroadcastTextWoTLK> BroadcastTexts => GetTable<BroadcastTextWoTLK>();
    public ITable<SpellDbcWoTLK> SpellDbc => GetTable<SpellDbcWoTLK>();
    public ITable<GameObjectTBC> GameObject => GetTable<GameObjectTBC>();
    public ITable<MySqlItemTemplate> ItemTemplate => GetTable<MySqlItemTemplate>();
    public ITable<CreatureModelInfoWoTLK> CreatureModelInfo => GetTable<CreatureModelInfoWoTLK>();
    public ITable<CreatureClassLevelStatTBC> CreatureClassLevelStats => GetTable<CreatureClassLevelStatTBC>();

    public ITable<CmangosString> Strings => GetTable<CmangosString>();
}