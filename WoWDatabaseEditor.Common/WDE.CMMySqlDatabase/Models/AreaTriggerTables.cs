using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

// areatrigger_teleport columns differ per expansion (heroic keys/quests appeared in TBC, the
// status columns disappeared in WoTLK), hence one model per expansion. Unmapped requirement
// properties read as 0/null and the matching per-expansion insert providers never emit them.

[Table(Name = "areatrigger_teleport")]
public class AreaTriggerTeleportClassic : IAreaTriggerTeleport
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "name")]
    public string? Name { get; set; }

    [Column(Name = "required_level")]
    public uint RequiredLevel { get; set; }

    [Column(Name = "required_item")]
    public uint RequiredItem { get; set; }

    [Column(Name = "required_item2")]
    public uint RequiredItem2 { get; set; }

    [Column(Name = "required_quest_done")]
    public uint RequiredQuestDone { get; set; }

    [Column(Name = "target_map")]
    public uint Map { get; set; }

    [Column(Name = "target_position_x")]
    public float X { get; set; }

    [Column(Name = "target_position_y")]
    public float Y { get; set; }

    [Column(Name = "target_position_z")]
    public float Z { get; set; }

    [Column(Name = "target_orientation")]
    public float O { get; set; }

    [Column(Name = "condition_id")]
    public uint ConditionId { get; set; }

    [Column(Name = "status_failed_text")]
    public string? StatusFailedText { get; set; }

    public uint HeroicKey => 0;
    public uint HeroicKey2 => 0;
    public uint RequiredQuestDoneHeroic => 0;
    public uint Status => 0;
}

[Table(Name = "areatrigger_teleport")]
public class AreaTriggerTeleportTBC : IAreaTriggerTeleport
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "name")]
    public string? Name { get; set; }

    [Column(Name = "required_level")]
    public uint RequiredLevel { get; set; }

    [Column(Name = "required_item")]
    public uint RequiredItem { get; set; }

    [Column(Name = "required_item2")]
    public uint RequiredItem2 { get; set; }

    [Column(Name = "heroic_key")]
    public uint HeroicKey { get; set; }

    [Column(Name = "heroic_key2")]
    public uint HeroicKey2 { get; set; }

    [Column(Name = "required_quest_done")]
    public uint RequiredQuestDone { get; set; }

    [Column(Name = "required_quest_done_heroic")]
    public uint RequiredQuestDoneHeroic { get; set; }

    [Column(Name = "target_map")]
    public uint Map { get; set; }

    [Column(Name = "target_position_x")]
    public float X { get; set; }

    [Column(Name = "target_position_y")]
    public float Y { get; set; }

    [Column(Name = "target_position_z")]
    public float Z { get; set; }

    [Column(Name = "target_orientation")]
    public float O { get; set; }

    [Column(Name = "condition_id")]
    public uint ConditionId { get; set; }

    [Column(Name = "status")]
    public uint Status { get; set; }

    [Column(Name = "status_failed_text")]
    public string? StatusFailedText { get; set; }
}

[Table(Name = "areatrigger_teleport")]
public class AreaTriggerTeleportWoTLK : IAreaTriggerTeleport
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "name")]
    public string? Name { get; set; }

    [Column(Name = "required_level")]
    public uint RequiredLevel { get; set; }

    [Column(Name = "required_item")]
    public uint RequiredItem { get; set; }

    [Column(Name = "required_item2")]
    public uint RequiredItem2 { get; set; }

    [Column(Name = "heroic_key")]
    public uint HeroicKey { get; set; }

    [Column(Name = "heroic_key2")]
    public uint HeroicKey2 { get; set; }

    [Column(Name = "required_quest_done")]
    public uint RequiredQuestDone { get; set; }

    [Column(Name = "required_quest_done_heroic")]
    public uint RequiredQuestDoneHeroic { get; set; }

    [Column(Name = "target_map")]
    public uint Map { get; set; }

    [Column(Name = "target_position_x")]
    public float X { get; set; }

    [Column(Name = "target_position_y")]
    public float Y { get; set; }

    [Column(Name = "target_position_z")]
    public float Z { get; set; }

    [Column(Name = "target_orientation")]
    public float O { get; set; }

    [Column(Name = "condition_id")]
    public uint ConditionId { get; set; }

    public uint Status => 0;
    public string? StatusFailedText => null;
}

[Table(Name = "areatrigger_tavern")]
public class AreaTriggerTavern : IAreaTriggerTavern
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "name")]
    public string? Name { get; set; }
}

[Table(Name = "areatrigger_involvedrelation")]
public class AreaTriggerQuestRelation : IAreaTriggerQuestRelation
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "quest")]
    public uint Quest { get; set; }
}

[Table(Name = "scripted_areatrigger")]
public class ScriptedAreaTrigger : IScriptedAreaTrigger
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Id { get; set; }

    [Column(Name = "ScriptName")]
    public string ScriptName { get; set; } = "";
}
