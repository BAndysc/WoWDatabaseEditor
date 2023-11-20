using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.CoreVersion
{
    [AutoRegister]
    [SingleInstance]
    public class UnspecifiedCoreVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures, IEventAiFeatures
    {
        public string Tag => "unspecified";
        public string FriendlyName => "Unspecified";
        public ImageUri Icon { get; } = new ImageUri("Icons/core_unknown.png");
        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public IEventAiFeatures EventAiFeatures => this;
        public ISet<Type> UnsupportedTables => new HashSet<Type>();
        public bool SpawnGroupTemplateHasType => false;
        public ISet<SmartScriptType> SupportedTypes => new HashSet<SmartScriptType>();
        public bool AlternativeTrinityDatabase => false;
        public WaypointTables SupportedWaypoints => 0;
        public PhasingType PhasingType => PhasingType.PhaseIds;
        public GameVersion Version { get; } = default;

        public DatabaseTable TableName => DatabaseTable.WorldTable("(null)");
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.None;
        public CharacterClasses AllClasses => CharacterClasses.None;
    }
}