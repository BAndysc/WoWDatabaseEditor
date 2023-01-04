using LinqToDB;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityCataDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => this.GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureCata> Creature => this.GetTable<MySqlCreatureCata>();
    public ITable<MySqlBroadcastText> BroadcastTexts => this.GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => this.GetTable<TrinityString>();
    public ITable<TrinityMySqlSpellDbc> SpellDbc => this.GetTable<TrinityMySqlSpellDbc>();
    public ITable<MySqlGameObjectCata> GameObject => this.GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCataQuestTemplate> CataQuestTemplate => this.GetTable<MySqlCataQuestTemplate>();
    public ITable<MySqlCataQuestTemplateAddon> CataQuestTemplateAddon => this.GetTable<MySqlCataQuestTemplateAddon>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => this.GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonCata> CreatureAddon => this.GetTable<MySqlCreatureAddonCata>();
    public ITable<MySqlCreatureTemplateAddonCata> CreatureTemplateAddon => this.GetTable<MySqlCreatureTemplateAddonCata>();
    public ITable<WaypointDataCata> WaypointDataCata => this.GetTable<WaypointDataCata>();
    public ITable<SmartScriptWaypointCata> SmartScriptWaypointCata => this.GetTable<SmartScriptWaypointCata>();
    public ITable<MySqlBroadcastTextLocale> BroadcastTextLocale => this.GetTable<MySqlBroadcastTextLocale>();
    public ITable<MySqlEventScriptNoCommentLine> EventScripts => this.GetTable<MySqlEventScriptNoCommentLine>();
    public ITable<MySqlWaypointScriptNoCommentLine> WaypointScripts => this.GetTable<MySqlWaypointScriptNoCommentLine>();
    public ITable<MySqlSpellScriptNoCommentLine> SpellScripts => this.GetTable<MySqlSpellScriptNoCommentLine>();
    public ITable<MySqlSmartScriptLine> SmartScript => this.GetTable<MySqlSmartScriptLine>();
    
    public ITable<ItemLootTemplateWithCurrency> ItemLootTemplateWithCurrency => this.GetTable<ItemLootTemplateWithCurrency>();
    public ITable<PickpocketingLootTemplateWithCurrency> PickpocketingLootTemplateWithCurrency => this.GetTable<PickpocketingLootTemplateWithCurrency>();
    public ITable<CreatureLootTemplateWithCurrency> CreatureLootTemplateWithCurrency => this.GetTable<CreatureLootTemplateWithCurrency>();
    public ITable<DisenchantLootTemplateWithCurrency> DisenchantLootTemplateWithCurrency => this.GetTable<DisenchantLootTemplateWithCurrency>();
    public ITable<ProspectingLootTemplateWithCurrency> ProspectingLootTemplateWithCurrency => this.GetTable<ProspectingLootTemplateWithCurrency>();
    public ITable<MillingLootTemplateWithCurrency> MillingLootTemplateWithCurrency => this.GetTable<MillingLootTemplateWithCurrency>();
    public ITable<ReferenceLootTemplateWithCurrency> ReferenceLootTemplateWithCurrency => this.GetTable<ReferenceLootTemplateWithCurrency>();
    public ITable<SpellLootTemplateWithCurrency> SpellLootTemplateWithCurrency => this.GetTable<SpellLootTemplateWithCurrency>();
    public ITable<MailLootTemplateWithCurrency> MailLootTemplateWithCurrency => this.GetTable<MailLootTemplateWithCurrency>();
    public ITable<GameObjectLootTemplateWithCurrency> GameObjectLootTemplateWithCurrency => this.GetTable<GameObjectLootTemplateWithCurrency>();
    public ITable<FishingLootTemplateWithCurrency> FishingLootTemplateWithCurrency => this.GetTable<FishingLootTemplateWithCurrency>();
    public ITable<SkinningLootTemplateWithCurrency> SkinningLootTemplateWithCurrency => this.GetTable<SkinningLootTemplateWithCurrency>();
}