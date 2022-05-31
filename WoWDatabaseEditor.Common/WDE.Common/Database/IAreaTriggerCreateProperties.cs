namespace WDE.Common.Database;

public interface IAreaTriggerCreateProperties
{
    uint Id { get; }
    uint AreaTriggerId { get; }
    string ScriptName { get; }
}