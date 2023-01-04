using LinqToDB;
using WDE.CMMySqlDatabase.Models.Wrath;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models;

public class WoTLKDatabase : BaseDatabaseTables
{
    public ITable<CreatureTemplateWoTLK> CreatureTemplate => this.GetTable<CreatureTemplateWoTLK>();
    public ITable<GameObjectTemplateWoTLK> GameObjectTemplate => this.GetTable<GameObjectTemplateWoTLK>();
    public ITable<CreatureWoTLK> Creature => this.GetTable<CreatureWoTLK>();
    public ITable<CreatureAddonWrath> CreatureAddon => this.GetTable<CreatureAddonWrath>();
    public ITable<CreatureTemplateAddonWrath> CreatureTemplateAddon => this.GetTable<CreatureTemplateAddonWrath>();
    public ITable<BroadcastTextWoTLK> BroadcastTexts => this.GetTable<BroadcastTextWoTLK>();
    public ITable<SpellDbcWoTLK> SpellDbc => this.GetTable<SpellDbcWoTLK>();
    public ITable<GameObjectWoTLK> GameObject => this.GetTable<GameObjectWoTLK>();
    public ITable<MySqlItemTemplate> ItemTemplate => this.GetTable<MySqlItemTemplate>();
    public ITable<CreatureModelInfoWoTLK> CreatureModelInfo => this.GetTable<CreatureModelInfoWoTLK>();
    public ITable<CreatureClassLevelStatWoTLK> CreatureClassLevelStats => this.GetTable<CreatureClassLevelStatWoTLK>();

    public ITable<CmangosString> Strings => this.GetTable<CmangosString>();
}