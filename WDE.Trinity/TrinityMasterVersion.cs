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
    public class TrinityMasterVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures, IEventAiFeatures
    {
        public string Tag => "TrinityMaster";
        public string FriendlyName => "TrinityCore Dragonflight";
        public ImageUri Icon { get; } = new ImageUri("Icons/core_tc.png");
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;
        public IEventAiFeatures EventAiFeatures => this;
        public PhasingType PhasingType => PhasingType.PhaseIds; 
        public GameVersion Version { get; } = new(10, 0, 5, 49444);

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>() {typeof(INpcText), typeof(ICreatureClassLevelStat), typeof(IBroadcastText)};
        public bool AlternativeTrinityDatabase => false;
        public WaypointTables SupportedWaypoints => WaypointTables.WaypointData;
        public bool SpawnGroupTemplateHasType => false;

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

        public DatabaseTable TableName => DatabaseTable.WorldTable("smart_scripts");
        public bool DifficultyInSeparateColumn => true;
        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public bool HasConditionStringValue => true;
        public CharacterRaces AllRaces => CharacterRaces.AllDragonflight;
        public CharacterClasses AllClasses => CharacterClasses.AllDragonflight;
        public EventScriptType SupportedEventScripts => EventScriptType.Event | EventScriptType.Waypoint | EventScriptType.Spell;
    }
}