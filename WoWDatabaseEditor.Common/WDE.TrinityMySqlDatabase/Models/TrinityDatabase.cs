using System;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.TrinityMySqlDatabase.Models
{
    public class TrinityDatabase : DataConnection
    {
        private readonly bool alternateNames;
        private readonly bool master;
        private readonly bool azeroth;
        private readonly bool cata;
        private readonly bool wrath;

        public TrinityDatabase(ICoreVersion coreVersion) : base("Trinity")
        {
            this.alternateNames = coreVersion.DatabaseFeatures.AlternativeTrinityDatabase;
            wrath = coreVersion.Tag == "TrinityWrath";
            cata = coreVersion.Tag == "TrinityCata";
            azeroth = coreVersion.Tag == "Azeroth";
            master = coreVersion.Tag == "TrinityMaster";
        }

        public ITable<MySqlAreaTriggerScript> AreaTriggerScript => GetTable<MySqlAreaTriggerScript>();
        public ITable<ICreatureTemplate> CreatureTemplate => GetTable<MySqlCreatureTemplateWrath, MySqlCreatureTemplateWrath, MySqlCreatureTemplateMaster, ICreatureTemplate>(() => !master, () => master);
        public ITable<ICreature> Creature => GetTable<MySqlCreatureWrath, MySqlCreatureWrath, MySqlCreatureCata, ICreature>(() => wrath || azeroth, () => cata || master);
        public ITable<MySqlSmartScriptLine> SmartScript => GetTable<MySqlSmartScriptLine>();
        public ITable<MySqlGameObjectTemplate> GameObjectTemplate => GetTable<MySqlGameObjectTemplate>();
        public ITable<MySqlGameObject> GameObject => GetTable<MySqlGameObject>();
        public ITable<MySqlQuestTemplate> QuestTemplate => GetTable<MySqlQuestTemplate>();
        public ITable<MySqlQuestTemplateAddon> QuestTemplateAddon => GetTable<MySqlQuestTemplateAddon>();
        public ITable<MySqlQuestTemplateAddonWithScriptName> QuestTemplateAddonWithScriptName => GetTable<MySqlQuestTemplateAddonWithScriptName>();
        public ITable<MySqlAreaTriggerTemplate> AreaTriggerTemplate => GetTable<MySqlAreaTriggerTemplate>();
        public ITable<MySqlGameEvent> GameEvents => GetTable<MySqlGameEvent>();
        public ITable<MySqlConversationTemplate> ConversationTemplate => GetTable<MySqlConversationTemplate>();
        public ITable<MySqlConditionLine> Conditions => GetTable<MySqlConditionLine>();
        public ITable<MySqlSpellScriptName> SpellScriptNames => GetTable<MySqlSpellScriptName>();
        public ITable<MySqlGossipMenuLine> GossipMenus => GetTable<MySqlGossipMenuLine>();
        public ITable<MySqlGossipMenuOptionWrath> GossipMenuOptions => GetTable<MySqlGossipMenuOptionWrath>();
        public ITable<MySqlGossipMenuOptionCata> SplitGossipMenuOptions => GetTable<MySqlGossipMenuOptionCata>();
        public ITable<MySqlGossipMenuOptionAction> SplitGossipMenuOptionActions => GetTable<MySqlGossipMenuOptionAction>();
        public ITable<MySqlGossipMenuOptionBox> SplitGossipMenuOptionBoxes => GetTable<MySqlGossipMenuOptionBox>();
        public ITable<MySqlNpcText> NpcTexts => GetTable<MySqlNpcText>();
        public ITable<MySqlCreatureClassLevelStat> CreatureClassLevelStats => GetTable<MySqlCreatureClassLevelStat>();
        public ITable<IBroadcastText> BroadcastTexts => GetTable<MySqlBroadcastText, MySqlBroadcastTextAzeroth, IBroadcastText>(() => azeroth);
        public ITable<CoreCommandHelp> Commands => GetTable<CoreCommandHelp>();
        public ITable<ITrinityString> Strings => GetTable<TrinityString, ACoreString, ITrinityString>();
        public ITable<IDatabaseSpellDbc> SpellDbc => GetTable<TrinityMySqlSpellDbc, TrinityMasterMySqlServersideSpell, AzerothMySqlSpellDbc, IDatabaseSpellDbc>(() => master, () => alternateNames);
        public ITable<MySqlPointOfInterest> PointsOfInterest => GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => GetTable<MySqlCreatureText>();

        public bool UseSplitGossipOptions => cata || master;
        
        private ITable<T> GetTable<R, S, T>(Func<bool> sCond) where T : class
            where S : class, T
            where R : class, T
        {
            if (sCond())
                return GetTable<S>();
            return GetTable<R>();
        }
        
        private ITable<T> GetTable<R, S, U, T>(Func<bool> sCond, Func<bool> uCond) where T : class
            where S : class, T
            where R : class, T
            where U : class, T
        {
            if (sCond())
                return GetTable<S>();
            if (uCond())
                return GetTable<U>();
            return GetTable<R>();
        }
        
        private ITable<T> GetTable<R, S, T>() where T : class 
            where S : class, T
            where R : class, T
        {
            if (alternateNames)
                return GetTable<S>();
            return GetTable<R>();
        }
    }
}