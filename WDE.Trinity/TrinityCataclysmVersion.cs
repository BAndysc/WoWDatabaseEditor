using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Trinity
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityCataclysmVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures, IEventAiFeatures
    {
        public string Tag => "TrinityCata";
        public string FriendlyName => "The Cataclysm Preservation Project";
        public ImageUri Icon { get; } = new ImageUri("Icons/core_tc.png");

        public IDatabaseFeatures DatabaseFeatures => this;
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public IEventAiFeatures EventAiFeatures => this;
        public PhasingType PhasingType => PhasingType.PhaseIds;
        public GameVersion Version { get; } = new(4, 3, 4, 15595);

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate), typeof(IConversationTemplate), typeof(ICreatureClassLevelStat), typeof(ISceneTemplate), typeof(IAreaTriggerCreateProperties)};
        public bool AlternativeTrinityDatabase => false;
        public WaypointTables SupportedWaypoints => WaypointTables.WaypointData | WaypointTables.SmartScriptWaypoint | WaypointTables.ScriptWaypoint;
        public bool SpawnGroupTemplateHasType => false;

        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };

        public DatabaseTable TableName => DatabaseTable.WorldTable("smart_scripts");
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.AllCatataclysm;
        public CharacterClasses AllClasses => CharacterClasses.AllCataclysm;
        public EventScriptType SupportedEventScripts => EventScriptType.Event | EventScriptType.Waypoint | EventScriptType.Spell;
    }
}