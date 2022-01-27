using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [AutoRegister]
    [SingleInstance]
    public class UnspecifiedCoreVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures
    {
        public string Tag => "unspecified";
        public string FriendlyName => "Unspecified";
        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public ISet<Type> UnsupportedTables => new HashSet<Type>();
        public ISet<SmartScriptType> SupportedTypes => new HashSet<SmartScriptType>();
        public bool AlternativeTrinityDatabase => false;
        
        public string TableName => "(null)";
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.None;
        public CharacterClasses AllClasses => CharacterClasses.None;
    }
}