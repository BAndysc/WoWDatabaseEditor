using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCataclysmVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures
    {
        public string Tag => "TrinityCata";
        public string FriendlyName => "The Cataclysm Preservation Project";

        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public PhasingType PhasingType => PhasingType.PhaseIds;

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate), typeof(IConversationTemplate), typeof(ICreatureClassLevelStat), typeof(ISceneTemplate), typeof(IAreaTriggerCreateProperties)};
        public bool AlternativeTrinityDatabase => false;
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };

        public string TableName => "smart_scripts";
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.AllCatataclysm;
        public CharacterClasses AllClasses => CharacterClasses.AllCataclysm;
        public bool SupportsEventScripts => true;
    }
}