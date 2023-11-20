using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.AzerothCore
{
    [AutoRegister]
    [SingleInstance]
    public class AzerothCoreVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures, IEventAiFeatures
    {
        public string Tag => "Azeroth";
        public string FriendlyName => "AzerothCore Wrath of the Lich King";
        public ImageUri Icon { get; } = new ImageUri("Icons/core_ac.png");
        public ISmartScriptFeatures SmartScriptFeatures => this;
        public IConditionFeatures ConditionFeatures => this;
        public IGameVersionFeatures GameVersionFeatures => this;
        public IEventAiFeatures EventAiFeatures => this;
        public IDatabaseFeatures DatabaseFeatures => this;
        public bool SupportsRbac => false;
        public bool SupportsConditionTargetVictim => true;
        public PhasingType PhasingType => PhasingType.PhaseMasks;
        public GameVersion Version { get; } = new(3, 3, 5, 12340);

        public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate),
            typeof(IConversationTemplate),
            typeof(IAuthRbacPermission),
            typeof(IAuthRbacLinkedPermission),
            typeof(IPointOfInterest),
            typeof(ISceneTemplate), 
            typeof(IAreaTriggerCreateProperties),
            typeof(ISpawnGroupSpawn),
            typeof(ISpawnGroupFormation),
            typeof(ISpawnGroupTemplate)
        };
        
        public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
        {
            SmartScriptType.Creature,
            SmartScriptType.GameObject,
            SmartScriptType.AreaTrigger,
            SmartScriptType.TimedActionList,
        };

        public bool AlternativeTrinityDatabase => true;
        public WaypointTables SupportedWaypoints => WaypointTables.WaypointData | WaypointTables.SmartScriptWaypoint | WaypointTables.ScriptWaypoint;
        public bool SpawnGroupTemplateHasType => false;
        public DatabaseTable TableName => DatabaseTable.WorldTable("smart_scripts");

        public string ConditionsFile => "SmartData/conditions.json";
        public string ConditionGroupsFile => "SmartData/conditions_groups.json";
        public string ConditionSourcesFile => "SmartData/condition_sources.json";
        public CharacterRaces AllRaces => CharacterRaces.AllWrath;
        public CharacterClasses AllClasses => CharacterClasses.AllWrath;
        public EventScriptType SupportedEventScripts => EventScriptType.Event | EventScriptType.Waypoint | EventScriptType.Spell;
    }
}