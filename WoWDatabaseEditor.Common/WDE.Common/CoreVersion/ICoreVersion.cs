using System;
using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Common.CoreVersion
{
    [NonUniqueProvider]
    public interface ICoreVersion
    {
        string Tag { get; }
        string FriendlyName { get; }
        ImageUri Icon { get; }
        IDatabaseFeatures DatabaseFeatures { get; }
        ISmartScriptFeatures SmartScriptFeatures { get; }
        IConditionFeatures ConditionFeatures { get; }
        IGameVersionFeatures GameVersionFeatures { get; }
        IEventAiFeatures EventAiFeatures { get; }
        bool SupportsRbac => true;
        bool SupportsSpecialCommands => false;
        bool SupportsReverseCommands => false;
        EventScriptType SupportedEventScripts => 0;
        PhasingType PhasingType { get; }
        GameVersion Version { get; }
        bool HideRelatedItems => false;
        LootEditingMode LootEditingMode => LootEditingMode.PerLogicalEntity;
        // todo: this can be moved to settings as a configurable option
        IEnumerable<(DatabaseTable id, bool enabled)> TopBarQuickTableEditors => Array.Empty<(DatabaseTable, bool)>();
    }

    public struct GameVersion
    {
        public GameVersion(int major, int minor, int patch, int build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public int Build { get; }
    }

    public enum PhasingType
    {
        NoPhasing,
        PhaseMasks,
        PhaseIds,
        Both
    }

    public enum LootEditingMode
    {
        /// <summary>
        /// in the loot editor, you edit a specific LootId from *_loot_template. I.e. creature X can have loot id Y,
        /// but if you want to edit loot Y, you need to put this loot id
        /// </summary>
        PerDatabaseTable,
        
        /// <summary>
        /// in the loot editor, you edit loot for a specific entity, i.e. for a specific creature.
        /// I.e. you open the loot editor for a creature X and it will automatically load appropriate loot id 
        /// </summary>
        PerLogicalEntity
    }
    
    public interface IGameVersionFeatures
    {
        CharacterRaces AllRaces { get; }
        CharacterClasses AllClasses { get; }
    }

    public interface IConditionFeatures
    {
        string ConditionsFile { get; }
        string ConditionGroupsFile { get; }
        string ConditionSourcesFile { get; }
        bool HasConditionStringValue => false;
    }

    [Flags]
    public enum WaypointTables
    {
        WaypointData = 1,  // waypoint_data or waypoint_path
        SmartScriptWaypoint = 2, // waypoints
        ScriptWaypoint = 4, // script_waypoint
        MangosWaypointPath = 8, // waypoint_path
        MangosCreatureMovement = 16 // creature_movement(_template)
    }
    
    public interface IDatabaseFeatures
    {
        ISet<Type> UnsupportedTables { get; }
        bool AlternativeTrinityDatabase { get; }
        bool HasAiEntry => false;
        WaypointTables SupportedWaypoints { get; }
        bool SpawnGroupTemplateHasType { get; }
    }

    public interface IEventAiFeatures
    {
        string? ActionsPath => null;
        string? EventsPath => null;
        string? EventGroupPath => null;
        string? ActionGroupPath => null;
        bool IsSupported => false;
    }

    public interface ISmartScriptFeatures
    {
        ISet<SmartScriptType> SupportedTypes { get; }
        bool SupportsCreatingTimedEventsInsideTimedEvents => false;
        bool ProposeSmartScriptOnMainPage => true;
        bool SupportsConditionTargetVictim => false;
        string CreatureSmartAiName => "SmartAI";
        string GameObjectSmartAiName => "SmartGameObjectAI";
        bool DifficultyInSeparateColumn => false;

        string? ForceLoadTag => null;
        DatabaseTable TableName { get; }

        string? ActionsPath => null;
        string? EventsPath => null;
        string? TargetsPath => null;
        string? ActionGroupPath => null;
        string? EventGroupPath => null;
        string? TargetGroupPath => null;
    }
}