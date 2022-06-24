using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityMasterVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures
    {
        public string Tag => "TrinityMaster";
        public string FriendlyName => "TrinityCore Shadowlands";
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;
        public PhasingType PhasingType => PhasingType.PhaseIds;

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>() {typeof(INpcText), typeof(ICreatureClassLevelStat), typeof(IBroadcastText)};
        public bool AlternativeTrinityDatabase => false;

        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.Quest,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
            SmartScriptType.AreaTriggerEntity,
            SmartScriptType.AreaTriggerEntityServerSide,
            SmartScriptType.Scene
        };

        public string TableName => "smart_scripts";
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.AllShadowlands;
        public CharacterClasses AllClasses => CharacterClasses.AllShadowlands;
        public bool SupportsEventScripts => true;
    }
}