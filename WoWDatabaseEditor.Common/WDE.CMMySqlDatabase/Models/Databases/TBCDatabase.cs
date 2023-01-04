using LinqToDB;
using WDE.CMMySqlDatabase.Models.TBC;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models;

public class TBCDatabase : BaseDatabaseTables
{
    public ITable<CreatureTemplateTBC> CreatureTemplate => this.GetTable<CreatureTemplateTBC>();
    public ITable<GameObjectTemplateTBC> GameObjectTemplate => this.GetTable<GameObjectTemplateTBC>();
    public ITable<CreatureTBC> Creature => this.GetTable<CreatureTBC>();
    public ITable<CreatureAddonTBC> CreatureAddon => this.GetTable<CreatureAddonTBC>();
    public ITable<CreatureTemplateAddonTBC> CreatureTemplateAddon => this.GetTable<CreatureTemplateAddonTBC>();
    public ITable<BroadcastTextWoTLK> BroadcastTexts => this.GetTable<BroadcastTextWoTLK>();
    public ITable<SpellDbcWoTLK> SpellDbc => this.GetTable<SpellDbcWoTLK>();
    public ITable<GameObjectTBC> GameObject => this.GetTable<GameObjectTBC>();
    public ITable<MySqlItemTemplate> ItemTemplate => this.GetTable<MySqlItemTemplate>();
    public ITable<CreatureModelInfoWoTLK> CreatureModelInfo => this.GetTable<CreatureModelInfoWoTLK>();
    public ITable<CreatureClassLevelStatTBC> CreatureClassLevelStats => this.GetTable<CreatureClassLevelStatTBC>();

    public ITable<CmangosString> Strings => this.GetTable<CmangosString>();
}