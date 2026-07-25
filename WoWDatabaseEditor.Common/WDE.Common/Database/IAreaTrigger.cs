namespace WDE.Common.Database;

/// <summary>One CMaNGOS <c>areatrigger_teleport</c> row: entering the (DBC-defined) trigger
/// teleports the player to the target position, subject to the requirement columns. Which
/// requirement columns exist differs per expansion - unsupported ones read as 0/null and are
/// not emitted on save.</summary>
public interface IAreaTriggerTeleport
{
    uint Id { get; }
    string? Name { get; }
    uint RequiredLevel { get; }
    uint RequiredItem { get; }
    uint RequiredItem2 { get; }
    uint HeroicKey { get; }
    uint HeroicKey2 { get; }
    uint RequiredQuestDone { get; }
    uint RequiredQuestDoneHeroic { get; }
    uint Map { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
    float O { get; }
    uint ConditionId { get; }
    uint Status { get; }
    string? StatusFailedText { get; }
}

/// <summary>One CMaNGOS <c>areatrigger_tavern</c> row: the trigger area grants rest state.</summary>
public interface IAreaTriggerTavern
{
    uint Id { get; }
    string? Name { get; }
}

/// <summary>One CMaNGOS <c>areatrigger_involvedrelation</c> row: entering the trigger completes
/// an exploration quest objective.</summary>
public interface IAreaTriggerQuestRelation
{
    uint Id { get; }
    uint Quest { get; }
}

/// <summary>One CMaNGOS <c>scripted_areatrigger</c> row: the trigger fires a C++/script-library
/// script registered under the given name.</summary>
public interface IScriptedAreaTrigger
{
    uint Id { get; }
    string ScriptName { get; }
}

public class AbstractAreaTriggerTeleport : IAreaTriggerTeleport
{
    public uint Id { get; set; }
    public string? Name { get; set; }
    public uint RequiredLevel { get; set; }
    public uint RequiredItem { get; set; }
    public uint RequiredItem2 { get; set; }
    public uint HeroicKey { get; set; }
    public uint HeroicKey2 { get; set; }
    public uint RequiredQuestDone { get; set; }
    public uint RequiredQuestDoneHeroic { get; set; }
    public uint Map { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
    public uint ConditionId { get; set; }
    public uint Status { get; set; }
    public string? StatusFailedText { get; set; }
}

public class AbstractAreaTriggerTavern : IAreaTriggerTavern
{
    public uint Id { get; set; }
    public string? Name { get; set; }
}

public class AbstractAreaTriggerQuestRelation : IAreaTriggerQuestRelation
{
    public uint Id { get; set; }
    public uint Quest { get; set; }
}

public class AbstractScriptedAreaTrigger : IScriptedAreaTrigger
{
    public uint Id { get; set; }
    public string ScriptName { get; set; } = "";
}
