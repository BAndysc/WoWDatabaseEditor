namespace WDE.Common.Database;

public interface ISpawnGroupTemplate
{
    uint Id { get; }
    string Name { get; }
    SpawnGroupTemplateType Type { get; }
    uint? TrinityFlags { get; }
    uint? MangosFlags { get; }
}

public enum SpawnGroupTemplateType
{
    Creature = 0,
    GameObject = 1,
    Any = 2
}

public class AbstractSpawnGroupTemplate : ISpawnGroupTemplate
{
    public uint Id { get; set; }
    public string Name { get; set; } = "";
    public SpawnGroupTemplateType Type { get; set; }
    public uint? TrinityFlags { get; set; }
    public uint? MangosFlags { get; set; }
}