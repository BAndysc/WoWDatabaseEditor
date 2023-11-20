using System;
using System.Collections.Generic;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.CMaNGOS;

[AutoRegister]
[SingleInstance]
public class CMangosTbcVersion : ICoreVersion, IDatabaseFeatures, ISmartScriptFeatures, IConditionFeatures, IGameVersionFeatures, IEventAiFeatures
{
    public string Tag => "CMaNGOS-TBC";
    public string FriendlyName => "CMaNGOS The Burning Crusade";
    public ImageUri Icon { get; } = new ImageUri("Icons/core_cmangos.png");
    public ISmartScriptFeatures SmartScriptFeatures => this;
    public IConditionFeatures ConditionFeatures => this;
    public IGameVersionFeatures GameVersionFeatures => this;
    public IEventAiFeatures EventAiFeatures => this;
    public IDatabaseFeatures DatabaseFeatures => this;
    public bool SupportsRbac => false;
    public bool SupportsConditionTargetVictim => false;
    public PhasingType PhasingType => PhasingType.NoPhasing;
    public GameVersion Version { get; } = new(2, 4, 3, 8606);

    public ISet<Type> UnsupportedTables { get; } = new HashSet<Type>{typeof(IAreaTriggerTemplate),
        typeof(IConversationTemplate),
        typeof(IAuthRbacPermission),
        typeof(IAuthRbacLinkedPermission),
        typeof(IPointOfInterest),
        typeof(ISceneTemplate), 
        typeof(IAreaTriggerCreateProperties)
    };
        
    public ISet<SmartScriptType> SupportedTypes { get; } = new HashSet<SmartScriptType>
    {
    };

    public bool AlternativeTrinityDatabase => true;
    public WaypointTables SupportedWaypoints => WaypointTables.MangosWaypointPath | WaypointTables.MangosCreatureMovement;
    public bool SpawnGroupTemplateHasType => true;
    public DatabaseTable TableName => DatabaseTable.WorldTable("smart_scripts");

    public string ConditionsFile => "SmartData/conditions.json";
    public string ConditionGroupsFile => "SmartData/conditions_groups.json";
    public string ConditionSourcesFile => "SmartData/condition_sources.json";
    public CharacterRaces AllRaces => CharacterRaces.AllTbc;
    public CharacterClasses AllClasses => CharacterClasses.AllTbc;
    bool IEventAiFeatures.IsSupported => true;
}