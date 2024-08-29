using LinqToDB;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models;

public class TrinityMasterDatabase : BaseTrinityDatabase
{
    public ITable<MySqlCreatureTemplateMaster> CreatureTemplate => this.GetTable<MySqlCreatureTemplateMaster>();
    public ITable<MySqlCreatureTemplateDifficulty> CreatureTemplateDifficulty => this.GetTable<MySqlCreatureTemplateDifficulty>();
    public ITable<CreatureTemplateModel> CreatureTemplateModel => this.GetTable<CreatureTemplateModel>();
    public ITable<MySqlCreatureMaster> Creature => this.GetTable<MySqlCreatureMaster>();
    public ITable<MySqlBroadcastText> BroadcastTexts => this.GetTable<MySqlBroadcastText>();
    public ITable<TrinityString> Strings => this.GetTable<TrinityString>();
    public ITable<TrinityMasterMySqlServersideSpell> SpellDbc => this.GetTable<TrinityMasterMySqlServersideSpell>();
    public ITable<MySqlGameObjectCata> GameObject => this.GetTable<MySqlGameObjectCata>();
    public ITable<MySqlCreatureModelInfoShadowlands> CreatureModelInfo => this.GetTable<MySqlCreatureModelInfoShadowlands>();
    public ITable<MySqlCreatureAddonMaster> CreatureAddon => this.GetTable<MySqlCreatureAddonMaster>();
    public ITable<MySqlCreatureTemplateAddonMaster> CreatureTemplateAddon => this.GetTable<MySqlCreatureTemplateAddonMaster>();
    public ITable<MySqlWrathQuestTemplateAddon> QuestTemplateAddon => this.GetTable<MySqlWrathQuestTemplateAddon>();
    public ITable<MySqlPlayerChoice> PlayerChoice => this.GetTable<MySqlPlayerChoice>();
    public ITable<MySqlPlayerChoiceResponse> PlayerChoiceResponse => this.GetTable<MySqlPlayerChoiceResponse>();
    public ITable<MySqlQuestObjective> QuestObjective => this.GetTable<MySqlQuestObjective>();
    public ITable<MySqlPhaseName> PhaseNames => this.GetTable<MySqlPhaseName>();
    public ITable<MySqlEventScriptLine> EventScripts => this.GetTable<MySqlEventScriptLine>();
    public ITable<MySqlSpellScriptLine> SpellScripts => this.GetTable<MySqlSpellScriptLine>();
    public ITable<MasterMySqlSmartScriptLine> SmartScript => this.GetTable<MasterMySqlSmartScriptLine>();
    public ITable<MasterWaypointData> WaypointData => this.GetTable<MasterWaypointData>();
    public ITable<MySqlConditionLineMaster> ConditionsMaster => this.GetTable<MySqlConditionLineMaster>();
    public ITable<MySqlConversationActor> ConversationActor => this.GetTable<MySqlConversationActor>();
    
    public ITable<ItemMasterLootTemplate> ItemMasterLootTemplate => this.GetTable<ItemMasterLootTemplate>();
    public ITable<PickpocketingMasterLootTemplate> PickpocketingMasterLootTemplate => this.GetTable<PickpocketingMasterLootTemplate>();
    public ITable<CreatureMasterLootTemplate> CreatureMasterLootTemplate => this.GetTable<CreatureMasterLootTemplate>();
    public ITable<DisenchantMasterLootTemplate> DisenchantMasterLootTemplate => this.GetTable<DisenchantMasterLootTemplate>();
    public ITable<ProspectingMasterLootTemplate> ProspectingMasterLootTemplate => this.GetTable<ProspectingMasterLootTemplate>();
    public ITable<MillingMasterLootTemplate> MillingMasterLootTemplate => this.GetTable<MillingMasterLootTemplate>();
    public ITable<ReferenceMasterLootTemplate> ReferenceMasterLootTemplate => this.GetTable<ReferenceMasterLootTemplate>();
    public ITable<SpellMasterLootTemplate> SpellMasterLootTemplate => this.GetTable<SpellMasterLootTemplate>();
    public ITable<MailMasterLootTemplate> MailMasterLootTemplate => this.GetTable<MailMasterLootTemplate>();
    public ITable<GameObjectMasterLootTemplate> GameObjectMasterLootTemplate => this.GetTable<GameObjectMasterLootTemplate>();
    public ITable<FishingMasterLootTemplate> FishingMasterLootTemplate => this.GetTable<FishingMasterLootTemplate>();
    public ITable<SkinningMasterLootTemplate> SkinningMasterLootTemplate => this.GetTable<SkinningMasterLootTemplate>();
}