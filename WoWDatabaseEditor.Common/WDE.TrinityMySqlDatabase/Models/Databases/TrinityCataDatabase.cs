using LinqToDB;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityCataDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateWrath> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath>();
    public ITable<MySqlCreatureCata> Creature => GetTable<MySqlCreatureCata>();
    public ITable<MySqlBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => GetTable<TrinityString>();
    public ITable<TrinityMySqlSpellDbc> SpellDbc => GetTable<TrinityMySqlSpellDbc>();
    public ITable<MySqlGameObjectCata> GameObject => GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCataQuestTemplate> CataQuestTemplate => GetTable<MySqlCataQuestTemplate>();
    public ITable<MySqlCataQuestTemplateAddon> CataQuestTemplateAddon => GetTable<MySqlCataQuestTemplateAddon>();
    public ITable<MySqlCreatureModelInfo> CreatureModelInfo => GetTable<MySqlCreatureModelInfo>();
    public ITable<MySqlCreatureAddonCata> CreatureAddon => GetTable<MySqlCreatureAddonCata>();
    public ITable<MySqlCreatureTemplateAddonCata> CreatureTemplateAddon => GetTable<MySqlCreatureTemplateAddonCata>();
    public ITable<WaypointDataCata> WaypointDataCata => GetTable<WaypointDataCata>();
    public ITable<SmartScriptWaypointCata> SmartScriptWaypointCata => GetTable<SmartScriptWaypointCata>();
    public ITable<MySqlBroadcastTextLocale> BroadcastTextLocale => GetTable<MySqlBroadcastTextLocale>();
    public ITable<MySqlEventScriptNoCommentLine> EventScripts => GetTable<MySqlEventScriptNoCommentLine>();
    public ITable<MySqlWaypointScriptNoCommentLine> WaypointScripts => GetTable<MySqlWaypointScriptNoCommentLine>();
    public ITable<MySqlSpellScriptNoCommentLine> SpellScripts => GetTable<MySqlSpellScriptNoCommentLine>();
    public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
    
    public ITable<ItemLootTemplateWithCurrency> ItemLootTemplateWithCurrency => GetTable<ItemLootTemplateWithCurrency>();
    public ITable<PickpocketingLootTemplateWithCurrency> PickpocketingLootTemplateWithCurrency => GetTable<PickpocketingLootTemplateWithCurrency>();
    public ITable<CreatureLootTemplateWithCurrency> CreatureLootTemplateWithCurrency => GetTable<CreatureLootTemplateWithCurrency>();
    public ITable<DisenchantLootTemplateWithCurrency> DisenchantLootTemplateWithCurrency => GetTable<DisenchantLootTemplateWithCurrency>();
    public ITable<ProspectingLootTemplateWithCurrency> ProspectingLootTemplateWithCurrency => GetTable<ProspectingLootTemplateWithCurrency>();
    public ITable<MillingLootTemplateWithCurrency> MillingLootTemplateWithCurrency => GetTable<MillingLootTemplateWithCurrency>();
    public ITable<ReferenceLootTemplateWithCurrency> ReferenceLootTemplateWithCurrency => GetTable<ReferenceLootTemplateWithCurrency>();
    public ITable<SpellLootTemplateWithCurrency> SpellLootTemplateWithCurrency => GetTable<SpellLootTemplateWithCurrency>();
    public ITable<MailLootTemplateWithCurrency> MailLootTemplateWithCurrency => GetTable<MailLootTemplateWithCurrency>();
    public ITable<GameObjectLootTemplateWithCurrency> GameObjectLootTemplateWithCurrency => GetTable<GameObjectLootTemplateWithCurrency>();
    public ITable<FishingLootTemplateWithCurrency> FishingLootTemplateWithCurrency => GetTable<FishingLootTemplateWithCurrency>();
    public ITable<SkinningLootTemplateWithCurrency> SkinningLootTemplateWithCurrency => GetTable<SkinningLootTemplateWithCurrency>();
}